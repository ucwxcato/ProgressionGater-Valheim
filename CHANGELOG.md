# Changelog

## 0.2.3 - 2026-09-09

- Updated Valheim 1.0 admin-list handling to use the `V_<steamid64>` format.

## 0.2.2 - 2026-09-09

- Made server admin authorization accept Valheim peer UID, socket identity, and the loaded admin-list identities.

## 0.2.1 - 2026-09-09

- Removed the cheat-gate dependency from `pg_*` commands; server-side admin authorization remains mandatory.
- Progression Gater commands no longer require ServerDevcommands.

## 0.2.0 - 2026-09-09

- Updated the Thunderstore dependency to BepInExPack Valheim 5.4.2350 for Valheim 1.0.
- Refreshed the documented build workflow for separate BepInEx and Valheim reference locations.
- Registered Progression Gater commands outside Valheim's cheat gate so they do not depend on ServerDevcommands.

## 0.1.2 - 2026-09-06

- Enforced boss recipe gates at final craft execution so other crafting patches cannot bypass them.
- Made the recipe availability check a final-result override for better mod compatibility.

## 0.1.1 - 2026-09-05

- Made blocked crafting recipes fail silently instead of repeating HUD notices while the crafting UI is open.

## 0.1.0 - 2026-09-04

- Extracted server-wide boss progression from ValheimHouseholds.
- Added configurable boss definitions, keyword rules, and exact-prefab rules.
- Added server-to-client rule and state synchronization.
- Added authenticated remote admin commands.
- Added independent summon and crafting gates plus optional admin bypass.
- Added Discord webhook announcements for boss unlocks and actual boss defeats.
