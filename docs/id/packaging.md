# Pengemasan dan rilis

## Rilis otomatis

Mendorong tag versi akan membangun semua installer di GitHub Actions dan menerbitkan rilis:

```bash
git tag v0.1.0
git push origin v0.1.0
```

`.github/workflows/release.yml` menjalankan tes, lalu tiga job platform:

| Job | Runner | Hasil |
|---|---|---|
| windows | `windows-latest` | `TarkamArena.Setup.exe` (Inno Setup), `TarkamArena.msix` (Microsoft Store), zip portabel, zip server |
| linux | `ubuntu-latest` | `TarkamArena.AppImage`, `TarkamArena.deb`, `TarkamArena.rpm`, tarball server |
| macos | `macos-14` | `TarkamArena.dmg` (bundle `.app` Apple Silicon) |

## Build lokal

```bash
dotnet publish src/TarkamArena.App -c Release -r win-x64 --self-contained -p:PublishReadyToRun=true -o out/win-x64
iscc /DAppVersion=0.1.0 /DSourceDir=..\..\out\win-x64 packaging\windows\TarkamArena.iss
pwsh packaging/msix/build-msix.ps1 -Version 0.1.0 -PublishDir out/win-x64

dotnet publish src/TarkamArena.App -c Release -r linux-x64 --self-contained -o out/linux-x64
bash packaging/linux/build-linux-packages.sh 0.1.0 out/linux-x64 out/installers

dotnet publish src/TarkamArena.App -c Release -r osx-arm64 --self-contained -o out/osx-arm64
bash packaging/macos/build-dmg.sh 0.1.0 out/osx-arm64 out/installers
```

Folder `assets` disalin ke sebelah executable oleh berkas proyek (sumber mentah Rodin dan concept art
tidak ikut).

## Tanda tangan digital

Build CI belum ditandatangani. Untuk distribusi publik:

- **Windows**: tandatangani `TarkamArena.exe` dan setup dengan `signtool` dan sertifikat code signing;
  MSIX ditandatangani Microsoft Store saat dikirim.
- **macOS**: tandatangani dengan sertifikat Developer ID dan notarisasi dengan `notarytool`.

## Konsol

Paket Xbox, PlayStation, dan Nintendo Switch membutuhkan SDK berlisensi, dev kit, dan kunci tanda
tangan dari pemilik platform, yang tidak bisa dipakai dari repositori publik. Lihat
[packaging/consoles/README.md](../../packaging/consoles/README.md) untuk gambaran port-nya.
