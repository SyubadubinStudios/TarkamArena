"""Render transparent portraits from exported GLBs, not the pre-export scene.

blender -b -P tools/blender/capture_character_previews.py -- --models assets/models/characters --output assets/generated/previews
"""
import argparse
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from audit_characters import ROOT, load_model, set_clip


def capture(path, output):
    rig, meshes=load_model(path)
    body=max(meshes,key=lambda o:len(o.data.vertices))
    height=max(v.co.z for v in body.data.vertices)-min(v.co.z for v in body.data.vertices)
    set_clip(rig,'Idle',0)
    scene=bpy.context.scene
    scene.render.engine='BLENDER_EEVEE'
    scene.render.film_transparent=True
    scene.render.resolution_x=512
    scene.render.resolution_y=640
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'
    scene.render.image_settings.color_mode='RGBA'
    cam_data=bpy.data.cameras.new('PortraitCamera')
    cam_data.lens=70
    camera=bpy.data.objects.new('PortraitCamera',cam_data)
    scene.collection.objects.link(camera)
    camera.location=(.75*height,-2.7*height,.64*height)
    camera.rotation_euler=(Vector((0,0,.52*height))-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.camera=camera
    for location,energy in [((2,-3,4),900),((-3,-1,2),300),((0,3,3),600)]:
        data=bpy.data.lights.new('PortraitLight','AREA')
        data.energy=energy
        data.size=3
        light=bpy.data.objects.new('PortraitLight',data)
        scene.collection.objects.link(light)
        light.location=location
        light.rotation_euler=(Vector((0,0,1))-Vector(location)).to_track_quat('-Z','Y').to_euler()
    world=bpy.data.worlds.new('PortraitWorld')
    world.use_nodes=True
    background=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND')
    background.inputs[0].default_value=(.09,.2,.48,1)
    background.inputs[1].default_value=.6
    scene.world=world
    scene.render.filepath=str(output/f'{path.stem}.png')
    bpy.ops.render.render(write_still=True)
    print('[preview]',path.stem,flush=True)


if __name__=='__main__':
    ap=argparse.ArgumentParser()
    ap.add_argument('--models',type=Path,default=ROOT/'assets/models/characters')
    ap.add_argument('--output',type=Path,required=True)
    ap.add_argument('--only',default='')
    args=ap.parse_args(sys.argv[sys.argv.index('--')+1:])
    output=(ROOT/args.output).resolve()
    output.mkdir(parents=True,exist_ok=True)
    for path in sorted((ROOT/args.models).glob('*.glb')):
        if args.only and path.stem not in args.only.split(','):continue
        capture(path,output)
