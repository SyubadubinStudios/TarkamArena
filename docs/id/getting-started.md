# Memulai

## Pasang rilis

Unduh installer untuk platformmu dari
[rilis terbaru](https://github.com/SyubadubinStudios/TarkamArena/releases/latest):

| Platform | Berkas | Catatan |
|---|---|---|
| Windows 10/11 x64 | `TarkamArena.Setup.exe` | Installer biasa dengan pintasan Start menu dan desktop |
| Windows (Store) | `TarkamArena.msix` | Paket Microsoft Store (belum ditandatangani sampai dikirim ke Store) |
| Windows portabel | `TarkamArena-win-x64-portable.zip` | Ekstrak lalu jalankan `TarkamArena.exe` |
| Linux x64 | `TarkamArena.AppImage` | `chmod +x TarkamArena.AppImage && ./TarkamArena.AppImage` |
| Debian / Ubuntu | `TarkamArena.deb` | `sudo apt install ./TarkamArena.deb`, lalu jalankan `tarkam-arena` |
| Fedora / openSUSE | `TarkamArena.rpm` | `sudo dnf install ./TarkamArena.rpm` |
| macOS (Apple Silicon) | `TarkamArena.dmg` | Seret ke Applications. Build belum dinotarisasi: klik kanan → Open saat pertama kali |

## Kebutuhan sistem

- Windows 10+ 64-bit, distribusi Linux modern, atau macOS 12+ di Apple Silicon.
- GPU dengan Direct3D 12, Vulkan, atau Metal (ThreeNet merender lewat wgpu). Intel UHD 620 sanggup
  menjalankan game di preset *Sedang*.
- RAM 4 GB, ruang disk 1 GB.
- Keyboard (dua pemain bisa berbagi satu keyboard) atau hingga dua gamepad.

Data simpanan ada di `%APPDATA%\SyubadubinStudios\TarkamArena\save.db` (Windows) atau
`~/.config/SyubadubinStudios/TarkamArena/save.db` (Linux/macOS). Galat tak terduga dicatat di
`log.txt` pada folder yang sama.

## Build dari kode sumber

Kebutuhan: [.NET 10 SDK](https://dotnet.microsoft.com/download) (versi dikunci di `global.json`).

```bash
git clone https://github.com/SyubadubinStudios/TarkamArena
cd TarkamArena
dotnet build TarkamArena.slnx
dotnet test tests/TarkamArena.Tests            # 58 tes: simulasi, netcode, konten, audio...
dotnet run --project src/TarkamArena.App       # game-nya
```

Opsi baris perintah:

| Opsi | Fungsi |
|---|---|
| `--fullscreen` | Mulai layar penuh (F11 atau Alt+Enter untuk berganti kapan saja) |
| `--lang en` / `--lang id` | Paksa bahasa antarmuka |
| `--fight` | Langsung masuk pertarungan (pintasan pengembangan) |
| `--capture <folder>` | Mengunjungi setiap layar dan menyimpan screenshot (dipakai untuk dokumentasi ini) |

Menjalankan satu tes:

```bash
dotnet test tests/TarkamArena.Tests --filter "FullyQualifiedName~Mercy_revives_the_loser"
```

## Menjalankan server online

```bash
dotnet run --project src/TarkamArena.Server       # http://localhost:5080, berkas SQLite
```

Lihat [Bermain online](online.md) untuk PostgreSQL dan deployment.

## Alat bantu

```bash
dotnet run --project tools/TarkamArena.Tools -- stages        # render thumbnail arena
dotnet run --project tools/TarkamArena.Tools -- portraits     # render potret petarung tanpa model buatan
dotnet run --project tools/TarkamArena.Tools -- scene pasar-becek pesilat pocong 300 shot.png
dotnet run --project tools/TarkamArena.Tools -- music Horeg horeg.wav
dotnet run --project tools/TarkamArena.Tools -- docs          # perbarui characters.md dan stages.md
python tools/blender/process_all.py                            # model Rodin -> glb ber-rig dan beranimasi
```
