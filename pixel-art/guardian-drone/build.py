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

# ---- cosmetic skins -------------------------------------------------------
IND_DARK, IND_SHADOW, IND_STEEL, IND_LIGHT, IND_WHITE = '#1d1a17', '#3d3329', '#7a6a52', '#b8a37e', '#f0e2c4'
IND_AMBER, IND_TEAL, IND_RED = '#f2a63b', '#4f8f7a', '#b04e3a'
IND_PAL = [IND_DARK, IND_SHADOW, IND_STEEL, IND_LIGHT, IND_WHITE, IND_AMBER, IND_TEAL, IND_RED]

STL_DARK, STL_SHADOW, STL_STEEL, STL_LIGHT, STL_WHITE = '#0d141b', '#1e2b35', '#3d5663', '#6f8b99', '#c9d8dd'
STL_CYAN, STL_TEAL, STL_AMBER, STL_RED = '#49d6e0', '#0c6b66', '#e0a33a', '#a53a3a'
STL_PAL = [STL_DARK, STL_SHADOW, STL_STEEL, STL_LIGHT, STL_WHITE, STL_CYAN, STL_TEAL, STL_AMBER, STL_RED]

def vcanvas(pal): return Sprite(64, 40, palette=pal)

def rotor_frame(pal, frame, blade, light, hub):
    s = vcanvas(pal)
    reach = 7 if frame == 0 else 3
    for x, y in ((10, 9), (54, 9), (10, 23), (54, 23)):
        s.line(x - reach, y, x + reach, y, blade)
        s.line(x, y - reach // 2, x, y + reach // 2, light)
        s.rect(x - 1, y - 1, x + 1, y + 1, hub)
    return s

def variant_tool(pal, dark, steel, light, accent, teal):
    s = Sprite(24, 12, palette=pal)
    s.polygon([(4,3),(11,3),(14,5),(21,5),(21,8),(12,8),(10,10),(4,9)], dark)
    s.rect(5,4,10,8,steel); s.line(5,4,9,4,light)
    s.rect(11,5,19,6,light); s.rect(19,5,21,7,accent)
    s.rect(5,6,7,7,teal)
    return s

# Industrial: square housings, amber/steel, heavier arms.
s = vcanvas(IND_PAL)
for cx, cy in ((10,9),(54,9),(10,23),(54,23)):
    s.rect(cx-9, cy-8, cx+9, cy+8, IND_DARK)
    s.rect(cx-7, cy-6, cx+7, cy+6, IND_STEEL)
    s.rect(cx-5, cy-4, cx+5, cy+4, IND_SHADOW)
    s.rect(cx-3, cy-2, cx+3, cy+2, IND_LIGHT)
    s.rect(cx-1, cy-1, cx+1, cy+1, IND_AMBER)
    for bx, by in ((cx-7,cy-6),(cx+7,cy-6),(cx-7,cy+6),(cx+7,cy+6)):
        s.rect(bx,by,bx,by,IND_WHITE)
s.rect(14,8,50,12,IND_DARK); s.rect(16,9,48,11,IND_STEEL)
s.rect(8,20,56,24,IND_DARK); s.rect(10,21,54,23,IND_STEEL)
s.line(20,12,28,20,IND_DARK); s.line(44,12,36,20,IND_DARK)
s.rect(20,10,44,30,IND_DARK)
s.rect(22,12,42,28,IND_WHITE)
s.rect(22,22,42,28,IND_STEEL)
s.rect(28,14,38,20,IND_TEAL)
s.rect(27,14,29,20,IND_WHITE); s.rect(24,16,32,18,IND_WHITE)
s.rect(34,15,41,19,IND_DARK)
s.rect(30,26,34,32,IND_DARK); s.rect(31,27,33,31,IND_AMBER)
s.rect(24,32,28,35,IND_DARK); s.rect(36,32,40,35,IND_DARK)
s.rect(4,4,6,6,IND_AMBER); s.rect(58,4,60,6,IND_AMBER)
save(s,'hull_1')
save(rotor_frame(IND_PAL,0,IND_WHITE,IND_LIGHT,IND_AMBER),'rotor0_1')
save(rotor_frame(IND_PAL,1,IND_WHITE,IND_LIGHT,IND_AMBER),'rotor1_1')
save(variant_tool(IND_PAL,IND_DARK,IND_STEEL,IND_LIGHT,IND_AMBER,IND_TEAL),'tool_1')

# Stealth: slim swept arms, dark teal.
s = vcanvas(STL_PAL)
for cx, cy in ((10,9),(54,9),(10,23),(54,23)):
    s.ellipse(cx-7, cy-6, cx+7, cy+6, STL_DARK)
    s.ellipse(cx-5, cy-4, cx+5, cy+4, STL_STEEL)
    s.ellipse(cx-3, cy-2, cx+3, cy+2, STL_SHADOW)
    s.rect(cx-1, cy-1, cx+1, cy+1, STL_CYAN)
s.line(22,14,10,9,STL_DARK); s.line(22,15,10,10,STL_STEEL)
s.line(42,14,54,9,STL_DARK); s.line(42,15,54,10,STL_STEEL)
s.line(22,24,10,23,STL_DARK); s.line(22,25,10,24,STL_STEEL)
s.line(42,24,54,23,STL_DARK); s.line(42,25,54,24,STL_STEEL)
s.polygon([(26,10),(38,10),(44,18),(42,28),(22,28),(20,18)], STL_DARK)
s.polygon([(27,12),(37,12),(41,18),(40,26),(24,26),(23,18)], STL_SHADOW)
s.rect(28,15,36,25,STL_STEEL)
s.rect(30,18,34,22,STL_CYAN)
s.rect(34,15,41,19,STL_DARK)
s.rect(30,26,34,32,STL_DARK); s.rect(31,27,33,31,STL_CYAN)
s.line(24,32,22,35,STL_DARK); s.line(40,32,42,35,STL_DARK)
s.rect(5,4,6,5,STL_CYAN); s.rect(58,4,59,5,STL_CYAN)
save(s,'hull_2')
save(rotor_frame(STL_PAL,0,STL_WHITE,STL_LIGHT,STL_CYAN),'rotor0_2')
save(rotor_frame(STL_PAL,1,STL_WHITE,STL_LIGHT,STL_CYAN),'rotor1_2')
save(variant_tool(STL_PAL,STL_DARK,STL_STEEL,STL_LIGHT,STL_CYAN,STL_TEAL),'tool_2')

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

skinpreview=Image.new('RGBA',(768,240),'#25323a'); sd=ImageDraw.Draw(skinpreview)
for i,(hull_key,rotor_key,tool_key,label,eye_color) in enumerate([
        ('hull','rotor0','tool','Guardian',CYAN),
        ('hull_1','rotor0_1','tool_1','Industrial',AMBER),
        ('hull_2','rotor0_2','tool_2','Stealth',STL_CYAN)]):
    img=Image.open(OUT/(hull_key+'.png')).convert('RGBA')
    for key, xy in [(rotor_key,(0,0)),(tool_key,(22,26))]:
        img.alpha_composite(Image.open(OUT/(key+'.png')).convert('RGBA'),xy)
    eye=Image.open(OUT/'eye.png').convert('RGBA')
    tinted=Image.new('RGBA',eye.size,eye_color); tinted.putalpha(eye.getchannel('A')); img.alpha_composite(tinted)
    skinpreview.alpha_composite(img.resize((256,160),Image.Resampling.NEAREST),(i*256,25))
    sd.text((i*256+95,204),label,fill=WHITE)
skinpreview.convert('RGB').save(HERE/'skin-preview.png')
print('Wrote 14 original pixel assets; previews:',HERE/'preview.png',HERE/'skin-preview.png')
