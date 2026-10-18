"""Make labelled contact sheets from real captures (Python + Pillow, outside Blender)."""
import argparse
import json
from pathlib import Path

from PIL import Image, ImageDraw


def main():
    ap=argparse.ArgumentParser()
    ap.add_argument('--engine',type=Path,required=True)
    ap.add_argument('--before',type=Path,required=True)
    ap.add_argument('--after',type=Path,required=True)
    ap.add_argument('--output',type=Path,required=True)
    args=ap.parse_args()
    args.output.mkdir(parents=True,exist_ok=True)
    manifest=json.loads((args.engine/'manifest.json').read_text())
    ids=[row['id'] for row in manifest]
    lines=['# Character hand review / Pemeriksaan tangan karakter','',
           'Captures use the exported GLBs through ThreeNet. / Capture memakai GLB hasil ekspor melalui ThreeNet.',
           '',f'{len(ids)} fighters; five poses each. / {len(ids)} petarung; lima pose masing-masing.','']
    for pose in ['idle','jab','heavy','block','victory']:
        lines += [f'## {pose.title()}','']
        for start in range(0,len(ids),14):
            sheet=Image.new('RGB',(1540,610),(23,29,38))
            draw=ImageDraw.Draw(sheet)
            for i,cid in enumerate(ids[start:start+14]):
                pic=Image.open(args.engine/f'{cid}-{pose}.png').convert('RGB')
                pic.thumbnail((220,275))
                x,y=(i%7)*220,(i//7)*305
                sheet.paste(pic,(x,y))
                draw.text((x+6,y+278),cid,fill='white')
            name=f'{pose}-{start//14+1}.jpg'
            sheet.save(args.output/name,quality=92)
            lines += [f'![{pose} {start//14+1}]({name})','']
    compare=['anak-stm','kasir-minimarket','bekantan','buzzer-politik','dukun-santet','cenderawasih']
    sheet=Image.new('RGB',(1440,1410),(23,29,38))
    draw=ImageDraw.Draw(sheet)
    for i,cid in enumerate(compare):
        for side,folder in enumerate([args.before,args.after]):
            x,y=(i%2)*720+side*360,(i//2)*470
            pic=Image.open(folder/f'{cid}-idle.png').convert('RGB')
            sheet.paste(pic,(x,y+25))
            draw.text((x+8,y+6),cid+(' / BEFORE' if side==0 else ' / AFTER'),fill='white')
    sheet.save(args.output/'comparison.jpg',quality=93)
    lines += ['## Before / After - Sebelum / Sesudah','',
              'Blender bind/animation comparison under the same lighting. / Perbandingan animasi Blender dengan pencahayaan yang sama.',
              '', '![Before and after](comparison.jpg)','']
    (args.output/'README.md').write_text('\n'.join(lines),encoding='utf-8')


if __name__=='__main__':main()
