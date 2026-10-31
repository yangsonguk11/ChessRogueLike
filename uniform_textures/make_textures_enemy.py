"""체크메이트 세일러 교복 — 적 버전 VRoid 텍스처 (v2 블레이저 템플릿 + 치마 템플릿).

v2와 같은 배치에 색만 바꾼다: 옷감은 검은 계열, 이너(가슴판)와 칼라는 흰색,
칼라 줄무늬·체커는 검은색으로 뒤집는다. 스카프는 주홍 그대로.

사용: python3 make_textures_enemy.py <템플릿 이미지 폴더> <출력 폴더>
템플릿 폴더에는 5.webp(상의 layer), 6.webp(상의 guide), 2.png(치마 layer), 4.png(치마 guide)가 있어야 한다.
"""
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageChops

SRC = Path(sys.argv[1])
OUT = Path(sys.argv[2])
OUT.mkdir(parents=True, exist_ok=True)

WHITE = (246, 244, 238)        # 이너·칼라
DARK = (28, 28, 34)            # 옷감(하복 몸판, 블레이저, 칼라 무늬)
DARK_SIDE = (21, 21, 26)       # 블레이저 옆판
SKIRT = (24, 24, 29)
LINE_DARK = (6, 6, 9)
SEAM = (66, 66, 78)
CREASE = (44, 44, 54)
PEWTER = (201, 206, 214)
ACCENT = (200, 65, 43)
KNOT_SHADE = (120, 40, 28)
SS = 2


class Canvas:
    def __init__(self, w, h, out_scale, bg, base_alpha=None):
        self.s = out_scale * SS
        self.size = (int(round(w * self.s)), int(round(h * self.s)))
        self.out_size = (int(round(w * out_scale)), int(round(h * out_scale)))
        self.img = Image.new("RGB", self.size, bg)
        self.d = ImageDraw.Draw(self.img)
        self.alpha = (Image.open(base_alpha).getchannel("A").resize(self.size, Image.BILINEAR)
                      if base_alpha else Image.new("L", self.size, 255))

    def P(self, pts):
        return [(x * self.s, y * self.s) for x, y in pts]

    def mask_poly(self, polys):
        m = Image.new("L", self.size, 0)
        md = ImageDraw.Draw(m)
        for pts in polys:
            md.polygon(self.P(pts), fill=255)
        return m

    def fill(self, pts, color, outline=None, width=0):
        self.d.polygon(self.P(pts), fill=color)
        if outline:
            self.line(list(pts) + [pts[0]], outline, width)

    def line(self, pts, color, width):
        self.d.line(self.P(pts), fill=color, width=max(1, int(round(width * self.s))), joint="curve")

    def dashed(self, pts, color, width, dash, gap):
        on, acc = True, 0.0
        for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
            seg = math.hypot(x1 - x0, y1 - y0)
            pos = 0.0
            while pos < seg:
                left = (dash if on else gap) - acc
                step = min(left, seg - pos)
                if on:
                    a = (x0 + (x1 - x0) * pos / seg, y0 + (y1 - y0) * pos / seg)
                    b = (x0 + (x1 - x0) * (pos + step) / seg, y0 + (y1 - y0) * (pos + step) / seg)
                    self.line([a, b], color, width)
                pos += step
                acc += step
                if acc >= (dash if on else gap) - 1e-9:
                    on, acc = not on, 0.0

    def circle(self, c, r, fill, outline=None, width=0):
        x, y = c
        box = [(x - r) * self.s, (y - r) * self.s, (x + r) * self.s, (y + r) * self.s]
        self.d.ellipse(box, fill=fill, outline=outline,
                       width=max(1, int(round(width * self.s))) if outline else 0)

    def paint(self, color, mask):
        self.img.paste(color, (0, 0) + self.size, mask)

    def set_alpha(self, polys, value):
        self.alpha.paste(value, (0, 0) + self.size, self.mask_poly(polys))

    def result(self, layer_path, shading_k, with_alpha):
        rgb = apply_shading(self.img.resize(self.out_size, Image.LANCZOS), layer_path, shading_k)
        if not with_alpha:
            return rgb
        out = rgb.convert("RGBA")
        out.putalpha(self.alpha.resize(self.out_size, Image.BILINEAR))
        return out


def _box(a, r, axis):
    pad = [(0, 0), (0, 0)]
    pad[axis] = (r + 1, r)
    c = np.cumsum(np.pad(a, pad, mode="edge"), axis=axis)
    hi = np.take(c, np.arange(2 * r + 1, c.shape[axis]), axis=axis)
    lo = np.take(c, np.arange(0, c.shape[axis] - 2 * r - 1), axis=axis)
    return (hi - lo) / (2 * r + 1)


def soft_blur(a, r):
    a = a.astype(np.float64)
    for _ in range(3):
        a = _box(_box(a, r, 0), r, 1)
    return a


def apply_shading(img, layer_path, k):
    """기본 텍스처의 음영을 곱한다. 투명부의 검은색이 번지지 않게 알파 가중 블러를 쓴다."""
    layer = Image.open(layer_path).convert("RGBA").resize(img.size, Image.BILINEAR)
    Lc = np.asarray(layer.convert("L")).astype(np.float32)
    A = np.asarray(layer.getchannel("A")).astype(np.float32) / 255.0
    num, den = soft_blur(Lc * A, 6), soft_blur(A, 6)
    Ln = np.where(den > 0.05, num / np.maximum(den, 1e-6), 255.0) / 255.0
    f = np.clip(1.0 - k * (1.0 - Ln), 0.55, 1.0)[..., None]
    arr = np.asarray(img).astype(np.float32) * f
    return Image.fromarray(np.clip(arr + 0.5, 0, 255).astype(np.uint8))


def ident(p):
    return p


def quad(p0, c, p1, n=24):
    return [((1 - t) ** 2 * p0[0] + 2 * t * (1 - t) * c[0] + t * t * p1[0],
             (1 - t) ** 2 * p0[1] + 2 * t * (1 - t) * c[1] + t * t * p1[1])
            for t in (i / n for i in range(n + 1))]


def catmull(pts, n=16):
    out = []
    p = [pts[0]] + pts + [pts[-1]]
    for i in range(1, len(p) - 2):
        p0, p1, p2, p3 = p[i - 1], p[i], p[i + 1], p[i + 2]
        for k in range(n):
            t = k / n
            out.append(tuple(
                0.5 * (2 * p1[j] + (-p0[j] + p2[j]) * t
                       + (2 * p0[j] - 5 * p1[j] + 4 * p2[j] - p3[j]) * t * t
                       + (-p0[j] + 3 * p1[j] - 3 * p2[j] + p3[j]) * t ** 3)
                for j in range(2)))
    out.append(pts[-1])
    return out


# ================================================================ 상의(블레이저 템플릿, 2000x2000)
TOP_SCALE = 2048 / 2000
CX = 1000


def mirror(pts):
    return [(2 * CX - x, y) for x, y in pts]


A = (818.0, 427.0)
N = (912.0, 426.0)
C = (1000.0, 645.0)
T = (1000.0, 770.0)
_ul = math.hypot(T[0] - A[0], T[1] - A[1])
U = ((T[0] - A[0]) / _ul, (T[1] - A[1]) / _ul)
NRM = (U[1], -U[0])


def lf(t, d):
    return (A[0] + t * U[0] + d * NRM[0], A[1] + t * U[1] + d * NRM[1])


LAPEL = [A, N, C, T]
DICKEY = [N, mirror([N])[0], C]
_vl = math.hypot(C[0] - N[0], C[1] - N[1])
M = ((C[1] - N[1]) / _vl, -(C[0] - N[0]) / _vl)
STRIP = [N, (N[0] + 24 * M[0], N[1] + 24 * M[1]), (C[0] + 24 * M[0], C[1] + 24 * M[1]), C]
FRONT_OPAQUE = [[(745, 415), (1255, 415), (1255, 1235), (745, 1235)]]

CELL = 13
STRIPE_LINE = [lf(-150, 17), lf(520, 17), lf(520, 27), lf(-150, 27)]
CHECKER_CELLS = []
for _i in range(-12, 42):
    for _r in (0, 1):
        if (_i + _r) % 2 == 0:
            _d0 = 40 + CELL * _r
            CHECKER_CELLS.append([lf(CELL * _i, _d0), lf(CELL * _i + CELL, _d0),
                                  lf(CELL * _i + CELL, _d0 + CELL), lf(CELL * _i, _d0 + CELL)])


def fm(x, y):
    return (CX + 2.395 * (x - 200), 427 + 2.382 * (y - 51))


KNOT_BAND = [fm(187, 120)] + quad(fm(187, 120), fm(200, 113), fm(213, 120))[1:] + [fm(208, 158), fm(192, 158)]
KNOT = quad(fm(189, 156), fm(200, 151), fm(211, 156)) + quad(fm(209, 173), fm(200, 177), fm(191, 173))
KNOT_FOLDS = [[fm(195, 123), fm(197.5, 154)], [fm(205, 123), fm(202.5, 154)]]
KNOT_CREASES = [quad(fm(193, 160), fm(200, 164), fm(207, 160), 12), quad(fm(194, 168), fm(200, 171), fm(206, 168), 12)]
TAILS_SUMMER = ([fm(199, 172), fm(207, 171), fm(222, 252), fm(213, 246), fm(206, 256)],
                [fm(193, 171), fm(201, 172), fm(198, 264), fm(189, 255), fm(180, 262)])
TAILS_WINTER = ([fm(199, 172), fm(207, 171), fm(214, 224), fm(206, 218), fm(200, 226)],
                [fm(193, 171), fm(201, 172), fm(199, 228), fm(191, 221), fm(184, 228)])


def draw_front_collar(cv, tails):
    cv.fill(DICKEY, WHITE)
    for side in (ident, mirror):
        cv.fill(side(STRIP), ACCENT, LINE_DARK, 1.6)
    for side in (ident, mirror):
        cv.fill(side(LAPEL), WHITE)
        stripes = cv.mask_poly([side(STRIPE_LINE)] + [side(c) for c in CHECKER_CELLS])
        cv.paint(DARK, ImageChops.multiply(stripes, cv.mask_poly([side(LAPEL)])))
        cv.line(side([N, A, T]), LINE_DARK, 2.6)
    cv.fill(KNOT_BAND, ACCENT, LINE_DARK, 1.8)
    for f in KNOT_FOLDS:
        cv.line(f, KNOT_SHADE, 1.4)
    for t in tails:
        cv.fill(t, ACCENT, LINE_DARK, 1.8)
    cv.fill(KNOT, ACCENT, LINE_DARK, 1.8)
    for c in KNOT_CREASES:
        cv.line(c, KNOT_SHADE, 1.4)


FLAP_EDGE = 571.5
FLAP_BOTTOM = 780.0


def draw_back_collar(cv):
    flap = [(380, 380), (FLAP_EDGE, 380), (FLAP_EDGE, FLAP_BOTTOM), (380, FLAP_BOTTOM)]
    lines = [[(FLAP_EDGE - 27, 380), (FLAP_EDGE - 17, 380), (FLAP_EDGE - 17, FLAP_BOTTOM - 17), (FLAP_EDGE - 27, FLAP_BOTTOM - 17)],
             [(380, FLAP_BOTTOM - 27), (FLAP_EDGE - 17, FLAP_BOTTOM - 27), (FLAP_EDGE - 17, FLAP_BOTTOM - 17), (380, FLAP_BOTTOM - 17)]]
    cells = []
    for j in range(0, 30):
        y1 = FLAP_BOTTOM - 40 - CELL * j
        if y1 < 380:
            break
        for i in (range(0, 11) if j < 2 else (9, 10)):
            if (i + j) % 2 == 0:
                x0 = 388.5 + CELL * i
                cells.append([(x0, y1 - CELL), (x0 + CELL, y1 - CELL), (x0 + CELL, y1), (x0, y1)])
    for side in (ident, mirror):
        cv.fill(side(flap), WHITE)
        m = cv.mask_poly([side(p) for p in lines] + [side(c) for c in cells])
        cv.paint(DARK, ImageChops.multiply(m, cv.mask_poly([side(flap)])))
        cv.line(side([(FLAP_EDGE, 430), (FLAP_EDGE, FLAP_BOTTOM), (395, FLAP_BOTTOM)]), LINE_DARK, 2.6)


def top_canvas():
    cv = Canvas(2000, 2000, TOP_SCALE, DARK, SRC / "5.webp")
    cv.set_alpha(FRONT_OPAQUE, 255)
    return cv


def summer_top():
    cv = top_canvas()
    for side in (ident, mirror):
        cv.set_alpha([side([(0, 0), (520, 0), (520, 412), (0, 412)])], 0)
        cv.fill(side([(520, 0), (552, 0), (552, 412), (520, 412)]), WHITE)
        cv.fill(side([(531, 0), (538, 0), (538, 412), (531, 412)]), DARK)
    cv.dashed([(395, 1225), (1605, 1225)], SEAM, 2.2, 11, 8)
    draw_front_collar(cv, TAILS_SUMMER)
    draw_back_collar(cv)
    return cv.result(SRC / "5.webp", 1.3, True)


PS_FRONT = catmull([(745, 600), (790, 700), (822, 800), (845, 900), (852, 995), (848, 1100), (838, 1240), (836, 1262)])
PS_BACK = catmull([(645, 600), (605, 700), (578, 800), (556, 900), (548, 995), (552, 1100), (562, 1240), (563, 1262)])
TOP_EDGE = [(1012, 747), (1012, 1118)] + quad((1012, 1118), (1013, 1205), (1064, 1243))[1:]
UNDER_EDGE = quad((1000, 1132), (997, 1208), (942, 1243))


def winter_blazer():
    cv = top_canvas()
    for side in (ident, mirror):
        cv.dashed(side([(118, 18), (118, 400)]), SEAM, 2.4, 10, 7)
    for side in (ident, mirror):
        cv.fill(side(PS_FRONT + [(695, 1262), (695, 762), (745, 745)]), DARK_SIDE)
        cv.fill(side(PS_BACK + [(695, 1262), (695, 762), (645, 745)]), DARK_SIDE)
        cv.line(side(PS_FRONT), SEAM, 2.6)
        cv.line(side(PS_BACK), SEAM, 2.6)
        cv.line(side([(695, 762), (695, 1240)]), SEAM, 2.2)
        cv.line(side([(397, FLAP_BOTTOM), (397, 1240)]), SEAM, 2.6)
    cv.line([(397, 1135), (412, 1150), (412, 1240)], SEAM, 2.6)
    for side in (ident, mirror):
        cv.fill(side([(380, 979), (552, 979), (552, 1011), (380, 1011)]), DARK)
        cv.line(side([(395, 979), (552, 979), (552, 1011), (395, 1011)]), LINE_DARK, 2.4)
        cv.dashed(side([(395, 985), (546, 985)]), SEAM, 1.8, 8, 6)
        cv.dashed(side([(395, 1005), (546, 1005)]), SEAM, 1.8, 8, 6)
        cv.circle(side([(532, 995)])[0], 10, PEWTER, LINE_DARK, 2.0)
        cv.circle(side([(532, 995)])[0], 6, None, (150, 156, 170), 1.3)
        cv.circle(side([(529, 992)])[0], 2.4, (236, 238, 242))
    for side in (ident, mirror):
        flap = side([(762, 1108), (905, 1108), (903, 1140)] + quad((903, 1140), (833, 1150), (764, 1140))[1:])
        cv.fill(flap, DARK, LINE_DARK, 2.2)
        cv.dashed(side([(768, 1131), (898, 1131)]), SEAM, 1.8, 8, 6)
    cv.dashed([(395, 1225), (950, 1225)], SEAM, 2.4, 11, 8)
    cv.dashed([(1058, 1225), (1605, 1225)], SEAM, 2.4, 11, 8)
    gap = [(1006, 1125)] + UNDER_EDGE + [(942, 1275), (1064, 1275)] + list(reversed(TOP_EDGE[1:]))
    cv.set_alpha([gap], 0)
    cv.line([(1015.5, 750), (1015.5, 1116)], SEAM, 1.6)
    cv.line(TOP_EDGE, LINE_DARK, 3.2)
    cv.line(UNDER_EDGE, LINE_DARK, 3.0)
    for cy in (995, 1100):
        cv.circle((CX, cy), 13, PEWTER, LINE_DARK, 2.2)
        cv.circle((CX, cy), 8, None, (150, 156, 170), 1.5)
        cv.circle((CX - 4, cy - 4), 2.8, (236, 238, 242))
    draw_front_collar(cv, TAILS_WINTER)
    draw_back_collar(cv)
    return cv.result(SRC / "5.webp", 1.3, True)


# ================================================================ 치마(치마 템플릿, 1024x512)
def guide_lines(alpha, y):
    xs = np.where(alpha[y] > 128)[0]
    runs, s, p = [], xs[0], xs[0]
    for x in xs[1:]:
        if x != p + 1:
            runs.append((s, p))
            s = x
        p = x
    runs.append((s, p))
    return [(a + b) / 2 for a, b in runs if a > 0 and b < alpha.shape[1] - 1]


def skirt():
    cv = Canvas(1024, 512, 2.0, SKIRT)
    cv.dashed([(0, 201), (1024, 201)], SEAM, 1.6, 8, 5)
    # 검은 원단은 음영만으로 주름이 잘 안 보여, 주름마다 접힘선을 한 줄씩 넣는다
    alpha = np.asarray(Image.open(SRC / "4.png"))[..., 3]
    top, bot = guide_lines(alpha, 310), guide_lines(alpha, 495)
    paired = set()
    for k in range(len(top) - 1):
        if top[k + 1] - top[k] <= 16 and bot[k + 1] - bot[k] <= 18:
            paired.update((k, k + 1))
    for k in range(len(top)):
        if k not in paired:
            cv.line([(top[k], 302), (bot[k], 510)], CREASE, 1.6)
    return cv.result(SRC / "2.png", 1.6, False)


def preview(tex, guide_path, out_path, size):
    g = Image.open(guide_path).convert("RGBA").resize(size, Image.BILINEAR)
    bg = Image.new("RGBA", size, (150, 154, 162, 255))
    bg.alpha_composite(tex.convert("RGBA").resize(size, Image.LANCZOS))
    ga = np.asarray(g)[..., 3].astype(np.float32) / 255.0
    arr = np.asarray(bg).astype(np.float32)
    arr[..., :3] *= (1 - 0.45 * ga[..., None])
    Image.fromarray(arr.astype(np.uint8)).save(out_path)


if __name__ == "__main__":
    s = summer_top()
    s.save(OUT / "enemy_summer_top.png")
    w = winter_blazer()
    w.save(OUT / "enemy_winter_blazer.png")
    k = skirt()
    k.save(OUT / "enemy_skirt.png")
    pv = OUT / "preview"
    pv.mkdir(exist_ok=True)
    preview(s, SRC / "6.webp", pv / "enemy_summer_guide.png", (1000, 1000))
    preview(w, SRC / "6.webp", pv / "enemy_winter_guide.png", (1000, 1000))
    preview(k, SRC / "4.png", pv / "enemy_skirt_guide.png", (1024, 512))
    sheet = Image.new("RGBA", (1880, 900), (150, 154, 162, 255))
    for i, t in enumerate((s, w)):
        sheet.alpha_composite(t.resize((600, 600), Image.LANCZOS), (20 + i * 620, 20))
    sheet.paste(k.resize((600, 300), Image.LANCZOS), (1260, 20))
    sheet.convert("RGB").save(OUT / "enemy_preview_sheet.png")
    print("done", s.size, w.size, k.size)
