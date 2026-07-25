# Timberborn Autopilot

A self-playing, max-strategy Timberborn platform: a C# game mod that senses the full game state every tick, plans with meta-strategy heuristics (see [docs/STRATEGY.md](docs/STRATEGY.md)), and acts â€” placing buildings, engineering water, managing the economy, and hunting achievements â€” while you watch at high speed.

**Game:** Timberborn 1.0.13.1 (Steam), Mono backend, official mod support.
**Verified:** this skeleton compiles against the game's assemblies and the mod-loading path is confirmed from decompiled game code.

## How it hooks in

- Official mod loading: folder in `Documents\Timberborn\Mods\Autopilot\` with `manifest.json` + DLL. The game loads every DLL and calls `IModStarter.StartMod()`.
- DI: `[Context("Game")]` `IConfigurator` classes are auto-discovered; our services constructor-inject any game singleton (placement, districts, weather, science, water sim...).
- Loop: `ITickableSingleton.Tick()` runs the sense â†’ plan â†’ act cycle in-sim.
- No BepInEx needed. Harmony patching is officially sanctioned if we ever need to intercept (community "Harmony" mod, Workshop ID 3284904751).
- Achievements are NOT disabled by mods (verified by decompiling `Timberborn.Achievements` â€” no modded-state gating; the `DateSalter` flag is analytics-only). Avoid the dev console during achievement runs anyway, out of caution.

## Architecture

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ AutopilotService (ITickableSingleton) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚                                                                                          â”‚
â”‚  SENSE                      PLAN                              ACT                        â”‚
â”‚  WorldModel snapshot        GoalStack (utility AI):           Actuators:                 â”‚
â”‚  - stocks/goods             1. Survival (water/food/badtide)  - BuildPlacer (block objs) â”‚
â”‚  - population/wellbeing     2. Growth (pop, housing)          - ZonePlanner (crops/trees)â”‚
â”‚  - weather forecast         3. Science & unlocks              - PriorityManager (labor)  â”‚
â”‚  - water levels/moisture    4. Wellbeing milestones           - WaterEngineer (gates,    â”‚
â”‚  - power grid               5. Industry ratios                  dynamite, reservoirs)    â”‚
â”‚  - science points           6. Achievement objectives         - DistrictManager          â”‚
â”‚  - achievement progress     7. Wonder / endgame               - SpeedController          â”‚
â”‚                                                                                          â”‚
â”‚  CityPlanner: layout engine (road grid, housing blocks, farm blocks w/ beehive coverage, â”‚
â”‚  industry clusters near power, reservoir siting) â€” replans as terrain/economy changes    â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

- **Continuous replanning:** the planner re-evaluates every in-game day; nothing is a fixed script. Weather forecast changes, badwater source discovery, terrain changes from dynamite, and population curves all reshape the plan.
- **Water engineering module:** dam/floodgate placement across river cross-sections, reservoir excavation (dynamite below waterline), badtide bypass channels, sluice/gate automation via the official signal network, storage target = `pop Ã— 2.12 Ã— hazard_days Ã— 1.25`.
- **Achievement campaign:** most achievements fall out of one max run; specialty runs are scheduled for conflicting ones (power-wheels-only / water-wheels-only / wind-only, maple-pastry-only, no-dwellings pop, beaver-extinction + bots, both factions' wonders, cycle-15 wonder rush). Campaign state persists across saves.

## Repo layout

- `src/AutopilotMod/` â€” the C# mod (netstandard2.1, references game DLLs directly)
- `mod/manifest.json` â€” mod manifest deployed alongside the DLL
- `deploy.ps1` â€” build + copy to `Documents\Timberborn\Mods\Autopilot`
- `docs/STRATEGY.md` â€” researched meta-strategy compendium driving the planner
- `docs/MODDING.md` â€” modding API facts (verified from decompiled game code + ecosystem research)

## Build & run

```powershell
.\deploy.ps1
```

Then launch Timberborn â†’ Mod Manager shows "Autopilot" â†’ enable â†’ start/load a game. Heartbeat logs appear in `Player.log` (`%USERPROFILE%\AppData\LocalLow\Mechanistry\Timberborn\Player.log`).

## Monitoring & remote control

All served by the game's built-in HTTP API (default `localhost:8080`, started automatically by the mod):

- `/api/autopilot/status` â€” full colony telemetry as JSON
- `/api/autopilot/brain` â€” the brain's decision feed + suggestions
- `/api/autopilot/dashboard` â€” live desktop dashboard (auto-refreshes every 2s)
- `/api/autopilot/remote` â€” mobile control pad: brain on/off, speed presets (pauseâ€“10Ã—), vitals, feed
- Command endpoints: `/build`, `/path`, `/zone`, `/cut`, `/pause`, `/workers`, `/priority`, `/speed`, `/survey`, `/templates`, `/auto`

**Phone access (home Wi-Fi):** forward a LAN port to the localhost API and allow it through the firewall (run once, admin):

```powershell
netsh interface portproxy add v4tov4 listenaddress=0.0.0.0 listenport=8081 connectaddress=127.0.0.1 connectport=8080
netsh advfirewall firewall add rule name="Timberborn Autopilot Dashboard" dir=in action=allow protocol=TCP localport=8081
```

Then browse to `http://<your-pc-lan-ip>:8081/api/autopilot/remote` from your phone. (Exposes game control to your local network only; use a VPN like Tailscale for access away from home.)

## Brain Training mode (self-play loop) â€” IMPLEMENTED

Overnight unattended training. Start it with:

```powershell
.\training\watchdog.ps1
```

Options: `-MaxEpisodes 20 -MapName Plains -FactionId Folktails -MaxCycles 6 -GameSpeed 10 -EpisodeTimeoutMinutes 75`.
One-time setup: add `-skipModManager` to Timberborn's Steam Launch Options (Properties > General) so the mod screen never blocks unattended launches.
**Stop it** by creating `Documents\Timberborn\Autopilot\STOP` (or Ctrl+C in the watchdog window). The watchdog always disables training mode on exit, so normal play is never hijacked.

How it works (files live in `Documents\Timberborn\Autopilot\`):

- `training.json` â€” the on/off switch + episode settings (faction, map, settlement name, cycle horizon, game speed). Written by the watchdog; the mod reads it.
- When enabled, the mod's main-menu service auto-starts a fresh colony (`GameSceneLoader.StartNewGameInstantly`), the autopilot plays at 10Ã—, and the episode recorder snapshots the success measures every game day into `episodes/episode_*.jsonl`.
- **Objective function:** days-to-average-wellbeing-15 (the Iron Teeth unlock â€” an in-game gate, not an assumption). Success score = `10000 âˆ’ 10Ã—days`; horizon runs score on wellbeing+population progress; extinction scores negative. Stocks/happiness/science are logged as *diagnostics*, not objectives.
- Episode end â†’ mod writes `last-result.json` and exits the game; the watchdog scores it, appends `training-history.jsonl`, deletes the training save, **mutates 1â€“2 parameters** (Â±25% hill-climb around the best-so-far in `best-params.json`), writes `params.json`, and relaunches.
- `params.json` â€” every strategy tunable (marking radius, zone sizes, building targets, pump thresholds, planning cadence). The mod loads it each game start; hand-edit it anytime to experiment.
- Code-tier improvements: Claude reviews `training-history.jsonl` + episode logs between sessions and ships logic upgrades the parameter search can't discover.

Design notes (original plan):

1. **Episode runner (in-mod):** a MainMenu-context service reads `TrainingConfig.json` from the mod folder; when enabled it programmatically starts a new game (faction, difficulty, map, settlement name â€” the game's `NewGameConfiguration`/`GameSceneLoader` APIs support this) or loads a target save.
2. **Metrics & scoring:** every in-game day the brain snapshots the success measures (population, wellbeing, water/food stocks vs. targets, science, logs, buildings completed, drought survival) into an episode log (JSON on disk).
3. **Episode end conditions:** colony failure (extinction / no water production for N days / unprogressable) or a fixed cycle horizon. On failure the save is deleted; the episode is scored either way.
4. **Watchdog (external PowerShell):** relaunches the game after the mod calls the game's exit API, keeping the loop alive unattended.
5. **Improvement loop, two tiers:**
   - **Autonomous (overnight):** all strategy heuristics (goal thresholds, radii, worker counts, priorities, storage modes) externalized into a tunable parameter file; a simple optimizer mutates parameters between episodes and keeps what scores better â€” parameter search, fully unattended.
   - **Assisted (next session):** Claude reviews the episode logs and writes actual code/logic improvements â€” the deeper "spotted an improvement, write it" tier.

## Roadmap

1. **v0.1 (done):** skeleton mod, DI + tick loop verified, research corpus.
2. **v0.2 Sense:** WorldModel â€” read stocks, population, weather, water sim, science, achievements; JSON status endpoint piggybacking the game's built-in HTTP API (`localhost:8080`) for a live dashboard.
3. **v0.3 Act primitives:** place building/path, zone crops/trees, set priorities, pause/unpause, buy science unlocks, set game speed (unclamped â€” `SpeedManager.ChangeSpeed` takes any float).
4. **v0.4 Opening book:** days 1â€“20 build order (pump â†’ lumberjacks â†’ gatherer â†’ farm â†’ forester â†’ inventor â†’ mill â†’ dam) with site selection on arbitrary maps.
5. **v0.5 Water engineer:** dam/reservoir/bypass planning, drought/badtide survival loop.
6. **v0.6 Full economy:** industry ratios, wellbeing ladder, districts, bots, wonder.
   - Pending refinements: stairs orientation verification, waterside site *scoring* (prefer the closest/least-travel river tile â€” current pick can land far from town when a nearer upstream bank exists), trunk-road layout planning to minimize future path rework.
7. **v0.7 Achievement campaign:** multi-run scheduler for the full 100%.
8. **v0.8 Interactive prompts:** in-game/remote decision prompts ("target X next?", removal recommendations) â€” suggestions channel exists, UI pending.
9. **v0.9 Brain Training mode:** the self-play loop above, plus a systematic deep-dive into the game's mechanics/gates (decompiled source) to derive optimal routes rather than heuristics.

## Prior art worth borrowing from

- **ihsoft "Advanced Automation"** â€” IFTTT + scripting engine for in-game rules ([github.com/ihsoft/TimberbornMods](https://github.com/ihsoft/TimberbornMods))
- **Timberbot** â€” HTTP API mod (port 8085) built for LLM-driven play: validated placement, A* path building, ASCII maps ([github.com/abix-/TimberbornMods](https://github.com/abix-/TimberbornMods))
- **datvm's mods + modding guide** â€” Building Blueprints, Configurable Game Speed ([datvm.github.io/TimberbornMods/ModdingGuide](https://datvm.github.io/TimberbornMods/ModdingGuide/))
- **Official modding docs** â€” [github.com/mechanistry/timberborn-modding](https://github.com/mechanistry/timberborn-modding)

