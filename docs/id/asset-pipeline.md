# Pipeline aset

```
Concept Art ──► Rodin MCP ──► Blender MCP ──► Optimasi ──► ThreeNet ──► Build
 (Nano Banana)  (teks→glb 3D)  (rig, animasi)  (pangkas, 1K)  (muat glTF)  (disalin ke output)
```

Semua aset seni dalam game dibuat dengan pipeline ini:

| Aset | Alat | Lokasi |
|---|---|---|
| 42 model petarung | Rodin MCP `Generate3DFromPrompt` (prompt di `tools/rodin/characters.json`) | `assets/source/rodin/*.glb` (tidak di git, 404 MB) |
| Rigging, 14 klip animasi, pemangkasan, optimasi tekstur, render pratinjau | Blender 5.2 tanpa UI lewat `tools/blender/process_character.py` | `assets/models/characters/*.glb`, `assets/generated/previews/*.png` |
| 4 figuran penonton (instancing) | Rodin MCP + `tools/blender/process_prop.py` (2,5 ribu segitiga) | `assets/models/crowd/` |
| Concept art, key art, 18 backdrop arena lukis, 4 tekstur tanah, siluet wayang | Rodin MCP `GenerateImageWithNanoBanana2` | `assets/concept`, `assets/ui`, `assets/backdrops`, `assets/textures` |
| 13 suara announcer | Rodin MCP `GenerateVoiceWithElevenLabsV3` | `assets/audio/voice/*.mp3` |
| Musik dan efek suara | Sintesis prosedural di `TarkamArena.Engine.Audio` | dibuat saat runtime |

## Pemrosesan karakter

```bash
python tools/blender/process_all.py            # setiap assets/source/rodin/*.glb yang berubah
python tools/blender/process_all.py --only pesilat --force
```

`process_character.py` berjalan di Blender tanpa UI (`blender -b`):

1. Mengimpor glb Rodin, menggabungkan mesh, menyatukan vertex ganda.
2. Memangkas ke 24.000 segitiga.
3. Menskalakan ke tinggi petarung dari `Roster.cs` (agar model cocok dengan hurtbox), kaki di tanah,
   posisi di tengah.
4. Membuat armature humanoid 15 tulang dari mesh: bahu di 82 % tinggi, tangan di vertex terjauh dari
   garis tengah (cocok untuk T-pose maupun A-pose), lalu skinning dengan bobot otomatis, cadangan
   envelope bila bone-heat gagal.
5. Membuat set klip yang dibutuhkan runtime: `Idle, Walk, Jab, Heavy, Kick, Uppercut, Special, Grab,
   Hit, Block, Jump, KO, Dizzy, Victory`. Pose ditulis untuk T-pose dan dikompensasi dengan kemiringan
   lengan yang terukur.
6. Mengecilkan tekstur ke JPEG 1024 px dan mengekspor glb dengan satu animasi glTF per action, lalu
   merender pratinjau transparan (dipakai sebagai potret di UI). Tambahkan argumen kelima `sheet` untuk
   merender lembar pose dari samping.

Nama klip adalah kontrak dengan `GltfFighterModel`. Petarung tanpa glb memakai `ProceduralFighterModel`.

## Perbaikan lengan dan validasi ekspor

Rig otomatis lama dapat mengira ujung senjata atau sayap sebagai tangan. Untuk karakter yang sudah
ada, gunakan `repair_character_arms.py` dengan posisi bahu/siku/pergelangan/telapak yang ditinjau
dalam `arm_landmarks.json` serta wilayah properti kaku dalam `arm_skin_overrides.json`. Geometri
dan tekstur sumber dipertahankan; bobot lengan diperbaiki dan 14 animasi lengan dibuat ulang memakai
IK dua tulang. Koordinat landmark mengacu ke capture ortografis depan pada pose dasar, diperkecil
menjadi lebar 240 px. Tinjau ulang koordinat bila mesh sumber berubah.

Gunakan GLB asli sebagai masukan, bukan hasil perbaikan berulang. Cadangan awal pada checkout ini
ada di `out/character-audit/original-models/`. Dengan Blender tersedia pada PATH:

```bash
blender -b --python-exit-code 1 -P tools/blender/repair_character_arms.py -- --models out/character-audit/original-models --output out/character-audit/repaired
blender -b --python-exit-code 1 -P tools/blender/audit_characters.py -- --models out/character-audit/repaired --output out/character-audit/after
```

Periksa capture pose dasar/depan/samping, idle, jab, dan `audit.json`; kesalahan menggagalkan audit.

Mesh Rodin ter-weld: di bagian tangan yang menempel ke pinggang atau paha pada pose sumber, permukaan
lengan tersambung ke badan, dan setelah lengan hasil perbaikan bergerak sambungan itu meregang menjadi
lembaran yang menggantung dari tangan. `cut_arm_seams.py` melepaskannya tanpa merusak properti, rambut,
atau wajah: verteks setengah lengan/setengah badan di-snap ke satu sisi (campuran hanya dipertahankan
di sekitar bahu), permukaan berbobot lengan yang terletak di torso (rompi, bagian depan baju)
dikembalikan ke badan, semua klip dievaluasi, lalu hanya face jahitan lengan-badan yang meregang yang
dipotong; serpihan lepas dibuang dan lubang pendek ditutup.

```bash
blender -b --python-exit-code 1 -P tools/blender/cut_arm_seams.py -- --models out/character-audit/repaired --output out/character-audit/seamed
dotnet run --project tools/TarkamArena.Tools -- previews out/character-audit/review/idle - out/character-audit/seamed idle
dotnet run --project tools/TarkamArena.Tools -- previews out/character-audit/review/heavy - out/character-audit/seamed heavy
dotnet run --project tools/TarkamArena.Tools -- previews out/character-audit/review/victory - out/character-audit/seamed victory
```

`previews <dir> [id,id|-] [folder model|-] [idle|jab|heavy|block|victory]` merender lewat jalur glTF
game sendiri, sehingga yang ditinjau sama dengan yang dilihat pemain. Salin hanya petarung yang lolos
ke `assets/models/characters/`; pertahankan model asli bila masih ada bagian yang melayang. Pada
putaran ini 40 dari 42 dipromosikan; **genderuwo** (cakar melayang lepas dari lengan: landmark tangannya
tidak cocok dengan geometri tangan) dan **tukang-parkir** (sisi rompi ikut terangkat saat Victory)
tetap memakai model asli. Lalu capture ulang potret UI dan audit:

```bash
dotnet run --project tools/TarkamArena.Tools -- previews          # potret transparan -> assets/generated/previews
dotnet run --project tools/TarkamArena.Tools -- characters out/character-audit/engine
```

Perintah `characters` mengambil lima pose setiap petarung melalui model runtime, menolak GLB dengan
klip tidak lengkap tanpa menyembunyikan masalah lewat model cadangan, dan menulis manifest.
Sembilan petarung DLC prosedural juga diperiksa. Ketiga skrip Blender mendukung `--only id,id`.
Set `TARKAM_ASSETS` ke path absolut folder `assets` repositori saat memeriksa build pengembangan;
build ulang aplikasi sesudahnya untuk memperbarui salinan aset di samping executable.

## Menambah petarung

1. Tambahkan `CharacterDefinition` di `Roster.cs` (di akhir; id dipakai sebagai kunci data simpanan).
2. Tambahkan prompt di `tools/rodin/characters.json`, buat modelnya dengan Rodin MCP, simpan sebagai
   `assets/source/rodin/<id>.glb`.
3. `python tools/blender/process_all.py --only <id>`.
4. `dotnet run --project tools/TarkamArena.Tools -- docs` untuk memperbarui dokumentasi petarung.

## Arena

Sebuah arena adalah `StageDefinition` di `Stages.cs` ditambah `assets/backdrops/<id>.jpg` (lukisan 21:9).
`StageBuilder` menambahkan pencahayaan, tekstur tanah, dan properti set. Lingkungan buatan yang
diekspor dari Blender sebagai `assets/models/stages/<id>.glb` menggantikan set prosedural bila ada.
