# Host test of skip-aegis-verdict.ps1 (P98): the verdict passes real-shaped input and FAILS each missing or wrong capture.
#   pwsh -NoProfile -File tests\vm\skip-aegis-verdict.tests.ps1 [-RecordedReply <a saved StationGod inspect-contents reply of an Aegis suit>]
param([string] $RecordedReply = '')
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\skip-aegis-verdict.ps1"
$script:fails = 0
function Check([bool] $ok, [string] $what) { if ($ok) { Write-Host "ok   $what" } else { Write-Host "FAIL $what"; $script:fails++ } }

function Reply($rows) { [pscustomobject]@{ ok = $true; result = [pscustomobject]@{ slots = @($rows) } } }
function KeyRow($i, $key) { [pscustomobject]@{ index = $i; display_name = 'x'; string_key = $key } }
function LabelRow($i, $label) { [pscustomobject]@{ index = $i; display_name = $label; slot_class = 'None' } }
$aegisNames = 'Air Tank', 'Waste Tank', 'Coolant Tank', 'Battery', 'Chip', 'Mod', 'Filter1', 'Filter2'
$aegisKeyRows = @(0..7 | ForEach-Object { KeyRow $_ "AegisSlot$_" })
$aegisLabelRows = @(0..7 | ForEach-Object { LabelRow $_ $aegisNames[$_] })
$sesKeys = 'SuitEnhancementSuiteWater', 'SuitEnhancementSuiteFood', 'SuitEnhancementSuiteWaste', 'SuitEnhancementSuiteSharedPower1', 'SuitEnhancementSuiteSharedPower2'
$vanKeyRows = @(@(KeyRow 0 'Air') + @(0..4 | ForEach-Object { KeyRow (9 + $_) $sesKeys[$_] }))
$vanLabelRows = @(@(LabelRow 0 'Air Tank') + @(LabelRow 9 'Water'; LabelRow 10 'Food'; LabelRow 11 'Waste'; LabelRow 12 'Shared Power'; LabelRow 13 'Shared Power'))

$good = "[Info   :   BepInEx] Loading [Suit Enhancement Suite 0.7.2]`n[Info   :Suit Enhancement Suite] Suit Enhancement Suite 0.7.2 loaded: auto consume on`n[Info   :Aegis] Aegis 0.1.0 loaded.`n[Warning: Lattice] unrelated warning"
$goodB = "[Info   :Suit Enhancement Suite] Suit Enhancement Suite 0.7.2 loaded: auto consume on`n[Debug  :Suit Enhancement Suite] Auto consume: Recruit used 1 of ItemCerealBar, Nutrition 20 -> 21"

function Facts($aegis, $van, $adv = $true, $secs = 130, $lowered = 8, $after = '', $control = 1) {
    @{ aegisSlots = $aegis; vanillaSlots = $van; vanillaAdvanced = $adv; player = 'Recruit'; observedSeconds = $secs; needsLowered = $lowered; afterText = $after; controlLines = $control }
}
$aegisK = Get-SkipAegisSlots (Reply $aegisKeyRows); $vanK = Get-SkipAegisSlots (Reply $vanKeyRows)
$aegisL = Get-SkipAegisSlots (Reply $aegisLabelRows); $vanL = Get-SkipAegisSlots (Reply $vanLabelRows)
function Judge($mode, $facts, $log) { @(Get-SkipAegisVerdict $mode $facts $log) }
function Mentions($v, [string] $pat) { @($v | Where-Object { $_ -match $pat }).Count -gt 0 }

# Slot reading
Check ($aegisK.method -eq 'key' -and $aegisK.names.Count -eq 8) 'string_key rows read as keys'
Check ($aegisL.method -eq 'label') 'display_name-only rows read as labels'
Check ((Get-SkipAegisSlots $null).method -eq 'none') 'a null reply reads as none'
Check ((Get-SkipAegisSlots ([pscustomobject]@{ ok = $false; result = $null })).method -eq 'none') 'a failed reply reads as none'
Check ((Get-SkipAegisSlots (Reply @())).method -eq 'none') 'a reply with no rows reads as none'

# Run a
Check ((Judge 'a' (Facts $aegisK $vanK) $good).Count -eq 0) 'a: keys, clean log: PASS'
Check ((Judge 'a' (Facts $aegisL $vanL) $good).Count -eq 0) 'a: labels, clean log: PASS'
$v = Judge 'a' (Facts $null $vanK) $good; Check ((Mentions $v 'Aegis suit slots were not read')) 'a: no Aegis slot capture FAILS'
$v = Judge 'a' (Facts $aegisK $null) $good; Check ((Mentions $v 'vanilla suit slots were not read')) 'a: no vanilla slot capture FAILS'
$bad = Get-SkipAegisSlots (Reply @($aegisKeyRows + (KeyRow 20 'SuitEnhancementSuiteWater')))
$v = Judge 'a' (Facts $bad $vanK) $good; Check ((Mentions $v 'Aegis suit has SES slots')) 'a: an SES key on the Aegis suit FAILS'
$badL = Get-SkipAegisSlots (Reply @($aegisLabelRows + (LabelRow 20 'Water')))
$v = Judge 'a' (Facts $badL $vanL) $good; Check ((Mentions $v 'Aegis suit has SES slots')) 'a: an SES label on the Aegis suit FAILS'
$partial = Get-SkipAegisSlots (Reply @(KeyRow 0 'Air'; KeyRow 9 'SuitEnhancementSuiteWater'))
$v = Judge 'a' (Facts $aegisK $partial) $good; Check ((Mentions $v 'lacks SES slots')) 'a: a vanilla suit missing SES slots FAILS'
$v = Judge 'a' (Facts $aegisK $vanK $false) $good; Check ($v.Count -eq 0) 'a: a plain suit needs only three SES slots'
$three = Get-SkipAegisSlots (Reply @(KeyRow 9 'SuitEnhancementSuiteWater'; KeyRow 10 'SuitEnhancementSuiteFood'; KeyRow 11 'SuitEnhancementSuiteWaste'))
$v = Judge 'a' (Facts $aegisK $three $true) $good; Check ((Mentions $v 'SharedPower')) 'a: an advanced suit missing Shared Power FAILS'
$v = Judge 'a' (Facts $aegisK $vanK $true 60) $good; Check ((Mentions $v 'under 120 s')) 'a: a 60 s watch FAILS'
$v = Judge 'a' (Facts $aegisK $vanK $true 130 0) $good; Check ((Mentions $v 'never lowered')) 'a: needs never lowered FAILS'
$cl = "[Debug  :Suit Enhancement Suite] Auto consume: Recruit used 1 of ItemCerealBar, Nutrition 20 -> 21"
$v = Judge 'a' (Facts $aegisK $vanK $true 130 8 $cl) $good; Check ((Mentions $v 'name the Aegis wearer')) 'a: a consume line naming the wearer after the swap FAILS'
$v = Judge 'a' (Facts $aegisK $vanK) ($good + "`n$cl"); Check ($v.Count -eq 0) 'a: the same line before the swap (the EVA suit''s) is not held against Aegis'
$v = Judge 'a' (Facts $aegisK $vanK $true 130 8 $null) $good; Check ((Mentions $v 'after the Aegis suit was worn were not captured')) 'a: no after-swap capture FAILS'
$v = Judge 'a' (Facts $aegisK $vanK $true 130 8 '' 0) $good; Check ((Mentions $v 'no control')) 'a: no positive control FAILS'
$part = Get-SkipAegisSlots ([pscustomobject]@{ ok = $true; result = [pscustomobject]@{ device = [pscustomobject]@{ slot_count = 21 }; slots = @($aegisKeyRows) } })
$v = Judge 'a' (Facts $part $vanK) $good; Check ((Mentions $v 'of 21 Aegis suit slots')) 'a: 8 of 21 slots read FAILS (partial list)'
$v = Judge 'a' (Facts $aegisK $vanK $true 130 8 "[Info   :Suit Enhancement Suite] Auto swap: Recruit's Water slot was empty, now holds ItemWaterPacket from ItemToolBelt.") $good; Check ((Mentions $v 'name the Aegis wearer')) 'a: a swap line naming the wearer FAILS'
$v = Judge 'a' (Facts $aegisK $vanK) ($good + "`n[Debug  :Suit Enhancement Suite] Auto consume: Someone Else used 1 of X"); Check ($v.Count -eq 0) 'a: a consume line for another player is not the wearer''s'
$v = Judge 'a' (Facts $aegisK $vanK) ($good + "`n[Error  :Suit Enhancement Suite] boom"); Check ((Mentions $v 'SES error')) 'a: an SES error line FAILS'
$v = Judge 'a' (Facts $aegisK $vanK) ($good + "`n[Warning:Suit Enhancement Suite] Auto consume skipped a player or pass"); Check ((Mentions $v 'SES error')) 'a: an SES warning FAILS'
$v = Judge 'a' (Facts $aegisK $vanK) ($good + "`nSystem.NullReferenceException at SuitEnhancementSuite.SuitSlotLayout.AddSlots"); Check ((Mentions $v 'SES error')) 'a: an exception in the SES namespace FAILS'
$v = Judge 'a' (Facts $aegisK $vanK) "[Info   :Suit Enhancement Suite] Suit Enhancement Suite 0.7.2 loaded"; Check ((Mentions $v 'Aegis x.y.z loaded')) 'a: no Aegis loaded line FAILS'
$v = Judge 'a' (Facts $aegisK $vanK) "[Info   :Aegis] Aegis 0.1.0 loaded."; Check ((Mentions $v 'Suit Enhancement Suite x.y.z loaded')) 'a: no SES loaded line FAILS'
$v = Judge 'a' (Facts $aegisK $vanK) ''; Check ((Mentions $v 'log is empty')) 'a: an empty log FAILS'
$v = Judge 'a' $null $good; Check ((Mentions $v 'no facts')) 'a: no facts FAILS'

# Run b
Check ((Judge 'b' (Facts $null $vanK) $goodB).Count -eq 0) 'b: keys, a consume line: PASS'
$v = Judge 'b' (Facts $null $vanK) ($goodB -replace 'Auto consume', 'Auto other'); Check ((Mentions $v 'no SES auto-consume line')) 'b: no consume line FAILS'
$v = Judge 'b' (Facts $null $vanK) ($goodB -replace 'Recruit', 'Nobody'); Check ((Mentions $v 'no SES auto-consume line')) 'b: a consume line for another player FAILS'
$v = Judge 'b' (Facts $null $null) $goodB; Check ((Mentions $v 'vanilla suit slots were not read')) 'b: no suit slot capture FAILS'
$v = Judge 'b' (Facts $null $vanK $true 130 0) $goodB; Check ((Mentions $v 'never lowered')) 'b: needs never lowered FAILS'
$v = Judge 'b' (Facts $null $vanK) ($goodB + "`n[Info   :Aegis] Aegis 0.1.0 loaded."); Check ((Mentions $v 'SES alone')) 'b: Aegis loaded in the SES-alone run FAILS'
$v = Judge 'b' (Facts $null $vanK) ($goodB + "`n[Error  :Suit Enhancement Suite] boom"); Check ((Mentions $v 'SES error')) 'b: an SES error FAILS'
$v = Judge 'c' (Facts $null $vanK) $goodB; Check ((Mentions $v 'unknown mode')) 'an unknown mode FAILS'

# A recorded reply from a real run (an Aegis suit's inspect contents): read, no SES labels, so it is a clean Aegis capture.
if (-not $RecordedReply) { $RecordedReply = 'G:\StationeersRigs\VM\runs\aegis-dock-aegis\20261111-103302-pool8d22\dq-suit-contents.json' }
if (Test-Path $RecordedReply) {
    $raw = Get-Content -Raw $RecordedReply
    if ($raw.Trim()) {
        $rec = Get-SkipAegisSlots ($raw | ConvertFrom-Json)
        Check ($rec.method -eq 'label' -and $rec.names.Count -ge 5) "the recorded Aegis suit reply reads ($($rec.names.Count) slots, by $($rec.method))"
        Check ((Get-SkipAegisSesSlots $rec).Count -eq 0) 'the recorded Aegis suit has no SES slot'
        $v = Judge 'a' (Facts $rec $vanL) $good
        Check ($v.Count -eq 0) "a: the recorded Aegis suit with a vanilla capture: PASS ($($v -join '; '))"
    }
    else { Check $false "the recorded reply $RecordedReply is empty" }
}
else { Write-Host "info: no recorded reply at $RecordedReply; the recorded-reply checks are skipped" }

if ($script:fails) { Write-Host "$script:fails check(s) FAILED"; exit 1 }
Write-Host 'all checks passed'


