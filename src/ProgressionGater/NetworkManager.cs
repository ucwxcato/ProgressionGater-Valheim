using System;
using System.Collections.Generic;
using HarmonyLib;

namespace ProgressionGater
{
    internal static class NetworkManager
    {
        internal const string SyncRpc = "ProgressionGater.Sync.v1";
        internal const string MessageRpc = "ProgressionGater.Message.v1";

        internal static void Register(ZRoutedRpc rpc)
        {
            rpc.Register<ZPackage>(SyncRpc, (sender, package) =>
            {
                if (!IsFromServer(sender))
                {
                    Plugin.Log.LogWarning($"Rejected progression snapshot from non-server peer {sender}.");
                    return;
                }
                try { ProgressionService.ApplyServerSnapshot(package); }
                catch (Exception ex) { Plugin.Log.LogError($"Invalid progression snapshot: {ex.Message}"); }
            });

            rpc.Register<string>(MessageRpc, (sender, message) =>
            {
                if (!IsFromServer(sender)) return;
                Player.m_localPlayer?.Message(MessageHud.MessageType.Center, message);
            });

            AdminCommands.RegisterRpcHandlers(rpc);
        }

        internal static bool IsAdmin(ZNetPeer peer)
        {
            if (peer == null || ZNet.instance == null || !ZNet.instance.IsServer()) return false;
            string host = peer.m_socket?.GetHostName() ?? "";
            string uid = peer.m_uid.ToString();
            try
            {
                // Depending on the backend, Valheim may expose the player's
                // Steam/platform ID as either the peer UID or socket hostname.
                if ((host.Length > 0 && ZNet.instance.IsAdmin(host)) || ZNet.instance.IsAdmin(uid))
                    return true;

                // Fall back to the normalized list Valheim actually loaded.
                // This also handles platform-ID formatting differences.
                List<string> admins = ZNet.instance.GetAdminList();
                foreach (string admin in admins ?? new List<string>())
                {
                    if (string.Equals(admin?.Trim(), uid, StringComparison.OrdinalIgnoreCase) ||
                        (host.Length > 0 && string.Equals(admin?.Trim(), host, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }

                Plugin.Log.LogWarning($"Admin check denied for peer {uid}; socket identity '{host}'; loaded admins: {string.Join(",", admins ?? new List<string>())}");
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Admin check failed for peer {uid}: {ex.Message}");
                return false;
            }
        }

        internal static bool CanPeerBypass(long peerId)
        {
            if (!ModConfig.AdminBypass.Value || ZNet.instance == null) return false;
            if (!ZNet.instance.IsServer()) return ProgressionService.LocalCanBypass;
            ZNetPeer peer = ZNet.instance.GetPeer(peerId);
            return IsAdmin(peer);
        }

        internal static void SendMessage(long peerId, string message)
        {
            if (ZRoutedRpc.instance == null) return;
            ZRoutedRpc.instance.InvokeRoutedRPC(peerId, MessageRpc, new object[] { message });
        }

        internal static bool IsFromServer(long sender)
        {
            if (ZNet.instance == null || ZNet.instance.IsServer()) return false;
            ZNetPeer serverPeer = ZNet.instance.GetServerPeer();
            if (serverPeer != null && serverPeer.m_uid == sender) return true;
            try
            {
                long serverId = Traverse.Create(ZRoutedRpc.instance).Field("m_serverPeerID").GetValue<long>();
                return serverId != 0L && sender == serverId;
            }
            catch { return false; }
        }
    }
}
