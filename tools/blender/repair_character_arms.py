"""Rebind shipped fighters using reviewed anatomical landmarks, then rebuild arm motion.

Does not require ignored Rodin sources. Always write to a staging directory first.
blender -b -P tools/blender/repair_character_arms.py -- --output out/character-audit/repaired
Landmarks use pixels in the 240 x 293.333 orthographic bind-pose audit image;
each side lists shoulder, elbow, wrist and palm. Left is positive model X.
"""
import argparse
import json
import math
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from audit_characters import ROOT, load_model


def point_on_limb(x, y, height, verts):
    point = Vector(((x - 120) * 1.3 / (440 * 2/3), 0,
                    .5 + (440/3 - y) * 1.3 / (440 * 2/3))) * height
    # Sample the frontmost local surface cluster, avoiding backpacks/capes behind it.
    samples = sorted(v.y for v in verts
                     if abs(v.x-point.x) < .022*height and abs(v.z-point.z) < .022*height)
    if samples:
        point.y = (samples[int(len(samples)*.1)] + samples[int(len(samples)*.7)])*.5
    return point


def rotation_between(rest, head, tail, parent_delta):
    # Parallel-transport the parent's roll; independent world-space swings twist
    # elbows by up to 180 degrees on already-bent source arms.
    direction = parent_delta.to_3x3() @ (rest.tail_local-rest.head_local)
    swing = direction.rotation_difference(tail-head)
    return swing.to_matrix().to_4x4() @ parent_delta @ rest.matrix_local.to_3x3().to_4x4()


def solve_arm(shoulder, wrist, pole, a, b):
    delta = wrist-shoulder
    distance = max(abs(a-b)+.001, min(delta.length, (a+b)*.985))
    direction = delta.normalized()
    wrist = shoulder + direction*distance
    along = (a*a-b*b+distance*distance)/(2*distance)
    bend = pole-shoulder
    bend -= direction*bend.dot(direction)
    bend.normalize()
    elbow = shoulder + direction*along + bend*math.sqrt(max(0, a*a-along*along))
    return elbow, wrist


def target_for(clip, frame, side, length, height):
    sx = 1 if side == 'L' else -1
    guard = Vector((-sx*.025*height, -length*.52, -.04*height))
    target = guard.copy()
    pole = Vector((sx*.22, -.035, -.25))*height
    def pulse(peak, end):
        v = frame/peak if frame <= peak else (end-frame)/max(1, end-peak)
        v = max(0, min(1, v))
        return v*v*(3-2*v)
    if (clip=='Jab' and side=='R') or (clip=='Heavy' and side=='L'):
        target = guard.lerp(Vector((-sx*.025, -length*.97, -.015*height)), pulse(3 if clip=='Jab' else 10, 9 if clip=='Jab' else 17))
    elif clip=='Uppercut' and side=='R':
        target = guard.lerp(Vector((-sx*.02*height, -length*.55, length*.65)), pulse(5,14))
    elif clip in ('Special','Grab'):
        target = guard.lerp(Vector((sx*.02*height, -length*.90, -.03*height)), pulse(6,22) if clip=='Special' else pulse(5,20))
    elif clip=='Block':
        target = guard.lerp(Vector((-sx*.055, -.19, .06))*height, min(1,frame/3))
    elif clip=='Victory':
        target = Vector((sx*length*.30, -.08*height, length*.8))
        pole = Vector((sx*.3, 0, .05))*height
    elif clip=='KO':
        target = guard.lerp(Vector((sx*.12*height, -.02*height, -length*.85)), min(1,frame/18))
    elif clip=='Walk':
        target.y += math.sin(frame*math.pi/12)*(1 if side=='L' else -1)*.025*height
    elif clip in ('Idle','Dizzy'):
        target.z += math.sin(frame*math.pi/18)*.006*height
    return target, pole


def repair(path, output, landmarks):
    rig, meshes = load_model(path)
    # Authored clips use 30 fps; keep timing and frame-based contact poses unchanged.
    # Import defaults to 24 fps, so rescale existing keys before authoring replacements.
    fps = bpy.context.scene.render.fps
    for action in bpy.data.actions:
        for layer in action.layers:
            for strip in layer.strips:
                for bag in strip.channelbags:
                    for curve in bag.fcurves:
                        for key in curve.keyframe_points:
                            key.co.x *= 30/fps
                            key.handle_left.x *= 30/fps
                            key.handle_right.x *= 30/fps
    bpy.context.scene.render.fps = 30
    body = max(meshes, key=lambda o: len(o.data.vertices))
    rig.data.pose_position = 'REST'
    rig.animation_data.action = None
    for track in rig.animation_data.nla_tracks:
        track.mute = True
    bpy.context.view_layer.update()
    # glTF duplicates vertices along UV seams. Weld geometry without discarding loop UVs.
    bpy.ops.object.select_all(action='DESELECT')
    body.select_set(True)
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.remove_doubles(threshold=.00005)
    bpy.ops.object.mode_set(mode='OBJECT')
    verts = [rig.matrix_world.inverted() @ body.matrix_world @ v.co for v in body.data.vertices]
    old_names = {g.index:g.name for g in body.vertex_groups}
    old_weights = [{old_names[g.group]:g.weight for g in v.groups} for v in body.data.vertices]
    height = max(v.z for v in verts)-min(v.z for v in verts)
    points = {}
    for side, pixels in zip(('L','R'), landmarks):
        points[side] = [point_on_limb(pixels[i],pixels[i+1],height,verts) for i in range(0,8,2)]
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode='EDIT')
    for side, pts in points.items():
        for i, part in enumerate(('upper_arm','forearm','hand')):
            bone = rig.data.edit_bones[f'{part}.{side}']
            bone.use_connect = False
            bone.head, bone.tail = pts[i], pts[i+1]
            bone.roll = 0
    bpy.ops.object.mode_set(mode='OBJECT')
    # Preserve the source skin outside the arms. No whole-body heat recomputation.
    bpy.ops.object.select_all(action='DESELECT')
    body.select_set(True)
    rig.select_set(True)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')
    heat_names={g.index:g.name for g in body.vertex_groups}
    heat_weights=[{heat_names[g.group]:g.weight for g in v.groups} for v in body.data.vertices]
    mod=next((m for m in body.modifiers if m.type=='ARMATURE'),None)
    if mod is None:mod=body.modifiers.new('Armature','ARMATURE')
    mod.object=rig
    # Prevent heat diffusion across touching hands/clothing and distant accessories.
    def segment_distance(p,a,b):
        ab=b-a
        return (p-a-ab*max(0,min(1,(p-a).dot(ab)/ab.length_squared))).length
    arm_groups = {g.index for g in body.vertex_groups if g.name.startswith(('upper_arm.','forearm.','hand.'))}
    skin_colors = {}
    overrides=json.loads((Path(__file__).parent/'arm_skin_overrides.json').read_text()).get(path.stem,[])
    if path.stem in {'kasir-minimarket','tukang-parkir'}:
        material=body.data.materials[0]
        shader=next(n for n in material.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
        links=shader.inputs['Base Color'].links
        tex=links[0].from_node if links else None
        if tex and tex.type=='TEX_IMAGE' and body.data.uv_layers.active:
            img=tex.image
            pixels=np.empty(len(img.pixels),dtype=np.float32)
            img.pixels.foreach_get(pixels)
            pixels=pixels.reshape(img.size[1],img.size[0],4)
            for loop in body.data.loops:
                uv=body.data.uv_layers.active.data[loop.index].uv
                rgb=pixels[int(uv.y%1*img.size[1]),int(uv.x%1*img.size[0]),:3]
                skin_colors[loop.vertex_index]=bool(rgb[0] > rgb[2]*1.2 and rgb[0]>rgb[1]*1.1 and rgb[2]>.35*rgb[0] and rgb[0]>.08)
    # Assign longitudinal limb weights rather than allowing heat to diffuse from
    # fingers through an adjacent shirt. Keep palms rigid and blend only at joints.
    def smooth(value):
        value=max(0,min(1,value))
        return value*value*(3-2*value)
    non_arm=[b for b in rig.data.bones if not b.name.startswith(('upper_arm.','forearm.','hand.')) and b.name!='root']
    assignments=[]
    # The paddle is a separate connected mesh island carried across the back.
    rigid_back=set()
    if path.stem=='sangkuriang':
        parent=list(range(len(verts)))
        def root(i):
            while parent[i]!=i:
                parent[i]=parent[parent[i]]
                i=parent[i]
            return i
        for edge in body.data.edges:
            left,right=edge.vertices
            parent[root(left)]=root(right)
        islands={}
        for i in range(len(verts)):islands.setdefault(root(i),[]).append(i)
        for indices in islands.values():
            if len(indices)>100 and min(verts[i].y for i in indices)>.08*height:
                rigid_back.update(indices)
    polygons={
        'guru-matematika':[('R',[(98,55),(113,54),(139,241),(123,243)])],
        'dukun-santet':[('L',[(216,214),(231,212),(303,321),(298,327),(239,276),(239,260)])],
    }.get(path.stem,[])
    def inside_polygon(x,y,polygon):
        inside=False
        previous=polygon[-1]
        for current in polygon:
            ax,ay=previous;bx,by=current
            if (ay>y)!=(by>y) and x<(bx-ax)*(y-ay)/(by-ay)+ax:inside=not inside
            previous=current
        return inside
    for v, p, old in zip(body.data.vertices, verts, old_weights):
        choices=[]
        for side,pts in points.items():
            offset=0
            for i in range(3):
                ab=pts[i+1]-pts[i]
                t=max(0,min(1,(p-pts[i]).dot(ab)/ab.length_squared))
                distance=(p-pts[i]-ab*t).length
                choices.append((distance,side,offset+t*ab.length))
                offset+=ab.length
        distance,side,along=min(choices)
        pts=points[side]
        a,b=(pts[1]-pts[0]).length,(pts[2]-pts[1]).length
        body_distances=sorted((segment_distance(p,bone.head_local,bone.tail_local),bone.name) for bone in non_arm)
        arm_mix=sum(w for name,w in old.items() if name.startswith(('upper_arm.','forearm.','hand.')))
        # Limit obsolete weights that followed a weapon tip into a skirt or leg.
        arm_mix*=smooth((.09*height-distance)/(.035*height))
        # Wings and held objects previously stole all hand influences on some rigs.
        # Seed the real wrist/palm from the reviewed anatomical positions instead.
        distal_distance=segment_distance(p,pts[2],pts[3])
        arm_mix=max(arm_mix,smooth((.045*height-distal_distance)/(.025*height)))
        if path.stem in {'cenderawasih','reog-ponorogo'}:
            arm_mix=max(arm_mix,smooth((.06*height-distance)/(.035*height)))
        # Original torso/leg binding is retained. Remove stray arm influences above
        # the anatomical shoulder, which would otherwise tug the face or hair.
        shoulder_z=max(points['L'][0].z,points['R'][0].z)
        # Reog and the dancer already raise their fists above the shoulders in bind
        # pose. The head protection must not erase those outer hand influences.
        if path.stem not in {'reog-ponorogo','horeg-dancer'} or abs(p.x)<.13*height:
            arm_mix*=smooth((shoulder_z+.09*height-p.z)/(.09*height))
        touching_waist = path.stem in {'kasir-minimarket','tukang-parkir'} and .48*height<p.z<.67*height and abs(p.x)<.18*height
        if touching_waist and v.index in skin_colors:
            arm_mix=1.0 if skin_colors[v.index] else 0.0
        elbow_blend=min(.035*height,.15*a,.15*b)
        wrist_blend=min(.018*height,.15*b)
        upper_to_fore=smooth((along-a+elbow_blend)/(2*elbow_blend))
        fore_to_hand=smooth((along-a-b+wrist_blend)/(2*wrist_blend))
        weights={f'upper_arm.{side}':(1-upper_to_fore)*arm_mix,
                 f'forearm.{side}':upper_to_fore*(1-fore_to_hand)*arm_mix,
                 f'hand.{side}':fore_to_hand*arm_mix}
        trunk={name:w for name,w in old.items() if not name.startswith(('upper_arm.','forearm.','hand.'))}
        if not trunk:trunk={body_distances[0][1]:1}
        if path.stem in {'bekantan','orang-utan','komodo','harimau-sumatra','baphomet'} and p.y>.08*height and p.z<.60*height:
            weights={}
            trunk={'hips':1.0}
            arm_mix=0
        total=sum(trunk.values())
        weights.update({name:weight/total*(1-arm_mix) for name,weight in trunk.items()})
        # Solve the corrected, bent bind pose as a continuous surface. Preserve
        # the original non-arm rig while using heat weights around the joints.
        heat=heat_weights[v.index]
        weights={name:weight for name,weight in heat.items() if name.startswith(('upper_arm.','forearm.','hand.'))}
        arm_mix=min(1.0,sum(weights.values()))
        weights.update({name:weight/total*(1-arm_mix) for name,weight in trunk.items()})
        px=180+p.x/height*440/1.3
        py=220-(p.z/height-.5)*440/1.3
        for owner,x0,y0,x1,y1 in overrides:
            if x0<=px<=x1 and y0<=py<=y1:
                weights={f'hand.{owner}':1.0}
        for owner,polygon in polygons:
            if inside_polygon(px,py,polygon):weights={f'hand.{owner}':1.0}
        if v.index in rigid_back:weights={'chest':1.0}
        if path.stem=='pemadam-karhutla' and px<126 and 205<py<366:
            t=smooth((366-py)/146)
            weights={'hand.R':t,'chest':1-t}
        if path.stem=='anak-stm' and 230<=px<=260 and 247<=py<=345 and p.y<-.035*height:
            weights={'hand.L':1.0}
        assignments.append(weights)
    for group in list(body.vertex_groups):body.vertex_groups.remove(group)
    groups={bone.name:body.vertex_groups.new(name=bone.name) for bone in rig.data.bones}
    for index,weights in enumerate(assignments):
        for name,w in weights.items():
            if w>.0001:groups[name].add([index],w,'REPLACE')
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.vertex_group_limit_total(limit=4)
    bpy.ops.object.vertex_group_normalize_all(lock_active=False)
    rig.data.pose_position = 'POSE'
    arm_names = [f'{part}.{side}' for side in ('L','R') for part in ('upper_arm','forearm','hand')]
    for action in list(bpy.data.actions):
        rig.animation_data.action = action
        rig.animation_data.action_slot = action.slots[0]
        start, end = [int(v) for v in action.frame_range]
        # Remove the old arm curves before evaluating the replacement motion.
        for layer in action.layers:
            for strip in layer.strips:
                for bag in strip.channelbags:
                    for curve in list(bag.fcurves):
                        if any(f'pose.bones["{name}"]' in curve.data_path for name in arm_names):
                            bag.fcurves.remove(curve)
        for frame in range(start,end+1):
            bpy.context.scene.frame_set(frame)
            chest = rig.pose.bones['chest']
            chest_delta = chest.matrix @ chest.bone.matrix_local.inverted()
            for side, pts in points.items():
                a, b = (pts[1]-pts[0]).length, (pts[2]-pts[1]).length
                target, pole = target_for(action.name,frame,side,a+b,height)
                elbow, wrist = solve_arm(pts[0],pts[0]+target,pts[0]+pole,a,b)
                ends = [pts[0],elbow,wrist,wrist+(wrist-elbow).normalized()*(pts[3]-pts[2]).length]
                parent_delta = Matrix.Identity(4)
                for i, part in enumerate(('upper_arm','forearm','hand')):
                    pb = rig.pose.bones[f'{part}.{side}']
                    # Carry the authored rest orientation through an anatomical swing.
                    desired = (parent_delta @ pb.bone.matrix_local.to_3x3().to_4x4()
                               if part=='hand' else rotation_between(pb.bone,ends[i],ends[i+1],parent_delta))
                    parent_delta = desired @ pb.bone.matrix_local.to_3x3().to_4x4().inverted()
                    desired.translation = ends[i]
                    pb.matrix = chest_delta @ desired
                    pb.rotation_mode = 'QUATERNION'
                    pb.keyframe_insert('rotation_quaternion',frame=frame,group=pb.name)
                    bpy.context.view_layer.update()
        print('[repair] animated',path.stem,action.name,flush=True)
    rig.animation_data.action = None
    for pb in rig.pose.bones:
        pb.matrix_basis = Matrix.Identity(4)
    bpy.ops.object.select_all(action='DESELECT')
    rig.select_set(True)
    body.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.export_scene.gltf(filepath=str(output / path.name),export_format='GLB',
        use_selection=True,export_animations=True,export_animation_mode='ACTIONS',
        export_skins=True,export_yup=True,export_apply=False,export_image_format='AUTO')
    print('[repair] exported',path.stem,flush=True)


if __name__=='__main__':
    ap=argparse.ArgumentParser()
    ap.add_argument('--output',type=Path,required=True)
    ap.add_argument('--models',type=Path,default=ROOT/'assets/models/characters')
    ap.add_argument('--only',default='')
    args=ap.parse_args(sys.argv[sys.argv.index('--')+1:])
    output=(ROOT/args.output).resolve()
    source=(ROOT/args.models).resolve()
    if source==output:raise SystemExit('Use a staging output directory, separate from the source models')
    output.mkdir(parents=True,exist_ok=True)
    table=json.loads((Path(__file__).parent/'arm_landmarks.json').read_text())
    for path in sorted(source.glob('*.glb')):
        if args.only and path.stem not in args.only.split(','):
            continue
        repair(path,output,table[path.stem])
