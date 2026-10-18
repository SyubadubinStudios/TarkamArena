"""
Procedural decor kit for Tarkam Arena (Blender MCP step of the content pipeline).

Each builder makes one prop from primitives with PBR materials; `export(id)` writes
assets/source/decor/<id>.glb, which tools/blender/process_prop.py then normalises to 1 m.
Run inside Blender:  exec(open(r"<repo>/tools/blender/decor_kit.py").read()); build("garuda")
"""
import bpy
import math
import os

REPO = os.environ.get("TARKAM_REPO", r"C:\experiment\syubadubin\TarkamArena")
OUT = os.path.join(REPO, "assets", "source", "decor")
_mats = {}


def clear():
    # Work in a scene of our own so the user's open scene is never touched.
    scene = bpy.data.scenes.get("tarkam-decor") or bpy.data.scenes.new("tarkam-decor")
    if bpy.context.window is not None:
        bpy.context.window.scene = scene
    for o in list(scene.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for m in list(bpy.data.meshes):
        if m.users == 0:
            bpy.data.meshes.remove(m)
    _mats.clear()


def mat(hexcol, rough=0.7, metal=0.0, emit=0.0, alpha=1.0):
    key = (hexcol, rough, metal, emit, alpha)
    if key in _mats:
        return _mats[key]
    m = bpy.data.materials.new(f"m{hexcol:06x}_{len(_mats)}")
    m.use_nodes = True
    bsdf = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    c = ((hexcol >> 16 & 255) / 255, (hexcol >> 8 & 255) / 255, (hexcol & 255) / 255, 1)
    c = tuple(x ** 2.2 for x in c[:3]) + (1,)
    bsdf.inputs["Base Color"].default_value = c
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = metal
    if emit > 0:
        bsdf.inputs["Emission Color"].default_value = c
        bsdf.inputs["Emission Strength"].default_value = emit
    if alpha < 1:
        bsdf.inputs["Alpha"].default_value = alpha
    _mats[key] = m
    return m


def _finish(o, m, rot):
    if rot:
        o.rotation_euler = rot
    o.data.materials.append(m)
    return o


def box(loc, size, m, rot=None):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.active_object
    o.scale = size
    return _finish(o, m, rot)


def cyl(loc, r, depth, m, rot=None, verts=16, r2=None):
    if r2 is None:
        bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=depth, location=loc)
    else:
        bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r, radius2=r2, depth=depth, location=loc)
    return _finish(bpy.context.active_object, m, rot)


def cone(loc, r, depth, m, rot=None, verts=16):
    return cyl(loc, r, depth, m, rot, verts, r2=0)


def sph(loc, scale, m, rot=None, segs=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segs, ring_count=max(6, segs // 2), radius=1, location=loc)
    o = bpy.context.active_object
    o.scale = scale if isinstance(scale, tuple) else (scale, scale, scale)
    bpy.ops.object.shade_smooth()
    return _finish(o, m, rot)


def tor(loc, R, r, m, rot=None, seg=24):
    bpy.ops.mesh.primitive_torus_add(major_radius=R, minor_radius=r, major_segments=seg, minor_segments=8, location=loc)
    bpy.ops.object.shade_smooth()
    return _finish(bpy.context.active_object, m, rot)


def wheel(x, y, z, r=0.35, w=0.2, axis="y"):
    rot = (math.pi / 2, 0, 0) if axis == "y" else (0, math.pi / 2, 0)
    cyl((x, y, z), r, w, mat(0x151515, 0.9), rot, verts=20)
    cyl((x, y, z), r * 0.55, w * 1.05, mat(0xB0B0B0, 0.3, 0.8), rot, verts=12)


def _palette(name):
    """Bakes every material into one 16x16-swatch palette texture (+ emission map) and one material,
    because the game draws a decor model as a single mesh with a single material."""
    used = []
    for o in bpy.context.scene.objects:
        if o.type == "MESH":
            for m in o.data.materials:
                if m not in used:
                    used.append(m)
    S, N = 8, 16
    px = [0.0] * (S * N) * (S * N) * 4
    ex = [0.0] * (S * N) * (S * N) * 4
    slot = {}
    any_emit = False
    for i, m in enumerate(used):
        bsdf = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
        c = bsdf.inputs["Base Color"].default_value
        e = bsdf.inputs["Emission Strength"].default_value
        any_emit |= e > 0
        gx, gy = i % N, i // N
        slot[m.name] = ((gx + 0.5) / N, (gy + 0.5) / N)
        for y in range(gy * S, gy * S + S):
            for x in range(gx * S, gx * S + S):
                k = (y * S * N + x) * 4
                # The palette image is sRGB, the material colours are linear.
                sc = [c[j] ** (1 / 2.2) for j in range(3)]
                px[k:k + 4] = [sc[0], sc[1], sc[2], 1]
                f = min(1.0, e / 4) if e > 0 else 0
                ex[k:k + 4] = [sc[0] * f, sc[1] * f, sc[2] * f, 1]
    img = bpy.data.images.new(f"{name}-palette", S * N, S * N)
    img.pixels = px
    img.file_format = "PNG"
    img.pack()
    pal = bpy.data.materials.new(f"{name}-palette")
    pal.use_nodes = True
    nt = pal.node_tree
    bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = img
    tex.interpolation = "Closest"
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.75
    if any_emit:
        eimg = bpy.data.images.new(f"{name}-emit", S * N, S * N)
        eimg.pixels = ex
        eimg.file_format = "PNG"
        eimg.pack()
        etex = nt.nodes.new("ShaderNodeTexImage")
        etex.image = eimg
        etex.interpolation = "Closest"
        nt.links.new(etex.outputs["Color"], bsdf.inputs["Emission Color"])
        bsdf.inputs["Emission Strength"].default_value = 4.0
    for o in bpy.context.scene.objects:
        if o.type != "MESH":
            continue
        me = o.data
        if not me.uv_layers:
            me.uv_layers.new()
        uv = me.uv_layers.active.data
        for poly in me.polygons:
            m = me.materials[poly.material_index] if me.materials else None
            u, v = slot.get(m.name, (0.03, 0.03)) if m else (0.03, 0.03)
            for li in poly.loop_indices:
                uv[li].uv = (u, v)
        me.materials.clear()
        me.materials.append(pal)


def export(name):
    os.makedirs(OUT, exist_ok=True)
    _palette(name)
    bpy.ops.object.select_all(action="DESELECT")
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.join()
    path = os.path.join(OUT, f"{name}.glb")
    bpy.ops.export_scene.gltf(filepath=path, export_format="GLB", use_selection=True, export_animations=False)
    tris = sum(len(p.vertices) - 2 for p in bpy.context.active_object.data.polygons)
    print("exported", name, len(meshes), "parts", tris, "tris")
    return path
