$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'src/ProgressionGater/ProgressionGater.csproj'
dotnet build $project -c Release

