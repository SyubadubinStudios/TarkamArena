# Progress — development tracking / Pelacakan pengembangan

*Dibuat oleh Ariana Mischa Fadhila dari Syubadubin Studios.*

## 2026-10-18 — v0.2.0

### Done / Selesai

- **Signature moves / Jurus pamungkas**: every fighter has three signature moves of their own
  (`↓↘→+S`, `↓↙←+S`, `↓↓+S`) paid for with the new Jurus meter (costs 250/400/700). Nine kinds:
  summoned spirits and creatures, crowd charges (protesters, ojol convoys, arisan moms, sound trucks),
  giant weapons (including Jaka Tarub's pacul), weather strikes (lightning, fire rain, water bombs),
  lift & slam, tornadoes, barrages, quakes and waves. 126 hand-authored moves for the 42 main fighters,
  themed fallbacks for DLC and creator fighters.
  Setiap petarung punya tiga jurus pamungkas sendiri dengan meter Jurus baru dan sembilan jenis efek.
- **Simulation**: signature effects live in `MatchState.Summons` (fixed-size, rolls back with the
  match); super freeze, invulnerable wind-up, super cancel from normals; meter gain on hit, on being
  hit and on parry; Training refills the meter. Step stays allocation-free.
- **AI**: the CPU performs the motions and spends its meter (more often on harder levels).
- **VFX**: `SignatureRenderer` builds a rig per fighter and signature from primitives (ghosts,
  flying heads, birds, beasts, protesters with placards, ojol bikes, sound trucks, carts, poles,
  blades, the pacul, storm clouds and bolts, lift rings, wind funnels, quake shards, wave walls,
  sound rings) with a themed particle emitter each; super flash dims the arena; shockwaves, shake and
  camera punch on impacts; per-kind sounds.
- **HUD**: Jurus meter with cost diamonds and shimmer; full-width cut-in banner in the move's colours
  with the fighter portrait, kind and input; move list in character select and the pause menu.
- **UI/UX pass** (commit 1c06e63): animated title and menus, spanduk banner-wipe transitions, VS
  loading card with Rodin 2D art, battle HUD with portrait plates, brush callouts, letterbox,
  vignette, KO slow motion, more VFX (counter sparks, shockwaves, aura, projectile trails).
- **Tools**: `signatures <dir> [ids]` renders one offscreen frame per signature (used for the docs);
  the app takes `--fight --jurus --fighters a,b` for a CPU signature demo.
- **Quality**: 65 automated tests passing (new: every signature of every fighter deals damage, meter
  cost, input slots, determinism, CPU usage, meter gain).
- **Docs**: signature section in the EN/ID gameplay guides with screenshots; every fighter page lists
  its three signatures.
- **Living arenas / Arena yang hidup**: new Rodin ground textures for the arenas that had plain floors
  (jungle floor, burnt peat, Bromo volcanic sand, beach sand, Borobudur andesite slabs, courtroom
  parquet); 46 decor models placed per arena by `StageDressing` — 22 from Rodin (trees, bushes, ferns,
  rocks, rice, carts, scooters, jukung, ponies, guardian lions, Buddhas, penjor, ondel-ondel, reog mask,
  sound truck…) and 24 modelled procedurally in Blender via MCP once the Rodin credits ran out (fire
  truck, water cannon, jeep, ontel, colonial lamps, gubuk, scarecrow, hornbill totem, sesajen, canang,
  bale kulkul, sasando, judges' bench, Garuda, frangipani…); instanced ground clutter; wind sway with gusts for trees,
  bushes, grass, flags and umbul-umbul; chasing string lights and flickering lamps; speaker cabinets that
  rattle on the beat; more locals in the peatland and courtroom.
  Lantai bertekstur baru, dekor dari Rodin, vegetasi bergoyang kena angin, lampu kelap-kelip, speaker bergetar.

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

- Attack animations are shared across fighters (one clip set); signature moves reuse a generic
  wind-up clip, and the summoned props are built from primitives rather than authored models.
- Costumes and poses are unlockable but not yet visible in matches.
- The DLC fighters and creator fighters use the procedural body (no Rodin model yet).
- ThreeNet 0.12 `CreateCapsuleGeometry` shades inverted; the engine builds its own capsule mesh.
- Ranked play needs a reachable UDP port on one peer (no NAT punch-through yet).
- Builds are unsigned (SmartScreen / Gatekeeper warnings).

### Next / Berikutnya

See [PLAN.md](PLAN.md) milestone 0.2.
