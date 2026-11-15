"""Original quad-rotor Guardian. References inform silhouette only; no copied pixels."""
import sys
from pathlib import Path
sys.path.insert(0, str(Path.home() / '.game-art-tools/pixel-art-studio/scripts'))
from pixelstudio import Sprite
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'src/GuardianDrone/Assets'
OUT.mkdir(parents=True, exist_ok=True)
HERE = Path(__file__).resolve().parent
DARK, SHADOW, STEEL, LIGHT, WHITE, CYAN, TEAL, AMBER = '#18232e', '#354957', '#718b93', '#acbec0', '#e9dfbd', '#8cf4df', '#3f9c9d', '#e6a45a'
RED = '#ad604c'
PAL = [DARK, SHADOW, STEEL, LIGHT, WHITE, CYAN, TEAL, AMBER, RED]

def canvas(w=64, h=40): return Sprite(w, h, palette=PAL)
def save(s, name):
    s.save_png(str(OUT / (name + '.png')))
    return Image.open(OUT / (name + '.png')).convert('RGBA')

s = canvas()
# Two darker rear motors establish four-rotor perspective.
for x in [16,40]:
    s.rect(x,8,x+10,12,DARK); s.rect(x+1,8,x+9,10,STEEL)
    s.line(x+2,8,x+8,8,LIGHT)
s.polygon([(21,12),(24,10),(31,17),(28,21)],SHADOW)
s.polygon([(44,12),(46,13),(39,21),(35,18)],SHADOW)
# Exposed front outriggers: motors are separate from the fuselage.
s.polygon([(12,17),(26,16),(29,20),(13,22)],DARK)
s.rect(14,17,27,19,STEEL);s.line(15,17,25,17,LIGHT)
s.polygon([(38,17),(51,16),(53,22),(38,22)],DARK)
s.rect(39,17,51,19,STEEL);s.line(40,17,49,17,LIGHT)
for x in [4,45]:
    s.ellipse(x,13,x+15,24,DARK)
    s.rect(x+2,15,x+13,20,STEEL)
    s.line(x+3,14,x+11,14,LIGHT)
    s.rect(x+3,21,x+12,23,SHADOW)
    s.rect(x+4,18,x+7,19,AMBER)
    s.line(x+9,18,x+11,18,DARK)
    s.line(x+9,15,x+11,15,WHITE)
# Landing skids, central armor pod and an unmistakable gimbal below it.
s.line(27,24,23,34,DARK);s.line(28,25,24,34,STEEL)
s.line(39,24,44,34,DARK);s.line(40,24,45,34,STEEL)
s.rect(19,34,27,35,DARK);s.line(20,34,25,34,LIGHT)
s.rect(41,34,49,35,DARK);s.line(42,34,47,34,STEEL)
s.polygon([(26,11),(38,11),(43,16),(42,25),(35,29),(26,25),(22,18)],DARK)
s.polygon([(27,12),(37,12),(41,16),(38,19),(25,19),(24,17)],WHITE)
s.polygon([(25,20),(39,20),(40,24),(34,27),(27,24)],STEEL)
s.polygon([(28,24),(38,24),(34,27),(29,25)],SHADOW)
s.line(27,12,36,12,LIGHT);s.line(25,17,30,17,LIGHT)
s.rect(31,15,41,20,DARK);s.rect(32,16,39,18,TEAL)
s.rect(26,20,30,24,TEAL);s.line(28,20,28,24,WHITE);s.line(26,22,30,22,WHITE)
s.rect(32,27,35,31,DARK);s.rect(33,27,34,30,STEEL)
s.line(33,11,33,5,DARK);s.line(34,10,34,6,STEEL);s.rect(33,4,35,5,AMBER)
save(s,'hull')
s.save_silhouette(str(HERE/'silhouette.png'), scale=8)
s.stats()
s = canvas(); s.rect(36,16,39,18,WHITE); save(s,'eye')
for frame in range(2):
    s = canvas()
    for x,y,span in [(21,7,8),(45,7,8),(12,12,10),(52,12,10)]:
        s.rect(x-1,y+1,x+1,y+3,DARK)
        reach=span if frame==0 else span-3
        s.line(x-reach,y,x+reach,y,LIGHT)
        s.line(x-reach+1,y-1,x+reach-1,y-1,WHITE if y==12 else STEEL)
        s.rect(x-1,y-1,x+1,y+1,SHADOW)
    save(s,'rotor'+str(frame))
s = canvas(24,12)
s.polygon([(4,3),(11,3),(14,5),(21,5),(21,8),(12,8),(10,10),(4,9)],DARK)
s.rect(5,4,10,8,STEEL);s.line(5,4,9,4,LIGHT)
s.rect(11,5,19,6,LIGHT);s.rect(19,5,21,7,AMBER)
s.rect(5,6,7,7,TEAL); save(s,'tool')
s = canvas(16,20)
s.rect(4,3,11,17,DARK); s.rect(5,4,10,15,STEEL); s.rect(6,5,9,9,TEAL)
s.line(6,6,9,6,CYAN); s.rect(6,12,7,13,AMBER); s.rect(9,12,9,13,LIGHT)
s.line(7,3,7,1,STEEL); save(s,'controller')

preview=Image.new('RGBA',(768,240),'#25323a'); d=ImageDraw.Draw(preview)
for i, mode in enumerate(['idle','guard','medical']):
    img=Image.open(OUT/'hull.png').convert('RGBA')
    for key, xy in [('rotor0',(0,0)),('tool',(22,26))]:
        img.alpha_composite(Image.open(OUT/(key+'.png')).convert('RGBA'),xy)
    eye=Image.open(OUT/'eye.png').convert('RGBA')
    color=CYAN if i==0 else AMBER if i==1 else '#63f6a1'
    tinted=Image.new('RGBA',eye.size,color); tinted.putalpha(eye.getchannel('A'));img.alpha_composite(tinted)
    preview.alpha_composite(img.resize((256,160),Image.Resampling.NEAREST),(i*256,25))
    if i == 0:
        mask=img.getchannel('A');silhouette=Image.new('RGBA',img.size,DARK);silhouette.putalpha(mask)
        silhouette.resize((512,320),Image.Resampling.NEAREST).save(HERE/'silhouette-complete.png')
    d.text((i*256+95,204),mode,fill=WHITE)
preview.convert('RGB').save(HERE/'preview.png')
print('Wrote 6 original pixel assets; preview:',HERE/'preview.png')
