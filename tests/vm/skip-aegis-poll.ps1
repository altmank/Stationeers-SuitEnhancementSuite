# vm-client-run -OnPoll hook for tests\vm\skip-aegis-proof.ps1. Dot-sourced in every poll inside rig-lib's Watch-RigGame ($r, Invoke-Rig,
# Rig-Log, Set-RigPhase, Stop-Rig in scope), Windows PowerShell 5.1. Own variables sp* only; state in $r.Info.sp. Mode in $env:SES_PROOF_MODE:
#   a  SES + Aegis: the player WEARS an Aegis suit, a vanilla Hardsuit lies beside, need items in the inventory, needs held low for 135 s
#   b  SES alone:   the player wears a Hardsuit with water and food in its SES slots, needs lowered; ends at the first auto-consume line
#   wait      pipe, world, the save's own player (P51), the player's name
#   selfcheck SES (and, in a, Aegis) said it loaded
#   equip     old suit stowed in the world's locker; the suits and items built (StationGod build item)
#   needs     every 15 s: console `player setnutrition/sethydration <name> 8`, a heal each pass (the vacuum would otherwise kill the player)
#   capture   the slots of the suits (inspect slots: string_key; else inspect contents: display_name), the log's SES/Aegis lines
#   verdict   tests\vm\skip-aegis-verdict.ps1 over the facts; any missing check is a FAIL
. "$env:SES_PROOF_RIGVM\host\aegis\sgpipe.ps1"
. "$env:SES_PROOF_VERDICT"
$spPipe = $env:SES_PROOF_PIPE
$spMode = $env:SES_PROOF_MODE
if (-not $r.Info.Contains('sp')) { $r.Info.sp = [ordered]@{ i = 0; until = (Get-Date).AddSeconds(-1).ToString('o'); tries = 0; t0 = $null; lastTick = $null; lowered = 0; heals = 0 } }
$sp = $r.Info.sp
$spSteps = @('wait', 'selfcheck', 'control', 'equip', 'needs', 'capture', 'verdict', 'idle')
if ((Get-Date) -lt [datetime]$sp.until) { return }

if (-not $bepLog) { Stop-Rig infra 'skip-aegis: the run gave no BepInEx log path' }
$spLog = $bepLog
$spFail = { param($why) Stop-Rig fail "skip-aegis ($spMode): $why" }
$spWait = { param($s) $sp.until = (Get-Date).AddSeconds($s).ToString('o') }
$spSave = { param($name, $obj) try { $obj.raw | Set-Content -Encoding utf8 (Join-Path $r.Res "sp-$name.json") } catch {} }
$spLines = { param($pattern) @(Invoke-Rig -ArgumentList $spLog, $pattern { param($f, $p) if (Test-Path $f) { Get-Content -LiteralPath $f | ? { $_ -match $p } } }) }
$spCall = { param($name, $method, $params) $spX = Invoke-AegisSg -Pipe $spPipe -Method $method -Params $params -TimeoutMs 30000; & $spSave $name $spX; if (-not $spX.ok) { & $spFail "${name}: $($spX.error)" }; $spX }
$spTry = { param($name, $method, $params) $spX = Invoke-AegisSg -Pipe $spPipe -Method $method -Params $params -TimeoutMs 30000; & $spSave $name $spX; if (-not $spX.ok) { Rig-Log "skip-aegis: $name failed: $($spX.error)" }; $spX }
$spCmd = { param($name, $line)
    $spX = & $spTry "cmd-$name" server @{ what = 'command'; command = $line; confirm = $true }
    $spOut = if ($spX.ok) { (@($spX.reply.result.output | % { "$($_.text)" }) -join ' | ') } else { "error: $($spX.error)" }
    Rig-Log "skip-aegis: $line -> $spOut"
    [pscustomobject]@{ ok = $spX.ok; out = $spOut } }
# A suit's slots: `inspect slots` (string_key) first, `inspect contents` (display_name) when that refuses; the raw replies are kept.
$spSlots = { param($name, $id)
    $spA = & $spTry "slots-$name" inspect @{ what = 'slots'; id = "$id"; list_limits = @{ slots = 64 } }
    $spS = if ($spA.ok) { Get-SkipAegisSlots $spA.reply } else { @{ method = 'none'; names = @() } }
    # A key list shorter than the device's slot_count is partial: the complete label list from inspect contents is used instead.
    $spRowsOverride = $null
    $spPartial = ($spS.method -eq 'key' -and $spS.expected -gt 0 -and @($spS.names).Count -lt $spS.expected)
    if ($spPartial) {
        # The full read is cut by the reply budget (21 slots of logic values): read each slot alone, 'inspect slots slot=<i>'.
        $spKeys = @(); $spAll = $true; $spRowsAll = @()
        for ($spI = 0; $spI -lt $spS.expected; $spI++) {
            $spOne = & $spTry "slots-$name-$spI" inspect @{ what = 'slots'; id = "$id"; slot = $spI }
            $spRow = if ($spOne.ok) { @($spOne.reply.result.slots)[0] } else { $null }
            if ($spRow -and $spRow.PSObject.Properties['string_key'] -and "$($spRow.string_key)" -ne '') { $spKeys += "$($spRow.string_key)"; $spRowsAll += $spRow } else { $spAll = $false }
        }
        if ($spAll -and $spKeys.Count -eq $spS.expected) { $spS = @{ method = 'key'; names = $spKeys; expected = $spS.expected }; $spPartial = $false; $spRowsOverride = $spRowsAll }
    }
    if ($spPartial) { Rig-Log "skip-aegis: slots of ${name}: inspect slots gave $(@($spS.names).Count) of $($spS.expected) keys, reading by contents" }
    if ($spS.method -ne 'key' -or $spPartial) {
        $spB = & $spTry "contents-$name" inspect @{ what = 'contents'; id = "$id"; depth = 1; include = @('empty') }
        $spS2 = if ($spB.ok) { Get-SkipAegisSlots $spB.reply } else { @{ method = 'none'; names = @() } }
        if ($spS2.method -ne 'none') { $spS = $spS2; $spA = $spB }
    }
    $spRows = @(); if ($spA.ok -and $spA.reply.result.slots) { $spRows = @($spA.reply.result.slots) }; if ($spRowsOverride) { $spRows = @($spRowsOverride) }
    Rig-Log "skip-aegis: slots of $name ($id) read by $($spS.method): $($spS.names -join ', ')"
    [pscustomobject]@{ slots = $spS; rows = $spRows } }
# The slot index of the row whose string_key is $key or whose display_name is $label.
$spIndex = { param($rows, $key, $label)
    foreach ($spR in $rows) { if (($spR.PSObject.Properties['string_key'] -and "$($spR.string_key)" -eq $key) -or "$($spR.display_name)" -eq $label) { return [int]$spR.index } }
    $null }
$spPlaced = { param($x) $spP = @($x.reply.result.placed); if ($spP.Count -gt 0 -and "$($spP[0])") { "$($spP[0])" } else { '' } }

$spStep = $spSteps[$sp.i]
$spNext = $true
switch ($spStep) {
    'wait' {
        $sp.tries++
        $spW = Invoke-AegisSg -Pipe $spPipe
        if (-not $spW.ok -or -not $spW.welcome) { if ($sp.tries % 6 -eq 1) { Rig-Log "skip-aegis: pipe $spPipe not up yet ($($spW.error))" }; $spNext = $false; break }
        if (-not "$($spW.welcome.server.world.id)") { if ($sp.tries % 6 -eq 1) { Rig-Log 'skip-aegis: pipe up, no world yet' }; $spNext = $false; break }
        $spP = Invoke-AegisSg -Pipe $spPipe -Method inspect -Params @{ what = 'contents'; id = 'player'; depth = 1; include = @('empty') }
        if (-not $spP.ok) { if ($sp.tries % 6 -eq 1) { Rig-Log "skip-aegis: player not readable yet ($($spP.error))" }; $spNext = $false; break }
        & $spSave 'player-before' $spP
        $sp.playerRef = "$($spP.reply.result.reference_id)"; $spWant = "$($r.Info.savePlayer.ref)"
        if ($spWant -and $sp.playerRef -ne $spWant) { Stop-Rig infra "player: character $($sp.playerRef) is not the save's ${spWant} (P51)" }
        $sp.player = "$($spP.reply.result.display_name)"
        if (-not $sp.player) { & $spFail 'the player has no display name' }
        $spPos = $spP.reply.result.position
        $sp.pos = if ($spPos -is [array]) { @($spPos | % { [double]$_ }) } else { @([double]$spPos.x, [double]$spPos.y, [double]$spPos.z) }
        $spWorldTxt = Join-Path "$env:SES_PROOF_WORLD" 'world.txt'
        $spLk = if (Test-Path $spWorldTxt) { [regex]::Match((Get-Content -Raw $spWorldTxt), '(?m)^locker (\d+)').Groups[1].Value } else { '' }
        if (-not $spLk) { & $spFail "world.txt names no locker to stow the start suit in ($spWorldTxt)" }
        $sp.locker = $spLk
        Rig-Log "skip-aegis: world $($spW.welcome.server.world.id), character $($sp.playerRef) '$($sp.player)' at $($sp.pos -join ' '), locker $($sp.locker), mode $spMode"
        Set-RigPhase 'proof-world'
    }
    'selfcheck' {
        $spL = & $spLines 'Suit Enhancement Suite \d+\.\d+\.\d+ loaded|Aegis \d+\.\d+\.\d+ loaded\.'
        $spSes = @($spL | ? { $_ -match 'Suit Enhancement Suite \d+\.\d+\.\d+ loaded' }).Count
        $spAeg = @($spL | ? { $_ -match 'Aegis \d+\.\d+\.\d+ loaded\.' }).Count
        if (-not $spSes -or ($spMode -eq 'a' -and -not $spAeg)) { $spNext = $false; if ($sp.tries++ -gt 60) { & $spFail "no loaded line (SES $spSes, Aegis $spAeg; mode $spMode)" }; & $spWait 5; break }
        Rig-Log "skip-aegis: $($spL -join ' | ')"
    }
    'control' {
        # Positive control (run a): while the player still wears the start suit, SES must serve them (an auto-consume line), or a quiet log
        # after the Aegis suit goes on would prove nothing. Needs lowered; the suit's own Water and Food slots refill from the inventory.
        if ('a' -ne $spMode) { break }
        if (-not $sp.t0c) { $sp.t0c = (Get-Date).ToString('o') }
        $spN = & $spCmd 'ctl-nutrition' "player setnutrition $($sp.player) 8"
        $spH = & $spCmd 'ctl-hydration' "player sethydration $($sp.player) 8"
        $spC = & $spLines "Auto consume: $([regex]::Escape($sp.player)) "
        if (@($spC).Count -lt 1) {
            if (((Get-Date) - [datetime]$sp.t0c).TotalSeconds -gt 60) { & $spFail 'control: SES never served the player in the start suit (no Auto consume line in 60 s)' }
            $spNext = $false; & $spWait 5; break
        }
        $sp.controlLines = @($spC).Count
        Rig-Log "skip-aegis: control: SES served the player in the start suit ($($sp.controlLines) consume line(s)): $(@($spC)[0])"
    }
    'equip' {
        $spP = & $spCall 'player-start' inspect @{ what = 'contents'; id = 'player'; depth = 1; include = @('empty') }
        $spSuitSlot = [int](Find-AegisSlot $spP.reply '^Suit\b')
        $spOcc = (Get-AegisRow $spP.reply $spSuitSlot).occupant
        if ($spOcc) { $null = & $spCall "stow-$($spOcc.prefab_name)" move @{ what = 'item'; id = "$($spOcc.reference_id)"; to = $sp.locker; slot = 'auto'; allow = @('in_use'); confirm = $true } }
        if ($spMode -eq 'a') {
            $spX = & $spCall 'build-aegis' build @{ what = 'item'; prefab = 'ItemSuitAegis'; to = 'player'; slot = $spSuitSlot; confirm = $true }
            $sp.aegisId = & $spPlaced $spX
            $spP = & $spCall 'player-suit-on' inspect @{ what = 'contents'; id = 'player'; depth = 1; include = @('empty') }
            $spRow = Get-AegisRow $spP.reply $spSuitSlot
            if ($spRow.occupant.prefab_name -ne 'ItemSuitAegis') { & $spFail 'the suit slot does not hold ItemSuitAegis' }
            if (-not $sp.aegisId) { $sp.aegisId = "$($spRow.occupant.reference_id)" }
            $sp.logMark = [int](Invoke-Rig -ArgumentList $spLog { param($f) if (Test-Path $f) { @(Get-Content -LiteralPath $f).Count } else { 0 } })
            Rig-Log "skip-aegis: log mark $($sp.logMark): lines after it are judged for the Aegis wearer"            # The vanilla suit lies on the ground beside the player: a Hardsuit, an advanced suit with all five SES keys.
            $spAt = @(($sp.pos[0] + 2), ($sp.pos[1] + 0.5), $sp.pos[2])
            $spX = & $spCall 'build-vanilla' build @{ what = 'item'; prefab = 'ItemHardSuit'; at = $spAt; confirm = $true }
            $sp.vanId = & $spPlaced $spX
            if (-not $sp.vanId) { & $spFail "the Hardsuit was built but the reply names no id (see sp-build-vanilla.json)" }
            # Need items in the inventory: SES would refill and use them if it served this player.
            $null = & $spTry 'items-water' build @{ what = 'item'; prefab = 'ItemWaterPacket'; to = 'player'; slot = 'auto'; confirm = $true }
            $null = & $spTry 'items-food' build @{ what = 'item'; prefab = 'ItemCerealBar'; to = 'player'; slot = 'auto'; confirm = $true }
            Rig-Log "skip-aegis: Aegis suit $($sp.aegisId) worn, Hardsuit $($sp.vanId) on the ground"
        }
        else {
            $spX = & $spCall 'build-suit' build @{ what = 'item'; prefab = 'ItemHardSuit'; to = 'player'; slot = $spSuitSlot; confirm = $true }
            $sp.vanId = & $spPlaced $spX
            $spP = & $spCall 'player-suit-on' inspect @{ what = 'contents'; id = 'player'; depth = 1; include = @('empty') }
            $spRow = Get-AegisRow $spP.reply $spSuitSlot
            if ($spRow.occupant.prefab_name -ne 'ItemHardSuit') { & $spFail 'the suit slot does not hold ItemHardSuit' }
            if (-not $sp.vanId) { $sp.vanId = "$($spRow.occupant.reference_id)" }
            $spR = & $spSlots 'worn-early' $sp.vanId
            $spW = & $spIndex $spR.rows 'SuitEnhancementSuiteWater' 'Water'
            $spF = & $spIndex $spR.rows 'SuitEnhancementSuiteFood' 'Food'
            if ($null -eq $spW -or $null -eq $spF) { & $spFail "the worn Hardsuit has no Water or Food slot (index water '$spW' food '$spF'; slots: $($spR.slots.names -join ', '))" }
            $null = & $spCall 'fill-water' build @{ what = 'item'; prefab = 'ItemWaterPacket'; to = "$($sp.vanId)"; slot = $spW; confirm = $true }
            $null = & $spCall 'fill-food' build @{ what = 'item'; prefab = 'ItemCerealBar'; to = "$($sp.vanId)"; slot = $spF; confirm = $true }
            Rig-Log "skip-aegis: Hardsuit $($sp.vanId) worn, water in slot $spW, food in slot $spF"
        }
        & $spWait 3
    }
    'needs' {
        if (-not $sp.t0) { $sp.t0 = (Get-Date).ToString('o') }
        $spSince = ((Get-Date) - [datetime]$sp.t0).TotalSeconds
        $spTick = if ($sp.lastTick) { ((Get-Date) - [datetime]$sp.lastTick).TotalSeconds } else { 999 }
        if ($spTick -ge 15) {
            $sp.lastTick = (Get-Date).ToString('o')
            $spN = & $spCmd 'nutrition' "player setnutrition $($sp.player) 8"
            $spH = & $spCmd 'hydration' "player sethydration $($sp.player) 8"
            if ($spN.out -match 'Setting nutrition' -and $spH.out -match 'Setting hydration') { $sp.lowered++ }
            $null = & $spTry 'heal' write @{ what = 'health'; player = $sp.player; confirm = $true }
        }
        else {
            # Between the console ticks: a heal every pass, so the vacuum never leaves the wearer dead (a dead player is skipped by SES anyway).
            $null = & $spTry 'heal' write @{ what = 'health'; player = $sp.player; confirm = $true }
        }
        $sp.heals++
        $spC = & $spLines 'Auto consume:|Auto swap:'
        if ($spMode -eq 'b') {
            if (@($spC | ? { $_ -match 'Auto consume' }).Count -ge 1 -and $sp.lowered -ge 1) { Rig-Log "skip-aegis: auto consume seen after $([int]$spSince) s: $(@($spC | ? { $_ -match 'Auto consume' })[0])"; $sp.observed = [int]$spSince }
            elseif ($spSince -gt 150) { & $spFail "no auto-consume line within 150 s of lowering the needs ($($sp.lowered) set rounds)" }
            else { $spNext = $false; & $spWait 4; break }
        }
        else {
            if ($spSince -lt 135) { $spNext = $false; & $spWait 4; break }
            $sp.observed = [int]$spSince
            Rig-Log "skip-aegis: watched $($sp.observed) s, $($sp.lowered) rounds of lowered needs, $(@($spC).Count) SES consume/swap line(s) in the log"
        }
    }
    'capture' {
        if ($spMode -eq 'a') {
            # A suit lying loose is not a device; held, it is: the Hardsuit goes into the player's left hand so inspect slots gives its keys.
            $spP = & $spCall 'player-for-hold' inspect @{ what = 'contents'; id = 'player'; depth = 1; include = @('empty') }
            $spHand = Find-AegisSlot $spP.reply 'Left Hand'
            if ($null -eq $spHand) { & $spFail 'no Left Hand slot to hold the Hardsuit in' }
            $null = & $spCall 'hold-vanilla' move @{ what = 'item'; id = "$($sp.vanId)"; to = 'player'; slot = [int]$spHand; confirm = $true }
            & $spWait 2
        }
        $sp.van = & $spSlots 'vanilla' $sp.vanId
        $sp.aeg = $null
        if ($spMode -eq 'a') { $sp.aeg = & $spSlots 'aegis' $sp.aegisId }
        $spA = & $spCmd 'list' 'player list'
        Rig-Log "skip-aegis: player list: $($spA.out)"
    }
    'verdict' {
        $spPat = 'Suit Enhancement Suite|SuitEnhancementSuite|Aegis \d|Auto consume|Auto swap|Shared power|^\[(Error|Fatal|Warning)'
        $spText = (@(& $spLines $spPat) -join "
")
        $spFacts = [ordered]@{
            aegisSlots = if ($sp.aeg) { $sp.aeg.slots } else { $null }
            vanillaSlots = $sp.van.slots; vanillaAdvanced = $true
            player = $sp.player; observedSeconds = [int]$sp.observed; needsLowered = [int]$sp.lowered; controlLines = [int]$sp.controlLines
            afterText = $(if ($spMode -eq 'a') { (@(Invoke-Rig -ArgumentList $spLog, $sp.logMark { param($f, $m) if (Test-Path $f) { Get-Content -LiteralPath $f | Select-Object -Skip $m | ? { $_ -match 'Auto consume|Auto swap|Shared power' } } }) -join "
") } else { '' })
        }
        try { ([ordered]@{ mode = $spMode; facts = $spFacts; log = $spText } | ConvertTo-Json -Depth 8) | Set-Content -Encoding utf8 (Join-Path $r.Res 'skip-facts.json') } catch {}
        $spM = @(Get-SkipAegisVerdict $spMode $spFacts $spText)
        if ($spM.Count -gt 0) { & $spFail "FAIL: $($spM -join ' || ')" }
        Rig-Log "skip-aegis ($spMode): PASS (player '$($sp.player)', vanilla suit slots by $($sp.van.slots.method), observed $($sp.observed) s, $($sp.lowered) rounds)"
        Set-RigPhase 'proof-done'
    }
    'idle' { $spNext = $false }
}
if ($spNext) { $sp.i++ }







