# Timberborn 1.0.13 — Verified Gameplay Mechanics Reference

Extracted from decompiled game assemblies (ilspycmd over `Timberborn_Data/Managed`) and the
shipped blueprint JSON data (`Timberborn_Data/StreamingAssets/Modding/Blueprints.zip`).
All numbers below are from **game data files or code literals**, not the wiki.
Citations are `AssemblyNamespace/Class` (source) or `Blueprints/<path>.blueprint.json` (data).

Conventions:
- 1 in-game day = 24 in-game hours = 768 ticks; 1 tick = 0.6 real seconds at 1× speed
  (`Configurations/DayNightCycle.blueprint.json`: `ConfiguredDayLengthInTicks: 768`;
  `Configurations/TickTime.blueprint.json`: `TickIntervalInSeconds: 0.6`).
  So 1 tick = 1.875 in-game minutes; 1 in-game hour = 32 ticks = 19.2 real seconds;
  1 day = 460.8 real seconds ≈ 7.7 real minutes at 1×.
- Daytime = hours 0–16, nighttime = 16–24 (`ConfiguredDaytimeLengthInHours: 16`;
  `Timberborn.TimeSystem/DayNightCycle.BoundsInHours`). New games start at hour 4
  (`HoursPassedOnNewGame: 4`).

---

## 1. Wellbeing

### Per-beaver wellbeing (integer score)
`Timberborn.Wellbeing/WellbeingTracker.UpdateWellbeing`: a beaver's Wellbeing =
**sum over all its needs of `NeedManager.GetNeedWellbeing(needId)`**.

Per need (`Timberborn.NeedSystem/Need.Wellbeing` + `Timberborn.NeedSpecs/NeedSpec`):
- If need disabled → 0.
- If need "favorable" → `FavorableWellbeing` (from need blueprint).
- If unfavorable → `UnfavorableWellbeing` (negative or 0).

"Favorable" (`Need.IsFavorable`): for normal needs, `Points > 0`; for never-positive needs
(Max ≤ 0, e.g. Injury, BadwaterContamination), favorable means `Points == 0`.

So wellbeing is **binary per need**: you get the full `FavorableWellbeing` the moment points
are above 0, regardless of how full the bar is. Keeping a need barely positive is as good as full.

### Average wellbeing
`Timberborn.Wellbeing/WellbeingService`:
- `AverageGlobalWellbeing` = `GlobalWellbeingTrackerRegistry.Registry.GetAverageWellbeing()`,
  recomputed **every tick** (`WellbeingService.Tick`).
- `WellbeingTrackerRegistry.GetAverageWellbeing` = `RoundToInt(mean of all registered
  trackers' Wellbeing)`; 0 if no trackers.
- **Only beavers count.** `WellbeingConfigurator` adds `WellbeingTracker` to every `Character`
  but `WellbeingTrackerRegistrar` (which registers into the global/district registries) is a
  decorator on `Beaver` only. Bots have wellbeing (for their own tiers) but do **not** affect
  the global average. Dead beavers are unregistered on death (`WellbeingTrackerRegistrar.OnDied`).
- `AverageDistrictWellbeing` is the same math over the currently-selected district's population.

### Which needs exist and their wellbeing weights (beaver, from `Blueprints/Needs/*.json`)
Needs are faction-filtered via `Blueprints/NeedCollections/NeedCollection.{Common,Folktails,IronTeeth}.json`.

Common (all beavers): BadwaterContamination, Campfire, ChippedTeeth, Hunger, Injury, Lantern,
Roof, RooftopTerrace, Shelter, Shrub, Sleep, Thirst, WetFur.

Key spec fields (`NeedSpec`): `StartingValue`, `MinimumValue`, `MaximumValue`, `DailyDelta`
(points/day drift), `ImportanceMultiplier` (AI appraisal weight), `FavorableWellbeing` /
`UnfavorableWellbeing` (wellbeing contribution).

| Need | Group | Range | DailyDelta | FavWB | UnfavWB | Notes |
|---|---|---|---|---|---|---|
| Hunger | Basic | −3..1 (start 1) | −0.8 | +1 | −10 | Lethal at −3; crit penalties: WorkingSpeed −0.5, GrowthSpeed −0.4 |
| Thirst | Basic | −3..1 (start 1) | −0.7 | +1 | −10 | Lethal at −3; crit penalty: MovementSpeed −0.25 |
| Sleep | Basic | −0.2..0.8 (start 1→clamped) | −0.6 | +1 | −1 | Crit action = sleep on ground; penalty MovementSpeed −0.1 |
| Shelter | Basic | −0.2..0.8 | −0.3 | +1 | −3 | Filled by sleeping in housing |
| WetFur | Basic | 0..1 | −0.3 | +1 | 0 | |
| Injury | Basic | −1..0 | +0.1 (heals) | 0 | −2 | Never-positive |
| BadwaterContamination | Basic | −1..0 | 0 | 0 | −10 | Penalty MovementSpeed −0.7 |
| ChippedTeeth | Basic | −1..0 | 0 | 0 | −1 | Penalty CuttingSuccessChance −0.75 |
| BeeSting | Basic (FT) | −1..0 | +0.75 | 0 | −1 | |
| Carrots/Kohlrabi/SunflowerSeeds/MangroveFruit | Nutrition | 0..1 | −0.05 | +1 | 0 | raw foods |
| Bread/CattailCracker/Grilled*/Fermented*/CornRation/EggplantRation/AlgaeRation | Nutrition | 0..1 | −0.05 | +2 | 0 | processed foods |
| MaplePastry | Nutrition | 0..1 | −0.05 | +3 | 0 | |
| Coffee (IT) | Nutrition | 0..1 | −0.05 | +3 | 0 | ImportanceMultiplier 1.25 |
| Campfire/ContemplationSpot/RooftopTerrace | SocialLife | 0..1 | −0.2 | +1 | 0 | |
| Agora (FT) | SocialLife | 0..1 | −0.1 | +3 | 0 | |
| DanceHall (FT) | SocialLife | 0..1 | −0.1 | +5 | 0 | |
| Lido/SwimmingPool/Scratcher | Fun | 0..1 | −0.2 | +1 | 0 | |
| Carousel/Books/MudBath/MudPit/WindTunnel/ExercisePlaza | Fun | 0..1 | −0.1 | +3 | 0 | Books ImpMul 1.25 |
| Motivatorium (IT) | Fun | 0..1 | −0.1 | +5 | 0 | |
| Detailer | Fun | 0..1 | −0.05 | +1 | 0 | |
| Roof/Shrub/Lantern/Scarecrow/Weathervane/BeaverBust/Bell/Brazier | Aesthetics | 0..1 | −0.4 | +1 | 0 | passive aura |
| BeaverStatue/BulletinPole/DecorativeClock | Aesthetics | 0..1 | −0.3/−0.4 | +2 | 0 | |
| FarmerMonument/LaborerMonument | Awe | 0..1 | −0.3 | +3 | 0 | |
| BrazierOfBonding/FlameOfUnity | Awe | 0..1 | −0.3 | +5 | 0 | |
| FountainOfJoy/TributeToIngenuity | Awe | 0..1 | −0.3 | +8 | 0 | |
| EarthRecultivator/EarthRepopulator | Awe | 0..1 | −0.01 | +10 | 0 | wonders |
| Antidote (FT) | Basic | 0..1 | −1.0 | 0 | 0 | contamination cure |

(FT = Folktails-only, IT = Iron Teeth-only; full lists in the NeedCollection blueprints.)

### Wellbeing tier bonuses
`Timberborn.Wellbeing/WellbeingTierManager` listens to `WellbeingTracker.WellbeingChanged` and
adds/removes bonuses on the entity's `BonusManager`. Tier lookup:
`WellbeingTierService` picks the spec set by character type (BeaverAdult / BeaverChild / Bot);
`WellbeingTier.TryGetTierBonus` returns the bonus for the highest threshold ≤ current wellbeing.
Above the last defined tier, bonuses extrapolate: +`MultiplierIncrement` per additional
`WellbeingThreshold` wellbeing (`WellbeingTier.GetCalculatedBonus`).

`BonusManager` (Timberborn.BonusSystem): each bonus type has value `1.0 + Σ deltas`, clamped
to `[MinimumValue, MaximumValue]` from `Blueprints/BonusTypes/*`:
MovementSpeed [0.25, 2.0], WorkingSpeed [0.05, 1000], GrowthSpeed [0.1, 2.0],
LifeExpectancy [0.1, 1000], CarryingCapacity [0.1, 10], CuttingSuccessChance [0.01, 1.0].

Adult tiers (`Blueprints/WellbeingTiers/WellbeingTier.Adult.*.json`) — value shown is the
**delta added to the 1.0 base multiplier**:

- WorkingSpeed: wb5→+0.2, 10→+0.4, 15→+0.6, 20→+0.8, 25→+1.0, 30→+1.2, 35→+1.4, 40→+1.6,
  45→+1.8, 50→+2.0, 55→+2.2, 60→+2.4, 70→+2.6, 80→+2.8, 90→+2.9, 100→+3.0
  (i.e. every 5 wellbeing = +20% work speed up to wb 60).
- MovementSpeed: wb2→+0.05, 12→+0.15, 22→+0.3, 32→+0.4, 42→+0.5, 52→+0.6, 62→+0.65,
  72→+0.7, 82→+0.75, 92→+0.8. (Clamped at 2.0 total by bonus type max.)
- LifeExpectancy: wb7→+0.2, 17→+0.4, 27→+0.6, 37→+0.8, 47→+1.0, 57→+1.1 … 97→+1.5.
- Child GrowthSpeed: wb5→+0.05, 10→+0.1, then +0.05 per 4 wb up to 54→+0.65, 60→+0.7,
  70→+0.75, 80→+0.8, 90→+0.85, 100→+0.9.
- Bot tiers key off bot wellbeing 0/1/2 (bot needs: Biofuel/Energy give 0 when favorable;
  boosts Catalyst/Grease/PunchCard/ControlTower give +1 each):
  WorkingSpeed +0.65/+1.1/+1.7, MovementSpeed +0.3/+0.6/+0.8.

### Reaching average wellbeing 15 (Iron Teeth unlock)
Basic needs kept favorable give +5 (Hunger, Thirst, Sleep, Shelter, WetFur each +1).
The remaining 10 must come from Nutrition variety (+1..+3 per food type), Fun/Social/Aesthetics
buildings, etc. Example: basic 5 + two processed foods (2+2) + Campfire 1 + three aesthetics
auras 3 + Carousel 3 = 16. Because each need is binary, **variety breadth matters, not depth**.
The check uses the **instantaneous global average** (see §8), so every living beaver
(including children and fresh spawns; children share most needs) drags the average.

---

## 2. Needs — decay and satisfaction mechanics

`Timberborn.NeedSystem/Need` + `NeedManager` (ticked per character every game tick):

- Decay: `_deltaPointsPerUpdate = (DailyDelta / 24) * FixedDeltaTimeInHours` applied every tick
  **unless an effect was applied that tick** (`Need.Update`). Points clamp to [Min, Max].
- Death: needs with `LethalNeedSpec` (Hunger, Thirst — `Timberborn.MortalSystem/LethalNeedSpec`)
  kill when points hit `MinimumValue` (−3): `MortalSystem/MortalNeeder.NeedDeathUpdate` →
  `Mortal.DiePubliclyAsSoonAsPossible`. Time from full to death with no food:
  Hunger 4.0 pts / 0.8 per day = **5 days**; Thirst 4.0/0.7 ≈ **5.7 days**.
  Critical state (status icon, penalties) starts when the need goes unfavorable (points ≤ 0),
  i.e. 1.0/0.8 = 1.25 days after last meal, 1.0/0.7 ≈ 1.43 days after last drink.
- Difficulty scaling: `Timberborn.GameFactionSystem/NeedModificationService.ModifyIfEligible`
  multiplies `DailyDelta` of Hunger, Thirst, and the whole Nutrition group by the game-mode
  `FoodConsumption`/`WaterConsumption` (Easy 0.4/0.4, Normal 1.0/1.0, Hard 1.0/1.0 —
  `Blueprints/NewGameModes/*`).
- Satisfaction: consuming a good applies its `ConsumptionEffects` (`Blueprints/Goods/*`), e.g.
  Water: Thirst +0.33; Carrot: Hunger +0.3, Carrots-need +0.2; Bread: Hunger +0.3, Bread +0.2.
  Effects are scaled by `NeedSpec.Effectiveness` (1.0 for all beaver needs).
  Non-`Wastable` needs (Hunger/Thirst/Sleep and all Nutrition foods are wastable=false…
  note: raw basic needs false, Nutrition foods true) — `Need.NonWastingEffectCount` prevents
  consuming items whose points would overflow the bar.
  **Daily consumption on Normal: ≈ 0.8/0.3 ≈ 2.67 food units and 0.7/0.33 ≈ 2.12 water per
  beaver per day.**
- Sleep: `Timberborn.SleepSystem/Sleeper`. Sleeping in housing applies the dwelling's
  `SleepEffects` (`DwellingSpec` in every housing blueprint): Sleep +0.2/h, Shelter +0.3/h.
  Sleeping outside (`SleeperSpec.SleepOutsideEffects` on the beaver blueprint): Sleep +0.15/h
  only (no Shelter), and collapsing from exhaustion (critical sleep) runs at 0.66× rate
  (`Sleeper.ToSleepEffects`). Wake-up: at next daytime start (or when the Sleep bar would be
  full) + random 0–0.25 h offset. Full night 8 h in a house ≈ +1.6 Sleep (bar is 1.0 wide from
  −0.2 to 0.8, so a full night always fills it; decay 0.6/day means one skipped night hurts).
- Social/Fun/Aesthetics/Awe needs are filled by visiting or being in range of the matching
  building (need id = building; `Timberborn.NeedApplication/AreaNeedApplier` for passive auras,
  attraction enter effects for others). Aesthetics decay fast (−0.3..−0.4/day) but are ambient;
  Fun/Social decay slower (−0.1..−0.2/day) but need visits.
- Behavior choice: when idle/needy, the AI appraises actions:
  `Need.TryAppraise` → `points × ImportanceMultiplier × (1 + PointsToMax × 0.1)`
  (`Need.ApplyAttractiveness`). Hunger/Thirst/Sleep have ImportanceMultiplier 3.0–3.5 so they
  dominate; Coffee/Books 1.25; everything else 1.0.
- Unfavorable-need penalties: `Timberborn.NeedBehaviorSystem/NeedPenaltyManager` adds the
  `PunitiveNeedSpec.Penalties` bonus deltas while the need is unfavorable (see table in §1).

---

## 3. Work, workday, movement

### Work speed
- Recipe work: `Timberborn.Workshops/ProduceExecutor.Tick` calls
  `manufactory.IncreaseProductionProgress(deltaTimeInHours × worker.WorkingSpeedMultiplier)`.
  A recipe finishes when progress ≥ `RecipeSpec.CycleDurationInHours` (from `Blueprints/Recipes/*`).
- `Timberborn.WorkSystem/Worker.Tick`: `WorkingSpeedMultiplier = BonusManager.Multiplier("WorkingSpeed")`
  = 1.0 + wellbeing tier delta (§1) − 0.5 if Hunger unfavorable (PunitiveNeedSpec), etc.,
  clamped [0.05, 1000]. **Wellbeing is the dominant productivity lever: wb 25 = 2× base output,
  wb 50 = 3×.**
- Buildings can grant on-the-job bonuses via `WorkSystem/WorkplaceBonuses` (`WorkplaceBonusesSpec.WorkerBonuses`,
  data-driven per building blueprint).

### Workday
`Timberborn.WorkSystem/WorkingHoursManager`:
- `_startHours` = daytime start = hour 0. Default `WorkedPartOfDay` = daytime length / 24 =
  16/24, i.e. **default workday = 16 h (hours 0–16)**. `EndHours = start + WorkedPartOfDay*24`.
- `AreWorkingHours` = `start ≤ HoursPassedToday < EndHours`. Player-adjustable via the UI
  (sets `WorkedPartOfDay`); persisted in save. Worker types with
  `WorkerTypeSpec.IgnoresWorkingHours` (bots) work around the clock
  (`WorkSystem/WorkerWorkingHours`).

### Walking speed
`Timberborn.WalkingSystem/WalkerSpeedManager` + character blueprints:
- BeaverAdult: `BaseWalkingSpeed 2.7` m/s, `BaseSlowedSpeed 1.35`; BeaverChild: 1.35 / 0.65;
  Bots: 2.7 / 1.35 (`Blueprints/Characters/*`).
- Actual speed = base × `BonusManager.Multiplier("MovementSpeed")` (wellbeing tiers, need
  penalties; clamp [0.25, 2.0]).
- Carrying anything ⇒ slowed base (1.35): `Timberborn.Carrying/GoodCarrier.IsMovementSlowed = IsCarrying`.
  (Haulers effectively move at half speed while loaded.) Adult carry capacity =
  `BaseLiftingCapacity 14` × CarryingCapacity multiplier (`GoodCarrier.LiftingCapacity`).
- Swimming: −0.3 to the movement multiplier, floor at multiplier 0.25
  (`WalkerSpeedManager.GetWalkerSpeedAtCurrentPosition`, `SwimmingPenalty = 0.3f`).

### Do paths make beavers faster?
Mostly **no** — paths are about connectivity, not raw speed:
- Movement speed along a path segment = `distance/cost` of the nav-mesh edge
  (`Timberborn.Navigation/FlowFieldPathNode.NormalizedSpeed`, used as a multiplier in
  `Timberborn.CharacterMovementSystem/PathFollower.GetMovementSpeed`).
- Flat terrain edges: cost = horizontal distance (`NavMeshEdge.CreateDefault`), i.e. speed ×1.
  Dirt Path edges: cost 1.0 per tile (`Blueprints/Buildings/Paths/Path/Path.blueprint.json`,
  `BlockObjectNavMeshSettingsSpec.EdgeGroups[].Cost`), i.e. also ×1. **Walking on a path is the
  same speed as walking on flat open ground.**
- What paths DO: they are `IsPath/IsRoad` edges forming the *road* nav-mesh used for district
  connectivity, building doorstep links, hauling routes and district range
  (`Timberborn.Navigation/DistrictRoadFlowFieldGenerator`, `RoadAStarPathfinder`); they also
  cross obstacles (fields/forests block straight lines) and change elevation via stairs.
- Genuinely faster transport is data-driven per building: Iron Teeth Tubeway edge cost 0.25
  → **4× speed** (matches `MovementSpeedBoostingBuildingSpec.BoostPercentage: 300`);
  Folktails Zipline `BoostPercentage: 150` (2.5×, zipline system computes its own path speed).
  Stairs have some 0.4-cost edges to compensate vertical geometry.

---

## 4. Growth: crops, trees, soil moisture

### Growth timer
`Timberborn.Growing/Growable`: a single `ITimeTrigger` of length `GrowableSpec.GrowthTimeInDays`
(game-time; no moisture speed scaling). Growth **pauses** while the resource is dying
(`DyingNaturalResource.StartedDying`) and resumes when re-watered. There is no partial growth
speed from moisture level — moisture is binary alive/dying for growth purposes.

### Drying out and death
- `Timberborn.SoilMoistureSystem/DryObject`: dry ⇔ `SoilMoistureService.SoilIsMoist(coords)`
  is false ⇔ moisture level == 0 (`SoilMoistureService.SoilIsMoist`: `> 0f`).
- `Timberborn.NaturalResourcesMoisture/WateredNaturalResource`: when dry, a death timer of
  `DaysToDieDry × random(0.9, 1.1)` runs; if re-moistened before it fires, the timer **resets**.
  When it fires → `LivingNaturalResource.Die` (dead plant, yields lost / must be cleared).
- Flooding: `FloodableNaturalResourceSpec` (per resource): dies after `DaysToDie` submerged
  above `MaxWaterHeight` (e.g. Carrot: 2 days, max height 0).

### Growth times & drought tolerance (`Blueprints/NaturalResources/**`)
| Resource | GrowthTimeInDays | DaysToDieDry |
|---|---|---|
| Dandelion 3, Kohlrabi 3, Carrot 4 | 3–4 | 8 / 2 / 2 |
| Cassava 5, Sunflower 5, Potato 6 | 5–6 | 3 / 3.5 / 1 |
| Cattail 8, Soybean 8, Canola 9, Coffee bush 9 | 8–9 | 1 / 0.25 / 0.5 / 8 |
| Corn 10, Wheat 10 | 10 | 2 / 0.5 |
| Blueberry 12, Eggplant 12, Spadderdock 12 | 12 | 9 / 1 / 0.3 |
| Birch 7, Mangrove 10, Pine 12 | 7–12 | 11 / 6 / 13 |
| Chestnut 23, Maple 28, Oak 30 | 23–30 | 8 / 12 / 15 |

Yields/harvest are per-blueprint (`CuttableSpec/GatherableSpec.Yielder.Yield`, e.g. Carrot →
3 Carrots, `RemovalTimeInHours 1.2`; work time scales with WorkingSpeed multiplier).
Planting time also data-driven (`PlantableSpec.PlantTimeInHours`, Carrot 0.2 h).

### Soil moisture / irrigation model
`Timberborn.SoilMoistureSystem/MoistureCalculationTask` + `Configurations/SoilMoistureSimulator.blueprint.json`:
- Water tiles get a source moisture = `2 × clusterSaturation`. `clusterSaturation` grows with
  the size of the contiguous water body: each water tile counts 8-neighbour watered tiles + 1
  (`WateredNeighborsCountingTask`), then takes max(own, best-neighbour − 1) capped at
  `MaxClusterSaturation 8` (`ClusterSaturationCalculationTask`). So a big lake/river reaches
  saturation 8 → source moisture 16; a 1-tile puddle only 1 → moisture 2.
- Land moisture = max over neighbours of (neighbour moisture − cost), cost 1.0 orthogonal /
  1.414 diagonal per tile, and −`VerticalSpreadCostMultiplier 6` per level of height difference.
  Moisture rises by ≤ `MoistureSpreadingRate 6.66`/tick and decays by `MoistureDecayRate 1.25`
  /tick when the source disappears (fast: dries within ~13 ticks of losing water).
  Contamination scales moisture down; ≥ `MaximumWaterContamination 0.53` ⇒ no irrigation.
- **Practical irrigation radius from a large (saturation-8) water body ≈ 15 tiles** on flat
  ground (moisture 16 minus ~1/tile, floor 0.01), less around corners/up levels.
- Evaporation feedback: small water bodies evaporate faster. Modifier per water tile =
  `0.0595·n² + 0.101·n + 0.72` with `n = 10 − clusterSaturation`
  (`SoilMoistureSimulationTaskStarter.InitializeEvaporationModifiers`): sat 8 → ×1.16,
  sat 1 → ×6.45.

---

## 5. Science

- Storage: `Timberborn.ScienceSystem/ScienceService` (plain int SciencePoints, add/subtract).
- Generation: **only via recipes** with `ProducedSciencePoints`
  (`Timberborn.Workshops/Manufactory` line calling `_scienceService.AddPoints(CurrentRecipe.ProducedSciencePoints)`
  on cycle completion). Data (`Blueprints/Recipes/…`):
  - `Recipe.SciencePoints`: 1 SP per 1.0 h cycle — used by **Inventor** (both factions),
    1 worker, costs 12 Logs (`Blueprints/Buildings/Science/Inventor/*`).
  - `Recipe.SciencePointsObservatory`: 10 SP per 3.0 h cycle — **Observatory** (Folktails),
    4 workers (each producing independently), science cost 1000.
  - `Recipe.SciencePointsNumbercruncher`: 10 SP per 1.0 h cycle — **Numbercruncher** (Iron Teeth,
    bot-crewed), science cost 1500.
- Effective rate scales with the worker's WorkingSpeedMultiplier (cycle progress =
  hours × multiplier, §3). So one Inventor at wellbeing 0 makes ~16 SP per 16-h workday;
  at wb 25 (2×) ~32 SP/day.
- Science is spent to unlock buildings (`BuildingSpec.ScienceCost` per building) via
  `ScienceSystem/BuildingUnlockingService`; some ruins refund SP
  (`Timberborn.Demolishing/DemolishableScienceReward`).

---

## 6. Water, pumps, drought

### Pumps (data-driven recipes, `CycleDurationInHours` scaled by WorkingSpeed)
- Water Pump (both factions): recipe `Water` — 1 Water per 0.33 h, 1 worker, MaxDepth 2
  (`Blueprints/Buildings/Water/WaterPump/*`, `Blueprints/Recipes/Recipe.Water.blueprint.json`).
  ≈ 48 water per 16-h day at 1× work speed.
- Large Water Pump (Folktails): recipe `Water.Efficient` — 5 Water per 1.0 h, 3 workers,
  MaxDepth 4. (Each worker runs cycles; nominal 5/h per active worker cycle.)
- Deep Water Pump (Iron Teeth): recipe `Water`, MaxDepth 6, 1 worker.
- Pump intake depth = `WaterInputSpec.MaxDepth`; needs water at the intake tile.

### Water simulation (`Configurations/WaterSimulator.blueprint.json`, `Timberborn.WaterSystem`)
- Cellular column simulation; per-substep flow factor `WaterFlowFactor 2.25`,
  outflow balancing 0.8, spill threshold 0.1 (`WaterSimulationTaskStarter`).
- Evaporation (`WaterParametersUpdateTask` ~line 251): depth < 0.02 ⇒ `FastEvaporationSpeed
  0.001`/s, else `NormalEvaporationSpeed 0.0001`/s, × the cluster-size modifier from §4.
  At 460.8 sim-seconds/day, a large open body loses ≈ 0.046 depth/day (×1.16 ⇒ ~0.053);
  small ponds several× faster.

### Weather cycle & drought duration
`Timberborn.WeatherSystem/WeatherService` + `GameCycleSystem/GameCycleService`:
- A cycle = temperate days then hazardous days; hazard starts on cycle day
  `TemperateWeatherDuration + 1`, ends at cycle end.
- Temperate duration: uniform int in `[Min, Max]` from game mode
  (`TemperateWeatherDurationService.GenerateDuration`): Easy 16–19, Normal 13–17, Hard 5–8.
- Hazard type: `HazardousWeatherRandomizer.GetRandomWeatherForCycle` — Badtide iff
  `cycle > CyclesBeforeRandomizingBadtide` (Easy 5, Normal 4, Hard 3) and rand <
  badtide chance (0.4 all modes), with a streak-damping rule: after a streak of the same
  hazard, chance^(streak+1) < 0.05 ⇒ chance halved, < 0.025 ⇒ forced switch
  (`GetModifiedBadtideChance`). Otherwise drought.
- **Duration formula** (`DroughtWeather.GetDurationAtCycle`, identical in `BadtideWeather`):
  ```
  handicap(cycle) = lerp(HandicapMultiplier, 1.0, clamp01((cycle-1)/HandicapCycles))
  duration = round( uniform(handicap*MinDuration, handicap*MaxDuration) ), min 1 if MinDuration>0
  ```
  Normal mode drought: Min 5, Max 9, HandicapMultiplier 0.38, HandicapCycles 5 →
  cycle 1: uniform(1.9, 3.42) ≈ 2–3 days; cycle 3: ~3–6; cycle 6+: 5–9 days.
  Normal badtide: Min 4, Max 8, handicap 0.15 over 5 cycles.
  Hard drought: 15–30 days (handicap 0.2 over 12 cycles). Easy: 2–4 (0.25 over 8).
- During drought, water sources stop and evaporation drains everything; irrigated moisture
  then decays (fast, §4), crops start their `DaysToDieDry` timers.

---

## 7. Population: reproduction, aging, death

### Reproduction — Folktails (housing-based)
`Timberborn.Reproduction/ProcreationHouse` (component on housing):
- Trigger: whenever a beaver **enters** the dwelling (`Enterable.EntererAdded`, i.e. typically
  nightly at bedtime).
- Conditions (all must hold):
  1. `Dwelling.HasFreeChildSlots` — dwelling has space and fewer children than ChildSlots;
     `ChildSlots = floor(MaxBeavers/3)`, `AdultSlots = MaxBeavers − ChildSlots`
     (`Timberborn.DwellingSystem/Dwelling.Awake`).
  2. `Dwelling.UnderpopulatedByChildren` — children in dwelling < `NumberOfAdultDwellers / 2`
     (integer division!) — **so a dwelling needs ≥ 2 assigned adults for any kits**.
  3. The entering beaver has a `Procreator` and **no need in critical state**; and at least one
     *other* adult dweller in the house also satisfies that (`CanProcreate`, `HasDwellerToProcreateWith`).
  4. Random roll: `DailySpawningChance = 0.1875` (18.75% per qualifying entry).
- Newborn spawns at the building entrance, is assigned to that dwelling and district
  (`NewbornSpawner.SpawnChild`).
- Housing capacities (`DwellingSpec.MaxBeavers`): MiniLodge 1 (never breeds), Lodge 3
  (1 child slot), DoubleLodge 6 (2), TripleLodge 9 (3); Iron Teeth: Rowhouse 5, LargeRowhouse 8,
  Barrack 10, LargeBarrack 16.

### Reproduction — Iron Teeth (Breeding Pods)
`Timberborn.Reproduction/BreedingPod`: consumes `NutrientsPerCycle` each 1.0-day cycle,
5 cycles per beaver (`Blueprints/Buildings/Housing/BreedingPod`: 1 Water + 1 Berries per cycle,
spawns a **child**; AdvancedBreedingPod: 1 Berries + 1 Extract per cycle, `SpawnAdults: true`).
Progress halts if nutrients missing (timer simply doesn't restart until stocked).

### Aging
- `Timberborn.LifeSystem/LifeService` (`Configurations/LifeService.blueprint.json`):
  `AverageLifespan 50` days, `DaysOfChildhood 6`.
- Child → adult: `Timberborn.Beavers/Child` accumulates
  `FixedDeltaTimeInHours / (6·24) × GrowthSpeed multiplier` per tick — childhood is 6 days at
  wellbeing < 5, down to ~3.2 days at high wellbeing (GrowthSpeed max relevant tier +0.9).
- Old age: `LifeSystem/LifeProgressor.Tick`: `LifeProgress += (tickHours/24)/AverageLifespan
  ÷ LifeExpectancyMultiplier`. Dies when `LifeProgress > ExpectedLongevity`, where
  `BeaverLongevity.ExpectedLongevity` = uniform(0.9, 1.1) rolled at birth.
  **Effective lifespan = 50 × uniform(0.9,1.1) × (1 + LifeExpectancy tier delta) days** —
  e.g. wb 47 ⇒ ×2.0 ⇒ ~90–110 days.
- New-game starting pop (`Blueprints/NewGameModes/*`): 9 adults + 4 children, with random
  initial age progress (adults 10–70% of adulthood, children 10–80% of childhood).

### Death conditions
- Starvation/dehydration: lethal need at MinimumValue (−3), see §2 — `MortalSystem/MortalNeeder`.
- Old age: `LifeProgressor.ShouldDie` (above).
- Drowning/badwater etc. handled by separate killers (`MortalSystem/CharacterKiller` callers).
- On death: corpse persists 0.75–1.75 h then despawns (`Mortal.SetBodyDisappearanceTimestamp`);
  the beaver immediately stops counting toward wellbeing average (§1) and vacates jobs/housing.

---

## 8. New-game gates: faction unlock

- `Timberborn.FactionSystem/FactionUnlockingService`: unlock state is a **player-data flag**
  (`FactionUnlocked_<Id>` via `IPlayerDataService`) — global across saves, not per-save.
- The only gameplay caller is `Timberborn.FactionGoalsSystem/FactionGoalsUnlocker.Tick`
  (runs **every tick** in a game session): for each faction with an `UnlockableFactionSpec`
  that is still locked, it unlocks when
  ```
  FactionService.Current.Id == spec.PrerequisiteFaction
  && WellbeingService.AverageGlobalWellbeing >= spec.AverageWellbeingToUnlock
  ```
- Data (`Blueprints/Factions/Faction.IronTeeth.blueprint.json`): PrerequisiteFaction
  "Folktails", `AverageWellbeingToUnlock: 15`.
- The compared number is the **instantaneous, colony-wide (all districts) average beaver
  wellbeing, rounded to nearest int**, recomputed each tick (§1). It only needs to touch 15
  for a single tick — e.g. right after everyone eats/sleeps — and the unlock is permanent
  (persisted in player data). Bots are excluded from the average; children are included.
- Dev/debug paths also exist (`UnlockAllFactions` / `LockAllFactions`).

---

## Uncertainty / not verified here

- Slider bounds for player-adjustable working hours (UI assembly not inspected; engine accepts
  any `WorkedPartOfDay`, default 16/24).
- Exact hauling/builder behavior trees, district import/export rules and crop-yield bonus
  buildings (Fertilizer etc.) were out of scope.
- `Wastable` flags: Hunger/Thirst/Sleep are `Wastable: false` (no overfill waste); most
  building-satisfied needs `true`; per-need values in the table are from the blueprints and
  can be re-checked in `Blueprints/Needs/*.json`.
- Attraction/aura ranges (`RangedEffectSystem`) and per-building need-application rates are
  data-driven per building blueprint and not tabulated here.
