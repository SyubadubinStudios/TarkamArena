# Architecture

## Solution layout

```
TarkamArena.slnx
├─ src/TarkamArena.Core          pure .NET: simulation, content, AI, modes, progression (no dependencies)
├─ src/TarkamArena.Physics       BepuPhysics world for cosmetic props and debris
├─ src/TarkamArena.Engine        ThreeNet presentation: fighters, stages, crowd, VFX, camera, audio, input
├─ src/TarkamArena.Persistence   SQLite save data (profile, settings, match history)
├─ src/TarkamArena.Networking    LiteNetLib rollback netcode + SignalR client for online services
├─ src/TarkamArena.App           Avalonia 12 desktop app: screens, HUD, navigation
├─ src/TarkamArena.Server        ASP.NET Core + SignalR: matchmaking, ratings, clans (PostgreSQL / SQLite)
├─ tools/TarkamArena.Tools       offscreen renders, music export, docs generation, online smoke test
├─ tools/blender, tools/rodin    asset pipeline scripts and generation prompts
└─ tests/TarkamArena.Tests       xUnit
```

```
          Avalonia App ──────────────► Persistence (SQLite)
           │      │   └──────────────► Networking ──► LiteNetLib (UDP peer to peer)
           │      │                        └───────► SignalR ──► Server ──► PostgreSQL
           │      ▼
           │    Engine (ThreeNet: rendering, audio, input)  ──► Physics (BepuPhysics)
           ▼      │
          Core ◄──┘   (deterministic simulation: the single source of truth)
```

## The deterministic simulation

`MatchSimulation.Step(ref MatchState, MatchContext, ReadOnlySpan<InputFrame>)` advances a match by one
60 Hz frame.

- **Fixed-point maths** (`Fixed`, 16.16) instead of floats, so every machine computes bit-identical
  results.
- **One unmanaged struct.** `MatchState` holds every fighter, projectile, hazard, meter and the event
  list in `[InlineArray]` buffers. A rollback snapshot is a struct copy; `Checksum()` hashes its
  bytes for desync detection.
- **Zero allocations per frame.** A unit test (`Step_does_not_allocate`) checks it with
  `GC.GetAllocatedBytesForCurrentThread`.
- **Immutable lookup data** (frame data, finishers, stage size) lives in `MatchContext`, built once
  per match from the content definitions.
- **Events, not callbacks.** Each step writes `MatchEvent`s (hit, parry, KO, finisher…) that the
  presentation reads: the simulation never calls into rendering or audio.

Frame data comes from `MoveSetFactory`, derived from each fighter's archetype and stats; finisher
sequences come from `FinisherFactory`, seeded by the fighter id so they never change.

## Presentation

`ArenaPresenter` owns the ThreeNet `Scene` and reads the state every rendered frame:

- **Fighters**: `GltfFighterModel` plays the 14 skinned clips exported by the Blender pipeline,
  cross-fading with an `AnimationMixer` and time-scaling attack clips to their frame data.
  `ProceduralFighterModel` builds a primitive body from `BodyShape` (DLC, creator fighters, fallback).
- **Stages**: `StageBuilder` sets sky, fog and lights from the stage palette, tiles a painted ground
  texture, hangs a 21:9 painted backdrop (Rodin) and places hand-written set pieces per arena.
- **Crowd**: `NpcDirector` (in Core) runs the Living Arena behaviour; `CrowdRenderer` draws every
  NPC role with GPU instancing (one draw call per role) from authored low-poly crowd models.
- **Physics props**: `ArenaPhysics` (BepuPhysics) simulates crates, fruit and finisher debris; it
  never feeds back into the fight.
- **VFX**: preallocated CPU particle systems for sparks, dust, finisher bursts, confetti and weather,
  plus volumetric fire for Rage.
- **Camera**: frames all active fighters, shakes on impacts, orbits during finishers.

The Avalonia `FightScreen` hosts a `ThreeNetView`, runs the simulation with a fixed-step
accumulator inside the view's `Frame` event, and updates the HUD (custom-drawn life bars, meters
and the crowd VU meter) without allocating.

## Audio

`MusicGenerator` composes a seamless loop for each of the 14 music styles (gamelan, angklung, degung,
talempong, sasando, baleganjur, reog, metal, rock, punk, EDM, orchestral hybrid, dangdut koplo, sound
horeg) from the `Synth` instruments: bronze keys with "ombak" beating, gongs, kendang, angklung, suling,
slompret, Karplus-Strong strings, 808 subs and drums. `SoundBank` synthesises every sound effect. The
announcer lines are recorded with ElevenLabs. `AudioDirector` plays it all through ThreeNet's
`AudioEngine`, with the crowd bed following the crowd meter.

## Performance and memory

- Hot paths (simulation, HUD, crowd) are allocation-free: structs, spans, inline arrays, reused buffers.
- Instancing for the crowd, shared `MeshCache` geometries and materials, cached textures and images
  decoded at the size they are displayed (`Bitmap.DecodeToWidth`).
- Character models are decimated to 24k triangles with 1K JPEG textures (about 1.2 MB each); crowd
  extras to 2.5k triangles with 512 px textures.
- Graphics presets scale render resolution, shadows, SSAO, SSR, MSAA and motion blur.
- Music is rendered on a worker thread; Bepu runs multithreaded.
