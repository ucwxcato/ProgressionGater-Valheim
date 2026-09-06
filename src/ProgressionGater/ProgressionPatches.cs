using System;
using HarmonyLib;

namespace ProgressionGater
{
    [HarmonyPatch]
    internal static class ProgressionPatches
    {
        [HarmonyPatch(typeof(ZRoutedRpc), MethodType.Constructor, typeof(bool))]
        [HarmonyPostfix]
        private static void OnRoutedRpcCreated(ZRoutedRpc __instance)
        {
            if (__instance == null) return;
            ProgressionService.ClearServerSnapshot();
            NetworkManager.Register(__instance);
        }

        [HarmonyPatch(typeof(ZRoutedRpc), nameof(ZRoutedRpc.AddPeer))]
        [HarmonyPostfix]
        private static void OnPeerAdded(ZNetPeer peer)
        {
            if (!ProgressionService.IsServer || peer == null) return;
            ProgressionService.SendToPeer(peer);
        }

        [HarmonyPatch(typeof(ZoneSystem), "RPC_SetGlobalKey")]
        [HarmonyPostfix]
        private static void OnGlobalKeySet(string name)
        {
            if (!ProgressionService.IsServer || !ProgressionService.Enabled || string.IsNullOrWhiteSpace(name)) return;
            try
            {
                BossDefinition boss = ProgressionService.Rules.ResolveGlobalKey(name);
                if (boss == null || !ProgressionService.MarkDefeated(boss)) return;
                Plugin.Log.LogInfo($"Boss defeat recorded from global key '{name}': {boss.DisplayName}");
                DiscordWebhook.PostBossDefeated(boss);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Boss-defeat processing failed for key '{name}': {ex}");
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Recipe), typeof(bool), typeof(int), typeof(int))]
        [HarmonyPostfix]
        private static void OnHaveRequirements(Player __instance, Recipe recipe, ref bool __result)
        {
            if (__instance == null || ProgressionService.CraftingBlockedBy(recipe) == null) return;

            // HaveRequirements runs continuously while crafting UI is open.
            // Refuse the locked recipe silently so this check cannot spam HUD notices.
            __result = false;
        }

        [HarmonyPatch(typeof(InventoryGui), "DoCrafting", typeof(Player))]
        [HarmonyPrefix]
        private static bool OnDoCrafting(Player player, Recipe ___m_craftRecipe)
        {
            BossDefinition blockedBy = ProgressionService.CraftingBlockedBy(___m_craftRecipe);
            if (blockedBy == null) return true;

            player?.Message(MessageHud.MessageType.Center,
                $"Locked until {blockedBy.DisplayName} has been defeated.");
            Plugin.Log.LogWarning(
                $"Blocked locked recipe craft: item={___m_craftRecipe?.m_item?.name ?? "unknown"}, boss={blockedBy.Id}");
            return false;
        }

        [HarmonyPatch(typeof(OfferingBowl), nameof(OfferingBowl.UseItem))]
        [HarmonyPrefix]
        private static bool OnOfferingBowlUseItem(OfferingBowl __instance, Humanoid user, ref bool __result)
        {
            if (!ProgressionService.GateBossSummons || ProgressionService.LocalCanBypass) return true;
            BossDefinition boss = ProgressionService.Rules.Resolve(__instance);
            if (boss == null || ProgressionService.IsSummonUnlocked(boss)) return true;

            user?.Message(MessageHud.MessageType.Center,
                $"{boss.DisplayName} is locked. A server admin must unlock this boss before it can be summoned.");
            __result = false;
            return false;
        }

        [HarmonyPatch(typeof(OfferingBowl), "RPC_SpawnBoss")]
        [HarmonyPrefix]
        private static bool OnOfferingBowlSpawnBoss(OfferingBowl __instance, long senderId)
        {
            if (!ProgressionService.GateBossSummons) return true;
            BossDefinition boss = ProgressionService.Rules.Resolve(__instance);
            if (boss == null || ProgressionService.IsSummonUnlocked(boss) || NetworkManager.CanPeerBypass(senderId)) return true;

            Plugin.Log.LogWarning($"Blocked locked boss summon: boss={boss.Id}, sender={senderId}");
            NetworkManager.SendMessage(senderId,
                $"{boss.DisplayName} is locked. A server admin must unlock this boss before it can be summoned.");
            return false;
        }
    }
}
