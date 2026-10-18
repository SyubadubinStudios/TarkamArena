# Arsitektur

## Struktur solusi

```
TarkamArena.slnx
├─ src/TarkamArena.Core          .NET murni: simulasi, konten, AI, mode, progres (tanpa dependensi)
├─ src/TarkamArena.Physics       dunia BepuPhysics untuk properti dan puing kosmetik
├─ src/TarkamArena.Engine        presentasi ThreeNet: petarung, arena, penonton, VFX, kamera, audio, input
├─ src/TarkamArena.Persistence   data simpanan SQLite (profil, pengaturan, riwayat pertandingan)
├─ src/TarkamArena.Networking    rollback netcode LiteNetLib + klien SignalR untuk layanan online
├─ src/TarkamArena.App           aplikasi desktop Avalonia 12: layar, HUD, navigasi
├─ src/TarkamArena.Server        ASP.NET Core + SignalR: matchmaking, rating, klan (PostgreSQL / SQLite)
├─ tools/TarkamArena.Tools       render offscreen, ekspor musik, pembuatan dokumentasi, uji online
├─ tools/blender, tools/rodin    skrip pipeline aset dan prompt pembuatan
└─ tests/TarkamArena.Tests       xUnit
```

```
          Aplikasi Avalonia ─────────► Persistence (SQLite)
           │      │   └──────────────► Networking ──► LiteNetLib (UDP peer to peer)
           │      │                        └───────► SignalR ──► Server ──► PostgreSQL
           │      ▼
           │    Engine (ThreeNet: rendering, audio, input)  ──► Physics (BepuPhysics)
           ▼      │
          Core ◄──┘   (simulasi deterministik: satu-satunya sumber kebenaran)
```

## Simulasi deterministik

`MatchSimulation.Step(ref MatchState, MatchContext, ReadOnlySpan<InputFrame>)` memajukan pertandingan
satu frame 60 Hz.

- **Matematika fixed-point** (`Fixed`, 16.16) alih-alih float, sehingga setiap mesin menghasilkan
  hasil yang identik sampai ke bit.
- **Satu struct unmanaged.** `MatchState` menyimpan semua petarung, proyektil, rintangan, meter, dan
  daftar event dalam buffer `[InlineArray]`. Snapshot rollback cukup salinan struct; `Checksum()`
  meng-hash byte-nya untuk mendeteksi desync.
- **Nol alokasi per frame.** Tes `Step_does_not_allocate` memeriksanya dengan
  `GC.GetAllocatedBytesForCurrentThread`.
- **Data rujukan tetap** (frame data, finisher, ukuran arena) ada di `MatchContext`, dibuat sekali
  per pertandingan.
- **Event, bukan callback.** Setiap langkah menulis `MatchEvent` (kena, tangkis, KO, finisher…) yang
  dibaca lapisan presentasi: simulasi tidak pernah memanggil rendering atau audio.

Frame data berasal dari `MoveSetFactory` berdasarkan gaya dan atribut petarung; urutan finisher dari
`FinisherFactory`, di-seed dengan id petarung sehingga tidak pernah berubah.

## Presentasi

`ArenaPresenter` memiliki `Scene` ThreeNet dan membaca state setiap frame render:

- **Petarung**: `GltfFighterModel` memutar 14 klip skinned hasil pipeline Blender, dengan cross-fade
  `AnimationMixer` dan kecepatan klip serangan disesuaikan dengan frame data-nya.
  `ProceduralFighterModel` merakit tubuh dari primitif sesuai `BodyShape` (DLC, kreator, cadangan).
- **Arena**: `StageBuilder` mengatur langit, kabut, dan cahaya dari palet arena, ubin tekstur tanah
  lukis, backdrop lukis 21:9 (Rodin), dan properti set khusus tiap arena.
- **Penonton**: `NpcDirector` (di Core) menjalankan perilaku Living Arena; `CrowdRenderer` menggambar
  setiap peran NPC dengan GPU instancing (satu draw call per peran) dari model figuran low-poly.
- **Properti fisika**: `ArenaPhysics` (BepuPhysics) mensimulasikan peti, buah, dan puing finisher;
  tidak pernah memengaruhi pertarungan.
- **VFX**: sistem partikel CPU yang dialokasikan di awal untuk percikan, debu, ledakan finisher,
  konfeti, dan cuaca, ditambah api volumetrik untuk mode Amarah.
- **Kamera**: membingkai semua petarung aktif, bergetar saat benturan, berputar saat finisher.

`FightScreen` di Avalonia memuat `ThreeNetView`, menjalankan simulasi dengan akumulator fixed-step di
event `Frame`, dan memperbarui HUD (bar darah, meter, dan VU meter sorakan yang digambar sendiri)
tanpa alokasi.

## Audio

`MusicGenerator` menyusun loop mulus untuk 14 gaya musik (gamelan, angklung, degung, talempong,
sasando, baleganjur, reog, metal, rock, punk, EDM, orkestra hibrida, dangdut koplo, sound horeg) dari
instrumen `Synth`: bilah perunggu dengan "ombak", gong, kendang, angklung, suling, slompret, dawai
Karplus-Strong, sub 808, dan drum. `SoundBank` mensintesis setiap efek suara. Suara announcer direkam
dengan ElevenLabs. `AudioDirector` memutar semuanya lewat `AudioEngine` ThreeNet, dengan suara
penonton mengikuti meter sorakan.

## Performa dan memori

- Jalur panas (simulasi, HUD, penonton) bebas alokasi: struct, span, inline array, buffer dipakai ulang.
- Instancing untuk penonton, geometri dan material bersama di `MeshCache`, tekstur ter-cache, gambar
  didekode seukuran tampilannya (`Bitmap.DecodeToWidth`).
- Model petarung dipangkas ke 24 ribu segitiga dengan tekstur JPEG 1K (sekitar 1,2 MB per model);
  figuran ke 2,5 ribu segitiga dengan tekstur 512 px.
- Preset grafis mengatur resolusi render, bayangan, SSAO, SSR, MSAA, dan motion blur.
- Musik dirender di thread pekerja; Bepu berjalan multithread.
