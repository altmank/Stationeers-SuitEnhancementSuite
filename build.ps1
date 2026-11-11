#Requires -Version 7.5
<#
.SYNOPSIS
  Build Suit Enhancement Suite and stage it.

.DESCRIPTION
  Builds the mod DLL from src\ and assembles .\package\ from the repo's About folder and the DLL.
  With -Deploy, copies the package into <ModsRoot>\<repo folder name> (ModsRoot defaults to
  Documents\My Games\Stationeers\mods), holding the bt game lock only around the remove-and-copy.
  The mod name is the repo folder name: src\<name>.csproj builds <name>.dll.

  -DeployOnly copies the existing package without building; it refuses unless the package DLL's
  InformationalVersion +sha is HEAD and the working tree is clean.
  -LockScript is the game lock script (default $env:STATIONEERS_GAME_LOCK); a deploy refuses without one unless
  -NoLock. -LockHeld: the caller already holds the lock (not taken again). -Owner and -Item
  (item:SuitEnhancementSuite/<id>) are passed to acquire; -ExpectedMinutes 2 is fixed.

  The version is written in three places (csproj <Version>, Plugin.PluginVersion, About.xml <Version>);
  the build fails when they differ. About.xml <Description> must stay under 8000 characters (Workshop limit).

.EXAMPLE
  .\build.ps1 -Deploy
#>
[CmdletBinding()]
param(
    [string]$GameDir = $env:STATIONEERS_DIR,
    [switch]$Deploy,
    [switch]$DeployOnly,
    [string]$ModsRoot = (Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'My Games\Stationeers\mods'),
    [string]$LockScript = $env:STATIONEERS_GAME_LOCK,
    [switch]$LockHeld,
    [switch]$NoLock,
    [string]$Owner = 'beta-ses',
    [string]$Item = '',
    [switch]$Force,
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
# The mod name is the csproj name (not the folder: a worktree folder is named for its branch).
$name = @(Get-ChildItem (Join-Path $root 'src') -Filter *.csproj)[0].BaseName
$package = Join-Path $root 'package'

function Invoke-Deploy {
    # A running game holds the old DLL and keeps running it; the player may be in game.
    if (-not $Force -and (Get-Process -Name 'rocketstation' -ErrorAction SilentlyContinue)) {
        throw 'Stationeers is running: close the game before deploying.'
    }
    if (-not (Test-Path (Join-Path $package 'About\About.xml'))) { throw "No package at $package." }
    $takeLock = -not ($LockHeld -or $NoLock)
    if ($takeLock -and (-not $LockScript -or -not (Test-Path $LockScript))) {
        throw "Deploy refused: no game lock script ('$LockScript'). Set STATIONEERS_GAME_LOCK, pass -LockScript, or -NoLock."
    }
    $mods = Join-Path $ModsRoot $name
    if ($takeLock) {
        # A hashtable: an array splat would pass '-Item' as a positional string.
        $acquire = @{ Owner = $Owner; ExpectedMinutes = 2 }
        if ($Item) { $acquire.Item = $Item }
        & $LockScript acquire @acquire | ForEach-Object { Write-Host "   $_" }
        if ($LASTEXITCODE -ne 0) { throw 'Deploy failed: could not take the game lock.' }
    }
    try {
        # No backup of the previous install: -DeployOnly takes only HEAD's build, so every deployed version is a commit.
        if (Test-Path $mods) { Remove-Item $mods -Recurse -Force }
        New-Item -ItemType Directory -Path $mods -Force | Out-Null
        Copy-Item "$package\*" $mods -Recurse
    }
    finally {
        if ($takeLock) { & $LockScript release -Owner $Owner | ForEach-Object { Write-Host "   $_" } }
    }
    Write-Host "Deployed to $mods"
}

$head = (git -C $root rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $head -notmatch '^[0-9a-f]{40}$') {
    throw 'Cannot read HEAD (git rev-parse HEAD failed): the build needs a git checkout.'
}

if ($DeployOnly) {
    if (git -C $root status --porcelain --untracked-files=all) { throw 'Deploy refused: the working tree is dirty.' }
    $pkgDll = Join-Path $package "$name.dll"
    if (-not (Test-Path $pkgDll)) { throw "Deploy refused: no package DLL at $pkgDll; run build.ps1 first." }
    $pv = [Diagnostics.FileVersionInfo]::GetVersionInfo($pkgDll).ProductVersion
    if ($pv -notmatch '\+([0-9a-f]+)$' -or -not $head.StartsWith($Matches[1])) {
        throw "Deploy refused: package DLL is '$pv', HEAD is $head; run build.ps1 first."
    }
    Invoke-Deploy
    return
}

if (-not $GameDir) {
    $GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Stationeers'
}
if (-not (Test-Path (Join-Path $GameDir 'rocketstation_Data\Managed\Assembly-CSharp.dll'))) {
    throw "Stationeers not found at '$GameDir'. Pass -GameDir or set STATIONEERS_DIR."
}

$csproj = Join-Path $root "src\$name.csproj"
$plugin = Join-Path $root 'src\Plugin.cs'
$about = Join-Path $root 'About\About.xml'
if (-not (Test-Path $csproj)) { throw "No project at ${csproj}: the mod name is taken from the csproj in src." }

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

# HEAD's sha stamps InformationalVersion (+<sha>); without it the SDK drops it once a gc packs the branch ref.
Write-Host "Building $name $version ($head) against $GameDir"
dotnet build $csproj -c $Configuration -p:GameDir="$GameDir" -p:SourceRevisionId=$head --nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

$dll = Join-Path $root "src\bin\$Configuration\$name.dll"
if (-not (Test-Path $dll)) { throw "Build produced no DLL at $dll" }

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

if ($Deploy) { Invoke-Deploy }
