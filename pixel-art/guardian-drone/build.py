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
# Four separated rotor discs, each with a two-pixel dark rim.
for x, y in ((2, 2), (46, 2), (2, 16), (46, 16)):
    s.ellipse(x, y, x + 16, y + 14, DARK)
    s.ellipse(x + 2, y + 2, x + 14, y + 12, STEEL)
    s.ellipse(x + 5, y + 4, x + 11, y + 10, LIGHT)
    s.rect(x + 7, y + 6, x + 9, y + 8, AMBER)
# Thick arms, drawn under the central pod.
s.rect(16, 8, 48, 12, DARK)
s.rect(8, 20, 56, 24, DARK)
s.rect(18, 9, 46, 11, STEEL)
s.rect(10, 21, 54, 23, STEEL)
# Body sits in front of the arms: ivory pod, dark underside, large medical mark.
s.rect(22, 10, 42, 28, DARK)
s.rect(24, 12, 40, 20, WHITE)
s.rect(24, 20, 40, 26, STEEL)
s.rect(30, 14, 40, 19, TEAL)
s.rect(25, 21, 31, 25, CYAN)
s.line(28, 21, 28, 25, DARK)
s.line(25, 23, 31, 23, DARK)
s.rect(30, 26, 34, 32, DARK)
s.rect(31, 27, 33, 31, AMBER)
save(s,'hull')
s.save_silhouette(str(HERE/'silhouette.png'), scale=8)
s.stats()
s = canvas(); s.rect(36,16,39,18,WHITE); save(s,'eye')
for frame in range(2):
    s = canvas()
    reach = 7 if frame == 0 else 3
    for x, y in ((10, 8), (54, 8), (10, 22), (54, 22)):
        s.line(x - reach, y, x + reach, y, WHITE)
        s.line(x, y - reach // 2, x, y + reach // 2, LIGHT)
        s.rect(x - 1, y - 1, x + 1, y + 1, AMBER)
    save(s, 'rotor' + str(frame))
s = canvas(24,12)
s.polygon([(4,3),(11,3),(14,5),(21,5),(21,8),(12,8),(10,10),(4,9)],DARK)
s.rect(5,4,10,8,STEEL);s.line(5,4,9,4,LIGHT)
s.rect(11,5,19,6,LIGHT);s.rect(19,5,21,7,AMBER)
s.rect(5,6,7,7,TEAL); save(s,'tool')
s = canvas(16,16)
s.ellipse(0,0,6,6,DARK); s.ellipse(9,0,15,6,DARK)
s.ellipse(0,8,6,14,DARK); s.ellipse(9,8,15,14,DARK)
s.rect(3,3,12,5,STEEL); s.rect(6,2,9,12,STEEL)
s.rect(5,4,10,9,WHITE); s.rect(7,6,8,11,AMBER)
save(s,'controller')

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
