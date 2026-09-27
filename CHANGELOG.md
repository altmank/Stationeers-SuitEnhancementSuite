# Changelog

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
