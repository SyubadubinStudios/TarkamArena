# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Tarkam Arena** ("Legendary Fighting Tournament from Indonesia"), a 3D fighting game on .NET 10. The brief
is `tarkam.txt`; the full game design (in Bahasa Indonesia) is `solution-design.md`. Roadmap: `PLAN.md`;
development log: `Progress.md`. Keep both up to date when finishing work.

## Commands

```bash
dotnet build TarkamArena.slnx
dotnet test tests/TarkamArena.Tests                                   # all tests (~10 s)
dotnet test tests/TarkamArena.Tests --filter "FullyQualifiedName~Rollback"   # a subset / single test
dotnet run --project src/TarkamArena.App [-- --fight | --lang en | --capture docs/screenshots]
dotnet run --project src/TarkamArena.App -- --fight --jurus --fighters kuyang,kang-ojol [--snap <dir>]   # CPU signature-move demo
dotnet run --project src/TarkamArena.Server                           # http://localhost:5080 (SQLite; ConnectionStrings__Postgres for PostgreSQL)
dotnet run --project tools/TarkamArena.Tools -- scene <stage> <p1> <p2> <frames> <out.png>   # offscreen render to check visuals
dotnet run --project tools/TarkamArena.Tools -- docs                  # regenerate docs/{en,id}/characters.md and stages.md
dotnet run --project tools/TarkamArena.Tools -- signatures <dir> [id,id]   # one offscreen frame per signature move (window-independent)
dotnet run --project tools/TarkamArena.Tools -- previews [dir] [id,id|-] [models|-] [idle|jab|heavy|block|victory]   # transparent portraits via the runtime (default: assets/generated/previews)
blender -b -P tools/blender/cut_arm_seams.py -- --models out/character-audit/repaired --output out/character-audit/seamed   # after repair_character_arms.py; see docs/en/asset-pipeline.md
python tools/blender/process_all.py [--only id] [--force]             # Rodin glb -> rigged/animated glb (headless Blender 5.2)
```

`--capture <dir>` drives the real app through every screen and saves screenshots; it is how README/docs
images are produced. Kill stale `TarkamArena.dll` processes if a run hangs (the window title is "Tarkam Arena").
The tour keeps the window topmost and waits for `FightScreen.Ready` before each fight shot, because the
first frames of an arena can take tens of seconds on a busy/integrated GPU. `TARKAM_TRACE=1` writes load
timings to `%TEMP%/tarkam-trace.txt`.

## Architecture (big picture)

- `TarkamArena.Core` has no dependencies and is the single source of truth. `MatchSimulation.Step` is
  deterministic: fixed-point `Fixed`/`FixedVector2` only, no floats, clocks or allocations (a test asserts
  zero allocations). All mutable match data is in the unmanaged `MatchState` struct using `[InlineArray]`
  buffers, so rollback snapshots are struct copies and `Checksum()` hashes raw bytes. Never add reference
  types or floats to `MatchState`/`FighterState`.
- The simulation communicates outward only through `MatchEvent`s in `state.CurrentEvents`; presentation
  (`TarkamArena.Engine.ArenaPresenter`, `AudioDirector`, the HUD) reads state and events and must never
  influence the fight. `NpcDirector` and `ArenaPhysics` (BepuPhysics) are cosmetic and may use floats.
- Signature moves ("Jurus pamungkas") are data in `Combat/Signatures.cs` (`SignatureCatalog`, three per
  fighter, keyed by id; archetype fallback otherwise). Their live effects are `Summon` structs in
  `MatchState.Summons`, stepped in `MatchSimulation.Signatures.cs`; `SignatureRenderer` (Engine) draws them.
  The in-window render loop stalls when the game window is in the background, so check visuals with the
  `signatures`/`scene` tool commands rather than desktop screenshots.
- Arena dressing lives in `Rendering/StageDressing.cs` (partial `StageBuilder`): decor models are
  `assets/models/decor/<id>.glb`, one mesh + one material, 1 m tall with the origin at the base. Rodin
  outputs go through `tools/blender/process_prop.py`; procedural ones are built in Blender with
  `tools/blender/decor_kit.py` + `decor_builders.py` (colours baked into a palette texture). Wind sway,
  blinking lights and beat shakers are lists on `StageRig`, animated in `ArenaPresenter.AnimateSet`.
- Content is data in `Roster.cs` and `Stages.cs`. Order is stable (append only). Frame data is derived by
  `MoveSetFactory` from archetype + stats; finisher inputs by `FinisherFactory` (seeded by id, 4 tokens,
  disjoint pools per kind so no sequence is a suffix of another — a test checks this).
- All player-facing text is bilingual: `LocalizedText` in content, `Loc.T("key")` table in
  `Localization/Loc.cs` (a test checks every key has EN and ID).
- `TarkamArena.App` (Avalonia 12) builds UI in C# with helpers in `Controls/Ui.cs` (no per-screen XAML);
  styles/tokens are in `Styles/Theme.axaml`. Screens derive from `Screen` (menu actions via
  `HandleAction`, nav list via `Nav(...)`); `MainWindow` owns the screen stack. `FightScreen` runs the sim
  on a fixed 60 Hz accumulator inside `ThreeNetView.Frame` and defers navigation with `Dispatcher.Post`.
  `MatchPlan` carries mode/rules/slots and an `OnFinished` callback used by story, tournament, survival
  and online flows.
- `TarkamArena.Networking`: `RollbackSession` (2-frame delay, prediction, rollback from snapshot ring,
  checksum desync detection) over `IPacketTransport` (`LiteNetTransport` UDP, `LoopbackTransport` for
  tests); `HandshakeExchange` for LAN; `OnlineServices` is the SignalR client of `TarkamArena.Server`.
- Assets live in `assets/` and are copied next to the executable by `TarkamArena.App.csproj`
  (`assets/source` with raw Rodin models is git-ignored). `AssetLocator` finds the folder at runtime.
  Fighter glTFs must contain the clip names `Idle, Walk, Jab, Heavy, Kick, Uppercut, Special, Grab, Hit,
  Block, Jump, KO, Dizzy, Victory` (contract with `GltfFighterModel`); missing models fall back to
  `ProceduralFighterModel`.

## Gotchas

- The offscreen renderer caches resources by scene handle and handles are reused after a scene is
  disposed: render many scenes either in one long-lived scene or with a fresh renderer per scene.
- ThreeNet 0.12's `CreateCapsuleGeometry` shades inverted; use `MeshCache.Capsule` (custom mesh).
- ThreeNet glTF models face +Z; fighters are yawed ±90° to face ±X.
- Bash heredocs containing apostrophes break in this environment; write files with the Write tool.
- Avalonia: don't name extension methods after control events (`Click`), and detach a control
  (`Child = null`) before re-parenting it.

## Requirements from the brief (tarkam.txt)

- Performance and memory efficiency (avoid allocations in hot paths; pooling, spans, structs).
- Documentation complete and bilingual (English + Bahasa Indonesia), with screenshots in README and docs.
- Credit "Dibuat oleh Ariana Mischa Fadhila dari Syubadubin Studios" in docs and app (title screen
  footer, main menu, credits, installers).
- UI work uses the project `frontend-design` skill (identity: tarp blue, spanduk yellow, chilli red,
  Bungee / Barlow Semi Condensed / IBM Plex Mono, misregistered sign lettering, crowd VU meter).
- 3D assets, images and 2D art via Rodin MCP; sculpting, rigging and animation via Blender MCP
  (headless Blender scripts in `tools/blender` are the reproducible form of that step).
