# ProgressionGater development

## Build

The project targets .NET Framework 4.8 and expects Valheim/BepInEx reference DLLs in `lib/`.

```powershell
dotnet build src/ProgressionGater/ProgressionGater.csproj -c Release
```

Output: `src/ProgressionGater/bin/Release/net48/net48/ProgressionGater.dll`

Never commit the reference DLLs in `lib/`.

