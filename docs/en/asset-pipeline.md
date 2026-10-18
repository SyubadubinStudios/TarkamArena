# Asset pipeline

```
Concept Art ──► Rodin MCP ──► Blender MCP ──► Optimization ──► ThreeNet ──► Build
 (Nano Banana)  (text→3D glb)  (rig, animate)  (decimate, 1K)   (glTF load)   (copied to output)
```

All art in the game was produced with this pipeline:

| Asset | Tool | Where |
|---|---|---|
| 42 fighter models | Rodin MCP `Generate3DFromPrompt` (prompts in `tools/rodin/characters.json`) | `assets/source/rodin/*.glb` (not in git, 404 MB) |
| Rigging, 14 animation clips, decimation, texture optimisation, preview renders | Blender 5.2 headless via `tools/blender/process_character.py` | `assets/models/characters/*.glb`, `assets/generated/previews/*.png` |
| 4 instanced crowd extras | Rodin MCP + `tools/blender/process_prop.py` (2.5k tris) | `assets/models/crowd/` |
| Concept art, key art, 18 painted stage backdrops, 4 ground textures, wayang silhouettes | Rodin MCP `GenerateImageWithNanoBanana2` | `assets/concept`, `assets/ui`, `assets/backdrops`, `assets/textures` |
| 13 announcer lines | Rodin MCP `GenerateVoiceWithElevenLabsV3` | `assets/audio/voice/*.mp3` |
| Music and sound effects | Procedural synthesis in `TarkamArena.Engine.Audio` | generated at runtime |

## Character processing

```bash
python tools/blender/process_all.py            # every assets/source/rodin/*.glb that changed
python tools/blender/process_all.py --only pesilat --force
```

`process_character.py` runs in Blender without UI (`blender -b`):

1. Imports the Rodin glb, joins meshes, merges doubles.
2. Decimates to 24,000 triangles.
3. Scales to the fighter's height from `Roster.cs` (so the model matches the hurtbox), puts the feet
   on the ground, centres it.
4. Builds a 15-bone humanoid armature from the mesh: shoulders at 82 % height, hands at the vertices
   furthest from the centre line (works for T-pose and A-pose), and skins it with automatic weights,
   falling back to envelopes when bone-heat fails.
5. Authors the clip set the runtime expects: `Idle, Walk, Jab, Heavy, Kick, Uppercut, Special, Grab,
   Hit, Block, Jump, KO, Dizzy, Victory`. Poses are written for a T-pose and compensated for the
   measured arm droop.
6. Scales textures to 1024 px JPEG and exports glb with one glTF animation per action, then renders a
   transparent preview (used as the portrait in the UI). Pass `sheet` as a fifth argument to render a
   side-view contact sheet of the key poses.

The clip names are a contract with `GltfFighterModel`. A fighter without a glb falls back to
`ProceduralFighterModel`.

## Adding a fighter

1. Add a `CharacterDefinition` to `Roster.cs` (append; ids are save-data keys).
2. Add a prompt to `tools/rodin/characters.json`, generate the model with Rodin MCP and save it as
   `assets/source/rodin/<id>.glb`.
3. `python tools/blender/process_all.py --only <id>`.
4. `dotnet run --project tools/TarkamArena.Tools -- docs` to refresh the roster docs.

## Stages

A stage is a `StageDefinition` in `Stages.cs` plus `assets/backdrops/<id>.jpg` (21:9 painted plate).
`StageBuilder` adds lighting, ground texture and set pieces. An authored environment exported from
Blender as `assets/models/stages/<id>.glb` replaces the procedural set when present.
