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

## Arm repair and export validation

The original automatic rig can mistake a weapon or wing tip for a hand. For existing fighters,
use `repair_character_arms.py` with the reviewed shoulder/elbow/wrist/palm positions in
`arm_landmarks.json` and rigid prop regions in `arm_skin_overrides.json`. It preserves source
geometry and textures, rebinds the arms, and rebuilds all 14 arm animations with two-bone IK.
The landmark coordinates refer to the front orthographic bind-pose audit (scaled to 240 px wide).
Review them whenever the source mesh changes; they are not universal humanoid proportions.

Keep the original GLBs as the input; do not repeatedly repair already-repaired exports. This
checkout's baseline is backed up under `out/character-audit/original-models/`. With Blender on PATH:

```bash
blender -b --python-exit-code 1 -P tools/blender/repair_character_arms.py -- --models out/character-audit/original-models --output out/character-audit/repaired
blender -b --python-exit-code 1 -P tools/blender/audit_characters.py -- --models out/character-audit/repaired --output out/character-audit/after
```

Inspect the rest/front/side, idle and jab captures and `audit.json`; errors fail the audit.

Rodin meshes are welded: where a hand rested on the hip or thigh in the source pose, the arm surface
is stitched to the body, and once the repaired arms move those stitches stretch into sheets hanging
from the hands. `cut_arm_seams.py` frees them without touching props, hair or faces: it snaps
half-arm/half-body vertices to one side (blending is kept only around the shoulder), returns
arm-weighted surface that lies on the torso (vests, shirt fronts) to the body, evaluates every clip
and cuts only the arm-to-body seam faces that stretch, then removes the loose fragments and closes
short holes.

```bash
blender -b --python-exit-code 1 -P tools/blender/cut_arm_seams.py -- --models out/character-audit/repaired --output out/character-audit/seamed
dotnet run --project tools/TarkamArena.Tools -- previews out/character-audit/review/idle - out/character-audit/seamed idle
dotnet run --project tools/TarkamArena.Tools -- previews out/character-audit/review/heavy - out/character-audit/seamed heavy
dotnet run --project tools/TarkamArena.Tools -- previews out/character-audit/review/victory - out/character-audit/seamed victory
```

`previews <dir> [id,id|-] [models dir|-] [idle|jab|heavy|block|victory]` renders through the game's
own glTF path, so what you review is what players see. Copy only the fighters that pass to
`assets/models/characters/`; keep the original model for any that still show floating parts.
In this pass 40 of 42 were promoted; **genderuwo** (claws float off the arms: its hand landmarks
do not match the hand geometry) and **tukang-parkir** (vest sides lift with the arms in Victory)
keep their original models. Then recapture the UI portraits and the audit:

```bash
dotnet run --project tools/TarkamArena.Tools -- previews          # transparent portraits -> assets/generated/previews
dotnet run --project tools/TarkamArena.Tools -- characters out/character-audit/engine
```

The `characters` command captures five poses per roster entry using the actual runtime model,
rejects authored GLBs with missing clips instead of silently using a fallback, and writes a manifest.
It also covers the nine procedural DLC fighters. All three Blender scripts accept `--only id,id`.
Set `TARKAM_ASSETS` to the repository's absolute `assets` path when checking a development build;
rebuild the app afterwards so copied content beside the executable is refreshed.

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
