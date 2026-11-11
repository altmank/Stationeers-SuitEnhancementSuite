# The skip-aegis proof's verdict (dot-sourced by skip-aegis-poll.ps1 and by skip-aegis-verdict-offline.ps1): pure functions over what the
# poll read, so a saved run can be judged again without a VM. Windows PowerShell 5.1 and pwsh 7 both run it. A capture that is missing
# FAILS the verdict (P98): nothing here passes on the absence of data.

# StationGod's slot reply -> @{ method = 'key' | 'label' | 'none'; names = [string[]] }.
#   `inspect slots` rows carry string_key (the slot's StringKey: SES's keys all start SuitEnhancementSuite);
#   `inspect contents` rows carry only display_name, and SES registers the labels Water, Food, Waste and Shared Power for its keys.
# A reply that is null, not ok, or has no slot rows is 'none'.
function Get-SkipAegisSlots($reply) {
    $none = @{ method = 'none'; names = @() }
    if ($null -eq $reply) { return $none }
    if ($reply.PSObject.Properties['ok'] -and -not $reply.ok) { return $none }
    $res = $reply.result
    if ($null -eq $res) { return $none }
    $rows = @($res.slots)
    if ($rows.Count -eq 0 -or $null -eq $rows[0]) { return $none }
    $keys = @($rows | Where-Object { $_.PSObject.Properties['string_key'] -and "$($_.string_key)" -ne '' } | ForEach-Object { "$($_.string_key)" })
    if ($keys.Count -gt 0) { return @{ method = 'key'; names = $keys } }
    $labels = @($rows | Where-Object { $_.PSObject.Properties['display_name'] } | ForEach-Object { "$($_.display_name)" })
    if ($labels.Count -eq 0) { return $none }
    @{ method = 'label'; names = $labels }
}

# The SES slots an added-slot list holds, by the method it was read with: the keys, or the labels.
function Get-SkipAegisSesSlots($slots) {
    if ($slots.method -eq 'key') { return @($slots.names | Where-Object { $_ -like 'SuitEnhancementSuite*' }) }
    if ($slots.method -eq 'label') { return @($slots.names | Where-Object { $_ -in @('Water', 'Food', 'Waste', 'Shared Power', 'Voda', 'Jidlo', 'Odpad') }) }
    @()
}

# The slots a vanilla suit must carry: the five SES keys on an advanced suit, three otherwise.
function Test-SkipAegisSesSlotsComplete($slots, [bool] $advanced) {
    $missing = New-Object System.Collections.Generic.List[string]
    if ($slots.method -eq 'key') {
        $want = @('SuitEnhancementSuiteWater', 'SuitEnhancementSuiteFood', 'SuitEnhancementSuiteWaste')
        if ($advanced) { $want += @('SuitEnhancementSuiteSharedPower1', 'SuitEnhancementSuiteSharedPower2') }
        foreach ($k in $want) { if ($slots.names -notcontains $k) { $missing.Add($k) } }
    }
    elseif ($slots.method -eq 'label') {
        foreach ($k in @('Water', 'Food', 'Waste')) { if ($slots.names -notcontains $k) { $missing.Add($k) } }
        if ($advanced) {
            $n = @($slots.names | Where-Object { $_ -eq 'Shared Power' }).Count
            if ($n -lt 2) { $missing.Add("Shared Power x2 (found $n)") }
        }
    }
    else { $missing.Add('no slot reading') }
    , $missing
}

# BepInEx log lines naming a player in an SES action: auto consume, auto swap, shared power.
function Get-SkipAegisActionLines([string] $logText, [string] $player) {
    if (-not $player) { return @() }
    $p = [regex]::Escape($player)
    @($logText -split "`r?`n" | Where-Object { $_ -match "(Auto consume|Auto swap|Shared power)[^\r\n]*$p" })
}

# SES's own faults: an Error/Fatal line from any source that names the mod, a Warning from the mod's own log source, and an exception
# line naming its namespace.
function Get-SkipAegisSesErrors([string] $logText) {
    @($logText -split "`r?`n" | Where-Object {
        (($_ -match '^\[(Error|Fatal)\s*:') -and ($_ -match 'SuitEnhancementSuite|Suit Enhancement Suite')) -or
        ($_ -match '^\[Warning\s*:\s*Suit Enhancement Suite\]') -or
        ($_ -match 'Exception' -and $_ -match 'SuitEnhancementSuite\.')
    })
}

# $mode 'a' (SES + Aegis) or 'b' (SES alone). $facts: aegisSlots, vanillaSlots, vanillaAdvanced (bool), player (display name),
# observedSeconds, needsLowered (count of the console set commands that succeeded). $logText: the client's whole BepInEx log.
# Returns the list of what is missing; empty is a pass.
function Get-SkipAegisVerdict([string] $mode, $facts, [string] $logText) {
    $missing = New-Object System.Collections.Generic.List[string]
    if ($mode -notin @('a', 'b')) { $missing.Add("unknown mode '$mode'"); return $missing.ToArray() }
    if ($null -eq $facts) { $missing.Add('no facts captured'); return $missing.ToArray() }
    if ([string]::IsNullOrWhiteSpace($logText)) { $missing.Add('the game log is empty or was not read'); return $missing.ToArray() }
    if (-not "$($facts.player)") { $missing.Add('the player name was not read') }

    # Loaded: SES in both runs, Aegis in run a only.
    if ($logText -notmatch 'Suit Enhancement Suite \d+\.\d+\.\d+ loaded') { $missing.Add('no "Suit Enhancement Suite x.y.z loaded" line in the log') }
    if ($mode -eq 'a' -and $logText -notmatch 'Aegis \d+\.\d+\.\d+ loaded\.') { $missing.Add('no "Aegis x.y.z loaded." line in the log') }
    if ($mode -eq 'b' -and $logText -match 'Aegis \d+\.\d+\.\d+ loaded\.') { $missing.Add('run b is SES alone, but the log says Aegis loaded') }

    # No SES errors.
    $err = Get-SkipAegisSesErrors $logText
    if ($err.Count -gt 0) { $missing.Add("$($err.Count) SES error line(s), first: $("$($err[0])".Substring(0, [Math]::Min(200, "$($err[0])".Length)))") }

    # The worn vanilla suit (run b) or the loose one (run a) has every SES slot.
    if (-not $facts.vanillaSlots -or $facts.vanillaSlots.method -eq 'none') { $missing.Add('the vanilla suit slots were not read') }
    else {
        $m = Test-SkipAegisSesSlotsComplete $facts.vanillaSlots ([bool]$facts.vanillaAdvanced)
        if ($m.Count -gt 0) { $missing.Add("the vanilla suit lacks SES slots: $($m -join ', ') (read by $($facts.vanillaSlots.method): $($facts.vanillaSlots.names -join ', '))") }
    }

    $consume = @(Get-SkipAegisActionLines $logText "$($facts.player)" | Where-Object { $_ -match 'Auto consume' })
    if ($mode -eq 'a') {
        if (-not $facts.aegisSlots -or $facts.aegisSlots.method -eq 'none') { $missing.Add('the Aegis suit slots were not read') }
        else {
            $ses = Get-SkipAegisSesSlots $facts.aegisSlots
            if ($ses.Count -gt 0) { $missing.Add("the Aegis suit has SES slots: $($ses -join ', ')") }
            if (@($facts.aegisSlots.names).Count -lt 5) { $missing.Add("the Aegis suit reads only $(@($facts.aegisSlots.names).Count) slot(s): not an Aegis suit's list") }
        }
        if ([double]$facts.observedSeconds -lt 120) { $missing.Add("the needs were watched for $($facts.observedSeconds) s, under 120 s") }
        if ([int]$facts.needsLowered -lt 1) { $missing.Add('the needs were never lowered (no console set command succeeded)') }
        $acts = Get-SkipAegisActionLines $logText "$($facts.player)"
        if ($acts.Count -gt 0) { $missing.Add("$($acts.Count) SES action line(s) name the Aegis wearer, first: $("$($acts[0])".Substring(0, [Math]::Min(200, "$($acts[0])".Length)))") }
    }
    else {
        if ([int]$facts.needsLowered -lt 1) { $missing.Add('the needs were never lowered (no console set command succeeded)') }
        if ($consume.Count -lt 1) { $missing.Add("no SES auto-consume line for $($facts.player) in the log (Debug level on? needs lowered? item in the slot?)") }
    }
    # A plain array (never a list inside an array): callers wrap it in @() and count it.
    return $missing.ToArray()
}

