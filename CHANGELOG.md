# Changelog

## 0.7.4

- **Clearer settings descriptions and Workshop page:** every setting now says what it does in plain words.
- **The hygiene row label is now English unless the game is set to Czech.**

## 0.7.3

- **Aegis helmets get no Suit Enhancement Suite slots either, and Aegis suits are now spotted more reliably.**

## 0.7.2

- **Aegis suits are left alone.** Suits from the Aegis mod get no slots from SES. Without Aegis nothing changes.

## 0.7.1

- **Source code on GitHub:** https://github.com/altmank/Stationeers-SuitEnhancementSuite, linked from the Workshop
  page. No gameplay changes.

## 0.7.0

- **Shared Power moved to advanced suits.** The two Shared Power slots now sit on the Hardsuit and the HARM suit, after
  Waste, and charge while you wear the suit. Uniforms lose their Shared Power slots: take any batteries out of your
  uniform's two Shared Power slots before updating, or they drop at your feet on load. The Hardsuit's own
  battery now counts as the suit battery (charged first; `Charge Suit Battery` off skips it).
- **Items no longer fall out of added slots when you load a save.** Added slots always go after other mods' slots (for
  example StationeersLua's Suit Module).
- **No free refills while paused.** Using a waste bag, water or food now waits until the game has processed the last
  one, so the pause menu no longer refills and burns through your bags.
- **A full waste bag stays in the Waste slot** across a reload instead of dropping at your feet.
- **Auto-swap never takes items from your hands.**

## 0.6.0

- **Waste slot.** Suits and the Marine Vest get a Waste slot after the Food slot (Hardsuit: slot 10); no existing slot
  moves, so 0.5.0 saves load unchanged. It takes folded and part-filled Waste Bags. Once your waste passes 25 % the
  bag is used as by hand, through the suit (the game does not let you relieve yourself in a suit), so the suit stays
  clean while you carry bags. A part-filled bag stays in and is used until full. New setting `Waste Threshold` (25 to
  95 %).
- **Auto-swap from your inventory.** An empty Water, Food or Waste slot, an empty bottle, rotten food or a full waste
  bag is replaced from your hands, storage slots, backpack, belt and the containers in them: the fullest water, the
  best-quality food (portioned food before whole crops), a part-filled bag before a folded one. The spent item goes
  back where the fresh one came from, or to the first free slot; never deleted or dropped. New `Auto Swap` section
  with one switch per slot (Water, Food, Waste), all on.
- **New slot icon** for Waste.

## 0.5.0

- **New name: Suit Enhancement Suite.** New mod id, plugin id and config file
  (`BepInEx\config\net.xceled.stationeers.suitenhancementsuite.cfg`). Settings from the earlier config file are not
  read: set them again in the config editor.
- **The uniform's storage slots moved behind its own slots (breaks old saves' uniforms).** The uniform's own slots
  are back at their usual places 0 to 3, and the four storage slots follow at 4 to 7. A uniform saved with the old
  layout loads with the two groups swapped; open it and drag the items back (README, "Migrating from the old slot
  layout"). Suits and the Marine Vest are unchanged.
- **Shared Power slots.** Every uniform gets two battery slots. While you wear it, their batteries charge your suit
  battery first, then the batteries of what you hold, then every other battery you carry, up to 500 W per battery
  per power tick (the Battery Cell Charger's rate). The emptier Shared Power battery is used first so you can swap
  it. New `Shared Power` settings: Enabled, Transfer Rate, Loss, Charge Suit Battery.
- **New slot icons** for Water, Food and Shared Power, drawn to match the game's own slot icons.
