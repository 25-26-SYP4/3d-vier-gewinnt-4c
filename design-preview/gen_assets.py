"""
Finale Pollution-Assets -> direkt in Unity Resources.
Erzeugt: bg_pollution, icon_oil, icon_toxic, plate.
"""
import math, random
from PIL import Image, ImageDraw, ImageFilter, ImageChops

OUT = "../3d-vier-gewinnt/Assets/Resources/PollutionTheme"

def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))

def vgrad(w, h, stops):
    img = Image.new("RGB", (w, h)); px = img.load()
    for y in range(h):
        t = y / (h - 1)
        for i in range(len(stops) - 1):
            p0, c0 = stops[i]; p1, c1 = stops[i+1]
            if p0 <= t <= p1:
                col = lerp(c0, c1, (t - p0) / (p1 - p0 + 1e-9)); break
        else:
            col = stops[-1][1]
        for x in range(w):
            px[x, y] = col
    return img

def soft_blob(size, color, alpha):
    im = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    ImageDraw.Draw(im).ellipse([0, 0, size, size], fill=color + (alpha,))
    return im.filter(ImageFilter.GaussianBlur(size * 0.18))

def add_grain(img, amount=14):
    w, h = img.size
    noise = Image.effect_noise((w, h), amount).convert("L").point(lambda p: int((p - 128) * 0.5 + 128))
    return ImageChops.overlay(img.convert("RGB"), Image.merge("RGB", (noise, noise, noise)))

# ---------------- BACKGROUND ----------------
def make_bg():
    random.seed(7)
    W, H = 1920, 1080
    bg = vgrad(W, H, [
        (0.00, (38, 42, 40)), (0.45, (74, 70, 55)), (0.72, (140, 120, 78)),
        (0.86, (96, 86, 60)), (1.00, (44, 42, 36)),
    ]).convert("RGBA")
    bg.alpha_composite(soft_blob(900, (210, 180, 110), 150), (W - 1250, 150))
    bg.alpha_composite(soft_blob(420, (235, 205, 130), 180), (W - 1000, 360))
    haze = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(haze).rectangle([0, int(H*0.62), W, int(H*0.78)], fill=(150, 135, 95, 60))
    bg.alpha_composite(haze.filter(ImageFilter.GaussianBlur(40)))

    def factory(layer, x, base_y, scale, shade):
        d = ImageDraw.Draw(layer); bw = int(260*scale); bh = int(150*scale)
        d.rectangle([x, base_y - bh, x + bw, base_y], fill=shade)
        teeth = 4; tw = bw // teeth
        for i in range(teeth):
            tx = x + i*tw
            d.polygon([(tx, base_y-bh), (tx+tw, base_y-bh), (tx+tw, base_y-bh-int(40*scale))], fill=shade)
        stacks = []
        for sx in (x + int(40*scale), x + int(bw - 70*scale)):
            sw = int(46*scale); sh = int(220*scale); sy = base_y - bh - sh
            d.rectangle([sx, sy, sx+sw, base_y-bh+5], fill=shade)
            d.rectangle([sx-6, sy, sx+sw+6, sy+int(18*scale)], fill=lerp(shade,(0,0,0),0.3))
            stacks.append((sx + sw//2, sy))
        return stacks

    back = Image.new("RGBA", (W, H), (0,0,0,0)); sb = []
    for x in range(-100, W, 380):
        sb += factory(back, x, int(H*0.80), random.uniform(0.55,0.8), (70,70,66))
    bg.alpha_composite(back.filter(ImageFilter.GaussianBlur(3)))
    front = Image.new("RGBA", (W, H), (0,0,0,0)); sf = []
    for x in range(-150, W, 520):
        sf += factory(front, x, int(H*0.92), random.uniform(0.95,1.25), (34,33,30))
    bg.alpha_composite(front)

    def smoke(img, x, y, scale, tint):
        layer = Image.new("RGBA", (W, H), (0,0,0,0)); cx, cy = x, y
        for i in range(14):
            r = int((30 + i*14)*scale)
            cx += random.randint(-14, 22); cy -= int((34 + i*2)*scale)
            layer.alpha_composite(soft_blob(r*2, tint, max(0,150-i*9)), (cx-r, cy-r))
        img.alpha_composite(layer)
    for (sx, sy) in sb: smoke(bg, sx, sy, 0.7, (110,110,104))
    for (sx, sy) in sf: smoke(bg, sx, sy, 1.0, (95,95,90))

    bg.alpha_composite(Image.new("RGBA", (W, H), (120,110,80,28)))
    vig = Image.new("L", (W, H), 0)
    ImageDraw.Draw(vig).ellipse([-W*0.25, -H*0.25, W*1.25, H*1.25], fill=255)
    vig = vig.filter(ImageFilter.GaussianBlur(220))
    dark = Image.new("RGBA", (W, H), (10,10,8,255))
    dark.putalpha(ImageChops.invert(vig).point(lambda p: int(p*0.6)))
    bg.alpha_composite(dark)
    add_grain(bg).save(f"{OUT}/bg_pollution.png")
    print("bg_pollution.png")

# ---------------- BARRELS ----------------
def barrel(fname, body, rim, drip, symbol):
    S = 512
    im = Image.new("RGBA", (S, S), (0,0,0,0)); d = ImageDraw.Draw(im)
    sh = Image.new("RGBA",(S,S),(0,0,0,0))
    ImageDraw.Draw(sh).ellipse([130,430,382,490], fill=(0,0,0,120))
    im.alpha_composite(sh.filter(ImageFilter.GaussianBlur(10)))
    x0,y0,x1,y1 = 150,110,362,450
    d.rounded_rectangle([x0,y0,x1,y1], radius=26, fill=body)
    d.ellipse([x0,y0-22,x1,y0+22], fill=lerp(body,(255,255,255),0.18))
    d.ellipse([x0,y1-22,x1,y1+22], fill=lerp(body,(0,0,0),0.35))
    for ry in (190,300,400): d.rectangle([x0,ry,x1,ry+18], fill=rim)
    d.rectangle([x0+10,y0,x0+34,y1], fill=lerp(body,(255,255,255),0.22))
    random.seed(hash(fname) % 999)
    for _ in range(40):
        rx = random.randint(x0+10,x1-10); ry = random.randint(y0,y1); rr = random.randint(4,16)
        d.ellipse([rx,ry,rx+rr,ry+rr], fill=lerp(body,(20,15,5),0.5)+(110,))
    cx, cy = (x0+x1)//2, 280
    d.ellipse([cx-58,cy-58,cx+58,cy+58], fill=(245,240,225,255))
    d.ellipse([cx-58,cy-58,cx+58,cy+58], outline=(30,30,30,255), width=6)
    symbol(d, cx, cy)
    d.ellipse([250,452,286,500], fill=drip)
    d.polygon([(250,470),(286,470),(268,452)], fill=drip)
    im.filter(ImageFilter.SMOOTH).save(f"{OUT}/{fname}")
    print(fname)

def sym_oil(d, cx, cy):
    d.ellipse([cx-22,cy-8,cx+22,cy+34], fill=(20,18,16))
    d.polygon([(cx-22,cy+12),(cx+22,cy+12),(cx,cy-32)], fill=(20,18,16))

def sym_toxic(d, cx, cy):
    g = (40,120,40)
    d.ellipse([cx-26,cy-30,cx+26,cy+18], fill=g)
    d.rectangle([cx-16,cy+8,cx+16,cy+30], fill=g)
    d.ellipse([cx-16,cy-18,cx-4,cy-4], fill=(245,240,225))
    d.ellipse([cx+4,cy-18,cx+16,cy-4], fill=(245,240,225))

# ---------------- PLATE (HUD panel) ----------------
def make_plate():
    W, H = 512, 256
    im = Image.new("RGBA",(W,H),(0,0,0,0)); d = ImageDraw.Draw(im)
    # dunkle, leicht transparente Metallplatte
    d.rounded_rectangle([6,6,W-6,H-6], radius=28, fill=(38,40,36,235))
    d.rounded_rectangle([6,6,W-6,H-6], radius=28, outline=(90,86,70,255), width=5)
    # Glanz oben
    gl = Image.new("RGBA",(W,H),(0,0,0,0))
    ImageDraw.Draw(gl).rounded_rectangle([16,14,W-16,90], radius=20, fill=(255,255,255,22))
    im.alpha_composite(gl.filter(ImageFilter.GaussianBlur(8)))
    # Nieten
    for (rx,ry) in [(34,34),(W-34,34),(34,H-34),(W-34,H-34)]:
        d.ellipse([rx-9,ry-9,rx+9,ry+9], fill=(120,116,98,255))
        d.ellipse([rx-9,ry-9,rx+9,ry+9], outline=(30,30,26,255), width=2)
    # Rostflecken
    random.seed(3)
    for _ in range(60):
        rx = random.randint(20,W-20); ry = random.randint(20,H-20); rr = random.randint(2,9)
        d.ellipse([rx,ry,rx+rr,ry+rr], fill=(90,60,30,70))
    im.save(f"{OUT}/plate.png")
    print("plate.png")

make_bg()
barrel("icon_oil.png",   (96,70,40),  (60,42,22), (15,12,10),  sym_oil)
barrel("icon_toxic.png", (74,96,40),  (48,64,24), (120,170,40), sym_toxic)
make_plate()
print("ALL DONE ->", OUT)
