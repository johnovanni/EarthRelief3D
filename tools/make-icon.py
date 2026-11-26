"""Generate the app icon (src/EarthRelief3D.App/app.ico).

Run from the repo root:  python tools/make-icon.py      (needs Pillow; do not use -I)

Design: an orthographic globe from external/GlobeCore/src/GlobeCore.Earth/Assets/bluemarble-8192.jpg centred on
10 N, 30 E (Africa and the Middle East), fully lit (no night shade), with a pale-blue atmosphere rim, an anti-aliased edge
and a transparent background. Rendered once at 1024 px and downscaled with Lanczos for every size
(16, 24, 32, 48, 64, 128, 256). Pure Python (no numpy), so the render takes a little while.
"""
import math
import os

from PIL import Image

Image.MAX_IMAGE_PIXELS = None

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "external", "GlobeCore", "src", "GlobeCore.Earth", "Assets", "bluemarble-8192.jpg")
OUT_ICO = os.path.join(ROOT, "src", "EarthRelief3D.App", "app.ico")
SIZES = (16, 24, 32, 48, 64, 128, 256)

N = 1024
RADIUS = 500.0           # leaves a small transparent margin around the disc
CENTRE_LAT, CENTRE_LON = 10.0, 30.0
RIM = (130, 180, 255)


def smoothstep(e0, e1, x):
    t = min(1.0, max(0.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


def render():
    tex = Image.open(SRC).convert("RGB")
    tw, th = tex.size
    px = tex.load()

    def sample(lat, lon):
        fx = ((lon + 180.0) / 360.0 * tw - 0.5) % tw
        fy = min(max((90.0 - lat) / 180.0 * th - 0.5, 0.0), th - 1.001)
        x0, y0 = int(fx), int(fy)
        x1 = (x0 + 1) % tw
        y1 = y0 + 1
        ax, ay = fx - x0, fy - y0
        a, b, c, d = px[x0, y0], px[x1, y0], px[x0, y1], px[x1, y1]
        return [
            (a[i] * (1 - ax) + b[i] * ax) * (1 - ay) + (c[i] * (1 - ax) + d[i] * ax) * ay
            for i in range(3)
        ]

    p0 = math.radians(CENTRE_LAT)
    sin0, cos0 = math.sin(p0), math.cos(p0)
    img = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    out = img.load()
    c = (N - 1) / 2.0
    for j in range(N):
        y = (c - j) / RADIUS
        for i in range(N):
            x = (i - c) / RADIUS
            r2 = x * x + y * y
            r = math.sqrt(r2)
            cover = min(1.0, max(0.0, (1.0 - r) * RADIUS + 0.5))  # 1 px anti-aliased rim
            if cover <= 0.0:
                continue
            z = math.sqrt(max(0.0, 1.0 - r2))
            y2 = y * cos0 + z * sin0
            z2 = z * cos0 - y * sin0
            lat = math.degrees(math.asin(max(-1.0, min(1.0, y2))))
            lon = CENTRE_LON + math.degrees(math.atan2(x, z2))
            if lon > 180.0:
                lon -= 360.0
            elif lon < -180.0:
                lon += 360.0
            rgb = sample(lat, lon)
            rim = smoothstep(0.90, 1.0, r) * 0.6   # pale-blue atmosphere rim keeps the edge readable when small
            rgb = [rgb[k] * (1 - rim) + RIM[k] * rim for k in range(3)]
            out[i, j] = (int(rgb[0] + 0.5), int(rgb[1] + 0.5), int(rgb[2] + 0.5), int(cover * 255 + 0.5))
    return img


def main():
    master = render()
    master.save(OUT_ICO, format="ICO", sizes=[(s, s) for s in SIZES])
    print("done")


if __name__ == "__main__":
    main()
