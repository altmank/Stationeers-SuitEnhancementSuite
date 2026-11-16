# ses-logging-audit: SES at main 11beb37 vs _alpha\LOGGING.md (read only)

Logs: the owner's LogOutput.log (11-15 20:48): 3 SES lines (load, 2 HygieneHud), each x1. VM run ses-skip-aegis-b (SES 0.7.2): Auto swap x2 (Info), Auto consume x2 (Debug), Added suit slots x1 (Debug), each x1. No template repeats in either log; a real 30-minute session is not in evidence.

Format: severity | file:line | finding | fix | proof

## Findings
must | src/Hud/HygieneHud.cs:39 | Caught exception at Debug; loop runs every 1 s, so a persistent fault is 1 line/s in Player.log, nothing in LogOutput.log (F1/F2). | Warning once per message (ReportOnce pattern), cause + "row stays hidden, retrying every 1 s". | none
must | src/Slots/SlotDisplayPatches.cs:27 | Caught exception at Debug; slot names then show <NC:...> and nobody is told. | Warning once with exception, "slots show placeholder names". | none
must | src/Needs/AutoConsumer.cs:96 | Per-use LogDebug; string.Format with boxed floats built with no flag check. | Info behind area `consume`, guarded by Verbose.On. VM scripts must then set Verbose=consume. | none (vm to confirm scripts)
must | src/Slots/SlotLayoutPatches.cs:24 | Per-clothing Debug "Added suit slots" in the Thing.Awake prefix; many suits = many Player.log lines. | Verbose area `slots`, guarded, or drop. | none
must | src/Needs/AutoSwapper.cs:59 | Info per swap; will pass the 5-per-template ceiling in a busy 30-min session. | Verbose area `swap`; keep the :54 Warning. | none
must | src/Hud/HygieneHud.cs:91,122 | Two per-item load lines at Info, both doubling the mod name. No test matches them. | Fold into the load summary or area `hud`; drop prefix. | none
must | src/Plugin.cs | No `ses` command, no `ses diag`, no `ses log`, no [Logging] Verbose key, no area flags (LOGGING 4,5,8-9). | Add key, LogArea enum, one flag set, commands; check CommandsMap.ContainsKey("ses") first. | none (unit); vm for dedicated-server answers
should | src/Plugin.cs:32 | Load line has version + settings but no seat and no dependencies (LaunchPad hard dep, StationeersLua ordering, Aegis); PatchAll result ignored so failed patches are unseen. Awake is before the world, so seat unknown there. | Keep line (VM matches it); add world-start Info with seat and dependency versions; count patches vs expected, Warn names of failures. | none for builders; vm for seat
should | src/Plugin.cs:26,39-41 | "Settings changed" dumps the whole section, names no setting, no old value; a slider gives a line per step. | Log `Setting X: old -> new` from a last-value map; one line per on/off edge. | none
should | src/Needs/AutoConsumer.cs:33 | On a client the mod silently does nothing. | One Info at world start: "auto consume and shared power run on the host only". | none
should | AutoConsumer.cs:106, SharedPower.cs:136 | "reported once" Warnings: 16-message cap with no line when reached; no player/need inputs; no "what next". | Name player, say pass is skipped and retried in 2 s, log once when the cap is hit. | none
should | src/Needs/NeedUpkeep.cs:28 | Level and once-per-episode right; lacks "will retry", item without #Id. | Add both. | none
should | src/Needs/WasteBags.cs:64, AutoSwapper.cs:54 | Warnings per occurrence, not per episode; each pass can repeat. | Gate with ReportLatch; name Prefab#Id. | none
nit | src/Slots/SlotIcons.cs:64 | Warning fine; up to 4 lines at load. | Count in the load line instead. | none
nit | AutoSwapper.cs:59, AutoConsumer.cs:96 | Player named by DisplayName only; items by PrefabName, no #Id. | Add ReferenceId (append, don't reorder: VM scripts match). | none
nit | HygieneHud.cs:125+ | Clone fallback paths log nothing. | One Info when taken. | none
nit | tests | Settings ToString culture (de-DE) and 0-byte hot-path tests absent. | Add unit tests (LOGGING 7, 8.7). | none

## Hot paths
- Auto-consume coroutine: the Debug at :96 is built only after a successful use, but unguarded.
- Shared Power tick: no log on success path; clean. Add 1000-call GC test.
- Slot filters (NeedSlotFilterPatches.cs:17,28,39,52): no logging; clean. Slot display patches: only the :27 catch.
- ThingAwakeSlotsPatch: Debug inside the clothing `if`.

## Verbose areas: consume, swap, power, slots, hud.

## `ses` commands
- `ses`: help (verbs, areas); client and server.
- `ses diag [area]`: one block to the log, one console line "N lines in the BepInEx log": version, seat, deps, patches installed/failed, non-default config, verbose areas; per living player (Human#Id) per need slot: item, cooldown left, settle gate, latch, last refusal; power: suit, Shared Power cells and charge, last fault; slots: clothing count; hud: attach path.
- `ses log [area|all on|off]`: session only, Info per change, bad name lists valid ones.
- Dedicated: all three work; consume/swap/power data on host or server only (client says so); `hud` says a server has no HUD.

## Lines matched by tests or VM scripts (do not reword)
- `Suit Enhancement Suite \d+\.\d+\.\d+ loaded`: tests/vm/skip-aegis-poll.ps1:90-91, skip-aegis-proof.ps1:99, skip-aegis-verdict.ps1:79, verdict.tests.ps1:19,65-66.
- `Auto consume: <player> `: poll.ps1:103,167,195; verdict.ps1:55. Debug today; VM reads Player.log. Behind Verbose, the scripts must set Verbose=consume.
- `Auto swap:` (and fixture "Auto swap: Recruit's Water slot was empty, now holds ItemWaterPacket from ItemToolBelt."): poll.ps1:167,195; poll-dry.ps1:114; verdict.tests.ps1:60.
- `Shared power`: poll.ps1:195, verdict.ps1:55.
- Any SES Warning/Error fails the verdict (verdict.ps1:62-63): promoting the two Debug catches to Warning must stay silent in the standard run.
- No C# unit test matches log text.

## Proof: nearly all `-Proof none`; vm only for seat line, dedicated-server `ses diag`, and VM scripts under Verbose=consume.

GAPS 7
