# Progression Gater

A lightweight, server-wide Valheim progression scheduler. Administrators decide when bosses may be summoned; defeating a boss releases its configured crafting tier for everyone.

Progression Gater is designed for community servers that want a shared release schedule without adopting a large progression overhaul.

## Features

- Live admin commands for unlocking and relocking boss summons.
- Server-wide boss-defeat state with immediate client synchronization.
- Independent boss-summon and crafting gates.
- Configurable vanilla or modded bosses.
- Exact-prefab and case-insensitive keyword recipe rules.
- Optional matching against recipe ingredients as well as outputs.
- Optional admin bypass.
- Optional Discord webhook posts when a boss is unlocked or genuinely defeated.
- Config hot reload and server-authoritative rule synchronization.

This initial version gates boss altars and ordinary crafting recipes. It does not yet gate equipment use, building, cooking-station conversions, smelting, loot drops, traders, portals, or biome entry.

## Requirements

- Valheim
- BepInEx
- The mod must be installed on the dedicated server and every client.


## Installation

Copy `ProgressionGater.dll` into `BepInEx/plugins/` on the server and all clients. Start the server once to generate:

```text
BepInEx/config/com.catosvalheim.progressiongater.cfg
```

The server's rules and state are synchronized to clients. Client-side copies of the rule configuration do not override the server.

## Admin commands

Commands are authenticated against Valheim's server admin list.

```text
pg_status
pg_unlock next
pg_unlock <boss>
pg_lock <boss>
pg_defeat <boss>
pg_undefeat <boss>
pg_reset confirm
pg_webhook_test
```

`pg_unlock next` selects the first configured boss that has not been defeated. An explicit boss ID may be used to unlock out of order.

`pg_defeat` repairs/imports state administratively and intentionally does not send a boss-kill webhook. Only a real `RPC_SetGlobalKey` boss-defeat event sends that notification.

## Default progression

| Defeated boss | Crafting keywords released |
|---|---|
| Eikthyr | `Bronze` |
| The Elder | `Iron` |
| Bonemass | `Silver`, `Obsidian` |
| Moder | `BlackMetal` |
| Yagluth | Mistlands material keywords |
| The Queen | Ashlands material keywords |
| Fader | No default next-biome rule yet |

All seven bosses begin summon-locked and undefeated on a new configuration.

## Rule configuration

Boss records use this format:

```text
id|display name|boss prefab|defeat global key|alias,alias
```

Records are separated with semicolons. Their order controls `pg_unlock next`. Aliases may be used by commands and prefab lookup; alternate defeat-key aliases must begin with `defeated_`.

Keyword recipe rules use:

```text
bossId=keyword,keyword;bossId=keyword
```

Keywords match substrings without case sensitivity. For precise rules—especially modded content—prefer exact-prefab rules:

```text
elder=MyMod_IronWand,MyMod_IronStaff;moder=MyMod_PlainsBlade
```

When `MatchRecipeIngredients = true`, both recipe output and ingredient prefab names participate in matching. When false, only the output prefab is checked.

Invalid records and references to unknown boss IDs are ignored with warnings in the BepInEx log. If every boss definition is invalid, the built-in definitions are restored as a safe fallback.

## Discord webhook

Set these values in the server config:

```ini
[Discord]
WebhookUrl = https://discord.com/api/webhooks/...
ServerLabel = My Valheim Server
PostBossUnlocks = true
PostBossDefeats = true
```

Messages are configurable:

- Unlock tokens: `{boss}`, `{bossId}`, `{admin}`, `{server}`
- Defeat tokens: `{boss}`, `{bossId}`, `{server}`

The webhook suppresses Discord mentions. By default, only HTTPS Discord hosts are accepted. Compatible third-party endpoints require explicitly enabling `AllowNonDiscordWebhookHosts`.

Run `pg_webhook_test` after configuring the URL to verify delivery without changing progression state.

## Building

Place the required game/framework references in `lib/`, or use:

```powershell
.\scripts\setup-references.ps1 -SourceDirectory "C:\path\to\BepInEx\core"
.\scripts\build.ps1
```

Build output:

```text
src/ProgressionGater/bin/Release/net48/net48/ProgressionGater.dll
```

Reference DLLs and build output are gitignored.

## Runtime test checklist

Before the first public release, verify on a dedicated server:

1. A locked Eikthyr altar rejects a normal player.
2. `pg_unlock eikthyr` permits the summon and posts one unlock webhook.
3. Bronze crafting is blocked before the kill.
4. Killing Eikthyr records the defeat, posts one defeat webhook, and releases Bronze recipes for every connected client.
5. Reconnecting receives the same state.
6. `pg_lock eikthyr` blocks another summon without relocking Bronze recipes.
7. A non-admin cannot execute any state-changing command.

## Publishing status

The code is an initial extracted implementation. Before publishing to Thunderstore, choose a license, add a package icon/manifest, and complete the dedicated-server runtime checklist above.
