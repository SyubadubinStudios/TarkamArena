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
