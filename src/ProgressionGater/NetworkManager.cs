using System;
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
            if (host.Length == 0) return false;
            try { return ZNet.instance.IsAdmin(host); }
            catch { return false; }
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
