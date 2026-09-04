param(
    [Parameter(Mandatory = $true)]
    [string]$SourceDirectory
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$destination = Join-Path $repoRoot 'lib'
$required = @(
    'BepInEx.dll',
    '0Harmony20.dll',
    'UnityEngine.dll',
    'UnityEngine.CoreModule.dll',
    'assembly_valheim.dll',
    'assembly_utils.dll'
)

$resolvedSource = (Resolve-Path -LiteralPath $SourceDirectory).Path
$resolvedDestination = (Resolve-Path -LiteralPath $destination).Path

foreach ($name in $required) {
    $source = Join-Path $resolvedSource $name
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "Missing required reference: $source"
    }
    Copy-Item -LiteralPath $source -Destination (Join-Path $resolvedDestination $name) -Force
}

Write-Host "Copied $($required.Count) build references into $resolvedDestination"
