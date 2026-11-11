# The skip-aegis game proof (board item skip-aegis-vm-proof): SES leaves an Aegis suit and its wearer alone, still serves vanilla suits,
# and runs as before without Aegis. One host-only run per call on a pool rig through RigVM main's host\vm-client-run.ps1. Consumer
# script only: no -Rig (P45), the world's own saved player (P51/P52), nothing on a real game.
#
#   -Run a   SES + Aegis + StationGod: the player WEARS an Aegis suit, a vanilla Hardsuit lies beside, need items in the inventory, food and
#            water held low for 135 s. Passes when the Aegis suit has no SES slot, the Hardsuit has all five, no SES consume/swap/shared-power
#            line names the player, no SES error, both mods report loaded.
#   -Run b   SES + StationGod, no Aegis: the player wears a Hardsuit with water and food in its SES slots, needs lowered. Passes when the
#            suit has the SES slots, an "Auto consume:" line appears and SES logs no error.
#
# The consume line is a Debug line (src\Needs\AutoConsumer.cs): the run sets BepInEx [Logging.Disk] LogLevels to include Debug (the guest's
# BepInEx.cfg is backed up and put back by vm-client-run). The verdict is tests\vm\skip-aegis-verdict.ps1 (host-tested by
# skip-aegis-verdict.tests.ps1); a missing capture fails it.
#
#   pwsh -NoProfile -File _alpha\tools\run-hidden.ps1 -Log <log> tests\vm\skip-aegis-proof.ps1 -Run a -WorkDir <temp folder> -ExpectedMinutes 12
#
# Build first: pwsh -File build.ps1 (stamps +sha; the package must be what this commit ships). Aegis comes from its main checkout's built
# package (its package.stamp head must match HEAD on the shipping paths, as Aegis's own proofs check).
# Results: G:\StationeersRigs\VM\runs\ses-skip-aegis-<a|b>\<stamp>-<rig>\ (LogOutput.log, sp-*.json, skip-facts.json, run.txt).
#Requires -Version 7.5
param(
    [Parameter(Mandatory)] [ValidateSet('a', 'b')] [string] $Run,
    [Parameter(Mandatory)] [string] $WorkDir,
    [string] $Owner = 'ses-vm-proof',
    [string] $Item = 'item:SuitEnhancementSuite/skip-aegis-vm-proof',
    [int] $ExpectedMinutes = 0,
    [string] $AegisCheckout = 'G:\Programming\Git\StationeersMods\Aegis',
    # The Aegis commit to test, when main has moved past the built package: it must be the package.stamp head (the package is what is staged).
    [string] $AegisCommit = '',
    [string] $RigVMRepo = 'G:\Programming\Git\StationeersMods\RigVM',
    [string] $StationGod = 'G:\Programming\Git\StationeersMods\StationGodMCP\package',
    [string] $World = 'G:\StationeersRigs\worlds\two-docks',
    [int] $TimeoutSeconds = 1200
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\rigvm-host.ps1"
$repo = (Resolve-Path "$PSScriptRoot\..\..").Path
$shipping = '^(src/|About/|Assets/|Directory\.Build\.|[^/]*\.csproj$|[^/]*\.sln$|build\.ps1$)'
if (-not (Test-Path "$World\$(Split-Path $World -Leaf).save")) { Write-Host "refused: no save in $World"; exit 3 }
if (-not (Test-Path "$StationGod\About\About.xml")) { Write-Host "refused: no StationGodMCP at $StationGod"; exit 3 }

# The SES package must be what this commit ships: the DLL's +sha, then no shipping path changed since it.
$pkgDll = "$repo\package\SuitEnhancementSuite.dll"
if (-not (Test-Path $pkgDll)) { Write-Host 'refused: no package, run build.ps1'; exit 3 }
$pv = [Diagnostics.FileVersionInfo]::GetVersionInfo($pkgDll).ProductVersion
if ($pv -notmatch '^(\d+\.\d+\.\d+)\+([0-9a-f]+)$') { Write-Host "refused: package DLL version '$pv' has no +sha, run build.ps1"; exit 3 }
$sesVersion = $Matches[1]; $sesSha = $Matches[2]
$moved = @(git -C $repo diff --name-only $sesSha HEAD | ? { $_ -match $shipping })
if ($moved.Count) { Write-Host "refused: the package is from $sesSha and HEAD changed $($moved -join ', '): run build.ps1"; exit 3 }
if (@(git -C $repo status --porcelain --untracked-files=no | ? { $_.Substring(3) -match $shipping }).Count) { Write-Host 'refused: shipping files are modified in the working tree'; exit 3 }
Write-Host "skip-aegis-proof: SES $sesVersion built from $sesSha, HEAD $((git -C $repo rev-parse --short HEAD).Trim())"

$aegisHead = $null
if ($Run -eq 'a') {
    $stamp = Get-Content -Raw "$AegisCheckout\package.stamp" | ConvertFrom-Json
    $aegisHead = if ($AegisCommit) { (git -C $AegisCheckout rev-parse $AegisCommit).Trim() } else { (git -C $AegisCheckout rev-parse main).Trim() }
    if ($AegisCommit -and $aegisHead -ne $stamp.head) { Write-Host "refused: -AegisCommit $aegisHead is not the built package's commit $($stamp.head)"; exit 3 }
    $aegisMoved = @(git -C $AegisCheckout diff --name-only $stamp.head $aegisHead | ? { $_ -match '^(src/|About/|GameData/|Thruster/|Helmet/|Orb/|Assets/|Directory\.Build\.|[^/]*\.csproj$|[^/]*\.sln$|build\.ps1$)' })
    if ($aegisMoved.Count) { Write-Host "refused: Aegis's package is from $($stamp.head), main is $aegisHead and changed $($aegisMoved -join ', '): build Aegis first"; exit 3 }
    if (-not (Test-Path "$AegisCheckout\package\Aegis.dll")) { Write-Host 'refused: no Aegis package'; exit 3 }
    if (-not (Test-Path "$AegisCheckout\package\Body\aegis.aegisbody")) { Write-Host 'refused: the Aegis package ships no body (stage it with a prelaunch hook)'; exit 3 }
    Write-Host "skip-aegis-proof: Aegis main $aegisHead, package from $($stamp.head) (no shipping change since)"
}

# RigVM main, freshly extracted.
New-Item -ItemType Directory -Force $WorkDir | Out-Null
$sha = (git -C $RigVMRepo rev-parse main).Trim()
$rig = Join-Path $WorkDir "rigvm-$($sha.Substring(0, 12))"
if (Test-Path $rig) { Remove-Item -Recurse -Force $rig }
New-Item -ItemType Directory -Force $rig | Out-Null
git -C $RigVMRepo archive --format=zip -o "$rig.zip" $sha RIGVM_COMMIT host guest
Expand-Archive -Path "$rig.zip" -DestinationPath $rig -Force
Remove-Item -Force "$rig.zip"
Write-Host "skip-aegis-proof: RigVM main $sha extracted to $rig"

# The mods, each in a folder named for the mod, in load order.
$stage = Join-Path $WorkDir "mods-$Run-$(Get-Date -Format yyyyMMdd-HHmmss)"
New-Item -ItemType Directory -Force $stage | Out-Null
Copy-Item -Recurse $StationGod "$stage\StationGodMCP"
$mods = @("$stage\StationGodMCP")
if ($Run -eq 'a') { Copy-Item -Recurse "$AegisCheckout\package" "$stage\Aegis"; $mods += "$stage\Aegis" }
Copy-Item -Recurse "$repo\package" "$stage\SuitEnhancementSuite"
$mods += "$stage\SuitEnhancementSuite"

# Inputs for the poll hook. A pipe name for this run only: never "StationGodMCP", a real game's live one.
$env:SES_PROOF_RIGVM = $rig
$env:SES_PROOF_VERDICT = "$PSScriptRoot\skip-aegis-verdict.ps1"
$env:SES_PROOF_PIPE = "StationGodMCP-SESSKIP$($Run.ToUpper())-$(Get-Date -Format HHmmss)"
$env:SES_PROOF_MODE = $Run
$env:SES_PROOF_WORLD = $World
$cfg = @{
    'net.xceled.stationeers.stationgodmcp.cfg' = "[Pipe]`r`n`r`nName = $($env:SES_PROOF_PIPE)`r`n`r`n[Remote MCP]`r`n`r`nEnabled = false`r`n"
    'BepInEx.cfg' = "[Logging.Disk]`r`n`r`nLogLevels = Fatal, Error, Warning, Message, Info, Debug`r`n"
} | ConvertTo-Json -Compress
$ph = [ordered]@{ bepinex = 'Chainloader startup complete'; mods = '\[Global\]: Took \S+ to load mods'; ses = 'Suit Enhancement Suite \d+\.\d+\.\d+ loaded' }
$req = @('bepinex', 'mods', 'ses', 'proof-world', 'proof-done')
if ($Run -eq 'a') { $ph.aegis = 'Aegis: self-check (PASS|FAIL)'; $req = @('bepinex', 'mods', 'ses', 'aegis', 'proof-world', 'proof-done') }
$p = @{
    Mod = "ses-skip-aegis-$Run"; Owner = $Owner; Then = 'auto'; Mods = $mods; Save = $World; GameArgs = '-load {savename}'
    PhasesJson = ($ph | ConvertTo-Json -Compress); DonePhase = 'proof-done'; Require = $req
    NoteRegex = '(?m)^\[(Warning|Error|Fatal)\s*:[^\]\r\n]*\][^\r\n]*(Suit Enhancement Suite|SuitEnhancementSuite|Aegis)[^\r\n]*'
    BepCfgJson = $cfg; OnPoll = "$PSScriptRoot\skip-aegis-poll.ps1"
    TimeoutSeconds = $TimeoutSeconds; StallSeconds = 300; StuckSeconds = 300
    Item = $Item; ExpectedMinutes = $ExpectedMinutes; CheckScripts = @($PSScriptRoot)
}
$code = 3
try { $code = Invoke-RigEntry -Entry "$rig\host\vm-client-run.ps1" -Params $p -WorkDir $WorkDir }
finally { Remove-Item -Recurse -Force $stage -ErrorAction SilentlyContinue }
Write-Host "skip-aegis-proof: run $Run vm-client-run exit $code (SES $sesVersion $sesSha$(if ($aegisHead) { ", Aegis $aegisHead" }))"
exit $code

