# Suit Enhancement Suite

More room and less busywork in your clothing. Suits get Water, Food and Waste slots that keep you fed, watered and
relieved automatically and refill themselves from your inventory, advanced suits also get two Shared Power slots whose
batteries keep your suit, tools and other batteries charged, the Marine Vest gets eight storage slots, and every
uniform gets four storage slots.

## Slots

| Clothing | Added slots | Where |
| --- | --- | --- |
| Suits, spacesuits, Hardsuit, Icarus, HARM suit, other armour | Water, Food, Waste | after the suit's own slots and other mods' slots (Hardsuit with StationeersLua's Suit Module at 8: 9 Water, 10 Food, 11 Waste) |
| Advanced suits (Hardsuit, HARM suit) | also 2 Shared Power | after Waste (Hardsuit: 12, 13) |
| Marine Vest (body armour) | 8 storage, Water, Food, Waste | after its own slots; Water, Food and Waste are the last three |
| Uniforms (jumpsuits) | 4 storage | after the uniform's own slots: storage 4 to 7 |

The added slots always come after the clothing's own slots, so the game's slots keep their usual places. New slots
are only ever added at the end (0.6.0 added Waste behind Food), and always after any slots other mods add (for example
StationeersLua's Suit Module), whatever order the mods load in.

- **Storage** slots take anything.
- **Water** takes water bottles and anything else drunk like one.
- **Food** takes food: meals, bars, canned food, cooked vegetables and raw crops that have nutrition. It refuses seeds,
  eggs, pills and non-food items (the game shows its usual "is not" message).
- **Waste** takes waste bags that still have room: folded (empty) Waste Bags and part-filled ones. A full bag is
  refused.
- **Shared Power** takes any battery cell.

Only the suit you wear is eaten, drunk and relieved from, and only the advanced suit you wear shares power.

## Eating and drinking

Every 2 seconds, for every living player (after any auto-swap, below):
- **Water:** once at least `Water Top-Off Margin` % of your hydration capacity is empty, the Water slot's bottle is
  drunk until you are full, exactly as drinking by hand would.
- **Food items** (bars, meals, cans): once at least `Food Top-Off Margin` % of your stomach is empty, you eat just
  enough to be full. The rest stays in the slot.
- **Whole items** (raw crops, cooked vegetables): one whole item is eaten, as when eating by hand, but only when all
  of its nutrition fits, so none is wasted. A tomato (15) is eaten once 15 of the 50 nutrition points are free.
  A pumpkin (50) fills the whole stomach, so it would only ever fit at 0: below `Hunger Fallback` % nutrition a
  whole item is eaten anyway and the part that does not fit is lost, the same as eating it by hand.
- After a bite or a drink, that need waits `Cooldown` seconds before the next one.
- Raw crops count as raw food: like eating them by hand, they pull your food quality down (low food quality lowers
  your hydration capacity by a quarter). Cooked food keeps it up.
- Eating and drinking happen through a closed helmet.

## Waste

The game fills your stomach's waste as you drink. Past 25 % you can relieve yourself on a toilet or into a waste bag,
but not while wearing a suit: on Normal and Stationeer a suit blocks both, and at 100 % you wet the suit, which leaves
you Soiled (hygiene drops fast, you move slower) until the suit's inside air is emptied (a Suit Storage does it).

The Waste slot does it for you, through the suit:
- Once your waste passes `Waste Threshold` (25 % by default, the earliest the game allows by hand), the bag in the
  Waste slot is used exactly as using it by hand would: it takes all your waste, or as much as still fits.
- A part-filled bag stays in the slot and is used again next time, until it is full (one bag holds two full
  stomachs).
- Folded bags: one is unfolded into the slot and filled. If the slot held more than one folded bag, the rest move to
  a free inventory slot first; with no free slot, nothing happens until you make room (logged).
- Nothing happens when the difficulty has sanitation off (Creative, Easy): there is no waste.
- After a use, the need waits `Cooldown` seconds like eating and drinking.

## Auto-swap from your inventory

When the Water, Food or Waste slot is empty, or its item is spent, the mod moves a fresh one in from your
inventory: the worn suit's and uniform's storage slots, backpack, tool belt, helmet, glasses, and containers inside
them. Items in your hands are never taken.

| Slot | Spent means | Fresh one chosen |
| --- | --- | --- |
| Water | an empty bottle | the one holding the most water |
| Food | nothing left, or the food rotted | best food quality first (a meal before canned before cooked before raw crops); within a quality, portioned food (bars, cans, meals) before whole items, because it tops you off exactly; then the smallest whole item (a tomato before a pumpkin, it fits sooner), or the portioned item with the most food left |
| Waste | a full bag | a part-filled bag first (the fullest, to finish it), then a folded bag (just one is taken from a stack) |

- The spent item goes back where the fresh one came from. If it cannot go there (a stack of folded bags stays
  behind), it goes into the first free inventory slot, storage before hands. With no room anywhere the swap waits
  and the log says so once. Nothing is ever deleted or dropped.
- Each slot has its own switch (`Auto Swap` section), all on by default.
- Better food quality matters: raw crops pull your food quality down, and low food quality lowers your hydration
  capacity by a quarter.

## Shared Power

While you wear an advanced suit (Hardsuit, HARM suit), the batteries in its two Shared Power slots charge the other batteries you carry. On
every power tick (twice a second) each battery receives up to `Transfer Rate` W, the same rate as the game's
Battery Cell Charger:

1. your suit's battery first (unless `Charge Suit Battery` is off),
2. then the batteries of what you hold in your hands,
3. then every other battery you carry in a battery slot: helmet, tool belt, tools in your backpack or in the
   uniform's storage slots.

Within each step the battery with the lowest charge percentage comes first. Of the two Shared Power batteries, the
one holding less energy is used first, so it runs flat and can be swapped while the other stays full.

- A battery only counts when it sits in a battery slot (a suit's, a tool's, a helmet's). A spare battery lying in a
  storage slot or a backpack is not charged.
- The Shared Power batteries never charge each other.
- Nothing is created: each battery receives what the Shared Power batteries lose, less the `Loss` setting (0 % by
  default).
- A suit that is not worn (in a locker or a backpack) shares nothing. Basic, emergency and Icarus suits, spacesuits
  and the Marine Vest have no Shared Power slots.

## Settings

StationeersLaunchPad config editor, or `BepInEx\config\net.xceled.stationeers.suitenhancementsuite.cfg`. Changes
apply at once (eating and drinking within 2 seconds), no restart.

| Setting (section Auto Consume) | Default | What it does |
| --- | --- | --- |
| Enabled | On | Off: the Water, Food and Waste slots are plain storage; nothing is eaten, drunk, used or swapped automatically. The slots stay. |
| Food Top-Off Margin | 1 % | Eat once at least this much of the stomach is empty (0 to 75). 75 eats only below 25 %. |
| Water Top-Off Margin | 1 % | Drink once at least this much of the hydration capacity is empty (0 to 75). |
| Hunger Fallback | 25 % | Below this nutrition level a whole item is eaten even if part of it is wasted (0 to 50). 0: never waste; a pumpkin then waits until the stomach is empty. |
| Waste Threshold | 25 % | Use the Waste slot's bag once your waste passes this level (25 to 95). 25 is the earliest the game allows by hand. |
| Cooldown | 3 s | Least time between two automatic bites, two drinks or two bag uses, for one player (1 to 60). |

| Setting (section Auto Swap) | Default | What it does |
| --- | --- | --- |
| Water | On | Refill an empty Water slot, or swap out an empty bottle, from your inventory. |
| Food | On | Refill an empty Food slot, or swap out rotten food, from your inventory. |
| Waste | On | Refill an empty Waste slot, or swap out a full bag, from your inventory. |

Auto-swap only runs while Auto Consume `Enabled` is on.

| Setting (section Shared Power) | Default | What it does |
| --- | --- | --- |
| Enabled | On | Off: the Shared Power slots only hold batteries; nothing is charged. The slots stay. |
| Transfer Rate | 500 W | Most each battery receives per power tick (10 to 5000). 500 is the game's Battery Cell Charger rate. |
| Loss | 0 % | Share of the energy taken from a Shared Power battery that is lost on the way (0 to 90). 20: 100 J taken, 80 J arrive. |
| Charge Suit Battery | On | Off: the suit's battery is left alone; hands, helmet, tools and the rest are still charged. |

## Migrating from the old slot layout

Up to 0.5.0 the uniform's four storage slots came **before** its own slots. 0.5.0 moves them after the uniform's
own slots and adds the two Shared Power slots behind them. The game stores each item by slot position, so a uniform
saved with the old layout loads with its contents swapped:

| Uniform slot position | Old layout | New layout |
| --- | --- | --- |
| 0 to 3 | storage 1 to 4 | the uniform's own slots |
| 4 to 7 | the uniform's own slots | storage 1 to 4 |
| 8, 9 | not present | not present (0.5.0 and 0.6.0 had Shared Power here; 0.7.0 moved it to advanced suits) |

An item stored at old position `n` belongs at new position `n + 4` (storage), and an item in the uniform's own slot
at old position `n` belongs at `n - 4`. Suits and the Marine Vest did not change. The same mapping, as data for save
tools, is in `tools\slot_layout.json`.

After loading an old save, open the uniform and drag the items back: the old storage items (for example food) now
sit in the uniform's own slots, and your access card and other uniform items sit in the storage slots.

## Multiplayer

Eating, drinking, waste bags, auto-swap and power sharing run on the host (or dedicated server) only; the game sends
the results (nutrition, hydration, waste, item moves and quantities, battery charge) to everyone with its own updates. The slots are added by the
mod on each machine, so every player needs it. A player without it sees suits, vests and uniforms with missing slots.
No extra network traffic.

## Removing it

Take everything out of the added slots first (Water, Food, Waste, the vest's and uniforms' storage slots, the Shared
Power slots), then save. Without the mod those slots do not exist and their contents are lost or land in the wrong slot.

## Limits and known gaps

- The Life Functions panel shows the game's own hygiene and waste rows, which the game normally hides.
- Tested in game (0.6.0): nothing yet.
- Not yet tested in game (0.6.0): the Waste slot in the right place (Hardsuit 10) with its icon and label; full bags
  refused; a folded bag unfolded into the slot and filled above 25 % waste; a part-filled bag refilled; a stack of
  folded bags split (rest moved to storage); a full bag swapped for a folded one from the backpack; an empty water
  bottle swapped for the fullest one; food swapped in best-quality first; the "no room" warning with a full
  inventory; each `Auto Swap` switch off; everything on a client.
- Not yet tested in game (0.5.0): new slot icons and labels; uniform slots in the new order; Shared Power charging the
  suit battery first, then a tool in hand, then a tool on the belt; the emptier Shared Power battery drained first;
  `Charge Suit Battery` off; `Transfer Rate` and `Loss` changed live; charge percentages shown correctly on a client;
  an old-layout uniform loaded and sorted by hand. Carried over from earlier builds, also untested: topping off from a
  bar; a tomato eaten only with 15 free; a pumpkin eaten below 25 %; refusal of seeds and tools in the Food slot;
  water packets in the Water slot.

## Build

`.\build.ps1` builds against the installed game and stages `package\`; `-Deploy` installs it into
`Documents\My Games\Stationeers\mods\<repo folder name>` (game closed only). Unit tests:
`dotnet test tests\SuitEnhancementSuite.Tests`.
