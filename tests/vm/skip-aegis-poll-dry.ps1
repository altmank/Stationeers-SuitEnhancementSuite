# Host dry run of skip-aegis-poll.ps1 (P98): the real poll hook runs every step against the recorded replies and log of a finished run (a
# Copy-RunForRead.ps1 copy), with a fake clock, so each code path executes, not just parses. Fakes: Invoke-AegisSg (answers from the run's
# sp-*.json by call; defined in a copy of sgpipe.ps1 that the poll dot-sources), Invoke-Rig (runs the guest script block locally), Get-Date
# (a clock the harness advances 5 s a tick), Rig-Log, Set-RigPhase, Stop-Rig. The real sgpipe.ps1 helpers (Find-AegisSlot, Get-AegisRow) are used.
#   pwsh -NoProfile -File tests\vm\skip-aegis-poll-dry.ps1 -RunCopy <copy of a run folder> -Mode a|b [-Scenario ok|inject|withhold]
# Scenarios (mode a): ok = the run's own data, expects PASS; inject = a consume line naming the wearer appears after the Aegis suit goes on,
# expects FAIL naming the wearer; withhold = the Aegis suit's slot replies are missing, expects FAIL "were not read". Exit 0 when the
# scenario came out as expected, 1 when not. Needs a run whose sp-slots-*.json hold ok key replies (written by the current poll).
param(
    [Parameter(Mandatory)] [string] $RunCopy,
    [Parameter(Mandatory)] [ValidateSet('a', 'b')] [string] $Mode,
    [ValidateSet('ok', 'inject', 'withhold')] [string] $Scenario = 'ok',
    [string] $RigVM = 'G:\Programming\Git\StationeersMods\RigVM',
    [string] $World = 'G:\StationeersRigs\worlds\two-docks',
    # >0: a whole-suit inspect slots answers only this many rows, as the game's reply budget did for the 21-slot Aegis suit (runs the per-slot read)
    [int] $CutFull = 0
)
$ErrorActionPreference = 'Stop'
$work = Join-Path ([IO.Path]::GetTempPath()) "skipdry-$([guid]::NewGuid().ToString('N').Substring(0, 8))"
New-Item -ItemType Directory -Force "$work\host\aegis", "$work\res" | Out-Null
$log = Join-Path $work 'LogOutput.log'
Copy-Item "$RunCopy\LogOutput.log" $log
$real = git -C $RigVM show 'HEAD:host/aegis/sgpipe.ps1'
if ($LASTEXITCODE -ne 0) { throw "no sgpipe.ps1 at $RigVM" }

# The run's replies, indexed by kind (slots / contents) and reference id.
$replies = @{}; $byRef = @{}
foreach ($f in Get-ChildItem $RunCopy -Filter 'sp-*.json') {
    $txt = Get-Content -Raw $f.FullName
    if (-not $txt -or -not $txt.Trim()) { continue }
    try { $o = $txt | ConvertFrom-Json } catch { continue }
    $replies[$f.BaseName] = $o
    if ($o.ok -and $o.result -and $o.result.PSObject.Properties['reference_id'] -and $o.result.PSObject.Properties['slots'] -and $f.BaseName -notlike 'sp-player-*') {
        $kind = if ($f.BaseName -like 'sp-slots-*') { 'slots' } else { 'contents' }
        # the full read of a suit (not a one-slot read: those have the highest slot count in the device and one row)
        $key = "$kind|$($o.result.reference_id)"
        if (-not $byRef.ContainsKey($key) -or @($o.result.slots).Count -gt @($byRef[$key].result.slots).Count) { $byRef[$key] = $o }
    }
}
$withholdRef = ''
if ($Scenario -eq 'withhold') { $withholdRef = "$(@($replies.Values | ? { $_.result -and $_.result.PSObject.Properties['prefab_name'] -and $_.result.prefab_name -eq 'ItemSuitAegis' -and $_.result.PSObject.Properties['reference_id'] })[0].result.reference_id)" }
$script:playerCalls = 0
$script:fakeNow = [datetime]'2026-11-11T15:00:00'
$script:phase = ''; $script:stopped = ''

function Get-Date { param([string] $Format) if ($Format) { return $script:fakeNow.ToString($Format) }; $script:fakeNow }
function Rig-Log([string] $m) { Write-Host "  log: $m" }
function Set-RigPhase([string] $p) { $script:phase = $p; Write-Host "  phase: $p" }
function Stop-Rig([string] $kind, [string] $why) { $script:stopped = "$kind $why"; throw 'STOP-RIG' }
function Invoke-Rig { param([object[]] $ArgumentList = @(), [int] $TimeoutSeconds = 0, [Parameter(Position = 0)] [scriptblock] $Block) & $Block @ArgumentList }
function New-Reply($obj) {
    $raw = if ($obj) { $obj | ConvertTo-Json -Depth 30 -Compress } else { '' }
    [pscustomobject]@{ ok = $true; error = ''; welcome = $null; reply = $obj; raw = $raw }
}
function Fail-Reply([string] $why) { [pscustomobject]@{ ok = $false; error = $why; welcome = $null; reply = $null; raw = '' } }

# The fake Invoke-AegisSg goes after the real sgpipe.ps1 in the copy the poll dot-sources, so it wins every tick.
$fakeSg = @'
function Invoke-AegisSg([string] $Pipe, [string] $Method = '', [hashtable] $Params = @{}, [int] $TimeoutMs = 20000) {
    if (-not $Method) { $w = [pscustomobject]@{ server = [pscustomobject]@{ world = [pscustomobject]@{ id = 'dry-world' } } }; return [pscustomobject]@{ ok = $true; error = ''; welcome = $w; reply = $null; raw = '' } }
    switch ($Method) {
        'inspect' {
            $id = "$($Params.id)"
            if ($id -eq 'player') {
                $script:playerCalls++
                $name = @('sp-player-before', 'sp-player-start', 'sp-player-suit-on')[[Math]::Min($script:playerCalls, 3) - 1]
                if (-not $replies.ContainsKey($name)) { return Fail-Reply "dry: no recorded $name" }
                return New-Reply $replies[$name]
            }
            if ($id -eq $withholdRef) { return Fail-Reply 'dry: withheld' }
            $o = $byRef["$($Params.what)|$id"]
            if ($null -eq $o) { return Fail-Reply "dry: no recorded $($Params.what) reply for $id" }
            if ($Params.what -eq 'slots' -and $null -ne $Params.slot) {
                $row = @($o.result.slots | ? { "$($_.index)" -eq "$($Params.slot)" })[0]
                if (-not $row) { return Fail-Reply "dry: no slot $($Params.slot)" }
                return New-Reply ([pscustomobject]@{ type = 'reply'; ok = $true; result = [pscustomobject]@{ reference_id = $o.result.reference_id; device = $o.result.device; slots = @($row) } })
            }
            if ($Params.what -eq 'slots' -and $CutFull -gt 0) { return New-Reply ([pscustomobject]@{ type = 'reply'; ok = $true; result = [pscustomobject]@{ reference_id = $o.result.reference_id; device = $o.result.device; slots = @(@($o.result.slots)[0..($CutFull - 1)]) } }) }
            return New-Reply $o
        }
        'build' {
            if ($Params.prefab -eq 'ItemSuitAegis') { return New-Reply $replies['sp-build-aegis'] }
            if ($Params.prefab -eq 'ItemHardSuit' -and $Params.at) { return New-Reply $replies['sp-build-vanilla'] }
            if ($Params.prefab -eq 'ItemHardSuit') { return New-Reply $replies['sp-build-suit'] }
            return New-Reply ([pscustomobject]@{ type = 'reply'; ok = $true; result = [pscustomobject]@{ placed = @('999') } })
        }
        'move' { return New-Reply ([pscustomobject]@{ type = 'reply'; ok = $true; result = [pscustomobject]@{ ok = $true } }) }
        'write' { return New-Reply ([pscustomobject]@{ type = 'reply'; ok = $true; result = [pscustomobject]@{} }) }
        'server' {
            $c = "$($Params.command)"
            $text = if ($c -like 'player setnutrition*') { 'Setting nutrition of Recruit to 8.' } elseif ($c -like 'player sethydration*') { 'Setting hydration of Recruit to 8.' } elseif ($c -eq 'player list') { 'recruit' } else { 'ok' }
            return New-Reply ([pscustomobject]@{ type = 'reply'; ok = $true; result = [pscustomobject]@{ output = @([pscustomobject]@{ text = $text }) } })
        }
    }
    Fail-Reply "dry: unhandled method $Method"
}
'@
Set-Content -Encoding utf8 "$work\host\aegis\sgpipe.ps1" ($real + "`n" + $fakeSg)

# ---- the poll, tick by tick
$env:SES_PROOF_RIGVM = $work; $env:SES_PROOF_PIPE = 'dry'; $env:SES_PROOF_MODE = $Mode; $env:SES_PROOF_WORLD = $World
$env:SES_PROOF_VERDICT = "$PSScriptRoot\skip-aegis-verdict.ps1"
$bepLog = $log
$r = [pscustomobject]@{ Info = [ordered]@{ savePlayer = [pscustomobject]@{ ref = '138' } }; Res = "$work\res"; RunId = 'dry' }
$poll = "$PSScriptRoot\skip-aegis-poll.ps1"
$injected = $false
for ($tick = 1; $tick -le 600 -and -not $script:stopped -and $script:phase -ne 'proof-done'; $tick++) {
    $script:fakeNow = $script:fakeNow.AddSeconds(5)
    try { . $poll } catch { if ("$_" -ne 'STOP-RIG') { $script:stopped = "script error: $_ at line $($_.InvocationInfo.ScriptLineNumber)" } }
    if ($Scenario -eq 'inject' -and -not $injected -and $r.Info.Contains('sp') -and $null -ne $r.Info.sp.logMark) {
        Add-Content $log "[Info   :Suit Enhancement Suite] Auto swap: Recruit's Water slot was empty, now holds ItemWaterPacket from ItemToolBelt."
        $injected = $true
    }
}
Write-Host "dry: mode $Mode scenario $Scenario ended after $tick ticks: phase '$script:phase', stopped '$script:stopped'"
$ok = switch ($Scenario) {
    'ok' { $script:phase -eq 'proof-done' -and -not $script:stopped }
    'inject' { $Mode -eq 'a' -and $script:stopped -match 'name the Aegis wearer' }
    'withhold' { $Mode -eq 'a' -and $script:stopped -match 'Aegis suit slots were not read' }
}
Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
if ($ok) { Write-Host 'dry: AS EXPECTED'; exit 0 }
Write-Host 'dry: NOT AS EXPECTED'; exit 1



