using System;
using System.Collections.Generic;
using System.Linq;

namespace ProgressionGater
{
    internal static class ProgressionService
    {
        internal const int ProtocolVersion = 1;

        private static RuleCatalog _rules;
        private static HashSet<string> _clientSummonUnlocks = NewSet();
        private static HashSet<string> _clientDefeats = NewSet();
        private static bool _clientEnabled;
        private static bool _clientGateBossSummons;
        private static bool _clientGateCrafting;
        private static bool _clientMatchIngredients;
        private static bool _clientCanBypass;
        private static bool _hasServerSnapshot;

        internal static RuleCatalog Rules => _rules ?? (_rules = RuleCatalog.FromConfig());
        internal static bool IsServer => ZNet.instance != null && ZNet.instance.IsServer();
        internal static bool Enabled => IsServer || !_hasServerSnapshot ? ModConfig.Enabled.Value : _clientEnabled;
        internal static bool GateBossSummons => Enabled && (IsServer || !_hasServerSnapshot ? ModConfig.GateBossSummons.Value : _clientGateBossSummons);
        internal static bool GateCrafting => Enabled && (IsServer || !_hasServerSnapshot ? ModConfig.GateCrafting.Value : _clientGateCrafting);
        internal static bool MatchIngredients => IsServer || !_hasServerSnapshot ? ModConfig.MatchRecipeIngredients.Value : _clientMatchIngredients;
        internal static bool LocalCanBypass => IsServer
            ? ModConfig.AdminBypass.Value && (Player.m_localPlayer == null || ZNet.instance.LocalPlayerIsAdminOrHost())
            : _hasServerSnapshot && _clientCanBypass;

        internal static void Initialize()
        {
            _rules = RuleCatalog.FromConfig();
        }

        internal static void ReloadRules()
        {
            if (IsServer || ZNet.instance == null)
                _rules = RuleCatalog.FromConfig();
        }

        internal static bool IsSummonUnlocked(BossDefinition boss)
        {
            return boss != null && CurrentSummonUnlocks().Contains(boss.Id);
        }

        internal static bool IsDefeated(BossDefinition boss)
        {
            return boss != null && CurrentDefeats().Contains(boss.Id);
        }

        internal static bool Unlock(BossDefinition boss)
        {
            if (!IsServer || boss == null) return false;
            HashSet<string> state = ParseState(ModConfig.SummonUnlocks.Value);
            if (!state.Add(boss.Id)) return false;
            ModConfig.SummonUnlocks.Value = SerializeState(state);
            Plugin.SaveConfig();
            Broadcast();
            return true;
        }

        internal static bool Lock(BossDefinition boss)
        {
            if (!IsServer || boss == null) return false;
            HashSet<string> state = ParseState(ModConfig.SummonUnlocks.Value);
            if (!state.Remove(boss.Id)) return false;
            ModConfig.SummonUnlocks.Value = SerializeState(state);
            Plugin.SaveConfig();
            Broadcast();
            return true;
        }

        internal static bool MarkDefeated(BossDefinition boss)
        {
            if (!IsServer || boss == null) return false;
            HashSet<string> state = ParseState(ModConfig.DefeatedBosses.Value);
            if (!state.Add(boss.Id)) return false;
            ModConfig.DefeatedBosses.Value = SerializeState(state);
            Plugin.SaveConfig();
            Broadcast();
            return true;
        }

        internal static bool UnmarkDefeated(BossDefinition boss)
        {
            if (!IsServer || boss == null) return false;
            HashSet<string> state = ParseState(ModConfig.DefeatedBosses.Value);
            if (!state.Remove(boss.Id)) return false;
            ModConfig.DefeatedBosses.Value = SerializeState(state);
            Plugin.SaveConfig();
            Broadcast();
            return true;
        }

        internal static void Reset()
        {
            if (!IsServer) return;
            ModConfig.SummonUnlocks.Value = "";
            ModConfig.DefeatedBosses.Value = "";
            Plugin.SaveConfig();
            Broadcast();
        }

        internal static BossDefinition NextBoss()
        {
            return Rules.Bosses.FirstOrDefault(boss => !IsDefeated(boss));
        }

        internal static string Status(BossDefinition boss)
        {
            return $"{boss.DisplayName}: summon {(IsSummonUnlocked(boss) ? "unlocked" : "LOCKED")}; recipes {(IsDefeated(boss) ? "unlocked" : "locked")}";
        }

        internal static void SendToPeer(ZNetPeer peer)
        {
            if (!IsServer || peer == null || ZRoutedRpc.instance == null) return;
            var package = new ZPackage();
            package.Write(ProtocolVersion);
            package.Write(ModConfig.Enabled.Value);
            package.Write(ModConfig.GateBossSummons.Value);
            package.Write(ModConfig.GateCrafting.Value);
            package.Write(ModConfig.MatchRecipeIngredients.Value);
            package.Write(ModConfig.AdminBypass.Value && NetworkManager.IsAdmin(peer));
            Rules.Write(package);
            WriteSet(package, CurrentSummonUnlocks());
            WriteSet(package, CurrentDefeats());
            ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, NetworkManager.SyncRpc, new object[] { package });
        }

        internal static void Broadcast()
        {
            if (!IsServer || ZNet.instance == null || ZRoutedRpc.instance == null) return;
            foreach (ZNetPeer peer in ZNet.instance.GetPeers()) SendToPeer(peer);
        }

        internal static void ApplyServerSnapshot(ZPackage package)
        {
            int version = package.ReadInt();
            if (version != ProtocolVersion) throw new InvalidOperationException($"Unsupported protocol {version}; expected {ProtocolVersion}.");
            _clientEnabled = package.ReadBool();
            _clientGateBossSummons = package.ReadBool();
            _clientGateCrafting = package.ReadBool();
            _clientMatchIngredients = package.ReadBool();
            _clientCanBypass = package.ReadBool();
            _rules = RuleCatalog.Read(package);
            _clientSummonUnlocks = ReadSet(package);
            _clientDefeats = ReadSet(package);
            _hasServerSnapshot = true;
            Plugin.Log.LogInfo($"Server progression synchronized: summon=[{string.Join(",", _clientSummonUnlocks)}], defeated=[{string.Join(",", _clientDefeats)}]");
        }

        internal static void ClearServerSnapshot()
        {
            _hasServerSnapshot = false;
            _clientCanBypass = false;
            _rules = RuleCatalog.FromConfig();
        }

        private static HashSet<string> CurrentSummonUnlocks()
        {
            return IsServer || !_hasServerSnapshot ? ParseState(ModConfig.SummonUnlocks.Value) : _clientSummonUnlocks;
        }

        private static HashSet<string> CurrentDefeats()
        {
            return IsServer || !_hasServerSnapshot ? ParseState(ModConfig.DefeatedBosses.Value) : _clientDefeats;
        }

        private static HashSet<string> ParseState(string value)
        {
            var result = NewSet();
            foreach (string part in (value ?? "").Split(','))
            {
                string id = part.Trim();
                if (id.Length > 0) result.Add(id);
            }
            return result;
        }

        private static string SerializeState(HashSet<string> state)
        {
            return string.Join(",", Rules.Bosses.Where(boss => state.Contains(boss.Id)).Select(boss => boss.Id));
        }

        private static HashSet<string> NewSet()
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private static void WriteSet(ZPackage package, HashSet<string> state)
        {
            package.Write(state.Count);
            foreach (string value in state) package.Write(value);
        }

        private static HashSet<string> ReadSet(ZPackage package)
        {
            int count = package.ReadInt();
            if (count < 0 || count > 1000) throw new InvalidOperationException("Invalid state count in server snapshot.");
            HashSet<string> result = NewSet();
            for (int i = 0; i < count; i++) result.Add(package.ReadString());
            return result;
        }
    }
}

