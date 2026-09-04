using System;
using BepInEx.Configuration;

namespace ProgressionGater
{
    internal static class ModConfig
    {
        internal const string DefaultBossDefinitions =
            "eikthyr|Eikthyr|Eikthyr|defeated_eikthyr|deer;" +
            "elder|The Elder|gd_king|defeated_gdking|gdking;" +
            "bonemass|Bonemass|Bonemass|defeated_bonemass|;" +
            "moder|Moder|Dragon|defeated_dragon|;" +
            "yagluth|Yagluth|GoblinKing|defeated_goblinking|goblinking,defeated_gdking_varguts;" +
            "queen|The Queen|SeekerQueen|defeated_queen|seekerqueen;" +
            "fader|Fader|Fader|defeated_fader|";

        internal const string DefaultKeywordRules =
            "eikthyr=Bronze;" +
            "elder=Iron;" +
            "bonemass=Silver,Obsidian;" +
            "moder=BlackMetal;" +
            "yagluth=BlackCore,BlackMarble,Carapace,Mandible,Softtissue,YggdrasilWood,Eitr,ScaleHide,RoyalJelly,Sap,JotunPuffs,Magecap;" +
            "queen=Flametal,Grausten,AskHide,Asksvin,Morgen,CharredBone,CelestialFeather,CeramicPlate,BellFragment,Blackwood,Proustite,Sulfur,Fiddlehead,Vineberry,SmokePuff,Volture,Bonemaw";

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> GateBossSummons;
        internal static ConfigEntry<bool> GateCrafting;
        internal static ConfigEntry<bool> MatchRecipeIngredients;
        internal static ConfigEntry<bool> AdminBypass;
        internal static ConfigEntry<string> BossDefinitions;
        internal static ConfigEntry<string> KeywordRules;
        internal static ConfigEntry<string> ExactPrefabRules;
        internal static ConfigEntry<string> SummonUnlocks;
        internal static ConfigEntry<string> DefeatedBosses;
        internal static ConfigEntry<string> WebhookUrl;
        internal static ConfigEntry<string> WebhookServerLabel;
        internal static ConfigEntry<bool> WebhookPostUnlocks;
        internal static ConfigEntry<bool> WebhookPostDefeats;
        internal static ConfigEntry<bool> AllowNonDiscordWebhookHosts;
        internal static ConfigEntry<string> UnlockMessage;
        internal static ConfigEntry<string> DefeatMessage;

        internal static void Bind(ConfigFile config, Action changed)
        {
            Enabled = config.Bind("General", "Enabled", true, "Master switch. State continues to remain in this config while disabled.");
            GateBossSummons = config.Bind("Gates", "GateBossSummons", true, "Require an admin unlock before a configured boss can be summoned.");
            GateCrafting = config.Bind("Gates", "GateCrafting", true, "Block configured crafting recipes until their boss has been defeated.");
            MatchRecipeIngredients = config.Bind("Gates", "MatchRecipeIngredients", true, "Apply crafting rules to both recipe output and ingredient prefab names. False checks only the output.");
            AdminBypass = config.Bind("Gates", "AdminBypass", true, "Allow server admins to bypass local crafting and boss-summon gates.");

            BossDefinitions = config.Bind("Rules", "BossDefinitions", DefaultBossDefinitions,
                "Semicolon-separated records: id|display name|boss prefab|defeat global key|comma-separated aliases. Order defines pg_unlock next.");
            KeywordRules = config.Bind("Rules", "KeywordRules", DefaultKeywordRules,
                "Semicolon-separated bossId=keyword,keyword rules. A case-insensitive substring match gates a recipe.");
            ExactPrefabRules = config.Bind("Rules", "ExactPrefabRules", "",
                "Semicolon-separated bossId=prefab,prefab rules. Exact, case-insensitive recipe output/ingredient matching; useful for modded items.");

            SummonUnlocks = config.Bind("State", "SummonUnlocks", "", "Managed by pg_unlock/pg_lock. Comma-separated boss IDs.");
            DefeatedBosses = config.Bind("State", "DefeatedBosses", "", "Managed by boss kills and pg_defeat/pg_undefeat. Comma-separated boss IDs.");

            WebhookUrl = config.Bind("Discord", "WebhookUrl", "", "Discord webhook URL. Empty disables webhook posting.");
            WebhookServerLabel = config.Bind("Discord", "ServerLabel", "", "Optional server name included in webhook messages.");
            WebhookPostUnlocks = config.Bind("Discord", "PostBossUnlocks", true, "Post when an admin unlocks a boss summon.");
            WebhookPostDefeats = config.Bind("Discord", "PostBossDefeats", true, "Post when the game records a configured boss defeat.");
            AllowNonDiscordWebhookHosts = config.Bind("Discord", "AllowNonDiscordWebhookHosts", false, "Permit HTTPS webhook URLs outside discord.com/discordapp.com.");
            UnlockMessage = config.Bind("Discord", "UnlockMessage", "🔓 **{boss}** has been unlocked by **{admin}**.", "Tokens: {boss}, {bossId}, {admin}, {server}");
            DefeatMessage = config.Bind("Discord", "DefeatMessage", "⚔️ **{boss}** has been defeated. Its progression tier is now unlocked.", "Tokens: {boss}, {bossId}, {server}");

            Watch(Enabled, changed);
            Watch(GateBossSummons, changed);
            Watch(GateCrafting, changed);
            Watch(MatchRecipeIngredients, changed);
            Watch(AdminBypass, changed);
            Watch(BossDefinitions, changed);
            Watch(KeywordRules, changed);
            Watch(ExactPrefabRules, changed);
            Watch(WebhookUrl, changed);
            Watch(WebhookServerLabel, changed);
            Watch(WebhookPostUnlocks, changed);
            Watch(WebhookPostDefeats, changed);
            Watch(AllowNonDiscordWebhookHosts, changed);
            Watch(UnlockMessage, changed);
            Watch(DefeatMessage, changed);
        }

        private static void Watch<T>(ConfigEntry<T> entry, Action changed)
        {
            entry.SettingChanged += (_, __) => changed();
        }
    }
}
