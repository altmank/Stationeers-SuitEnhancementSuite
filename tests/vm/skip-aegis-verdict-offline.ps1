# Judges a saved skip-aegis run again, offline, with the same verdict the run's last step uses (skip-aegis-verdict.ps1).
#   pwsh -NoProfile -File tests\vm\skip-aegis-verdict-offline.ps1 -RunDir <a run folder or a Copy-RunForRead.ps1 copy of it> [-Mode a|b]
#   pwsh -NoProfile -File tests\vm\skip-aegis-verdict-offline.ps1 -LogFile <BepInEx log> -FactsFile <skip-facts.json> -Mode a
# The run folder holds skip-facts.json (mode, facts, the filtered log lines the poll judged) and LogOutput.log. The full LogOutput.log is judged
# unless -FactsLog is passed (then the log the poll saved). Exit 0 on a pass, 1 on any missing check, 3 when an input is missing.
param(
    [string] $RunDir = '',
    [string] $LogFile = '',
    [string] $FactsFile = '',
    [string] $Mode = '',
    [switch] $FactsLog
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\skip-aegis-verdict.ps1"
if ($RunDir) { if (-not $FactsFile) { $FactsFile = Join-Path $RunDir 'skip-facts.json' }; if (-not $LogFile) { $LogFile = Join-Path $RunDir 'LogOutput.log' } }
foreach ($p in $FactsFile, $LogFile) { if (-not $p -or -not (Test-Path $p)) { Write-Host "no input: '$p'"; exit 3 } }
$saved = Get-Content -Raw $FactsFile | ConvertFrom-Json
if (-not $Mode) { $Mode = "$($saved.mode)" }
$facts = @{ player = "$($saved.facts.player)"; observedSeconds = $saved.facts.observedSeconds; needsLowered = $saved.facts.needsLowered; vanillaAdvanced = [bool]$saved.facts.vanillaAdvanced; controlLines = $saved.facts.controlLines; afterText = $saved.facts.afterText }
foreach ($k in 'aegisSlots', 'vanillaSlots') {
    $s = $saved.facts.$k
    $facts[$k] = if ($s -and $s.method) { @{ method = "$($s.method)"; names = @($s.names | ForEach-Object { "$_" }); expected = [int]$s.expected } } else { $null }
}
$logText = if ($FactsLog) { "$($saved.log)" } else { Get-Content -Raw $LogFile }
$missing = @(Get-SkipAegisVerdict $Mode $facts $logText)
if ($missing.Count -eq 0) { Write-Host "PASS (mode $Mode, player '$($facts.player)')"; exit 0 }
Write-Host "FAIL (mode $Mode): $($missing.Count) missing"
$missing | ForEach-Object { Write-Host "  - $_" }
exit 1

