using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace ProgressionGater
{
    [HarmonyPatch]
    internal static class AdminCommands
    {
        private const string CommandRpc = "ProgressionGater.AdminCommand.v1";
        private const string ReplyRpc = "ProgressionGater.AdminReply.v1";
        private static bool _registered;
        private static Terminal _pendingTerminal;

        [HarmonyPatch(typeof(Terminal), "InitTerminal")]
        [HarmonyPostfix]
        private static void RegisterCommands()
        {
            if (_registered) return;
            _registered = true;
            Add("pg_unlock", "<next|boss> - allow a boss to be summoned", "unlock");
            Add("pg_lock", "<boss> - prevent a boss from being summoned", "lock");
            Add("pg_status", "show server-wide progression", "status");
            Add("pg_defeat", "<boss> - administratively mark a boss defeated", "defeat");
            Add("pg_undefeat", "<boss> - remove a recorded defeat", "undefeat");
            Add("pg_reset", "confirm - clear all Progression Gater state", "reset");
            Add("pg_webhook_test", "send a test Discord webhook", "webhook_test");
        }

        private static void Add(string name, string description, string command)
        {
            new Terminal.ConsoleCommand(name, description, args => Send(args, command), isCheat: true);
        }

        internal static void RegisterRpcHandlers(ZRoutedRpc rpc)
        {
            rpc.Register<ZPackage>(CommandRpc, HandleServerCommand);
            rpc.Register<ZPackage>(ReplyRpc, (sender, package) =>
            {
                if (!NetworkManager.IsFromServer(sender)) return;
                HandleClientReply(package);
            });
        }

        private static void Send(Terminal.ConsoleEventArgs args, string command)
        {
            string[] commandArgs = args.Args.Skip(1).ToArray();
            if (ZNet.instance == null)
            {
                args.Context.AddString("Not connected.");
                return;
            }

            if (ZNet.instance.IsServer())
            {
                string actor = Player.m_localPlayer?.GetPlayerName() ?? "Server console";
                foreach (string line in Run(command, commandArgs, true, actor)) args.Context.AddString(line);
                return;
            }

            ZNetPeer server = ZNet.instance.GetServerPeer();
            if (server == null || ZRoutedRpc.instance == null)
            {
                args.Context.AddString("Server connection is not ready.");
                return;
            }

            var package = new ZPackage();
            package.Write(command);
            package.Write(commandArgs.Length);
            foreach (string value in commandArgs) package.Write(value);
            _pendingTerminal = args.Context;
            ZRoutedRpc.instance.InvokeRoutedRPC(server.m_uid, CommandRpc, new object[] { package });
        }

        private static void HandleServerCommand(long sender, ZPackage package)
        {
            if (!ProgressionService.IsServer) return;
            ZNetPeer peer = ZNet.instance.GetPeer(sender);
            bool isAdmin = NetworkManager.IsAdmin(peer);
            string command;
            string[] args;
            try
            {
                command = package.ReadString();
                int count = package.ReadInt();
                if (count < 0 || count > 32) throw new InvalidOperationException("Invalid argument count.");
                args = new string[count];
                for (int i = 0; i < count; i++) args[i] = package.ReadString();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Rejected malformed admin command from {sender}: {ex.Message}");
                return;
            }

            string actor = peer?.m_playerName ?? $"peer {sender}";
            List<string> lines = Run(command, args, isAdmin, actor);
            var reply = new ZPackage();
            reply.Write(lines.Count);
            foreach (string line in lines) reply.Write(line ?? "");
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, ReplyRpc, new object[] { reply });
        }

        private static void HandleClientReply(ZPackage package)
        {
            try
            {
                int count = package.ReadInt();
                if (count < 0 || count > 256) throw new InvalidOperationException("Invalid reply count.");
                for (int i = 0; i < count; i++)
                {
                    string line = package.ReadString();
                    if (_pendingTerminal != null) _pendingTerminal.AddString(line);
                    else Plugin.Log.LogInfo(line);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Invalid admin-command reply: {ex.Message}");
            }
            finally { _pendingTerminal = null; }
        }

        private static List<string> Run(string command, string[] args, bool isAdmin, string actor)
        {
            if (!isAdmin) return new List<string> { "Admin only." };
            switch (command)
            {
                case "unlock": return Unlock(args, actor);
                case "lock": return Lock(args);
                case "status": return Status();
                case "defeat": return Defeat(args);
                case "undefeat": return Undefeat(args);
                case "reset": return Reset(args);
                case "webhook_test": return TestWebhook(actor);
                default: return new List<string> { $"Unknown command '{command}'." };
            }
        }

        private static List<string> Unlock(string[] args, string actor)
        {
            if (args.Length < 1) return new List<string> { "Usage: pg_unlock <next|boss>" };
            BossDefinition boss = string.Equals(args[0], "next", StringComparison.OrdinalIgnoreCase)
                ? ProgressionService.NextBoss()
                : ProgressionService.Rules.Resolve(args[0]);
            if (boss == null) return new List<string> { $"Unknown boss '{args[0]}'." };
            if (!ProgressionService.Unlock(boss)) return new List<string> { $"{boss.DisplayName} is already summon-unlocked.", ProgressionService.Status(boss) };
            Plugin.Log.LogInfo($"{actor} unlocked boss summon: {boss.DisplayName}");
            DiscordWebhook.PostBossUnlocked(boss, actor);
            return new List<string> { $"{boss.DisplayName} may now be summoned.", "Its gated recipes remain locked until it is defeated." };
        }

        private static List<string> Lock(string[] args)
        {
            BossDefinition boss = ResolveArgument(args, "Usage: pg_lock <boss>", out List<string> error);
            if (boss == null) return error;
            if (!ProgressionService.Lock(boss)) return new List<string> { $"{boss.DisplayName} is already summon-locked." };
            return new List<string> { $"{boss.DisplayName} can no longer be summoned. Its recorded defeat was not changed." };
        }

        private static List<string> Defeat(string[] args)
        {
            BossDefinition boss = ResolveArgument(args, "Usage: pg_defeat <boss>", out List<string> error);
            if (boss == null) return error;
            if (!ProgressionService.MarkDefeated(boss)) return new List<string> { $"{boss.DisplayName} is already recorded as defeated." };
            Plugin.Log.LogInfo($"Boss administratively marked defeated: {boss.DisplayName}");
            return new List<string> { $"{boss.DisplayName} marked defeated. No kill webhook was posted." };
        }

        private static List<string> Undefeat(string[] args)
        {
            BossDefinition boss = ResolveArgument(args, "Usage: pg_undefeat <boss>", out List<string> error);
            if (boss == null) return error;
            if (!ProgressionService.UnmarkDefeated(boss)) return new List<string> { $"{boss.DisplayName} was not recorded as defeated." };
            return new List<string> { $"Removed the recorded defeat for {boss.DisplayName}." };
        }

        private static List<string> Reset(string[] args)
        {
            if (args.Length != 1 || !string.Equals(args[0], "confirm", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "This clears all summon unlocks and defeats. Run: pg_reset confirm" };
            ProgressionService.Reset();
            Plugin.Log.LogWarning("All Progression Gater state was reset by an administrator.");
            return new List<string> { "All Progression Gater state has been cleared." };
        }

        private static List<string> TestWebhook(string actor)
        {
            if (string.IsNullOrWhiteSpace(ModConfig.WebhookUrl.Value))
                return new List<string> { "WebhookUrl is empty in the server config." };
            DiscordWebhook.PostTest(actor);
            return new List<string> { "Webhook test queued. Check Discord and the BepInEx log." };
        }

        private static List<string> Status()
        {
            var result = new List<string>
            {
                $"=== Progression Gater ({(ProgressionService.Enabled ? "enabled" : "disabled")}) ===",
                $"Boss summons: {(ProgressionService.GateBossSummons ? "gated" : "open")}; crafting: {(ProgressionService.GateCrafting ? "gated" : "open")}",
                $"Discord webhook: {(string.IsNullOrWhiteSpace(ModConfig.WebhookUrl.Value) ? "not configured" : "configured")}",
            };
            result.AddRange(ProgressionService.Rules.Bosses.Select(boss => "  " + ProgressionService.Status(boss)));
            return result;
        }

        private static BossDefinition ResolveArgument(string[] args, string usage, out List<string> error)
        {
            if (args.Length < 1)
            {
                error = new List<string> { usage };
                return null;
            }
            BossDefinition boss = ProgressionService.Rules.Resolve(args[0]);
            error = boss == null ? new List<string> { $"Unknown boss '{args[0]}'." } : null;
            return boss;
        }
    }
}
