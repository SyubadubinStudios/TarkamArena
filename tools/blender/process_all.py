"""
Batch step of the Tarkam Arena asset pipeline: runs process_character.py headless in Blender for
every Rodin source model and writes rigged, animated glb files plus preview renders.

    python tools/blender/process_all.py [--blender PATH] [--only id1,id2] [--force]

Heights come from src/TarkamArena.Core/Content/Roster.cs so the model matches the hurtbox the
simulation uses.
"""
import argparse
import os
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "assets" / "source" / "rodin"
MODELS = ROOT / "assets" / "models" / "characters"
PREVIEWS = ROOT / "assets" / "generated" / "previews"
SCRIPT = ROOT / "tools" / "blender" / "process_character.py"
DEFAULT_BLENDER = r"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"


def heights() -> dict[str, float]:
    text = (ROOT / "src" / "TarkamArena.Core" / "Content" / "Roster.cs").read_text(encoding="utf-8")
    result = {}
    for m in re.finditer(r'Id = "([a-z0-9-]+)".*?Body = new\(([\d.]+)f', text, re.S):
        result[m.group(1)] = float(m.group(2))
    return result


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--blender", default=os.environ.get("BLENDER", DEFAULT_BLENDER))
    ap.add_argument("--only", default="")
    ap.add_argument("--force", action="store_true")
    args = ap.parse_args()

    MODELS.mkdir(parents=True, exist_ok=True)
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    table = heights()
    only = {s for s in args.only.split(",") if s}
    failures = []
    for src in sorted(SOURCE.glob("*.glb")):
        cid = src.stem
        if only and cid not in only:
            continue
        dst = MODELS / f"{cid}.glb"
        if dst.exists() and not args.force and dst.stat().st_mtime > src.stat().st_mtime:
            print(f"skip {cid} (up to date)")
            continue
        height = table.get(cid, 1.72)
        preview = PREVIEWS / f"{cid}.png"
        cmd = [args.blender, "-b", "--factory-startup", "-P", str(SCRIPT), "--", str(src), str(dst), str(height), str(preview)]
        print(f"process {cid} (height {height} m)", flush=True)
        proc = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace")
        for line in proc.stdout.splitlines():
            if line.startswith("[tarkam]"):
                print("   ", line)
        if proc.returncode != 0 or not dst.exists():
            failures.append(cid)
            print(f"FAILED {cid}\n{proc.stdout[-2000:]}\n{proc.stderr[-2000:]}")
    print(f"done, {len(failures)} failures: {failures}")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
