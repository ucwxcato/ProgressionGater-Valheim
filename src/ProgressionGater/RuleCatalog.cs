using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ProgressionGater
{
    internal sealed class RuleCatalog
    {
        internal List<BossDefinition> Bosses { get; } = new List<BossDefinition>();

        internal static RuleCatalog FromConfig()
        {
            var catalog = Parse(ModConfig.BossDefinitions.Value);
            ApplyRules(catalog, ModConfig.KeywordRules.Value, exact: false);
            ApplyRules(catalog, ModConfig.ExactPrefabRules.Value, exact: true);
            return catalog;
        }

        private static RuleCatalog Parse(string raw)
        {
            var result = new RuleCatalog();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string record in Split(raw, ';'))
            {
                string[] fields = record.Split('|');
                if (fields.Length < 4)
                {
                    Plugin.Log.LogWarning($"Ignoring malformed boss definition: {record}");
                    continue;
                }

                string id = fields[0].Trim();
                string display = fields[1].Trim();
                string prefab = fields[2].Trim();
                string key = fields[3].Trim();
                if (id.Length == 0 || display.Length == 0 || prefab.Length == 0 || key.Length == 0 || !ids.Add(id))
                {
                    Plugin.Log.LogWarning($"Ignoring invalid or duplicate boss definition: {record}");
                    continue;
                }

                result.Bosses.Add(new BossDefinition
                {
                    Id = id,
                    DisplayName = display,
                    PrefabName = prefab,
                    GlobalKey = key,
                    Aliases = fields.Length >= 5 ? Split(fields[4], ',').ToArray() : Array.Empty<string>(),
                });
            }

            if (result.Bosses.Count == 0 && !string.Equals(raw, ModConfig.DefaultBossDefinitions, StringComparison.Ordinal))
            {
                Plugin.Log.LogError("No valid boss definitions were configured; loading built-in defaults.");
                return Parse(ModConfig.DefaultBossDefinitions);
            }
            return result;
        }

        private static void ApplyRules(RuleCatalog catalog, string raw, bool exact)
        {
            foreach (string record in Split(raw, ';'))
            {
                int separator = record.IndexOf('=');
                if (separator <= 0)
                {
                    Plugin.Log.LogWarning($"Ignoring malformed {(exact ? "exact" : "keyword")} rule: {record}");
                    continue;
                }

                string bossId = record.Substring(0, separator).Trim();
                BossDefinition boss = catalog.Resolve(bossId);
                if (boss == null)
                {
                    Plugin.Log.LogWarning($"Ignoring rule for unknown boss ID '{bossId}'.");
                    continue;
                }

                foreach (string value in Split(record.Substring(separator + 1), ','))
                {
                    if (exact) boss.ExactPrefabs.Add(value);
                    else boss.Keywords.Add(value);
                }
            }
        }

        private static IEnumerable<string> Split(string value, char separator)
        {
            return (value ?? "").Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .Where(part => part.Length > 0);
        }

        internal BossDefinition Resolve(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string needle = Normalize(value);
            return Bosses.FirstOrDefault(boss =>
                EqualsIgnoreCase(boss.Id, needle) ||
                EqualsIgnoreCase(boss.DisplayName, needle) ||
                EqualsIgnoreCase(boss.PrefabName, needle) ||
                EqualsIgnoreCase(boss.GlobalKey, needle) ||
                boss.Aliases.Any(alias => EqualsIgnoreCase(alias, needle)));
        }

        internal BossDefinition ResolveGlobalKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string needle = Normalize(value);
            return Bosses.FirstOrDefault(boss =>
                EqualsIgnoreCase(boss.GlobalKey, needle) ||
                boss.Aliases.Any(alias => alias.StartsWith("defeated_", StringComparison.OrdinalIgnoreCase) && EqualsIgnoreCase(alias, needle)));
        }

        internal BossDefinition Resolve(OfferingBowl bowl)
        {
            if (bowl == null) return null;
            try
            {
                GameObject prefab = Traverse.Create(bowl).Field<GameObject>("m_bossPrefab").Value;
                return prefab == null ? null : Resolve(prefab.name);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Could not identify offering-bowl boss: {ex.Message}");
                return null;
            }
        }

        internal IEnumerable<BossDefinition> RequiredBosses(Recipe recipe, bool includeIngredients)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (recipe?.m_item != null) names.Add(Normalize(recipe.m_item.name));
            if (includeIngredients && recipe?.m_resources != null)
            {
                foreach (Piece.Requirement requirement in recipe.m_resources)
                    if (requirement?.m_resItem != null) names.Add(Normalize(requirement.m_resItem.name));
            }

            foreach (BossDefinition boss in Bosses)
            {
                if (names.Any(name => boss.ExactPrefabs.Contains(name) ||
                    boss.Keywords.Any(keyword => name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)))
                    yield return boss;
            }
        }

        internal void Write(ZPackage package)
        {
            package.Write(Bosses.Count);
            foreach (BossDefinition boss in Bosses)
            {
                package.Write(boss.Id);
                package.Write(boss.DisplayName);
                package.Write(boss.PrefabName);
                package.Write(boss.GlobalKey);
                WriteStrings(package, boss.Aliases);
                WriteStrings(package, boss.Keywords);
                WriteStrings(package, boss.ExactPrefabs);
            }
        }

        internal static RuleCatalog Read(ZPackage package)
        {
            var result = new RuleCatalog();
            int count = package.ReadInt();
            if (count < 0 || count > 100) throw new InvalidOperationException("Invalid boss count in server snapshot.");
            for (int i = 0; i < count; i++)
            {
                var boss = new BossDefinition
                {
                    Id = package.ReadString(),
                    DisplayName = package.ReadString(),
                    PrefabName = package.ReadString(),
                    GlobalKey = package.ReadString(),
                    Aliases = ReadStrings(package),
                };
                foreach (string value in ReadStrings(package)) boss.Keywords.Add(value);
                foreach (string value in ReadStrings(package)) boss.ExactPrefabs.Add(value);
                result.Bosses.Add(boss);
            }
            return result;
        }

        private static void WriteStrings(ZPackage package, IEnumerable<string> values)
        {
            string[] array = values.ToArray();
            package.Write(array.Length);
            foreach (string value in array) package.Write(value);
        }

        private static string[] ReadStrings(ZPackage package)
        {
            int count = package.ReadInt();
            if (count < 0 || count > 1000) throw new InvalidOperationException("Invalid rule count in server snapshot.");
            var result = new string[count];
            for (int i = 0; i < count; i++) result[i] = package.ReadString();
            return result;
        }

        private static string Normalize(string value)
        {
            return (value ?? "").Replace("(Clone)", "").Trim();
        }

        private static bool EqualsIgnoreCase(string left, string right)
        {
            return string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);
        }
    }
}
