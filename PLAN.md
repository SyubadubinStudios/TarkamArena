# PLAN — Tarkam Arena roadmap / Peta jalan

*Dibuat oleh Ariana Mischa Fadhila dari Syubadubin Studios.*

Status legend / Keterangan: ✅ done/selesai · 🚧 in progress/berjalan · 📋 planned/direncanakan

## Milestone 0.1 — Playable foundation / Fondasi siap main ✅

| Area | Item | Status |
|---|---|---|
| Core | Deterministic fixed-point 60 Hz simulation, zero allocations per frame | ✅ |
| Core | Light/heavy/low, target combos, launcher, air combo, grab & tech, counter, parry, dodge, specials with motion inputs, rage, adrenaline, team combo | ✅ |
| Core | Finishers (3 Fatality, 2 Brutality, 2 Friendship, 1 Mercy, 1 Legendary) for every fighter | ✅ |
| Core | Modes: 1v1, 2v2, Tag, FFA, Survival, Story (9 chapters), Tournament, Ranked, Clan Wars, Custom, Training | ✅ |
| Core | Living Arena NPC director, celebrity recognition, arena events & hazards, crowd meter & reputation | ✅ |
| Core | Progression: XP, levels, coins, costumes/poses/intros/emotes/banners, 42 achievements, secret unlocks | ✅ |
| Core | Fighter creator (human / mystic / jawara, 24-point budget) | ✅ |
| Content | 51 fighters (42 hand-written + 9 DLC), 18 arenas, bilingual text | ✅ |
| Assets | 42 Rodin models rigged & animated with Blender (14 clips), 18 painted backdrops, crowd extras, textures, key art, announcer | ✅ |
| Engine | ThreeNet presenter, procedural sets, instanced crowd, BepuPhysics props, particles, fire, weather, camera director | ✅ |
| Audio | Procedural music for 14 styles, synthesised SFX, ElevenLabs announcer | ✅ |
| App | Avalonia 12 UI: all screens, HUD, gamepad & keyboard navigation, screenshot tour | ✅ |
| Online | LiteNetLib rollback netcode, LAN handshake, SignalR matchmaking server, Elo, clans, PostgreSQL/SQLite | ✅ |
| Ops | CI, release workflow with installers for Windows, Linux, macOS; MSIX | ✅ |
| Docs | README + docs in English and Bahasa Indonesia with screenshots | ✅ |

## Milestone 0.2 — Feel & polish / Rasa & polesan 🚧

- ✅ Signature moves (Jurus pamungkas): three per fighter, nine effect kinds, Jurus meter, cut-in,
  VFX, CPU usage, move lists. / Tiga jurus pamungkas per petarung.
- ✅ Hit-stop camera punch-in, slow-motion final hit, round-transition wipes in the spanduk style.
- ✅ Per-fighter move list in the pause menu (signature moves).
- Rodin/Blender-authored models for the signature props and summons (replace the primitive rigs).
- Per-move hand-authored animations in Blender for the five most played fighters (signature specials).
- Unique finisher cinematics per fighter (camera paths, props, Blender-authored clips).
- Costume rendering (tinted materials and alternate Rodin textures) and visible victory poses/intros.
- Key rebinding UI; rumble on hits.
- Destructible environment pieces per arena (stalls collapse, speaker walls topple).

## Milestone 0.3 — Online / Daring 📋

- Accounts and authentication for the server (OAuth / device login), anti-cheat result validation.
- NAT punch-through (LiteNetLib `NatPunchModule`) and relay fallback.
- Spectator mode and replays (inputs + seed are enough, the simulation is deterministic).
- Clan war seasons, weekly tournaments, cross-region matchmaking.
- Docker image and Helm chart for the server; PostgreSQL migrations.

## Milestone 0.4 — Content / Konten 📋

- Authored environment glTFs from Blender MCP for each arena (replacing procedural sets).
- DLC packs fully modelled: Horror Nusantara (Sundel Bolong, Tuyul, Palasik), Jawara Nusantara
  (Pendekar Betawi, Jawara Banten, Pendekar Madura), Future Indonesia (Cyborg Ojol, AI Dukun, Polisi Drone).
- Story expansion chapters; voice lines per fighter.
- Optional battle pass (cosmetic only).

## Milestone 1.0 — Release / Rilis 📋

- Code signing and notarisation; Microsoft Store submission.
- Accessibility: colour-blind palettes, input display, adjustable game speed in training.
- Console ports via ID@Xbox (GDK), PlayStation Partners and Nintendo Developer Portal (see packaging/consoles).
