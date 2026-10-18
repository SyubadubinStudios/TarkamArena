"""Free re-bound arms from the body without damaging props, hair or faces.

Follows repair_character_arms.py. Rodin delivers each fighter as one welded mesh, so where a hand
rested on the hip or thigh in the source pose the arm surface is stitched to the body. Once the arms
move, those stitches stretch into sheets that hang from the hands. This step:

1. snaps every vertex that is partly arm and partly body to one side, except around the shoulder
   where blending is real anatomy, so held props, hair and clothing move rigidly with one bone chain
   instead of smearing between hand and hip;
2. evaluates the skinned mesh across every clip and deletes only the seam faces - faces with both
   arm-owned and body-owned vertices - that stretch beyond a threshold;
3. drops the crumbs the cut leaves and closes short boundary loops, borrowing nearby UVs.

Writes to a staging directory, never over its input:
blender -b -P tools/blender/cut_arm_seams.py -- --models out/character-audit/repaired --output out/character-audit/seamed [--only id,id] [--ratio 2.0]
"""
import argparse
import sys
from pathlib import Path

import bmesh
import bpy
import numpy as np
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from audit_characters import ROOT, load_model

ARM = ('upper_arm.', 'forearm.', 'hand.')


def evaluated_positions(obj):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = obj.evaluated_get(depsgraph)
    mesh = evaluated.to_mesh()
    co = np.empty(len(mesh.vertices) * 3, dtype=np.float32)
    mesh.vertices.foreach_get('co', co)
    evaluated.to_mesh_clear()
    return co.reshape(-1, 3)


def segment_distance(p, a, b):
    ab = b - a
    t = max(0.0, min(1.0, (p - a).dot(ab) / max(ab.length_squared, 1e-9)))
    return (p - a - ab * t).length


def snap_weights(rig, body, height):
    """Arm/body mixing is allowed only near the shoulder; elsewhere a vertex follows one side."""
    names = {g.index: g.name for g in body.vertex_groups}
    to_rig = rig.matrix_world.inverted() @ body.matrix_world
    shoulders = [rig.data.bones[f'upper_arm.{s}'].head_local for s in ('L', 'R')]
    radius = 0.07 * height
    arm_segments = [(b.head_local, b.tail_local) for b in rig.data.bones if b.name.startswith(ARM)]
    trunk_bones = [b for b in rig.data.bones if b.name in ('hips', 'spine', 'chest')]
    trunk_segments = [(b.head_local, b.tail_local) for b in trunk_bones]
    for b in trunk_bones:
        if b.name not in body.vertex_groups:
            body.vertex_groups.new(name=b.name)
    snapped = 0
    for v in body.data.vertices:
        groups = [(names[g.group], g.group, g.weight) for g in v.groups if g.weight > 1e-4]
        arm = sum(w for n, _, w in groups if n.startswith(ARM))
        total = sum(w for _, _, w in groups)
        if total <= 0 or arm <= 0.02 * total:
            continue
        p = to_rig @ v.co
        if arm >= 0.98 * total:
            # Fully arm-owned surface lying on the torso (a vest, a shirt front) belongs to the body;
            # held props stay with the hand because they are not closer to a trunk bone.
            d_arm = min(segment_distance(p, a, b) for a, b in arm_segments)
            d_trunk = min(segment_distance(p, a, b) for a, b in trunk_segments)
            if d_arm > 0.08 * height and d_trunk < d_arm:
                for n, index, w in groups:
                    if n.startswith(ARM):
                        body.vertex_groups[index].remove([v.index])
                nearest = min(trunk_bones, key=lambda b: segment_distance(p, b.head_local, b.tail_local))
                body.vertex_groups[nearest.name].add([v.index], 1.0, 'ADD')
                snapped += 1
            continue
        if min((p - s).length for s in shoulders) < radius:
            continue
        # Clothing over the chest is often half-weighted to the arms; only surface that actually
        # lies along the arm bones may go with the arm, the rest stays on the body.
        near_arm = min(segment_distance(p, a, b) for a, b in arm_segments) < 0.06 * height
        keep_arm = arm >= 0.5 * total and near_arm
        for n, index, w in groups:
            if n.startswith(ARM) != keep_arm:
                body.vertex_groups[index].remove([v.index])
        snapped += 1
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.vertex_group_normalize_all(lock_active=False)
    return snapped


def arm_owned(body):
    names = {g.index: g.name for g in body.vertex_groups}
    owned = np.zeros(len(body.data.vertices), dtype=bool)
    for v in body.data.vertices:
        arm = sum(g.weight for g in v.groups if names[g.group].startswith(ARM))
        owned[v.index] = arm >= 0.5
    return owned


def worst_stretch(rig, body, edges, step):
    rig.data.pose_position = 'REST'
    bpy.context.view_layer.update()
    rest = evaluated_positions(body)
    rest_len = np.linalg.norm(rest[edges[:, 0]] - rest[edges[:, 1]], axis=1)
    height = float(rest[:, 2].max() - rest[:, 2].min())
    floor = 0.002 * height
    worst = np.ones(len(edges), dtype=np.float32)
    rig.data.pose_position = 'POSE'
    for track in rig.animation_data.nla_tracks:
        track.mute = True
    for action in bpy.data.actions:
        rig.animation_data.action = action
        if action.slots:
            rig.animation_data.action_slot = action.slots[0]
        start, end = (int(x) for x in action.frame_range)
        for frame in range(start, end + 1, step):
            bpy.context.scene.frame_set(frame)
            posed = evaluated_positions(body)
            length = np.linalg.norm(posed[edges[:, 0]] - posed[edges[:, 1]], axis=1)
            worst = np.maximum(worst, length / np.maximum(rest_len, floor))
    rig.animation_data.action = None
    bpy.context.scene.frame_set(0)
    return worst


def cut(path, output, ratio, step, crumb_limit, debug=False):
    rig, meshes = load_model(path)
    body = max(meshes, key=lambda o: len(o.data.vertices))
    rig.data.pose_position = 'REST'
    bpy.context.view_layer.update()
    height = max(v.co.z for v in body.data.vertices) - min(v.co.z for v in body.data.vertices)
    height *= body.matrix_world.to_scale().z
    snapped = snap_weights(rig, body, height)

    edges = np.empty(len(body.data.edges) * 2, dtype=np.int64)
    body.data.edges.foreach_get('vertices', edges)
    edges = edges.reshape(-1, 2)
    owned = arm_owned(body)
    worst = worst_stretch(rig, body, edges, step)
    # A seam edge joins an arm-owned vertex to a body-owned one; nothing else is cut.
    seam = owned[edges[:, 0]] != owned[edges[:, 1]]
    doomed_edges = set(np.nonzero(seam & (worst > ratio))[0].tolist())

    to_rig = rig.matrix_world.inverted() @ body.matrix_world
    distal = [(b.head_local, b.tail_local) for b in rig.data.bones if b.name.startswith(('forearm.', 'hand.'))]
    bm = bmesh.new()
    bm.from_mesh(body.data)
    bm.edges.ensure_lookup_table()
    doomed = {f for i in doomed_edges for f in bm.edges[i].link_faces}
    touched = {v for f in doomed for v in f.verts}
    bmesh.ops.delete(bm, geom=list(doomed), context='FACES')
    # Crumbs: small islands that the cut itself separated (never whole props or accessories).
    seen, crumbs, sizes = set(), [], []
    for face in bm.faces:
        if face in seen:
            continue
        island, stack = [], [face]
        seen.add(face)
        while stack:
            f = stack.pop()
            island.append(f)
            for e in f.edges:
                for g in e.link_faces:
                    if g not in seen:
                        seen.add(g)
                        stack.append(g)
        if any(v in touched for f in island for v in f.verts):
            sizes.append(len(island))
            # A body-owned fragment cut loose from the main surface is an old hand or finger that
            # stayed on the hip; an arm-owned one (a held weapon, a sleeve) follows the hand.
            verts = {v.index for f in island for v in f.verts}
            body_share = sum(1 for i in verts if not owned[i]) / len(verts)
            co = [v.co for f in island for v in f.verts]
            span = max((max(c[k] for c in co) - min(c[k] for c in co)) for k in range(3))
            if debug:
                print(f'  island {len(island)} faces, span {span / height:.2f}h, body {body_share:.2f}', flush=True)
            centre = sum((v.co for f in island for v in f.verts), Vector()) / sum(len(f.verts) for f in island)
            far_from_hand = min(segment_distance(to_rig @ centre, a, b) for a, b in distal) > 0.1 * height
            if (len(island) < crumb_limit or (len(island) < 400 and body_share > 0.5)
                    or (len(island) < 250 and body_share <= 0.5 and far_from_hand)):
                crumbs.extend(island)
    bmesh.ops.delete(bm, geom=crumbs, context='FACES')
    uv = bm.loops.layers.uv.active
    boundary = [e for e in bm.edges if e.is_boundary and all(v in touched for v in e.verts)]
    filled = bmesh.ops.holes_fill(bm, edges=boundary, sides=40)['faces']
    if uv is not None:
        for face in filled:
            for loop in face.loops:
                other = next((l for l in loop.vert.link_loops if l.face not in filled), None)
                if other is not None:
                    loop[uv].uv = other[uv].uv
    loose = [v for v in bm.verts if not v.link_faces]
    bmesh.ops.delete(bm, geom=loose, context='VERTS')
    bm.to_mesh(body.data)
    bm.free()
    body.data.update()

    rig.data.pose_position = 'POSE'
    bpy.ops.object.select_all(action='DESELECT')
    rig.select_set(True)
    for mesh in meshes:
        mesh.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.export_scene.gltf(filepath=str(output / path.name), export_format='GLB',
        use_selection=True, export_animations=True, export_animation_mode='ACTIONS',
        export_skins=True, export_yup=True, export_apply=False, export_image_format='AUTO')
    print(f'[seams] {path.stem}: {snapped} vertices snapped, {len(doomed)} seam faces cut, '
          f'{len(crumbs)} crumbs, {len(filled)} holes closed; cut islands {sorted(sizes)[:12]}', flush=True)


if __name__ == '__main__':
    ap = argparse.ArgumentParser()
    ap.add_argument('--models', type=Path, required=True)
    ap.add_argument('--output', type=Path, required=True)
    ap.add_argument('--only', default='')
    ap.add_argument('--ratio', type=float, default=2.0)
    ap.add_argument('--step', type=int, default=2)
    ap.add_argument('--crumbs', type=int, default=60)
    ap.add_argument('--debug', action='store_true')
    args = ap.parse_args(sys.argv[sys.argv.index('--') + 1:])
    source = (ROOT / args.models).resolve()
    output = (ROOT / args.output).resolve()
    if source == output:
        raise SystemExit('Use a staging output directory, separate from the input models')
    output.mkdir(parents=True, exist_ok=True)
    for path in sorted(source.glob('*.glb')):
        if args.only and path.stem not in args.only.split(','):
            continue
        cut(path, output, args.ratio, args.step, args.crumbs, args.debug)
