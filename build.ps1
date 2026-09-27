<#
.SYNOPSIS
  Build Suit Enhancement Suite and stage it.

.DESCRIPTION
  Builds the mod DLL from src\ and assembles .\package\ from the repo's About folder and the DLL.
  With -Deploy, copies the package into Documents\My Games\Stationeers\mods\<repo folder name>.
  The mod name is the repo folder name: src\<name>.csproj builds <name>.dll.

  The version is written in three places (csproj <Version>, Plugin.PluginVersion, About.xml <Version>);
  the build fails when they differ. About.xml <Description> must stay under 8000 characters (Workshop limit).

.EXAMPLE
  .\build.ps1 -Deploy
#>
[CmdletBinding()]
param(
    [string]$GameDir = $env:STATIONEERS_DIR,
    [switch]$Deploy,
    [switch]$Force,
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$name = Split-Path $root -Leaf

if (-not $GameDir) {
    $GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Stationeers'
}
if (-not (Test-Path (Join-Path $GameDir 'rocketstation_Data\Managed\Assembly-CSharp.dll'))) {
    throw "Stationeers not found at '$GameDir'. Pass -GameDir or set STATIONEERS_DIR."
}

$csproj = Join-Path $root "src\$name.csproj"
$plugin = Join-Path $root 'src\Plugin.cs'
$about = Join-Path $root 'About\About.xml'
if (-not (Test-Path $csproj)) { throw "No project at ${csproj}: the repo folder name must match the csproj name." }

$aboutText = Get-Content $about -Raw -Encoding UTF8
$versions = [ordered]@{
    'csproj'    = ([regex]::Match((Get-Content $csproj -Raw), '<Version>([^<]+)</Version>')).Groups[1].Value
    'plugin'    = ([regex]::Match((Get-Content $plugin -Raw), 'PluginVersion\s*=\s*"([^"]+)"')).Groups[1].Value
    'About.xml' = ([regex]::Match($aboutText, '<Version>([^<]+)</Version>')).Groups[1].Value
}
# @() so a single shared version stays an array; indexing a bare string yields one char.
$distinct = @($versions.Values | Sort-Object -Unique)
if ($distinct.Count -ne 1 -or [string]::IsNullOrWhiteSpace($distinct[0])) {
    $detail = ($versions.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join ', '
    throw "Version mismatch: $detail. Bump all three to the same value."
}
$version = $distinct[0]

$description = ([xml]$aboutText).ModMetadata.Description
if ($description.Length -ge 8000) { throw "About.xml <Description> is $($description.Length) characters; the Workshop limit is 8000." }

Write-Host "Building $name $version against $GameDir"
dotnet build $csproj -c $Configuration -p:GameDir="$GameDir" --nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

$dll = Join-Path $root "src\bin\$Configuration\$name.dll"
if (-not (Test-Path $dll)) { throw "Build produced no DLL at $dll" }

$package = Join-Path $root 'package'
if (Test-Path $package) { Remove-Item $package -Recurse -Force }
New-Item -ItemType Directory -Path $package | Out-Null
Copy-Item (Join-Path $root 'About') $package -Recurse
Copy-Item $dll $package

# Steam rejects workshop previews over 1 MB.
$thumbPath = Join-Path $package 'About\thumb.png'
if ((Test-Path $thumbPath) -and (Get-Item $thumbPath).Length -gt 1MB) {
    Write-Warning "About\thumb.png is over 1 MB. Steam caps previews at 1 MB and the game will silently fall back to a blank image."
}

Write-Host "Staged $package ($($description.Length) description characters)"

if ($Deploy) {
    # A running game holds the old DLL and keeps running it; LU may be playing.
    if (-not $Force -and (Get-Process -Name 'rocketstation' -ErrorAction SilentlyContinue)) {
        throw 'Stationeers is running: close the game before deploying.'
    }
    $mods = Join-Path ([Environment]::GetFolderPath('MyDocuments')) "My Games\Stationeers\mods\$name"
    if (Test-Path $mods) { Remove-Item $mods -Recurse -Force }
    New-Item -ItemType Directory -Path $mods -Force | Out-Null
    Copy-Item "$package\*" $mods -Recurse
    Write-Host "Deployed to $mods"
}
