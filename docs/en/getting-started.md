# Getting started

## Install a release

Download the installer for your platform from the
[latest release](https://github.com/SyubadubinStudios/TarkamArena/releases/latest):

| Platform | File | Notes |
|---|---|---|
| Windows 10/11 x64 | `TarkamArena.Setup.exe` | Standard installer with Start menu and desktop shortcuts |
| Windows (Store) | `TarkamArena.msix` | Microsoft Store package (unsigned until submitted to the Store) |
| Windows portable | `TarkamArena-win-x64-portable.zip` | Unzip and run `TarkamArena.exe` |
| Linux x64 | `TarkamArena.AppImage` | `chmod +x TarkamArena.AppImage && ./TarkamArena.AppImage` |
| Debian / Ubuntu | `TarkamArena.deb` | `sudo apt install ./TarkamArena.deb`, then run `tarkam-arena` |
| Fedora / openSUSE | `TarkamArena.rpm` | `sudo dnf install ./TarkamArena.rpm` |
| macOS (Apple Silicon) | `TarkamArena.dmg` | Drag to Applications. The build is not notarised: right-click → Open the first time |

## System requirements

- 64-bit Windows 10+, a modern Linux distribution, or macOS 12+ on Apple Silicon.
- A GPU with Direct3D 12, Vulkan or Metal (ThreeNet renders through wgpu). An Intel UHD 620 runs the
  game at the *Medium* preset.
- 4 GB RAM, 1 GB of disk space.
- Keyboard (two players can share one) or up to two gamepads.

Save data lives in `%APPDATA%\SyubadubinStudios\TarkamArena\save.db` (Windows) or
`~/.config/SyubadubinStudios/TarkamArena/save.db` (Linux/macOS). Unexpected errors are written to
`log.txt` in the same folder.

## Build from source

Requirements: the [.NET 10 SDK](https://dotnet.microsoft.com/download) (pinned in `global.json`).

```bash
git clone https://github.com/SyubadubinStudios/TarkamArena
cd TarkamArena
dotnet build TarkamArena.slnx
dotnet test tests/TarkamArena.Tests            # 58 tests: simulation, netcode, content, audio...
dotnet run --project src/TarkamArena.App       # the game
```

Useful command-line switches for the game:

| Switch | Effect |
|---|---|
| `--fullscreen` | Start in full screen (F11 or Alt+Enter toggles it any time) |
| `--lang en` / `--lang id` | Override the interface language |
| `--fight` | Jump straight into a fight (development shortcut) |
| `--capture <dir>` | Visit every screen and save screenshots (used for this documentation) |

Run a single test:

```bash
dotnet test tests/TarkamArena.Tests --filter "FullyQualifiedName~Mercy_revives_the_loser"
```

## Run the online server

```bash
dotnet run --project src/TarkamArena.Server       # http://localhost:5080, SQLite file
```

See [Online play](online.md) for PostgreSQL and deployment.

## Tools

```bash
dotnet run --project tools/TarkamArena.Tools -- stages        # render stage thumbnails
dotnet run --project tools/TarkamArena.Tools -- portraits     # render portraits for fighters without an authored model
dotnet run --project tools/TarkamArena.Tools -- scene pasar-becek pesilat pocong 300 shot.png
dotnet run --project tools/TarkamArena.Tools -- music Horeg horeg.wav
dotnet run --project tools/TarkamArena.Tools -- docs          # regenerate characters.md and stages.md
python tools/blender/process_all.py                            # Rodin models -> rigged, animated glb
```
