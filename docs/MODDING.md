# Timberborn 1.0 Modding Reference (verified for 1.0.13.1)

Facts below marked **[decompiled]** were verified directly from this install's assemblies; the rest come from official docs and community sources (July 2026).

## Load pipeline [decompiled]

1. `UserFolderModsProvider` scans `Documents\Timberborn\Mods\*` (folder auto-created on first launch); Steam Workshop mods merge in via `ModRepository`.
2. `ManifestLoader` requires `manifest.json` in the mod root: `Name`, `Version`, `Id`, `MinimumGameVersion`, optional `Description`, `RequiredMods`/`OptionalMods` (`[{"Id": "...", "MinimumVersion": "..."}]` — both force load order).
3. `ModCodeStarter` loads **every `*.dll` recursively** in the mod folder (`Assembly.Load(bytes)`), then instantiates each parameterless-ctor `IModStarter` and calls `StartMod(IModEnvironment)` (`ModPath`, `OriginPath`).
4. Bindito auto-discovers `IConfigurator` classes tagged `[Context("Game")]` (also `"MainMenu"`, `"MapEditor"`, `"Bootstrapper"` = all scenes) from all loaded assemblies. Constructor injection reaches every game singleton.

## Key lifecycle interfaces [decompiled]

- `Timberborn.SingletonSystem`: `ILoadableSingleton.Load()`, `IPostLoadableSingleton`, `IUpdatableSingleton` (per frame), `ILateUpdatableSingleton`, `IUnloadableSingleton`, `EventBus` (+`[OnEvent]` methods, register via `EventBus.Register(this)`).
- `Timberborn.TickSystem`: `ITickableSingleton.Tick()` (per game tick), `TickableComponent` for entity components, `IParallelTickableSingleton`.
- Entity components: inherit `BaseComponent`; `IAwakableComponent`, `IInitializableEntity`, `IFinishedStateListener`, `IPersistentEntity` (Save/Load via `ComponentKey`/`PropertyKey`).
- Persistence: `ISaveableSingleton` + `SingletonKey`/`PropertyKey` (see `HttpApi` for a minimal example).

## Useful facts [decompiled]

- `SpeedManager.ChangeSpeed(float)` — no upper clamp; sets `Time.timeScale`. `ChangeAndLockSpeed`/`UnlockSpeed` for exclusive control.
- Built-in HTTP API (`Timberborn.HttpApiSystem`): `HttpApi` singleton, default `http://localhost:8080/`, port saved per-settlement. Endpoint set is DI-provided (`IEnumerable<IHttpApiEndpoint>`) → **we can MultiBind our own `IHttpApiEndpoint`** to add REST routes for the dashboard. Vanilla routes: `/api/levers`, `/api/switch-on|off/<name>`, `/api/adapters`, webhooks on adapter state change.
- Official automation feature (`Timberborn.Automation[Buildings]`): signal network — sensors (Depth/Flow/Contamination/WeatherStation/PopulationCounter/ResourceCounter/ScienceCounter/PowerMeter), logic (Relay/Memory/Timer/Chronometer/Lever), actuators (Gate, Detonator, PausableBuildingTerminal, Indicator, Speaker, HttpLever/HttpAdapter).
- `ExternalModFinder` only flags BepInEx/trainers for analytics (`ModdedState`); `DateSalter` salts save IDs when modded/dev-mode. **No achievement gating found in `Timberborn.Achievements` / `Timberborn.AchievementSystem`.** Dev-mode-only structures are excluded from BuildEveryStructure.
- Dev console: **Alt+Shift+Z** (console), **Alt+Shift+X** (debug panel) — instant build, spawn resources/beavers, water tools. Avoid during achievement runs.
- Full decompiled game source for reference: run `ilspycmd -p -o <outdir> --referencepath <Managed> <Managed>\<Assembly>.dll` (dotnet tool `ilspycmd`).

## Build setup

- `netstandard2.1`, reference `Timberborn_Data\Managed\Timberborn.*.dll` + `Bindito.Core.dll` + `UnityEngine.CoreModule.dll` with `Private="false"` (see `src/AutopilotMod/AutopilotMod.csproj`).
- For `internal` access if ever needed: `BepInEx.AssemblyPublicizer.MSBuild` with `Publicize="true"`.
- Harmony: depend on community "Harmony" mod (Workshop 3284904751, `RequiredMods: [{"Id": "Harmony"}]`), then `new Harmony("TimberbornAutopilot").PatchAll()` in `StartMod`. Officially sanctioned.
- Settings UI: eMkaQQ's ModSettings (Workshop 3283831040, `Id: eMka.ModSettings`).

## Ecosystem source code to study

| Mod | What to borrow | Source |
|---|---|---|
| ihsoft Advanced Automation | rules/scripting engine, signals, actions (`Pausable.Pause()`, `Floodgate.SetHeight()`, `Manufactory.SetRecipe()`, dynamite drill-down) | github.com/ihsoft/TimberbornMods |
| Timberbot (abix-) | HTTP API for LLM play: validated building placement, A* path building, district I/O, ASCII maps, webhooks | github.com/abix-/TimberbornMods |
| Building Blueprints (datvm) | multi-building placement/serialization | github.com/datvm/TimberbornMods |
| Configurable Game Speed (datvm) | speed UI beyond 3x | github.com/datvm/TimberbornMods |
| Official examples | mod structure, blueprints, asset bundles | github.com/mechanistry/timberborn-modding |

## Data mods (no code)

JSON "Blueprints" override goods/buildings/recipes: `version-1.0\Blueprints\...` inside the mod folder. Localizations via `Localizations\enUS_*.csv`.
