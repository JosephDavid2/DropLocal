"""Deterministic optical stress fixtures, not a claim of physical camera testing."""
from PIL import Image, ImageFilter
import numpy as np
from pathlib import Path
root=Path(__file__).resolve().parent.parent
lines=(root/'dist/qr-test.matrix').read_text().splitlines()
grid=np.full((45,45),255,dtype=np.uint8)
for y,line in enumerate(lines[1:]):
    for x,c in enumerate(line): grid[y+4,x+4]=0 if c=='1' else 255
qr=Image.fromarray(grid).resize((900,900),Image.Resampling.NEAREST)
out=root/'tests/qr-frames';out.mkdir(exist_ok=True)
manifest=[]
def warp(name,corners,w=960,h=720,black=20,white=235,blur=.45,gradient=0,noise=0,read=True):
    # Inverse projective map from camera pixels to an independently rasterized QR.
    a=[];b=[]
    for (x,y),(u,v) in zip(corners,[(0,0),(900,0),(900,900),(0,900)]):
        a.extend([[x,y,1,0,0,0,-u*x,-u*y],[0,0,0,x,y,1,-v*x,-v*y]]);b.extend([u,v])
    coeff=np.linalg.solve(a,b)
    im=qr.transform((w,h),Image.Transform.PERSPECTIVE,coeff,Image.Resampling.BICUBIC,fillcolor=255)
    if blur:im=im.filter(ImageFilter.GaussianBlur(blur))
    pixels=np.asarray(im,dtype=float)/255*(white-black)+black
    if gradient:pixels+=np.linspace(-gradient,gradient,w)[None,:]
    if noise:pixels+=np.random.default_rng(814).normal(0,noise,(h,w))
    pixels=np.clip(pixels,0,255).astype(np.uint8)
    (out/(name+'.y')).write_bytes(pixels.tobytes());Image.fromarray(pixels).save(out/(name+'.png'))
    manifest.append(f'{name}.y {w} {h} '+('read' if read else 'reject'))
warp('fractional-modules',[(235.3,115.7),(574.8,115.7),(574.8,455.2),(235.3,455.2)])
warp('dark-monitor',[(235,115),(595,115),(595,475),(235,475)],black=9,white=96)
warp('bright-black',[(235,115),(595,115),(595,475),(235,475)],black=145,white=238)
warp('uneven-light',[(210,90),(615,90),(615,495),(210,495)],black=35,white=175,gradient=70,noise=3)
warp('perspective',[(230,120),(590,95),(640,480),(190,450)],blur=.55)
warp('perspective-dark',[(230,120),(590,95),(640,480),(190,450)],black=14,white=108,noise=2)
warp('focus-blur',[(195,75),(645,75),(645,525),(195,525)],blur=1.4,noise=3)
warp('small-modules',[(260,160),(440,160),(440,340),(260,340)],blur=.35)
warp('rotated-25deg',[(320,50),(655,205),(500,540),(165,385)],blur=.5)
for angle in [90,180,270]:
    im=Image.open(out/'perspective.png').rotate(angle,expand=True);name=f'perspective-rotation-{angle}';(out/(name+'.y')).write_bytes(im.tobytes());manifest.append(f'{name}.y {im.width} {im.height} read')
for name,array in [('blank',np.full((720,960),90,dtype=np.uint8)),('noise',np.random.default_rng(21).integers(0,256,(720,960),dtype=np.uint8))]:
    (out/(name+'.y')).write_bytes(array.tobytes());manifest.append(f'{name}.y 960 720 reject')
(out/'manifest.txt').write_text('\n'.join(manifest))
print(f'Generated {len(manifest)} optical fixtures in {out}')
