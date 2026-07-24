# Timberborn Max-Efficiency Strategy Compendium (v1.0 era, Updates 6–7 + 1.0, 2025–2026 meta)

Data compiled from timberborn.wiki.gg (official wiki), Fandom wiki, Steam guides, timberborn.org, FinalBoss, and community discussions. Numbers below are Normal/Hard difficulty unless noted.

---

## 0. Core numeric constants (foundation for all planning)

**Per-beaver daily consumption (Normal/Hard):**
- Food: **2.67/day** (hunger depletes 80%/day; each food item restores 30% → 80/30)
- Water: **2.12/day** (thirst depletes 70%/day; each water restores 33% → 70/33)
- Easy mode is 40% of this: 1.07 food, 0.85 water
- Planner formulas: `food_storage_needed = pop × drought_days × 2.67`; `water_storage_needed = pop × drought_days × 2.12`; community practice adds a **25% safety buffer**

**Weather cycle (Hard):** cycle = 1 temperate + 1 hazard season. Hazard duration starts <6 days, ramps over ~12 cycles to a cap of **30 days**. Default hazard split: **60% drought / 40% badtide**; after 5 consecutive droughts badtide chance rises to 70%; after 7, badtide is guaranteed — plan for badtide defense by mid-game no matter what.

**Water pumps:** basic pump ≈ 3 water/hr, **~40–45/day practical** → 1 pump per ~20 beavers. Folktails Large Water Pump (400 SP) ≈ 5 basic pumps (~240/day). Iron Teeth Deep Water Pump reaches **6 tiles deep** vs Folktails' 2 — the single biggest faction asymmetry.

**Housing cost efficiency (Folktails):** 3-bed lodge 12 logs (4/bed), **6-bed lodge 20 logs (3.33/bed — cheapest)**, 9-bed 35 logs (3.9/bed).

**Science:** no tech tree — everything purchasable from day 1 if you have SP. Inventor: free unlock, 1 worker. Observatory (FT): 1,000 SP, 200 hp, 4 workers. Numbercruncher (IT): 1,500 SP, 500 hp. Early workplaces cost **250 SP**; Gatherer Flag 150 SP; Large Water Pump 400 SP; Aquatic Farmhouse 1,500 SP; **Bot Assembler 10,000 SP** (top of the ladder).

---

## 1. Faction comparison for pure optimization

**Verdict: Iron Teeth for max growth/late-game scaling; Folktails for forgiving early game.**

Why IT wins on optimization:
- **Deterministic population control.** Breeding Pods convert 5 berries + 5 water + 5 days → 1 kit, pausable at will. Population becomes a controllable input variable rather than a stochastic function of housing/wellbeing — critical for an automated planner. Folktails breeding is RNG conditioned on shared housing + leisure.
- **Deep Water Pump (6 deep)** → narrow deep reservoirs, far better volume-per-surface-area (less evaporation), trivializes 30-day droughts.
- **Engines (200 hp, log-fueled)** = constant, on-demand power vs Folktails' variable wind; only ~110% capacity overprovision needed vs 130–150% for wind/water.
- Stackable storage, tubeways (omnidirectional, underwater/vertical transit), stronger badwater-extract economy (Grease recipe).

Folktails advantages: free food from berries/mushrooms early, Beehives (**−30% crop growth time**, ≈43% faster), wind power with zero fuel, easier no-attention sustainability. Best for the first 15 cycles on easy terrain; IT overtakes hard from mid-game.

## 2. Opening build order (days 1–20, ~day-granular)

- **Day 0 (paused):** survey map — river, berry clusters, forest, future dam/reservoir site.
- **Days 1–2:** Log Pile near trees → **2–3 Lumberjack Flags** → **1 Water Pump + Small Water Tank** on the river → **1 Gatherer Flag** on berries. Paths connecting all. Worker split for 9 starting beavers: ~3 logging, 1–2 water, 1–2 gathering, rest building.
- **Days 2–4:** first farm plot — **Carrots (FT) / Kohlrabi (IT)**, 1 Farmhouse, ~25–50 tiles zoned. **IT: build Breeding Pod day 3–4 and keep it fed** (berries+water). **Forester by day ~5** — mandatory, or you hit a wood crisis in mid-game (oaks/pines take 10–30 days).
- **Days 4–7:** 2nd water pump + 2nd small tank (target ≥2 tanks); Inventor (1 worker, run continuously forever); first **Dam row** across the river to raise water level and extend irrigation.
- **Days 8–10:** **Lumber Mill + power** (FT: Windmill; IT: Power Wheel then Engine). Planks gate nearly all tier-2 buildings. First housing block (FT: 6-bed lodges — cheapest logs/bed).
- **Days 10–15:** buy **Floodgates (~250 SP)**; complete main dam with 2–3 floodgate segment; 3rd–4th water tank. **Pre-first-drought checkpoint: ≥100 water, ≥3 days food stored for current pop** (10 beavers × 2.12 ≈ 21 water/day).
- **Days 15–20:** scale farms to ~1.5× consumption; Grill/processing building; second Inventor if labor allows; begin reservoir excavation planning (Dynamite unlock).
- **Districts:** do NOT split early. Second district (District Crossing since Update 6 — Distribution Posts are obsolete; paths extend infinitely) only when first district has **30–50 beavers, stable food/water, ≥100 logs + 50 planks banked**, and you need reach to a remote resource (ruins, farming basin).

Heuristics:
- IF day < 5 AND no water pump running THEN highest priority = pump.
- IF planks = 0 by day 10 THEN divert all labor to Lumber Mill chain.
- IF pop grows > food production/2.67 THEN pause Breeding Pods (IT) / stop building housing (FT).

## 3. Water & drought/badwater management

- **Storage targets:** `pop × 2.12 × longest_expected_drought × 1.25`. Examples: 25 beavers/20-day drought → ~1,400 water; 30 beavers/30-day → ~2,530. Tanks alone don't scale — the real answer is **reservoirs**: dam the river, then Dynamite-excavate the basin floor (each terrain block removed below waterline = +1 tile³ storage). IT digs 6 deep (Deep Pump), FT effectively 2 (or use Fluid Dumps/pumping cascades).
- **Deep > wide**: evaporation scales with surface area — narrow deep reservoirs retain far more through a 30-day drought.
- **Badtide defense (layered):** upstream Dam/Levee wall with **Floodgates closed during badtide**; a **bypass/diversion channel** blasted with Dynamite routing badwater around the colony; **Sluices** (Update 6+) as the automation layer — one-way valves that auto-close on contamination or open by depth threshold, making badtides hands-off. Update 6 also added dams at levee bottoms and aqueducts; 1.0 added **Badtide Drains, Water Seeps, Aquifers** and 20+ automation buildings (Sensors, Timers, Relays) enabling fully automated floodgate logic.
- **Fluid Dump:** dispenses 3 water/hr onto terrain — use to irrigate isolated farm pockets or keep a green zone alive during drought; also dumps badwater (disposal).
- Heuristics:
  - IF drought forecast AND stored_water < pop × 2.12 × drought_len × 1.25 THEN add pump shifts + close floodgates to top reservoir NOW.
  - IF badtide incoming THEN close main floodgates, open bypass, pause water wheels on contaminated channels, set Sluices to auto-contamination mode.
  - IF 5+ consecutive droughts have occurred THEN assume badtide next cycle (70%→100%).

## 4. Food economy

Per-tile-per-day efficiency (wiki.gg; parens = with Beehive, FT only):

| Folktails | Days | Food/tile/day | | Iron Teeth | Days | Food/tile/day |
|---|---|---|---|---|---|---|
| Carrot | 4 | 0.75 (1.07) | | Kohlrabi | 3 | 0.67 |
| Sunflower | 5 | 0.40 (0.57) | | Cassava | 5 | 0.50 |
| Potato→Grilled | 6 | 0.67 (0.95) | | Soybean→Fermented* | 8 | 0.83 |
| **Wheat→Bread** | 10 | **1.50 (2.14)** | | Corn→Ration | 10 | 1.00 |
| **Cattail→Cracker** | 8 | **1.50 (2.14)** | | **Eggplant→Ration*** | 12 | **1.50** |
| Spadderdock | 12 | 0.75 (1.07) | | Canola (oil input) | 9 | — |

*needs Canola Oil co-input. (A popular Steam math guide gives slightly lower raw numbers; treat wiki.gg as canonical but keep ±20% margin.)

- **Opening crop:** Carrot (FT) / Kohlrabi (IT) — fastest, raw-edible, drought-resilient because short cycles waste less standing crop.
- **Scaling crop:** Wheat→Bread and Cattail→Cracker (FT), Eggplant/Soybean chains (IT) — 2× the tile efficiency but require processing, power, labor; switch once planks/power are stable (~cycle 3–5).
- **Farmhouse ratios:** ~50 tiles/farmhouse for fast crops, 100+ for slow crops. **Beehive covers 7×7 (~39–48 effective tiles): 1 hive per farm block, non-negotiable for FT.**
- **Food variety** fills the Food need tiers → wellbeing; run 2–3 food types minimum, add luxury foods for top-end wellbeing.

## 5. Wellbeing optimization

Milestone bonuses scale in +20% steps —
- **Work speed (adults): up to +260% at WB 72+**
- Movement speed: up to +120% (compounds by cutting commute time)
- Kit growth speed: up to +70%; life expectancy: up to +75% (at WB 61+)

This is the strongest multiplier in the game: *100 beavers at 260% work speed out-produce 250 at base while eating 60% less.* Wellbeing is THE optimization target after survival is secured.

ROI ordering: 1) **shorter work hours** (free — set **12–14h**, never >16; FT stop breeding when exhausted at 18h+), 2) food variety + water quality, 3) cheap Fun/Social buildings (campfires, rooftop terraces), 4) Spirituality/Decor tiers, 5) commute reduction (dense housing next to jobs; Update 7 **ziplines (FT) / tubeways (IT)** slash travel time — build once colony spans >50 tiles).

Heuristic: IF avg wellbeing gain of next building ≥ 1 milestone for N workers THEN it beats adding ~0.2×N beavers of raw labor.

## 6. Population growth mechanics

- **Folktails:** kit born when 2 adults share a dwelling with a free child slot + leisure time + favorable wellbeing. Control pop via number of beds/lodges. Rule of thumb: at 60-day lifespan expect ~1 kit per 12 adults in steady state → build ~10% extra beds over target workforce.
- **Iron Teeth:** Breeding Pod = **5 berries + 5 water + 5 days → 1 kit**, fully pausable. Optimal curve: run pods flat-out during temperate seasons while `projected_pop × 2.67 ≤ food_production` and `× 2.12 ≤ water_production`; **pause pods 5+ days before any hazard season**.
- Work hours: 12–14h optimum; 24h only for short emergency pushes (drought pump crunch), then revert.
- Heuristic: IF (stored_food_days < 1.5 × next_hazard_len) OR (stored_water_days < 1.5 × next_hazard_len) THEN halt growth (pause pods / stop housing).

## 7. Industry chains & science

- Core chain: Logs → (Lumber Mill) Planks → (Gear Workshop) Gears → Treated Planks (planks + tar/resin) → Metal Blocks. Keep ~2 Lumberjack flags + sustained Forester coverage per Lumber Mill; 1 Gear Workshop serves several consumers early.
- **Science strategy:** 1 Inventor from day 1, second by day ~15, scale to 3–4 by cycle 5; replace with Observatory (FT, 1,000 SP) / Numbercruncher (IT, 1,500 SP) once you have 200/500 hp spare.
- **Unlock priority (typical costs):** Gatherer 150 → Floodgate/basic industry ~250 each → Large Water Pump 400 (FT) → Dynamite → Sluice → Aquatic Farmhouse 1,500 (FT) → mines/heavy industry → **Bot Assembler 10,000** endgame rush.
- Rush order rule: survival unlocks (floodgates, storage, water) > farming efficiency > dynamite (reservoir) > wellbeing > bots.

## 8. Power

| Source | HP | Notes |
|---|---|---|
| Power Wheel | 50 | 1 worker; constant |
| Water Wheel | up to ~180 | scales with flow; funnel into 1-tile channels; dies in droughts/closed floodgates |
| Windmill (FT) | ~120 peak | variable; Large Windmill 300 peak (~144 avg) |
| Engine (IT) | 200 | burns logs; constant |
| Large Water Wheel (IT) | >180 | best-in-class with engineered flow |

- Capacity rule: **variable sources → build 130–150% of demand + Gravity Batteries; constant sources → 110%.**
- **Gravity Battery:** 4,000 hph flat, **+2,000 hph per tile of drop**, max 62,000 hph; recharge overnight with idle Power Wheels/wind. Batteries + wind = FT's answer to drought.
- IT ratio: 1 Engine ≈ 3–4 workshops; keep a log buffer = engine_count × burn_rate × hazard_len.

## 9. Late game: metal, bots, badwater economy, logistics

- **Metal:** surface Ruins (Scavenger Flag) are finite — strip them first. Then **Mine on Underground Ruins = infinite scrap**: FT Mine consumes 1 Gear + 1 Treated Plank → 2 Scrap (build: 250 logs/200 treated planks/350 gears); IT **Efficient Mine** (300 TP/450 gears/300 logs, 10 workers) feeds **~2.77 Smelters**. **Smelter: 2 Scrap → 1 Metal Block per 2h.** Mines injure beavers → prime bot jobs.
- **Bots:** cost ≈ 2 Metal Blocks + 6+ Planks + 3 Gears + limbs + 36h assembly; run **24/7 with no food/water/housing** → 1 bot ≈ 2–3 beaver-equivalents with zero consumption. Priority bot placements: **water pumps (pump through droughts), mines/excavation (injury-immune), night-critical industry, haulers.** Precondition: stable input flows.
- **Badwater economy:** Badwater Rig extracts year-round → **Centrifuge: 5 badwater + logs → 1 Extract (2h, 200 hp)**; **Explosives Factory: 5 badwater → 1 Explosive (3h, 150 hp)** for mass dynamite/terraforming; Extract + Maple Syrup → Catalyst; IT: 1 Canola Oil + 1 Extract → **2 Grease**. IT should run badwater industry on a deliberately separate hydro-network from drinking/irrigation.
- **Logistics (Update 7):** FT ziplines / IT tubeways move beavers AND goods across any height — massive commute-time (=wellbeing =work speed) wins; 3D terrain/overhangs/tunnels enable hanging farms and compact vertical cities.

## 10. Benchmarks

- **"Rush B-eaver!" achievement:** wonder before **cycle 15** — the de facto speedrun target; minimal pop (~20–30), all surplus into planks/gears/metal.
- Community "mastery bar": **100+ beavers on Hard surviving 30-day droughts** (100 × 2.12 × 30 ≈ 6,360 water, 100 × 2.67 × 30 ≈ 8,000 food — reservoir + irrigated aquatic farms mandatory).
- Max-pop play = IT breeding pods + bot-staffed water/food + multi-district; practical cap ~1,000 (performance).

## 11. Planner-ready heuristic summary

```
CONSTANTS: food/beaver/day=2.67, water/beaver/day=2.12, buffer=1.25
IF cycle<2: build order = pump→logging×3→gatherer→carrot|kohlrabi→forester→inventor
IF planks==0 AND day>8: priority=lumber_mill+power
IF stored_water < pop*2.12*next_drought*1.25: expand pumps, close floodgates, pause pop growth
IF consecutive_droughts>=5: prepare badtide (sluices auto, bypass channel open)
IF pop>=30 AND logs>=100 AND planks>=50 AND resource_out_of_reach: new district (Crossing)
IF wellbeing<milestone AND survival_secured: next_build = cheapest unmet-need building
IF work_hours>14 AND not emergency: set 12–14
IF surface_ruins_depleted: build mine (ratio mine:smelter ≈ 1:2.77 IT)
IF SP>=10000 AND metal_chain_stable: rush bots → staff pumps/mines first
```

**Key open data gaps** (fill by datamining specs from game files at runtime): exact SP cost table per building, per-building construction costs, Inventor SP/hour rate, windmill wind uptime distribution.
