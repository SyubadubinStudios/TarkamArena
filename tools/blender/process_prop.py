"""
Static prop / crowd-extra step of the Tarkam Arena asset pipeline (no rig).

    blender -b -P tools/blender/process_prop.py -- <input.glb> <output.glb> <height_m> <triangle_budget>

Joins the Rodin mesh, decimates it to the budget (crowd extras are drawn with GPU instancing, so
they must be cheap), normalises the height, puts the feet on the ground, centres it, shrinks
textures to 512 px and exports a single-mesh glb.
"""
import bpy
import sys
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
SRC, DST, HEIGHT, BUDGET = argv[0], argv[1], float(argv[2]), int(argv[3])

for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj, do_unlink=True)
bpy.ops.import_scene.gltf(filepath=SRC)
meshes = [o for o in bpy.data.objects if o.type == "MESH"]
for o in meshes:
    mw = o.matrix_world.copy()
    o.parent = None
    o.matrix_world = mw
for o in [o for o in bpy.data.objects if o.type != "MESH"]:
    bpy.data.objects.remove(o, do_unlink=True)

bpy.ops.object.select_all(action="DESELECT")
for o in meshes:
    o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
if len(meshes) > 1:
    bpy.ops.object.join()
body = bpy.context.view_layer.objects.active
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

tris = sum(len(p.vertices) - 2 for p in body.data.polygons)
if tris > BUDGET:
    mod = body.modifiers.new("Decimate", "DECIMATE")
    mod.ratio = BUDGET / tris
    bpy.ops.object.modifier_apply(modifier=mod.name)

pts = [body.matrix_world @ v.co for v in body.data.vertices]
mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
s = HEIGHT / (mx.z - mn.z)
body.scale = (s, s, s)
bpy.ops.object.transform_apply(scale=True)
pts = [body.matrix_world @ v.co for v in body.data.vertices]
mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
body.location = (-(mn.x + mx.x) / 2, -(mn.y + mx.y) / 2, -mn.z)
bpy.ops.object.transform_apply(location=True)

for img in bpy.data.images:
    if img.size[0] > 512:
        img.scale(512, max(1, int(img.size[1] * 512 / img.size[0])))

bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.gltf(filepath=DST, export_format="GLB", use_selection=True, export_animations=False,
                          export_image_format="JPEG", export_jpeg_quality=80)
print("[tarkam] prop", DST, "tris", sum(len(p.vertices) - 2 for p in body.data.polygons), flush=True)
