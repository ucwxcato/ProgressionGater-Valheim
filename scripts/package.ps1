[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$project = Join-Path $repoRoot 'src\ProgressionGater\ProgressionGater.csproj'
$dllPath = Join-Path $repoRoot 'src\ProgressionGater\bin\Release\net48\net48\ProgressionGater.dll'
$manifestPath = Join-Path $repoRoot 'thunderstore\manifest.json'
$iconPath = Join-Path $repoRoot 'thunderstore\icon.png'
$readmePath = Join-Path $repoRoot 'README.md'
$changelogPath = Join-Path $repoRoot 'CHANGELOG.md'
$artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))

$manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
if ($manifest.name -ne 'ProgressionGater') {
    throw "Unexpected package name '$($manifest.name)'."
}
if ($manifest.version_number -notmatch '^\d+\.\d+\.\d+$') {
    throw 'Manifest version_number must use MAJOR.MINOR.PATCH format.'
}
if ([string]::IsNullOrWhiteSpace($manifest.description) -or $manifest.description.Length -gt 250) {
    throw 'Manifest description must contain 1-250 characters.'
}
if ($null -eq $manifest.dependencies -or $manifest.dependencies.Count -eq 0) {
    throw 'Manifest must declare at least the BepInEx dependency.'
}

Add-Type -AssemblyName System.Drawing
$icon = [System.Drawing.Image]::FromFile($iconPath)
try {
    if ($icon.Width -ne 256 -or $icon.Height -ne 256) {
        throw "Thunderstore icon must be 256x256 pixels; found $($icon.Width)x$($icon.Height)."
    }
}
finally {
    $icon.Dispose()
}

dotnet build $project -c Release
if ($LASTEXITCODE -ne 0) {
    throw "ProgressionGater build failed with exit code $LASTEXITCODE."
}

$assemblyVersion = [System.Reflection.AssemblyName]::GetAssemblyName($dllPath).Version
$manifestVersion = [System.Version]::Parse($manifest.version_number)
if ($assemblyVersion.Major -ne $manifestVersion.Major -or
    $assemblyVersion.Minor -ne $manifestVersion.Minor -or
    $assemblyVersion.Build -ne $manifestVersion.Build) {
    throw "DLL version $assemblyVersion does not match manifest version $manifestVersion."
}

$packageRoot = [System.IO.Path]::GetFullPath((Join-Path $artifactsRoot "package-$($manifest.version_number)"))
$outputPath = [System.IO.Path]::GetFullPath((Join-Path $artifactsRoot "Catosaur-ProgressionGater-$($manifest.version_number).zip"))
if (-not $packageRoot.StartsWith($artifactsRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Unsafe package staging path: $packageRoot"
}
if (-not $outputPath.StartsWith($artifactsRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Unsafe package output path: $outputPath"
}

New-Item -ItemType Directory -Path $artifactsRoot -Force | Out-Null
if (Test-Path -LiteralPath $packageRoot) {
    Remove-Item -LiteralPath $packageRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $packageRoot | Out-Null

Copy-Item -LiteralPath $dllPath -Destination (Join-Path $packageRoot 'ProgressionGater.dll')
Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $packageRoot 'manifest.json')
Copy-Item -LiteralPath $iconPath -Destination (Join-Path $packageRoot 'icon.png')
Copy-Item -LiteralPath $readmePath -Destination (Join-Path $packageRoot 'README.md')
Copy-Item -LiteralPath $changelogPath -Destination (Join-Path $packageRoot 'CHANGELOG.md')

Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $outputPath -CompressionLevel Optimal -Force

$archive = Get-Item -LiteralPath $outputPath
$hash = Get-FileHash -LiteralPath $outputPath -Algorithm SHA256
Write-Host "Created $($archive.FullName)"
Write-Host "Size: $($archive.Length) bytes"
Write-Host "SHA256: $($hash.Hash)"
