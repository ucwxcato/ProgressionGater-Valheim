# Changelog

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
