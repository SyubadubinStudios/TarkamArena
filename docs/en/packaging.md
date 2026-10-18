# Packaging and releases

## Automated releases

Pushing a version tag builds every installer on GitHub Actions and publishes a release:

```bash
git tag v0.1.0
git push origin v0.1.0
```

`.github/workflows/release.yml` runs the tests, then three platform jobs:

| Job | Runner | Produces |
|---|---|---|
| windows | `windows-latest` | `TarkamArena.Setup.exe` (Inno Setup), `TarkamArena.msix` (Microsoft Store), portable zip, server zip |
| linux | `ubuntu-latest` | `TarkamArena.AppImage`, `TarkamArena.deb`, `TarkamArena.rpm`, server tarball |
| macos | `macos-14` | `TarkamArena.dmg` (Apple Silicon `.app` bundle) |

## Local builds

```bash
dotnet publish src/TarkamArena.App -c Release -r win-x64 --self-contained -p:PublishReadyToRun=true -o out/win-x64
iscc /DAppVersion=0.1.0 /DSourceDir=..\..\out\win-x64 packaging\windows\TarkamArena.iss
pwsh packaging/msix/build-msix.ps1 -Version 0.1.0 -PublishDir out/win-x64

dotnet publish src/TarkamArena.App -c Release -r linux-x64 --self-contained -o out/linux-x64
bash packaging/linux/build-linux-packages.sh 0.1.0 out/linux-x64 out/installers

dotnet publish src/TarkamArena.App -c Release -r osx-arm64 --self-contained -o out/osx-arm64
bash packaging/macos/build-dmg.sh 0.1.0 out/osx-arm64 out/installers
```

The game's `assets` folder is copied next to the executable by the project file (raw Rodin sources
and concept art are excluded).

## Signing

The CI builds are unsigned. For public distribution:

- **Windows**: sign `TarkamArena.exe` and the setup with `signtool` and a code-signing certificate;
  the MSIX is signed by the Microsoft Store on submission.
- **macOS**: sign with a Developer ID certificate and notarise with `notarytool`.

## Consoles

Xbox, PlayStation and Nintendo Switch packages require the platform holders' licensed SDKs, dev kits
and signing keys, which cannot be used from a public repository. See
[packaging/consoles/README.md](../../packaging/consoles/README.md) for what a port involves.
