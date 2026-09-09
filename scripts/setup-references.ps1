param(
    [Parameter(Mandatory = $true)]
    [string]$GameManagedDirectory,

    [Parameter(Mandatory = $true)]
    [string]$BepInExCoreDirectory
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$destination = Join-Path $repoRoot 'lib'
$resolvedGameManaged = (Resolve-Path -LiteralPath $GameManagedDirectory).Path
$resolvedBepInExCore = (Resolve-Path -LiteralPath $BepInExCoreDirectory).Path
$resolvedDestination = (Resolve-Path -LiteralPath $destination).Path

$gameReferences = @(
    'UnityEngine.dll',
    'UnityEngine.CoreModule.dll',
    'assembly_valheim.dll',
    'assembly_utils.dll'
)
$bepInExReferences = @('BepInEx.dll', '0Harmony20.dll')

foreach ($name in $gameReferences) {
    $source = Join-Path $resolvedGameManaged $name
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "Missing required reference: $source"
    }
    Copy-Item -LiteralPath $source -Destination (Join-Path $resolvedDestination $name) -Force
}

foreach ($name in $bepInExReferences) {
    $source = Join-Path $resolvedBepInExCore $name
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "Missing required reference: $source"
    }
    Copy-Item -LiteralPath $source -Destination (Join-Path $resolvedDestination $name) -Force
}

Write-Host "Copied $($gameReferences.Count + $bepInExReferences.Count) build references into $resolvedDestination"
