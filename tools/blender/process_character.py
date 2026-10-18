"""
Tarkam Arena character pipeline step: Rodin MCP model -> game-ready rigged, animated glTF.

Run headless (no UI needed):
    blender -b -P tools/blender/process_character.py -- <input.glb> <output.glb> <height_m> [preview.png]

Steps (see docs/en/asset-pipeline.md):
  1. Import the Rodin glb, join meshes, merge doubles, apply transforms.
  2. Optimise: decimate to the triangle budget, keep UVs and textures.
  3. Normalise: scale to the fighter's height, feet on the ground, centred, facing -Y.
  4. Rig: build a 15-bone humanoid armature from the mesh proportions and skin it
     (automatic weights, envelope fallback when bone-heat fails on generated meshes).
  5. Animate: author the fighting-game clip set the runtime expects (names are a contract
     with TarkamArena.Engine.FighterAnimator).
  6. Export glb with every action as a separate glTF animation; optionally render a preview.
"""
import bpy
import math
import sys
from mathutils import Euler, Quaternion, Vector

TRI_BUDGET = 24000
FPS = 30

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
if len(argv) < 3:
    raise SystemExit("usage: blender -b -P process_character.py -- in.glb out.glb height [preview.png]")
SRC, DST, HEIGHT = argv[0], argv[1], float(argv[2])
PREVIEW = argv[3] if len(argv) > 3 else None
SHEET = len(argv) > 4 and argv[4] == "sheet"


def log(*a):
    print("[tarkam]", *a, flush=True)


# ---------------------------------------------------------------- 1. import & clean
for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj, do_unlink=True)

bpy.ops.import_scene.gltf(filepath=SRC)
meshes = [o for o in bpy.data.objects if o.type == "MESH"]
for o in bpy.data.objects:
    if o.type not in {"MESH"}:
        # Drop empties / cameras that came with the file, but keep mesh world transforms.
        for child in o.children:
            mw = child.matrix_world.copy()
            child.parent = None
            child.matrix_world = mw
for o in [o for o in bpy.data.objects if o.type != "MESH"]:
    bpy.data.objects.remove(o, do_unlink=True)

bpy.ops.object.select_all(action="DESELECT")
for o in meshes:
    o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
if len(meshes) > 1:
    bpy.ops.object.join()
body = bpy.context.view_layer.objects.active
body.name = "Body"
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.mesh.remove_doubles(threshold=0.0005)
bpy.ops.object.mode_set(mode="OBJECT")

# ---------------------------------------------------------------- 2. optimise
tris = sum(len(p.vertices) - 2 for p in body.data.polygons)
if tris > TRI_BUDGET:
    mod = body.modifiers.new("Decimate", "DECIMATE")
    mod.ratio = TRI_BUDGET / tris
    mod.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=mod.name)
log("triangles", tris, "->", sum(len(p.vertices) - 2 for p in body.data.polygons))

# ---------------------------------------------------------------- 3. normalise
def bounds(o):
    pts = [o.matrix_world @ v.co for v in o.data.vertices]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return mn, mx

mn, mx = bounds(body)
size = mx - mn
# Generated models are usually Z-up after import; if Y is the tallest axis, stand it up.
if size.y > size.z * 1.2:
    body.rotation_euler = Euler((math.radians(90), 0, 0))
    bpy.ops.object.transform_apply(rotation=True)
    mn, mx = bounds(body)
    size = mx - mn

scale = HEIGHT / size.z
body.scale = (scale, scale, scale)
bpy.ops.object.transform_apply(scale=True)
mn, mx = bounds(body)
body.location = (-(mn.x + mx.x) / 2, -(mn.y + mx.y) / 2, -mn.z)
bpy.ops.object.transform_apply(location=True)
mn, mx = bounds(body)
log("normalised size", tuple(round(v, 3) for v in (mx - mn)))

# ---------------------------------------------------------------- 4. rig
h = HEIGHT
verts = [v.co for v in body.data.vertices]

def width_at(z, band=0.03):
    xs = [v.x for v in verts if abs(v.z - z) < band * h]
    return (min(xs), max(xs)) if xs else (-0.1 * h, 0.1 * h)

# Generated models come in T-pose or A-pose. Shoulders sit at ~82% of the height; the hands are
# the vertices furthest from the centre line, which also tells us how far the arms droop.
shoulder_z = 0.82 * h
shoulder_x = 0.10 * h
hands = {}
for side, sx in (("L", 1), ("R", -1)):
    cand = [v for v in verts if sx * v.x > 0.15 * h and 0.3 * h < v.z < 0.95 * h]
    tip = max(cand, key=lambda v: sx * v.x) if cand else Vector((sx * 0.4 * h, 0, shoulder_z))
    hands[side] = (abs(tip.x), tip.z)
hand_x = max(hands["L"][0], hands["R"][0])
DROOP = {side: math.degrees(math.atan2(shoulder_z - hz, max(hx - shoulder_x, 1e-3))) for side, (hx, hz) in hands.items()}
hip_z = 0.50 * h
chest_z = shoulder_z - 0.10 * h
neck_z = shoulder_z + 0.04 * h
head_top = h
leg_x = 0.055 * h
log("hands", {k: (round(x, 3), round(z, 3)) for k, (x, z) in hands.items()}, "droop", {k: round(v, 1) for k, v in DROOP.items()})

arm_data = bpy.data.armatures.new("Rig")
rig = bpy.data.objects.new("Rig", arm_data)
bpy.context.collection.objects.link(rig)
bpy.ops.object.select_all(action="DESELECT")
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.object.mode_set(mode="EDIT")
eb = arm_data.edit_bones

def bone(name, head, tail, parent=None, connect=False):
    b = eb.new(name)
    b.head = Vector(head)
    b.tail = Vector(tail)
    b.roll = 0
    if parent:
        b.parent = eb[parent]
        b.use_connect = connect
    return b

bone("root", (0, 0, 0), (0, 0.15 * h, 0))
bone("hips", (0, 0, hip_z), (0, 0, hip_z + 0.08 * h), "root")
bone("spine", (0, 0, hip_z + 0.08 * h), (0, 0, chest_z), "hips", True)
bone("chest", (0, 0, chest_z), (0, 0, neck_z), "spine", True)
bone("head", (0, 0, neck_z), (0, 0, head_top), "chest", True)
for side, sx in (("L", 1), ("R", -1)):
    hx, hz = hands[side]
    sh = Vector((sx * shoulder_x, 0, shoulder_z - 0.02 * h))
    tip = Vector((sx * hx, 0, hz))
    elbow = sh.lerp(tip, 0.45)
    wrist = sh.lerp(tip, 0.86)
    bone(f"upper_arm.{side}", sh, elbow, "chest")
    bone(f"forearm.{side}", elbow, wrist, f"upper_arm.{side}", True)
    bone(f"hand.{side}", wrist, tip, f"forearm.{side}", True)
    bone(f"thigh.{side}", (sx * leg_x, 0, hip_z), (sx * leg_x, 0, 0.27 * h), "hips")
    bone(f"shin.{side}", (sx * leg_x, 0, 0.27 * h), (sx * leg_x, 0, 0.05 * h), f"thigh.{side}", True)
    bone(f"foot.{side}", (sx * leg_x, 0, 0.05 * h), (sx * leg_x, -0.10 * h, 0.01 * h), f"shin.{side}", True)
bpy.ops.object.mode_set(mode="OBJECT")

bpy.ops.object.select_all(action="DESELECT")
body.select_set(True)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.object.parent_set(type="ARMATURE_AUTO")

weighted = sum(1 for v in body.data.vertices if any(g.weight > 0.01 for g in v.groups))
coverage = weighted / max(1, len(body.data.vertices))
log("auto weight coverage", round(coverage, 3))
if coverage < 0.9:
    log("bone heat failed, falling back to envelopes")
    for vg in list(body.vertex_groups):
        body.vertex_groups.remove(vg)
    for b in arm_data.bones:
        b.envelope_distance = 0.12 * h
        b.head_radius = 0.06 * h
        b.tail_radius = 0.06 * h
    bpy.ops.object.select_all(action="DESELECT")
    body.select_set(True)
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.parent_set(type="ARMATURE_ENVELOPE")
    # Bake envelopes into vertex groups so glTF exports real skin weights.
    bpy.context.view_layer.objects.active = body
    mod = next(m for m in body.modifiers if m.type == "ARMATURE")
    mod.use_vertex_groups = False
    mod.use_bone_envelopes = True
    for b in arm_data.bones:
        body.vertex_groups.new(name=b.name)
    for v in body.data.vertices:
        world = body.matrix_world @ v.co
        best, best_d = None, 1e9
        for b in arm_data.bones:
            a, t = b.head_local, b.tail_local
            ab = t - a
            u = max(0.0, min(1.0, (world - a).dot(ab) / max(ab.length_squared, 1e-9)))
            d = (world - (a + ab * u)).length
            if d < best_d:
                best, best_d = b.name, d
        body.vertex_groups[best].add([v.index], 1.0, "REPLACE")
    mod.use_vertex_groups = True
    mod.use_bone_envelopes = False

# ---------------------------------------------------------------- 5. animate
bpy.context.scene.render.fps = FPS
bpy.context.view_layer.objects.active = rig
bpy.ops.object.mode_set(mode="POSE")
for pb in rig.pose.bones:
    pb.rotation_mode = "QUATERNION"

def armature_rot(pb, x=0.0, y=0.0, z=0.0):
    """Rotation expressed around armature-space axes (X right, Y back, Z up), converted to bone space."""
    if pb.name.startswith("upper_arm."):
        # Poses are authored for a horizontal T-pose arm; remove the rest droop of an A-pose.
        y = y - DROOP["L"] if pb.name.endswith(".L") else y + DROOP["R"]
    world = Euler((math.radians(x), math.radians(y), math.radians(z)), "XYZ").to_matrix().to_4x4()
    rest = pb.bone.matrix_local.to_3x3().to_4x4()
    return (rest.inverted() @ world @ rest).to_quaternion()

GUARD = {
    "upper_arm.L": (0, 52, -30), "upper_arm.R": (0, -52, 30),
    "forearm.L": (-60, 0, -95), "forearm.R": (-60, 0, 95),
    "chest": (0, 0, 8), "thigh.L": (-8, 0, 0), "thigh.R": (10, 0, 0), "shin.L": (14, 0, 0), "shin.R": (6, 0, 0),
}

def merge(base, extra):
    out = dict(base)
    out.update(extra)
    return out

def pose(**over):
    return merge(GUARD, {k.replace("_L", ".L").replace("_R", ".R"): v for k, v in over.items()})

CLIPS = {
    # name: (loop, [(frame, pose_dict, root_offset_z)])
    "Idle": (True, [
        (0, pose(), 0), (18, pose(chest=(3, 0, 8), shin_L=(20, 0, 0), shin_R=(12, 0, 0)), -0.015), (36, pose(), 0)]),
    "Walk": (True, [
        (0, pose(thigh_L=(-28, 0, 0), thigh_R=(22, 0, 0), shin_R=(30, 0, 0)), 0),
        (12, pose(thigh_L=(22, 0, 0), thigh_R=(-28, 0, 0), shin_L=(30, 0, 0)), 0.02),
        (24, pose(thigh_L=(-28, 0, 0), thigh_R=(22, 0, 0), shin_R=(30, 0, 0)), 0)]),
    "Jab": (False, [
        (0, pose(), 0),
        (3, pose(upper_arm_R=(0, -8, 88), forearm_R=(0, 0, 4), chest=(0, 0, -18)), 0),
        (9, pose(), 0)]),
    "Heavy": (False, [
        (0, pose(), 0),
        (6, pose(upper_arm_L=(0, 40, 40), chest=(0, 0, 35), hips=(0, 0, 10)), -0.02),
        (10, pose(upper_arm_L=(0, -5, -92), forearm_L=(0, 0, -6), chest=(10, 0, -35), hips=(0, 0, -15)), -0.03),
        (17, pose(), 0)]),
    "Kick": (False, [
        (0, pose(), 0),
        (5, pose(thigh_R=(-95, 0, 0), shin_R=(0, 0, 0), chest=(-12, 0, 0)), 0),
        (13, pose(), 0)]),
    "Uppercut": (False, [
        (0, pose(), -0.06),
        (5, pose(upper_arm_R=(0, -95, 60), forearm_R=(0, 0, 30), chest=(-15, 0, -25)), 0.05),
        (14, pose(), 0)]),
    "Special": (False, [
        (0, pose(), 0),
        (6, pose(upper_arm_L=(0, 10, -85), upper_arm_R=(0, -10, 85), forearm_L=(0, 0, 0), forearm_R=(0, 0, 0), chest=(8, 0, 0)), 0),
        (16, pose(upper_arm_L=(0, 10, -85), upper_arm_R=(0, -10, 85), forearm_L=(0, 0, 0), forearm_R=(0, 0, 0), chest=(8, 0, 0)), 0),
        (22, pose(), 0)]),
    "Grab": (False, [
        (0, pose(), 0),
        (5, pose(upper_arm_L=(0, 15, -80), upper_arm_R=(0, -15, 80), forearm_L=(0, 0, -30), forearm_R=(0, 0, 30)), 0),
        (12, pose(chest=(-25, 0, 0), upper_arm_L=(0, -60, -30), upper_arm_R=(0, 60, 30)), 0.02),
        (20, pose(), 0)]),
    "Hit": (False, [
        (0, pose(), 0),
        (3, pose(chest=(-22, 0, 10), head=(-20, 0, 0), upper_arm_L=(0, 30, 10), upper_arm_R=(0, -30, -10)), 0),
        (11, pose(), 0)]),
    "Block": (False, [
        (0, pose(), 0),
        (3, pose(upper_arm_L=(0, 25, -75), upper_arm_R=(0, -25, 75), forearm_L=(-105, 0, -10), forearm_R=(-105, 0, 10), chest=(12, 0, 0)), -0.03),
        (9, pose(upper_arm_L=(0, 25, -75), upper_arm_R=(0, -25, 75), forearm_L=(-105, 0, -10), forearm_R=(-105, 0, 10), chest=(12, 0, 0)), -0.03)]),
    "Jump": (False, [
        (0, pose(thigh_L=(-55, 0, 0), thigh_R=(-40, 0, 0), shin_L=(95, 0, 0), shin_R=(80, 0, 0)), 0),
        (18, pose(thigh_L=(-55, 0, 0), thigh_R=(-40, 0, 0), shin_L=(95, 0, 0), shin_R=(80, 0, 0)), 0)]),
    "KO": (False, [
        (0, pose(), 0),
        (8, pose(root=(-45, 0, 0), chest=(-15, 0, 0)), 0),
        (18, {"root": (-88, 0, 0), "upper_arm.L": (0, 20, 0), "upper_arm.R": (0, -20, 0)}, 0.05),
        (24, {"root": (-90, 0, 0), "upper_arm.L": (0, 20, 0), "upper_arm.R": (0, -20, 0)}, 0.05)]),
    "Dizzy": (True, [
        (0, pose(chest=(10, 12, 0), head=(15, 15, 0)), -0.04),
        (20, pose(chest=(10, -12, 0), head=(15, -15, 0)), -0.04),
        (40, pose(chest=(10, 12, 0), head=(15, 15, 0)), -0.04)]),
    "Victory": (True, [
        (0, {"upper_arm.L": (0, -70, 0), "upper_arm.R": (0, 70, 0), "forearm.L": (0, -20, 0), "forearm.R": (0, 20, 0)}, 0),
        (15, {"upper_arm.L": (0, -80, 10), "upper_arm.R": (0, 80, -10), "forearm.L": (0, -30, 0), "forearm.R": (0, 30, 0), "chest": (-8, 0, 0)}, 0.06),
        (30, {"upper_arm.L": (0, -70, 0), "upper_arm.R": (0, 70, 0), "forearm.L": (0, -20, 0), "forearm.R": (0, 20, 0)}, 0)]),
}

rig.animation_data_create()
for name, (loop, keys) in CLIPS.items():
    action = bpy.data.actions.new(name)
    action.use_fake_user = True
    rig.animation_data.action = action
    for frame, pose_dict, lift in keys:
        for pb in rig.pose.bones:
            x, y, z = pose_dict.get(pb.name, (0, 0, 0))
            pb.rotation_quaternion = armature_rot(pb, x, y, z)
            pb.keyframe_insert("rotation_quaternion", frame=frame)
        hips = rig.pose.bones["hips"]
        hips.location = (0, lift * h, 0)  # hips bone Y axis points up in armature space
        hips.keyframe_insert("location", frame=frame)
    # Push to NLA so the exporter writes every clip.
    track = rig.animation_data.nla_tracks.new()
    track.name = name
    strip = track.strips.new(name, int(keys[0][0]), action)
    track.mute = True
rig.animation_data.action = None
bpy.ops.object.mode_set(mode="OBJECT")
log("clips", list(CLIPS))

# ---------------------------------------------------------------- 6. export
# Optimise textures: generated models ship 2K PNGs; 1K JPEG is plenty for a fighter on screen.
for img in bpy.data.images:
    if img.size[0] > 1024:
        img.scale(1024, max(1, int(img.size[1] * 1024 / img.size[0])))
bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.gltf(
    filepath=DST,
    export_format="GLB",
    use_selection=True,
    export_animations=True,
    export_animation_mode="ACTIONS",
    export_skins=True,
    export_yup=True,
    export_apply=False,
    export_image_format="JPEG",
    export_jpeg_quality=85,
)
log("exported", DST)

if PREVIEW:
    scene = bpy.context.scene
    try:
        scene.render.engine = "BLENDER_EEVEE"
    except TypeError as e:
        log("engine fallback:", e)
        try:
            scene.render.engine = "BLENDER_EEVEE"
        except TypeError:
            scene.render.engine = "BLENDER_WORKBENCH"
    rig.animation_data.action = bpy.data.actions["Idle"]
    scene.frame_set(0)
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.lens = 70
    cam = bpy.data.objects.new("Cam", cam_data)
    bpy.context.collection.objects.link(cam)
    cam.location = (0.75 * h, -2.3 * h, 0.62 * h)
    direction = Vector((0, 0, 0.52 * h)) - cam.location
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam
    for loc, energy in (((2, -3, 4), 900), ((-3, -1, 2), 300), ((0, 3, 3), 600)):
        ld = bpy.data.lights.new("L", "AREA")
        ld.energy = energy
        ld.size = 3
        lo = bpy.data.objects.new("L", ld)
        lo.location = loc
        lo.rotation_euler = (Vector((0, 0, 1)) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
        bpy.context.collection.objects.link(lo)
    world = bpy.data.worlds.new("W")
    world.use_nodes = True
    bg = next(n for n in world.node_tree.nodes if n.type == "BACKGROUND")
    bg.inputs[0].default_value = (0.09, 0.2, 0.48, 1)
    bg.inputs[1].default_value = 0.6
    scene.world = world
    scene.render.film_transparent = True
    scene.render.resolution_x = 512
    scene.render.resolution_y = 640
    scene.render.filepath = PREVIEW
    bpy.ops.render.render(write_still=True)
    log("preview", PREVIEW)

    if SHEET:
        # Contact sheet of key poses, for checking the animation set by eye.
        scene.render.resolution_x = 256
        scene.render.resolution_y = 320
        # Profile view, like the in-game camera.
        cam.location = (2.6 * h, -0.25 * h, 0.6 * h)
        cam.rotation_euler = (Vector((0, 0, 0.5 * h)) - cam.location).to_track_quat("-Z", "Y").to_euler()
        for clip, frame in (("Jab", 3), ("Heavy", 10), ("Kick", 5), ("Block", 3), ("Grab", 12), ("Hit", 3), ("Jump", 0), ("KO", 24), ("Victory", 15), ("Uppercut", 5)):
            rig.animation_data.action = bpy.data.actions[clip]
            scene.frame_set(frame)
            scene.render.filepath = PREVIEW.replace(".png", f"_{clip}.png")
            bpy.ops.render.render(write_still=True)
        log("sheet done")
