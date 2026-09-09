# ProgressionGater development

## Build

The project targets .NET Framework 4.8 and expects Valheim/BepInEx reference DLLs in `lib/`.

```powershell
dotnet build src/ProgressionGater/ProgressionGater.csproj -c Release
```

Output: `src/ProgressionGater/bin/Release/net48/net48/ProgressionGater.dll`

Never commit the reference DLLs in `lib/`.

## Valheim 1.0 development setup

The current validated client runtime is Valheim `1.0.7` (network version `39`) with Unity `6000.0.75.2503836`. The current BepInExPack Valheim release is `5.4.2350`, which contains BepInEx `5.4.23.5`. Install that pack on every client and dedicated server; the pack version is distinct from the underlying BepInEx version.

The local Steam paths used for development are:

- Client game: `C:\Program Files (x86)\Steam\steamapps\common\Valheim`
- Client managed assemblies: `Valheim\valheim_Data\Managed`
- Dedicated server: `C:\Program Files (x86)\Steam\steamapps\common\Valheim dedicated server`
- Dedicated server BepInEx core: `Valheim dedicated server\BepInEx\core`
- r2modman test profile: `C:\Users\magni\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\ProgressionGater test`

Refresh build references with separate source directories because game assemblies and BepInEx core assemblies are not stored together:

```powershell
.\scripts\setup-references.ps1 `
  -GameManagedDirectory "C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Managed" `
  -BepInExCoreDirectory "C:\Program Files (x86)\Steam\steamapps\common\Valheim\BepInEx\core"
```

The dedicated server and client must run the same Valheim network version. A server still reporting `0.221.12` / network `36` cannot accept a `1.0.7` / network `39` client, even when BepInEx and the plugin load successfully.

## Local test server

`TEST_SERVER\start_progressiongater_test.bat` is a local, ignored harness. It rebuilds the plugin, deploys the latest `ProgressionGater.dll` to both the dedicated server and the r2modman `ProgressionGater test` profile, and copies the client's `JereKuusela-Server_devcommands\ServerDevcommands.dll` into the server plugin directory.

The launcher also copies `TEST_SERVER\adminlist.txt` into the server save directory and starts Valheim with `-savedir`. The test admin SteamID64 is `76561198062587799`. Valheim's authoritative admin list is the `adminlist.txt` under the active save directory, not a client-local config. The launcher refuses to start if the server game assembly is older than the refreshed local 1.0 reference.

The launcher currently uses the isolated test world `progressiongater_test`, port `2462`, and password `696969`. Stop any previously running server before restarting it so the new `-savedir`, plugin, and admin-list deployment are applied.

Close the Valheim client before running the launcher; Windows locks the loaded client DLL and cannot replace it while `valheim.exe` is running.

The `TEST_SERVER` directory is intentionally ignored because it contains local launchers, server files, configs, admin identity, worlds, and runtime state. Do not commit those files or live server data. Do not package for Thunderstore until a clean 1.0 client/server smoke test has passed.
