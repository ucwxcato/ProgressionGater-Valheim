# Isolated local test server

`start_progressiongater_test.bat` uses the existing Steam Valheim dedicated-server installation and launches a fresh `progressiongater_test` world on port 2462.

The launcher deploys only `ProgressionGater.dll`; it does not copy the old HouseholdHeim plugin set or configs. Run it manually by double-clicking it.

Add your Steam ID to the live server's `BepInEx/config/adminlist.txt` so the `pg_*` commands can authenticate. `adminlist.example.txt` is provided as a placeholder; real admin IDs remain ignored by Git.

Install the same DLL in a clean r2modman client profile containing BepInExPack Valheim, then connect to `127.0.0.1:2462`.

## First run

1. In r2modman, create a clean Valheim profile named `ProgressionGater Test`.
2. Install `denikson-BepInExPack_Valheim` in that profile.
3. Choose **Settings → Import local mod** and select `artifacts/Catosaur-ProgressionGater-0.1.0.zip` from this repository.
4. Optionally put your Discord webhook in the live config shown below. Do not commit the URL.
5. Double-click `start_progressiongater_test.bat` and wait for `Game server connected`.
6. Start modded Valheim through the clean profile and join `127.0.0.1:2462`.
7. Open F5, run `devcommands`, then run `pg_status` and `pg_webhook_test`.

The test server uses password `696969` and a fresh world named `progressiongater_test`.

The live server config remains authoritative at:

`C:\Program Files (x86)\Steam\steamapps\common\Valheim dedicated server\BepInEx\config\com.catosaur.progressiongater.cfg`
