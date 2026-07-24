# Timberborn Autopilot

A self-playing, max-strategy Timberborn platform: a C# game mod that senses the full game state every tick, plans with meta-strategy heuristics (see [docs/STRATEGY.md](docs/STRATEGY.md)), and acts — placing buildings, engineering water, managing the economy, and hunting achievements — while you watch at high speed.

**Game:** Timberborn 1.0.13.1 (Steam), Mono backend, official mod support.
**Verified:** this skeleton compiles against the game's assemblies and the mod-loading path is confirmed from decompiled game code.

## How it hooks in

- Official mod loading: folder in `Documents\Timberborn\Mods\Autopilot\` with `manifest.json` + DLL. The game loads every DLL and calls `IModStarter.StartMod()`.
- DI: `[Context("Game")]` `IConfigurator` classes are auto-discovered; our services constructor-inject any game singleton (placement, districts, weather, science, water sim...).
- Loop: `ITickableSingleton.Tick()` runs the sense → plan → act cycle in-sim.
- No BepInEx needed. Harmony patching is officially sanctioned if we ever need to intercept (community "Harmony" mod, Workshop ID 3284904751).
- Achievements are NOT disabled by mods (verified by decompiling `Timberborn.Achievements` — no modded-state gating; the `DateSalter` flag is analytics-only). Avoid the dev console during achievement runs anyway, out of caution.

## Architecture

```
┌────────────────────────── AutopilotService (ITickableSingleton) ─────────────────────────┐
│                                                                                          │
│  SENSE                      PLAN                              ACT                        │
│  WorldModel snapshot        GoalStack (utility AI):           Actuators:                 │
│  - stocks/goods             1. Survival (water/food/badtide)  - BuildPlacer (block objs) │
│  - population/wellbeing     2. Growth (pop, housing)          - ZonePlanner (crops/trees)│
│  - weather forecast         3. Science & unlocks              - PriorityManager (labor)  │
│  - water levels/moisture    4. Wellbeing milestones           - WaterEngineer (gates,    │
│  - power grid               5. Industry ratios                  dynamite, reservoirs)    │
│  - science points           6. Achievement objectives         - DistrictManager          │
│  - achievement progress     7. Wonder / endgame               - SpeedController          │
│                                                                                          │
│  CityPlanner: layout engine (road grid, housing blocks, farm blocks w/ beehive coverage, │
│  industry clusters near power, reservoir siting) — replans as terrain/economy changes    │
└──────────────────────────────────────────────────────────────────────────────────────────┘
```

- **Continuous replanning:** the planner re-evaluates every in-game day; nothing is a fixed script. Weather forecast changes, badwater source discovery, terrain changes from dynamite, and population curves all reshape the plan.
- **Water engineering module:** dam/floodgate placement across river cross-sections, reservoir excavation (dynamite below waterline), badtide bypass channels, sluice/gate automation via the official signal network, storage target = `pop × 2.12 × hazard_days × 1.25`.
- **Achievement campaign:** most achievements fall out of one max run; specialty runs are scheduled for conflicting ones (power-wheels-only / water-wheels-only / wind-only, maple-pastry-only, no-dwellings pop, beaver-extinction + bots, both factions' wonders, cycle-15 wonder rush). Campaign state persists across saves.

## Repo layout

- `src/AutopilotMod/` — the C# mod (netstandard2.1, references game DLLs directly)
- `mod/manifest.json` — mod manifest deployed alongside the DLL
- `deploy.ps1` — build + copy to `Documents\Timberborn\Mods\Autopilot`
- `docs/STRATEGY.md` — researched meta-strategy compendium driving the planner
- `docs/MODDING.md` — modding API facts (verified from decompiled game code + ecosystem research)

## Build & run

```powershell
.\deploy.ps1
```

Then launch Timberborn → Mod Manager shows "Autopilot" → enable → start/load a game. Heartbeat logs appear in `Player.log` (`%USERPROFILE%\AppData\LocalLow\Mechanistry\Timberborn\Player.log`).

## Roadmap

1. **v0.1 (done):** skeleton mod, DI + tick loop verified, research corpus.
2. **v0.2 Sense:** WorldModel — read stocks, population, weather, water sim, science, achievements; JSON status endpoint piggybacking the game's built-in HTTP API (`localhost:8080`) for a live dashboard.
3. **v0.3 Act primitives:** place building/path, zone crops/trees, set priorities, pause/unpause, buy science unlocks, set game speed (unclamped — `SpeedManager.ChangeSpeed` takes any float).
4. **v0.4 Opening book:** days 1–20 build order (pump → lumberjacks → gatherer → farm → forester → inventor → mill → dam) with site selection on arbitrary maps.
5. **v0.5 Water engineer:** dam/reservoir/bypass planning, drought/badtide survival loop.
6. **v0.6 Full economy:** industry ratios, wellbeing ladder, districts, bots, wonder.
7. **v0.7 Achievement campaign:** multi-run scheduler for the full 100%.

## Prior art worth borrowing from

- **ihsoft "Advanced Automation"** — IFTTT + scripting engine for in-game rules ([github.com/ihsoft/TimberbornMods](https://github.com/ihsoft/TimberbornMods))
- **Timberbot** — HTTP API mod (port 8085) built for LLM-driven play: validated placement, A* path building, ASCII maps ([github.com/abix-/TimberbornMods](https://github.com/abix-/TimberbornMods))
- **datvm's mods + modding guide** — Building Blueprints, Configurable Game Speed ([datvm.github.io/TimberbornMods/ModdingGuide](https://datvm.github.io/TimberbornMods/ModdingGuide/))
- **Official modding docs** — [github.com/mechanistry/timberborn-modding](https://github.com/mechanistry/timberborn-modding)
