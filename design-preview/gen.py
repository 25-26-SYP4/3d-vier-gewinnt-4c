"""
Pollution-Theme Asset-Generator (Vorschau) fuer 3D-Vier-Gewinnt.
Erzeugt: verschmutzungs-Hintergrund + zwei Spieler-Icons (Oel / Giftmuell).
Reiner PIL-Code, Flat-Illustration mit Smog/Dunst.
"""
import math, random
from PIL import Image, ImageDraw, ImageFilter, ImageChops

random.seed(7)
OUT = "."

# ---------- helpers ----------
def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))

def vgrad(w, h, stops):
    """stops: list of (pos0..1, (r,g,b))"""
    img = Image.new("RGB", (w, h))
    px = img.load()
    for y in range(h):
        t = y / (h - 1)
        # find segment
        for i in range(len(stops) - 1):
            p0, c0 = stops[i]; p1, c1 = stops[i+1]
            if p0 <= t <= p1:
                lt = (t - p0) / (p1 - p0 + 1e-9)
                col = lerp(c0, c1, lt)
                break
        else:
            col = stops[-1][1]
        for x in range(w):
            px[x, y] = col
    return img

def soft_blob(size, color, alpha):
    s = size
    im = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse([0, 0, s, s], fill=color + (alpha,))
    return im.filter(ImageFilter.GaussianBlur(s * 0.18))

def add_grain(img, amount=10):
    w, h = img.size
    noise = Image.effect_noise((w, h), amount).convert("L")
    noise = noise.point(lambda p: int((p - 128) * 0.5 + 128))
    noise_rgb = Image.merge("RGB", (noise, noise, noise))
    return ImageChops.overlay(img.convert("RGB"), noise_rgb)

# ============================================================
# BACKGROUND  1920x1080
# ============================================================
W, H = 1920, 1080
sky = vgrad(W, H, [
    (0.00, (38, 42, 40)),     # dunkles smog-gruen-grau oben
    (0.45, (74, 70, 55)),     # braungrau
    (0.72, (140, 120, 78)),   # dreckiger gelb-dunst horizont
    (0.86, (96, 86, 60)),
    (1.00, (44, 42, 36)),     # boden
])
bg = sky.convert("RGBA")
draw = ImageDraw.Draw(bg)

# trübe Dunst-Sonne
sun = soft_blob(900, (210, 180, 110), 150)
bg.alpha_composite(sun, (W - 1250, 150))
sun2 = soft_blob(420, (235, 205, 130), 180)
bg.alpha_composite(sun2, (W - 1000, 360))

# ferne Dunstschicht
haze = Image.new("RGBA", (W, H), (0, 0, 0, 0))
hd = ImageDraw.Draw(haze)
hd.rectangle([0, int(H*0.62), W, int(H*0.78)], fill=(150, 135, 95, 60))
haze = haze.filter(ImageFilter.GaussianBlur(40))
bg.alpha_composite(haze)

def factory(layer_img, x, base_y, scale, shade):
    """zeichnet eine Fabrik-Silhouette + Schlote, gibt Schlot-Positionen zurueck"""
    d = ImageDraw.Draw(layer_img)
    col = shade
    bw = int(260 * scale); bh = int(150 * scale)
    d.rectangle([x, base_y - bh, x + bw, base_y], fill=col)
    # Saegezahn-Dach
    teeth = 4; tw = bw // teeth
    for i in range(teeth):
        tx = x + i * tw
        d.polygon([(tx, base_y - bh), (tx + tw, base_y - bh),
                   (tx + tw, base_y - bh - int(40*scale))], fill=col)
    stacks = []
    for sx in (x + int(40*scale), x + int(bw - 70*scale)):
        sw = int(46 * scale); sh = int(220 * scale)
        sy = base_y - bh - sh
        d.rectangle([sx, sy, sx + sw, base_y - bh + 5], fill=col)
        d.rectangle([sx - 6, sy, sx + sw + 6, sy + int(18*scale)], fill=lerp(col,(0,0,0),0.3))
        stacks.append((sx + sw // 2, sy))
    return stacks

# Hintere Reihe (heller/dunstiger)
back_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
all_stacks_back = []
for x in range(-100, W, 380):
    s = random.uniform(0.55, 0.8)
    all_stacks_back += factory(back_layer, x, int(H*0.80), s, (70, 70, 66))
back_layer = back_layer.filter(ImageFilter.GaussianBlur(3))
bg.alpha_composite(back_layer)

# Vordere Reihe (dunkler, schaerfer)
front_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
all_stacks_front = []
for x in range(-150, W, 520):
    s = random.uniform(0.95, 1.25)
    all_stacks_front += factory(front_layer, x, int(H*0.92), s, (34, 33, 30))
bg.alpha_composite(front_layer)

# Rauchfahnen aus allen Schloten
def smoke(img, x, y, scale=1.0, tint=(120,118,110)):
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    n = 14
    cx, cy = x, y
    for i in range(n):
        r = int((30 + i * 14) * scale)
        cx += random.randint(-14, 22)
        cy -= int((34 + i*2) * scale)
        a = max(0, 150 - i * 9)
        blob = soft_blob(r * 2, tint, a)
        layer.alpha_composite(blob, (cx - r, cy - r))
    img.alpha_composite(layer)

for (sx, sy) in all_stacks_back:
    smoke(bg, sx, sy, scale=0.7, tint=(110, 110, 104))
for (sx, sy) in all_stacks_front:
    smoke(bg, sx, sy, scale=1.0, tint=(95, 95, 90))

# Smog-Schleier ueber alles
smog = Image.new("RGBA", (W, H), (120, 110, 80, 28))
bg.alpha_composite(smog)

# Vignette
vig = Image.new("L", (W, H), 0)
vd = ImageDraw.Draw(vig)
vd.ellipse([-W*0.25, -H*0.25, W*1.25, H*1.25], fill=255)
vig = vig.filter(ImageFilter.GaussianBlur(220))
dark = Image.new("RGBA", (W, H), (10, 10, 8, 255))
dark.putalpha(ImageChops.invert(vig).point(lambda p: int(p*0.6)))
bg.alpha_composite(dark)

bg_final = add_grain(bg, amount=14)
bg_final.save(f"{OUT}/bg_pollution.png")
print("bg_pollution.png", bg_final.size)

# ============================================================
# ICONS  512x512 (transparent) - Fass-Symbole
# ============================================================
def barrel(fname, body, rim, drip, symbol):
    S = 512
    im = Image.new("RGBA", (S, S), (0,0,0,0))
    d = ImageDraw.Draw(im)
    # Schatten
    sh = Image.new("RGBA",(S,S),(0,0,0,0))
    ImageDraw.Draw(sh).ellipse([130,430,382,490], fill=(0,0,0,120))
    im.alpha_composite(sh.filter(ImageFilter.GaussianBlur(10)))
    # Fasskoerper
    x0,y0,x1,y1 = 150, 110, 362, 450
    d.rounded_rectangle([x0,y0,x1,y1], radius=26, fill=body)
    # obere/untere Ellipse
    d.ellipse([x0, y0-22, x1, y0+22], fill=lerp(body,(255,255,255),0.18))
    d.ellipse([x0, y1-22, x1, y1+22], fill=lerp(body,(0,0,0),0.35))
    # Reifen
    for ry in (190, 300, 400):
        d.rectangle([x0, ry, x1, ry+18], fill=rim)
    # Glanzkante links
    d.rectangle([x0+10, y0, x0+34, y1], fill=lerp(body,(255,255,255),0.22))
    # Rost/Flecken
    random.seed(hash(fname) % 999)
    for _ in range(40):
        rx = random.randint(x0+10, x1-10); ry = random.randint(y0, y1)
        rr = random.randint(4, 16)
        d.ellipse([rx,ry,rx+rr,ry+rr], fill=lerp(body,(20,15,5),0.5)+(110,))
    # Symbol-Plakette
    cx, cy = (x0+x1)//2, 280
    d.ellipse([cx-58,cy-58,cx+58,cy+58], fill=(245,240,225,255))
    d.ellipse([cx-58,cy-58,cx+58,cy+58], outline=(30,30,30,255), width=6)
    symbol(d, cx, cy)
    # Tropfen unten
    d.ellipse([250, 452, 286, 500], fill=drip)
    d.polygon([(250,470),(286,470),(268,452)], fill=drip)
    im = im.filter(ImageFilter.SMOOTH)
    im.save(f"{OUT}/{fname}")
    print(fname, "ok")

def sym_oil(d, cx, cy):
    # Oel-Tropfen schwarz
    d.ellipse([cx-22,cy-8,cx+22,cy+34], fill=(20,18,16))
    d.polygon([(cx-22,cy+12),(cx+22,cy+12),(cx,cy-32)], fill=(20,18,16))

def sym_toxic(d, cx, cy):
    # Totenkopf-ish / Biohazard vereinfacht (gruen)
    g = (40,120,40)
    d.ellipse([cx-26,cy-30,cx+26,cy+18], fill=g)         # Kopf
    d.rectangle([cx-16,cy+8,cx+16,cy+30], fill=g)        # Kiefer
    d.ellipse([cx-16,cy-18,cx-4,cy-4], fill=(245,240,225)) # Auge l
    d.ellipse([cx+4,cy-18,cx+16,cy-4], fill=(245,240,225)) # Auge r

barrel("icon_oil.png",   body=(96,70,40),  rim=(60,42,22), drip=(15,12,10), symbol=sym_oil)
barrel("icon_toxic.png", body=(74,96,40),  rim=(48,64,24), drip=(120,170,40), symbol=sym_toxic)
print("done")
