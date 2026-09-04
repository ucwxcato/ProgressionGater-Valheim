using System;
using System.Collections.Generic;

namespace ProgressionGater
{
    internal sealed class BossDefinition
    {
        internal string Id;
        internal string DisplayName;
        internal string PrefabName;
        internal string GlobalKey;
        internal string[] Aliases = Array.Empty<string>();
        internal readonly HashSet<string> Keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        internal readonly HashSet<string> ExactPrefabs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}

