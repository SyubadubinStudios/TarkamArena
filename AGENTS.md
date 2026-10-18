# Repository Guidelines

## Project Structure & Module Organization

Tarkam Arena is a .NET 10 fighting game. `src/` contains `TarkamArena.Core` (deterministic combat and content), `App` (Avalonia UI), `Engine` (rendering/audio), `Networking`, `Physics`, `Persistence`, and `Server`, all using the `TarkamArena.` prefix. Tests live in `tests/TarkamArena.Tests/`. Runtime assets belong in `assets/`; tooling in `tools/TarkamArena.Tools/` and `tools/blender/`; installers in `packaging/`. Maintain English and Indonesian documentation under `docs/en/` and `docs/id/`.

## Build, Test, and Development Commands

Use the SDK specified in `global.json` (10.0.401, with feature-band roll-forward). Run from the repository root:

- `dotnet restore TarkamArena.slnx` — restore dependencies.
- `dotnet build TarkamArena.slnx -c Release --no-restore` — build all projects as CI does.
- `dotnet test tests/TarkamArena.Tests -c Release --no-build` — run the CI test suite after building.
- `dotnet test tests/TarkamArena.Tests --filter "FullyQualifiedName~Rollback"` — run focused rollback tests.
- `dotnet run --project src/TarkamArena.App` — launch the game.
- `dotnet run --project src/TarkamArena.Server` — start online services.
- `dotnet run --project tools/TarkamArena.Tools -- docs` — regenerate bilingual character and stage references.

## Coding Style & Naming Conventions

Follow existing C#: four-space indentation, file-scoped namespaces, braces on separate lines, PascalCase types/methods, camelCase locals/parameters, and `_camelCase` private fields. Match filenames to their primary type. Nullable references and implicit usings are enabled. Build-time .NET analyzers use `latest-recommended`; no dedicated formatter configuration exists. Manage dependency versions in `Directory.Packages.props`.

## Architecture Constraints

Keep `Core` dependency-free. Simulation steps must remain deterministic, fixed-point, and allocation-free; never add floats or reference fields to `MatchState` or `FighterState`. Presentation consumes state and events without affecting combat. Preserve roster/stage ordering by appending entries. Supply English and Indonesian text through `LocalizedText` or `Loc.T`.

## Testing Guidelines

Use xUnit `[Fact]`/`[Theory]` tests in `*Tests.cs`, with descriptive underscore-separated names such as `Jab_hits_an_idle_opponent`. Add regression tests for changed behavior, particularly rollback, determinism, allocations, and content/localization contracts. No numeric coverage threshold is configured. For visual changes, use the tools' `scene` or `signatures` commands and inspect output.

## Commit & Pull Request Guidelines

History uses concise descriptive subjects, sometimes prefixed by area, such as `Fix MSIX packaging: ...`; Conventional Commits are not established. Keep changes focused. PRs should explain behavior changes, link relevant issues, record validation, and include screenshots for UI/rendering changes. Update bilingual documentation, `PLAN.md`, and `Progress.md` when applicable.
