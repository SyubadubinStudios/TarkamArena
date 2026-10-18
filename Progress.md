# Progress — development tracking / Pelacakan pengembangan

*Dibuat oleh Ariana Mischa Fadhila dari Syubadubin Studios.*

## 2026-10-18 — v0.1.0

### Done / Selesai

- **Solution**: 7 projects + tools + tests on .NET 10 (`TarkamArena.slnx`), central package management.
- **Simulation (Core)**: fixed-point `MatchSimulation` with full combat system, projectiles, hazards,
  crowd meter, rounds, finisher window, mercy, tag battle and FFA. Zero-allocation step (tested).
- **Content**: 51 fighters, 18 arenas (7 added on request: Sound Horeg carnival, Riau peatland,
  Ponorogo square, Pengerupukan night, Protest avenue, Under the flyover, Courtroom), new NPC roles,
  arena events and music styles; `solution-design.md` updated.
- **AI**: `CpuBrain` with four difficulties (combo, anti-air, block/parry, finisher execution).
- **Modes**: Story (9 chapters), Tournament bracket, Survival, Training, Custom.
- **Progression**: XP/levels/coins, 42 achievements, unlockables, secret legends, Elo tiers, creator.
- **Assets** (Rodin MCP + Blender): 42 fighter glTFs (rigged, 14 clips, 24k tris, 1K textures),
  4 instanced crowd extras, 18 painted backdrops, 4 ground textures, key art, wayang silhouettes,
  13 announcer lines; headless Blender pipeline scripts (`tools/blender`).
- **Engine**: ThreeNet presenter, stage builder, crowd instancing, BepuPhysics props & debris,
  particles, volumetric rage fire, weather, camera director, procedural music & SFX, input mapper.
- **App**: Avalonia 12 — title, main menu, mode/character/stage select, fight HUD, pause, results,
  story, tournament, survival, online, creator (live 3D), collection, settings, credits.
- **Online**: rollback netcode (tested with latency + 20 % loss and real UDP), LAN handshake,
  SignalR server with matchmaking, Elo, leaderboard, clans; PostgreSQL or SQLite.
- **Persistence**: SQLite profile/settings/match history.
- **Quality**: 58 automated tests passing (simulation, determinism, rollback, UDP, content,
  localisation, progression, physics, persistence, audio).
- **Packaging**: Inno Setup, MSIX, AppImage/deb/rpm, dmg scripts; CI and release workflows.
- **Docs**: bilingual README and docs, generated roster/arena pages, screenshots from the real build.

### Known issues / Masalah yang diketahui

- Attack animations are shared across fighters (one clip set); signature specials reuse generic clips.
- Costumes and poses are unlockable but not yet visible in matches.
- The DLC fighters and creator fighters use the procedural body (no Rodin model yet).
- ThreeNet 0.12 `CreateCapsuleGeometry` shades inverted; the engine builds its own capsule mesh.
- Ranked play needs a reachable UDP port on one peer (no NAT punch-through yet).
- Builds are unsigned (SmartScreen / Gatekeeper warnings).

### Next / Berikutnya

See [PLAN.md](PLAN.md) milestone 0.2.
