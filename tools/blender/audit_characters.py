"""Inspect shipped glTFs in Blender; capture bind/animated poses and report skin health.

blender -b -P tools/blender/audit_characters.py -- --output out/character-audit/before
"""
import argparse
import json
import math
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
CLIPS = {'Idle', 'Walk', 'Jab', 'Heavy', 'Kick', 'Uppercut', 'Special',
         'Grab', 'Hit', 'Block', 'Jump', 'KO', 'Dizzy', 'Victory'}


def load_model(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(path))
    rig = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
    # The importer creates an unweighted Icosphere solely as a bone display widget.
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH' and o.find_armature() == rig]
    return rig, meshes


def set_clip(rig, name, frame=0):
    rig.data.pose_position = 'POSE'
    for track in rig.animation_data.nla_tracks:
        track.mute = True
    action = next(a for a in bpy.data.actions if a.name == name or a.name.startswith(name + '_'))
    rig.animation_data.action = action
    if action.slots:
        rig.animation_data.action_slot = action.slots[0]
    bpy.context.scene.frame_set(frame)
    bpy.context.view_layer.update()


def setup_capture(height, output, side=False):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x = 360
    scene.render.resolution_y = 440
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.film_transparent = False
    shading = scene.display.shading
    shading.light = 'STUDIO'
    shading.color_type = 'TEXTURE'
    shading.show_shadows = True
    shading.show_cavity = True
    shading.cavity_type = 'BOTH'
    shading.background_type = 'WORLD'
    if scene.world is None:
        scene.world = bpy.data.worlds.new('AuditWorld')
    scene.world.color = (0.045, 0.055, 0.07)
    cam = scene.camera
    if cam is None:
        data = bpy.data.cameras.new('AuditCamera')
        cam = bpy.data.objects.new('AuditCamera', data)
        scene.collection.objects.link(cam)
        scene.camera = cam
    cam.data.type = 'ORTHO'
    target = Vector((0, 0, height * 0.5))
    cam.data.ortho_scale = height * 1.3
    if side:
        # Extended fingers and long tails must stay inside the audit frame.
        points=[]
        for obj in scene.objects:
            if obj.type=='MESH' and obj.find_armature():
                evaluated=obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
                mesh=evaluated.to_mesh()
                points.extend(evaluated.matrix_world @ v.co for v in mesh.vertices)
                evaluated.to_mesh_clear()
        if points:
            low,high=min(v.y for v in points),max(v.y for v in points)
            target.y=(low+high)*.5
            cam.data.ortho_scale=max(height*1.3,(high-low)*440/360*1.12)
    cam.location = target + (Vector((4 * height, 0, 0)) if side else Vector((0, -4 * height, 0)))
    cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
    scene.render.filepath = str(output)
    bpy.ops.render.render(write_still=True)


def audit(path, output, captures=True):
    rig, meshes = load_model(path)
    rig.data.pose_position = 'REST'
    bpy.context.view_layer.update()
    body = max(meshes, key=lambda o: len(o.data.vertices))
    points = [body.matrix_world @ v.co for v in body.data.vertices]
    height = max(v.z for v in points) - min(v.z for v in points)
    bone_names = {b.name for b in rig.data.bones}
    unweighted, weights_error = 0, 0
    for mesh in meshes:
        groups = {g.index for g in mesh.vertex_groups if g.name in bone_names}
        for v in mesh.data.vertices:
            weights = [g.weight for g in v.groups if g.group in groups]
            if not weights or sum(weights) < 0.001:
                unweighted += 1
            elif abs(sum(weights) - 1) > 0.01:
                weights_error += 1
    names = {a.name.split('_Rig')[0] for a in bpy.data.actions}
    result = dict(id=path.stem, height=height, vertices=sum(len(o.data.vertices) for o in meshes),
                  triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes),
                  meshes=[o.name for o in meshes], unweighted=unweighted,
                  non_normalized=weights_error, clips=sorted(names), missing_clips=sorted(CLIPS-names),
                  arms={b.name: [list(b.head_local), list(b.tail_local)] for b in rig.data.bones
                        if 'arm.' in b.name or 'hand.' in b.name})
    result['hand_vertices'] = {
        side: sum(any(body.vertex_groups[g.group].name == f'hand.{side}' and g.weight>.5 for g in v.groups)
                  for v in body.data.vertices) for side in ('L','R')}
    result['nonfinite_vertices'] = sum(not all(math.isfinite(c) for c in p) for p in points)
    result['errors'] = []
    if unweighted or weights_error or result['nonfinite_vertices']:
        result['errors'].append('Invalid skin weights or nonfinite geometry')
    if result['missing_clips']:
        result['errors'].append('Missing required animations')
    if min(result['hand_vertices'].values())<10:
        result['errors'].append('A hand has fewer than ten strongly bound vertices')
    # Large edge expansion catches stretched clothing/props that valid weights alone miss.
    edges=np.array([tuple(e.vertices) for e in body.data.edges],dtype=np.int32)
    rest=np.array([tuple(v.co) for v in body.data.vertices])
    original=np.linalg.norm(rest[edges[:,0]]-rest[edges[:,1]],axis=1)
    result['deformation_warnings']=[]
    for clip in ('Idle','Jab','Heavy','Block','Victory'):
        set_clip(rig,clip,12 if clip=='Victory' else 3)
        evaluated=body.evaluated_get(bpy.context.evaluated_depsgraph_get())
        mesh=evaluated.to_mesh()
        coords=np.empty(len(mesh.vertices)*3,dtype=np.float32)
        mesh.vertices.foreach_get('co',coords)
        coords=coords.reshape(-1,3)
        lengths=np.linalg.norm(coords[edges[:,0]]-coords[edges[:,1]],axis=1)
        stretched=(lengths>original*3)&(lengths>.045*height)
        if np.any(stretched):
            result['deformation_warnings'].append(dict(clip=clip,edges=int(stretched.sum()),max_length=float(lengths[stretched].max())))
        evaluated.to_mesh_clear()
    if captures:
        rig.data.pose_position='REST'
        bpy.context.view_layer.update()
        setup_capture(height, output / f'{path.stem}-rest-front.png')
        setup_capture(height, output / f'{path.stem}-rest-side.png', side=True)
        set_clip(rig, 'Idle')
        setup_capture(height, output / f'{path.stem}-idle.png')
        set_clip(rig, 'Jab', 3)
        setup_capture(height, output / f'{path.stem}-jab.png', side=True)
        set_clip(rig, 'Victory', 12)
        setup_capture(height, output / f'{path.stem}-victory.png')
    print('[audit]', path.stem, json.dumps(result), flush=True)
    return result


if __name__ == '__main__':
    ap = argparse.ArgumentParser()
    ap.add_argument('--models', type=Path, default=ROOT / 'assets/models/characters')
    ap.add_argument('--output', type=Path, required=True)
    ap.add_argument('--only', default='')
    ap.add_argument('--no-capture', action='store_true')
    args = ap.parse_args(sys.argv[sys.argv.index('--')+1:])
    args.output = (ROOT / args.output).resolve()
    args.models = (ROOT / args.models).resolve()
    args.output.mkdir(parents=True, exist_ok=True)
    selected = set(args.only.split(',')) if args.only else None
    results = []
    for path in sorted(args.models.glob('*.glb')):
        if selected and path.stem not in selected:
            continue
        results.append(audit(path, args.output, not args.no_capture))
        (args.output / 'audit.json').write_text(json.dumps(results, indent=2), encoding='utf-8')
    if any(r['errors'] for r in results):
        raise SystemExit('Character audit failed; inspect audit.json')
