# Achievement Campaign Plan

Goal: 100% achievements + max progress. Iron Teeth is locked until you reach
**average wellbeing 15 while playing Folktails** (verified from game code:
`UnlockableFactionSpec.AverageWellbeingToUnlock`, checked by
`FactionUnlockingService`; unlocking also grants the UNLOCK_IRON_TEETH
achievement). So the campaign starts Folktails.

## Run 1 — Folktails: unlock Iron Teeth + broad sweep

**Primary objective: average wellbeing ≥ 15, fast.**

Wellbeing-15 rush on top of the standard opening (STRATEGY.md §2):

1. Days 1–10: standard survival opening (pump, lumberjacks, gatherer, carrots,
   forester, inventor, lumber mill). Survival first — dead beavers have no wellbeing.
2. Food variety early: berries (gatherer) + carrots = 2 food types; add potatoes
   → Grill (grilled food scores higher) by cycle 2.
3. Cheap wellbeing ladder, in ROI order (each tier feeds the average):
   - Work hours 12–14h (free wellbeing via leisure time)
   - Campfire (~Fun), Rooftop Terrace, Mud Bath when affordable
   - Water quality: plain water fine early; food variety matters more
   - Shrine/decor tier once planks flow
4. Wellbeing achievements cascade on the way: REACH_4 → REACH_10 →
   (15 unlocks Iron Teeth) → keep pushing to 20+ if the run is healthy.

**Secondary sweep (natural to Folktails / any-faction, grab in this run):**

- BUILD_CAMPFIRE, BUILD_DAM, PLANT_1000_TREES (forester runs constantly)
- CYCLE_5 / CYCLE_10 survival, SURVIVE_DROUGHT, SURVIVE_BADTIDE
- BEAVER_STUNG_BY_BEE (beehives are core Folktails infra anyway)
- GENERATE_POWER_WITH_WIND_TURBINES_ONLY — Folktails wind is default power;
  keep the grid pure wind until this pops, THEN diversify
- PRODUCE_PLANKS_IN_DAY threshold, DEMOLISH_AND_REBUILD, FLOOD_BUILDING (cheap stunts)
- CURE_CONTAMINATED_BEAVER (first badtide), PLUG_ANY_BADWATER_SOURCE
- ACTIVATE_WONDER_FOLKTAILS + BUILD_EVERY_STRUCTURE_FOLKTAILS if the colony
  matures; otherwise defer to a later Folktails run

**Run-1 constraints for the planner:**
- Power: wind-only until the wind achievement unlocks (then water wheels OK;
  power-wheels-only and water-wheels-only need separate runs)
- Do NOT rush game speed past 3–5x before wellbeing 15 on this run — observe first

## Run 2 — Iron Teeth: the max run (main event)

Deterministic breeding pods, deep pumps, engines. Target the mastery bar:
100+ beavers on Hard through 30-day droughts, then bots, then wonder.
Sweep: ACTIVATE_WONDER_IRON_TEETH, BUILD_EVERY_STRUCTURE_IRON_TEETH,
REACH_100/250/500_BEAVER_POPULATION, wellbeing 20→max, tubeway/zipline
network lengths, battery charge storage, refinery recipes, mines/bots chain.

## Specialty runs (conflicting constraints — short, targeted)

| Run | Achievements |
|---|---|
| Speedrun | BUILD_WONDER_BEFORE_CYCLE (cycle 15 "Rush B-eaver!") |
| Power wheels only | GENERATE_POWER_WITH_POWER_WHEELS_ONLY |
| Water wheels only | GENERATE_POWER_WITH_WATER_WHEELS_ONLY |
| Maple pastry diet | MAPLE_PASTRY_ONLY |
| No dwellings | REACH_POPULATION_WITHOUT_DWELLINGS |
| Extinction bots | BUILD_BOT_AFTER_BEAVER_EXTINCTION, BORN_BEAVER_AFTER_BEAVER_EXTINCTION |
| Misery run | BEAVER_DIES_MISERABLE, INJURED_JUST_BORN_BEAVER |
| Badtide streak | BADTIDE_STREAK, PLUG_ALL_BADWATER_SOURCES |
| Dynamite day | EXPLODE_DYNAMITE_IN_SINGLE_DAY, PLACE_DYNAMITE_AT_BOTTOM, EXPLODE_UNIT_WITH_DYNAMITE |
| Stunts | STACKED_HYDROPONIC_GARDENS, MANY_HEDGES, BUILD_HEIGHT_LIMIT, WORK_ALL_DAY_FOR_WEEK, MULTIPLE_WONDERS |

(IDs from decompiled `Timberborn.Achievements`; some may combine into fewer runs —
the campaign scheduler decides at runtime based on save state.)

## Sequencing rule for the autopilot

```
IF IronTeeth locked: faction=Folktails, objective=wellbeing>=15, sweep=passive
ELIF max_run_not_done: faction=IronTeeth, objective=max (STRATEGY.md), sweep=passive
ELSE: schedule shortest remaining specialty run
```
