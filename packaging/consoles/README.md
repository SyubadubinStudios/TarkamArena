# Console builds / Build konsol

**English.** Xbox, PlayStation and Nintendo Switch packages cannot be produced from a public
repository or a public CI runner. Each platform holder only gives its SDK, packaging tools and
signing keys to registered developers, under NDA, with a dev kit:

| Target | Programme | Package | What Tarkam Arena needs |
|---|---|---|---|
| Xbox Series X\|S / Xbox One | ID@Xbox + Microsoft GDK | `.xvc` / MSIXVC | A GDK host for the ThreeNet D3D12 back end, GDK input instead of the desktop gamepad layer |
| PlayStation 4/5 | PlayStation Partners | `.pkg` | A ThreeNet back end for the platform graphics API, platform audio and input |
| Nintendo Switch | Nintendo Developer Portal | `.nsp` | A ThreeNet back end for NVN, NativeAOT for the Switch toolchain |

The game is prepared for this: the simulation (`TarkamArena.Core`) is pure .NET with no platform
calls, rendering is isolated behind `TarkamArena.Engine`, input is abstracted by `InputMapper`,
and audio by `AudioDirector`. A console port replaces the Avalonia shell with a platform host
and keeps everything else.

The Windows `TarkamArena.msix` from the release workflow is the Microsoft Store package for
Windows PCs.

**Bahasa Indonesia.** Paket Xbox, PlayStation, dan Nintendo Switch tidak bisa dibuat dari
repositori atau CI publik. SDK, alat pengemasan, dan kunci tanda tangan hanya diberikan kepada
developer terdaftar (dengan NDA dan dev kit). Arsitektur game sudah disiapkan: simulasi murni
.NET, rendering terisolasi di `TarkamArena.Engine`, input dan audio terabstraksi. Port konsol cukup
mengganti shell Avalonia dengan host platform. `TarkamArena.msix` adalah paket Microsoft Store
untuk PC Windows.
