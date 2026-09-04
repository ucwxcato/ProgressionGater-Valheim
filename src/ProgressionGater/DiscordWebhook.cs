using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ProgressionGater
{
    internal static class DiscordWebhook
    {
        private static readonly HttpClient Client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        internal static void PostBossUnlocked(BossDefinition boss, string admin)
        {
            if (!ProgressionService.IsServer || !ModConfig.WebhookPostUnlocks.Value) return;
            string template = ModConfig.UnlockMessage.Value ?? "";
            Post(Expand(template, boss, admin), prefixServer: template.IndexOf("{server}", StringComparison.OrdinalIgnoreCase) < 0);
        }

        internal static void PostBossDefeated(BossDefinition boss)
        {
            if (!ProgressionService.IsServer || !ModConfig.WebhookPostDefeats.Value) return;
            string template = ModConfig.DefeatMessage.Value ?? "";
            Post(Expand(template, boss, ""), prefixServer: template.IndexOf("{server}", StringComparison.OrdinalIgnoreCase) < 0);
        }

        internal static void PostTest(string admin)
        {
            if (!ProgressionService.IsServer) return;
            Post($"✅ Progression Gater webhook test requested by **{SafeMentionText(admin ?? "Server admin")}**.", prefixServer: true);
        }

        private static string Expand(string template, BossDefinition boss, string admin)
        {
            string server = ModConfig.WebhookServerLabel.Value ?? "";
            return (template ?? "")
                .Replace("{boss}", SafeMentionText(boss?.DisplayName ?? "Unknown boss"))
                .Replace("{bossId}", SafeMentionText(boss?.Id ?? "unknown"))
                .Replace("{admin}", SafeMentionText(admin ?? "Server admin"))
                .Replace("{server}", SafeMentionText(server));
        }

        private static void Post(string message, bool prefixServer)
        {
            string configuredUrl = (ModConfig.WebhookUrl.Value ?? "").Trim();
            if (configuredUrl.Length == 0 || string.IsNullOrWhiteSpace(message)) return;
            if (!TryValidateUrl(configuredUrl, out Uri url)) return;

            string server = (ModConfig.WebhookServerLabel.Value ?? "").Trim();
            if (prefixServer && server.Length > 0) message = $"**[{SafeMentionText(server)}]** {message}";
            if (message.Length > 1900) message = message.Substring(0, 1900);
            _ = PostAsync(url, message);
        }

        private static async Task PostAsync(Uri url, string message)
        {
            try
            {
                string json = $"{{\"username\":\"Progression Gater\",\"allowed_mentions\":{{\"parse\":[]}},\"content\":\"{JsonEscape(message)}\"}}";
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                using (HttpResponseMessage response = await Client.PostAsync(url, content).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                        Plugin.Log.LogWarning($"Discord webhook returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Discord webhook post failed: {ex.Message}");
            }
        }

        private static bool TryValidateUrl(string raw, out Uri url)
        {
            url = null;
            if (!Uri.TryCreate(raw, UriKind.Absolute, out Uri parsed) || parsed.Scheme != Uri.UriSchemeHttps)
            {
                Plugin.Log.LogWarning("Discord WebhookUrl must be an absolute HTTPS URL.");
                return false;
            }

            if (!ModConfig.AllowNonDiscordWebhookHosts.Value)
            {
                string host = parsed.Host.ToLowerInvariant();
                bool discord = host == "discord.com" || host.EndsWith(".discord.com", StringComparison.Ordinal) ||
                               host == "discordapp.com" || host.EndsWith(".discordapp.com", StringComparison.Ordinal);
                if (!discord)
                {
                    Plugin.Log.LogWarning("WebhookUrl was not posted because its host is not Discord. Enable AllowNonDiscordWebhookHosts only for a trusted compatible endpoint.");
                    return false;
                }
            }
            url = parsed;
            return true;
        }

        private static string SafeMentionText(string value)
        {
            return (value ?? "").Replace("@", "@\u200B");
        }

        private static string JsonEscape(string value)
        {
            var builder = new StringBuilder(value.Length + 16);
            foreach (char character in value)
            {
                switch (character)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '"': builder.Append("\\\""); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (character < 32) builder.Append("\\u").Append(((int)character).ToString("x4"));
                        else builder.Append(character);
                        break;
                }
            }
            return builder.ToString();
        }
    }
}
