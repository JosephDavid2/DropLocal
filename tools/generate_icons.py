"""Generate native icon assets from one vector geometry. Requires Pillow."""
from pathlib import Path
from math import hypot
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "assets"
RES = ROOT / "android/app/src/main/res"
TEAL, NAVY = "#12E0DE", "#0C1825"
CURVES = [((28, 46), (54, 23), (80, 46)),
          ((37, 57), (54, 42), (71, 57)),
          ((46, 68), (54, 61), (62, 68))]
STROKE = 5
PATH = "M28,46 Q54,23 80,46 M37,57 Q54,42 71,57 M46,68 Q54,61 62,68"
DOT = "M54,78 m-3.25,0 a3.25,3.25 0,1 0,6.5 0 a3.25,3.25 0,1 0,-6.5 0"
SIZES = [16, 20, 24, 32, 40, 48, 64, 128, 256]

def points(curve):
    start, control, end = curve
    for i in range(101):
        t = i / 100
        yield tuple((1-t)**2 * start[k] + 2*(1-t)*t*control[k] + t*t*end[k] for k in (0, 1))

def write(relative, content):
    path = ROOT / relative
    path.parent.mkdir(parents=True, exist_ok=True)
    content += "\n"
    if not path.exists() or path.read_text(encoding="utf-8") != content:
        path.write_text(content, encoding="utf-8")

def render(size):
    supersample = 4
    dim = size * supersample
    scale = dim / 108
    image = Image.new("RGBA", (dim, dim), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle((0, 0, dim-1, dim-1), radius=24*scale, fill=TEAL)
    radius = STROKE * scale / 2
    for curve in CURVES:
        pts = [(x*scale, y*scale) for x, y in points(curve)]
        # Union of overlapping disks gives a smooth round stroke without raster line-join seams.
        for x, y in pts:
            draw.ellipse((x-radius,y-radius,x+radius,y+radius), fill=NAVY)
    x, y, r = 54*scale, 78*scale, 3.25*scale
    draw.ellipse((x-r,y-r,x+r,y+r), fill=NAVY)
    return image.resize((size, size), Image.Resampling.LANCZOS)

def vector(background=False, monochrome=False):
    color = "#FFFFFF" if monochrome else NAVY
    bg = f'<path android:fillColor="{TEAL}" android:pathData="M24,0 H84 Q108,0 108,24 V84 Q108,108 84,108 H24 Q0,108 0,84 V24 Q0,0 24,0 Z" />' if background else ""
    return f'''<vector xmlns:android="http://schemas.android.com/apk/res/android" android:width="108dp" android:height="108dp" android:viewportWidth="108" android:viewportHeight="108">
  {bg}
  <path android:fillColor="#00000000" android:strokeColor="{color}" android:strokeWidth="{STROKE}" android:strokeLineCap="round" android:pathData="{PATH}" />
  <path android:fillColor="{color}" android:pathData="{DOT}" />
</vector>'''

def adaptive(monochrome=False):
    extra = '\n  <monochrome android:drawable="@drawable/ic_launcher_monochrome" />' if monochrome else ""
    return f'''<adaptive-icon xmlns:android="http://schemas.android.com/apk/res/android">
  <background android:drawable="@color/launcher_background" />
  <foreground android:drawable="@drawable/ic_launcher_foreground" />{extra}
</adaptive-icon>'''

def main():
    ASSETS.mkdir(exist_ok=True)
    # Every visible foreground point plus its stroke stays in the circular 66dp safe area.
    for curve in CURVES:
        assert all(hypot(x-54,y-54)+STROKE/2 <= 33 for x,y in points(curve))
    assert hypot(54-54,78-54)+3.25 <= 33
    write("assets/drop-local.svg", f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 108 108" width="512" height="512">
  <rect width="108" height="108" rx="24" fill="{TEAL}" />
  <path d="{PATH.replace(',', ' ')}" fill="none" stroke="{NAVY}" stroke-width="{STROKE}" stroke-linecap="round" />
  <circle cx="54" cy="78" r="3.25" fill="{NAVY}" />
</svg>''')
    render(256).save(ASSETS / "drop-local-256.png")
    render(512).save(ASSETS / "drop-local-preview.png")
    render(256).save(ASSETS / "drop-local.ico", format="ICO", sizes=[(s,s) for s in SIZES], bitmap_format="bmp")
    write("android/app/src/main/res/drawable/ic_drop.xml", vector(background=True))
    write("android/app/src/main/res/drawable/ic_wifi.xml", vector())
    write("android/app/src/main/res/drawable/ic_launcher_foreground.xml", vector())
    write("android/app/src/main/res/drawable/ic_launcher_monochrome.xml", vector(monochrome=True))
    write("android/app/src/main/res/values/icon_colors.xml", f'<resources><color name="launcher_background">{TEAL}</color></resources>')
    write("android/app/src/main/res/mipmap-anydpi/ic_launcher.xml", vector(background=True))
    write("android/app/src/main/res/mipmap-anydpi-v26/ic_launcher.xml", adaptive())
    write("android/app/src/main/res/mipmap-anydpi-v33/ic_launcher.xml", adaptive(monochrome=True))
    with Image.open(ASSETS / "drop-local.ico") as icon:
        assert icon.ico.sizes() == {(s,s) for s in SIZES}
        for size in SIZES:
            frame = icon.ico.getimage((size,size))
            assert frame.size == (size,size)
    print("PASS: ICO frames", SIZES, "; adaptive foreground inside 66dp safe zone")

if __name__ == "__main__":
    main()
