"""
Procedural Tarkam Arena decor built with decor_kit (Blender MCP). Each builder models one prop
around the origin, Z up, roughly at real-world scale (process_prop.py normalises the height later).

    exec(open(r"<repo>/tools/blender/decor_kit.py").read())
    exec(open(r"<repo>/tools/blender/decor_builders.py").read())
    build("fire-truck")         # or build_all()
"""
import math
import random

PI = math.pi
WOOD, DARKWOOD, TEAK = 0x8C6A3A, 0x4A2E1A, 0x6B3F22
GOLD, RED, WHITE, BLACK = 0xD4AF37, 0xC8201E, 0xF3EEE1, 0x161616
STONE, MOSS = 0x6E6A60, 0x4A6A32


def traffic_cone():
    box((0, 0, 0.02), (0.5, 0.5, 0.04), mat(0x1A1A1A, 0.8))
    cyl((0, 0, 0.38), 0.2, 0.68, mat(0xF2620F, 0.5), r2=0.04, verts=20)
    cyl((0, 0, 0.3), 0.166, 0.1, mat(0xF3F3F3, 0.4), r2=0.155, verts=20)
    cyl((0, 0, 0.5), 0.118, 0.08, mat(0xF3F3F3, 0.4), r2=0.108, verts=20)


def fire_truck():
    R, Y, G = mat(RED, 0.35, 0.2), mat(0xF6C21B, 0.5), mat(0x1A2A3A, 0.1, 0.5)
    box((1.6, 0, 1.15), (1.6, 2.3, 1.7), R)            # cab
    box((2.35, 0, 1.45), (0.08, 2.0, 0.8), G)          # windscreen
    box((-1.4, 0, 1.2), (4.4, 2.4, 1.6), R)            # body
    box((-1.4, 1.21, 0.85), (4.4, 0.03, 0.18), Y)      # stripes
    box((-1.4, -1.21, 0.85), (4.4, 0.03, 0.18), Y)
    for x in (-2.6, -1.4, -0.2):
        box((x, 1.22, 1.35), (0.9, 0.04, 0.8), mat(0xA01818, 0.4, 0.3))  # lockers
    for side in (1, -1):
        cyl((-2.9, side * 1.0, 2.25), 0.25, 0.3, mat(0x888888, 0.3, 0.8), (PI / 2, 0, 0))  # hose reels
    for y in (0.5, -0.5):
        box((-1.3, y, 2.3), (4.6, 0.08, 0.08), mat(0xC0C0C0, 0.3, 0.9))  # ladder rails
    for i in range(12):
        box((-3.4 + i * 0.38, 0, 2.3), (0.05, 1.0, 0.05), mat(0xC0C0C0, 0.3, 0.9))
    box((1.6, 0, 2.08), (0.3, 1.6, 0.12), mat(0x2060FF, 0.2, 0, 4))  # light bar
    box((1.6, 0.5, 2.08), (0.32, 0.4, 0.13), mat(0xFF2020, 0.2, 0, 4))
    for x in (1.6, -0.6, -2.6):
        for y in (1.15, -1.15):
            wheel(x, y, 0.45, 0.45, 0.35)


def water_cannon():
    B, D = mat(0x2E3A4A, 0.5, 0.3), mat(0x1A2028, 0.6)
    box((0, 0, 1.2), (5.2, 2.4, 1.8), B)
    box((2.0, 0, 1.5), (1.2, 2.3, 1.3), D)
    box((2.62, 0, 1.6), (0.05, 2.0, 0.6), mat(0x203040, 0.1, 0.5))
    cyl((-0.5, 0, 2.3), 0.5, 0.4, D)
    cyl((0.4, 0, 2.45), 0.09, 1.6, mat(0x9A9A9A, 0.3, 0.8), (0, PI / 2 - 0.25, 0))
    box((0, 1.21, 1.0), (5.0, 0.02, 0.25), mat(0xF6C21B, 0.5))
    box((-0.5, 0, 2.65), (0.6, 0.4, 0.1), mat(0x2060FF, 0.2, 0, 4))
    for x in (1.8, -0.4, -1.9):
        for y in (1.15, -1.15):
            wheel(x, y, 0.45, 0.45, 0.35)


def jeep():
    B = mat(0x2E5A3A, 0.45, 0.2)
    box((0, 0, 0.95), (3.6, 1.7, 0.9), B)
    box((-0.3, 0, 1.75), (2.4, 1.6, 0.8), B)
    box((0.92, 0, 1.75), (0.04, 1.4, 0.6), mat(0x1A2A3A, 0.1, 0.5))
    for y in (0.6, -0.6):
        box((-0.3, y * 1.35, 1.8), (1.8, 0.03, 0.5), mat(0x1A2A3A, 0.1, 0.5))
    box((-0.3, 0, 2.2), (2.2, 1.5, 0.06), mat(0x3A3A3A, 0.7))  # roof rack
    for i in range(3):
        box((-0.9 + i * 0.6, 0.2, 2.35), (0.5, 0.5, 0.3), mat(0x8C6A3A, 0.8))  # luggage
    box((1.82, 0, 0.9), (0.06, 1.4, 0.4), mat(0x9A9A9A, 0.3, 0.8))
    for y in (0.55, -0.55):
        sph((1.84, y, 1.1), (0.04, 0.13, 0.13), mat(0xFFF2C0, 0.2, 0, 3))
    cyl((-1.85, 0, 1.0), 0.38, 0.22, mat(BLACK, 0.9), (0, PI / 2, 0))
    for x in (1.2, -1.2):
        for y in (0.85, -0.85):
            wheel(x, y, 0.42, 0.42, 0.3)


def ontel():
    F, S = mat(0x1A1A1A, 0.4, 0.6), mat(0xB0B0B0, 0.3, 0.9)
    for x in (0.55, -0.55):
        tor((x, 0, 0.36), 0.34, 0.025, F, (PI / 2, 0, 0))
        tor((x, 0, 0.36), 0.2, 0.006, S, (PI / 2, 0, 0))
    cyl((0.05, 0, 0.62), 0.02, 0.9, F, (0, PI / 2 - 0.15, 0))
    cyl((-0.25, 0, 0.5), 0.02, 0.5, F, (0, 0.5, 0))
    cyl((0.35, 0, 0.55), 0.02, 0.55, F, (0, -0.4, 0))
    cyl((-0.38, 0, 0.55), 0.02, 0.45, F, (0, 0.25, 0))
    box((-0.38, 0, 0.82), (0.24, 0.12, 0.05), mat(0x5A3A1E, 0.7))
    cyl((0.48, 0, 0.92), 0.015, 0.5, S, (PI / 2, 0, 0))
    box((0.72, 0, 0.78), (0.26, 0.32, 0.18), mat(0xA0783A, 0.9))  # basket
    sph((0.62, 0, 0.88), 0.05, mat(0xFFF2C0, 0.2, 0, 3))


def colonial_lamp():
    I = mat(0x1A1A1A, 0.4, 0.7)
    cyl((0, 0, 0.12), 0.22, 0.24, I, r2=0.15)
    cyl((0, 0, 1.6), 0.06, 2.9, I, r2=0.04)
    for z in (0.5, 1.2):
        tor((0, 0, z), 0.08, 0.02, I)
    for a in (0, PI):
        cyl((0.28 * math.cos(a), 0.28 * math.sin(a), 3.0), 0.02, 0.6, I, (0, PI / 2, a))
        cyl((0.55 * math.cos(a), 0, 2.92), 0.12, 0.3, mat(0xFFE2A0, 0.2, 0, 5), r2=0.09)
        cone((0.55 * math.cos(a), 0, 3.12), 0.16, 0.14, I)
    sph((0, 0, 3.15), 0.07, mat(GOLD, 0.3, 0.8))


def gubuk():
    B, T = mat(0xA88A50, 0.8), mat(0x9A7A3A, 0.95)
    for x in (-1, 1):
        for y in (-0.8, 0.8):
            cyl((x, y, 0.9), 0.07, 1.8, B)
    box((0, 0, 0.8), (2.3, 1.9, 0.08), mat(0x8C6A3A, 0.85))
    for i in range(9):
        cyl((-1.05 + i * 0.26, 0, 0.86), 0.04, 1.9, B, (PI / 2, 0, 0))
    for side in (1, -1):
        box((0, side * 0.6, 2.25), (2.8, 1.5, 0.08), T, (side * 0.75, 0, 0))
    box((0, 0, 2.7), (2.9, 0.15, 0.1), T)
    box((-1.12, 0, 1.3), (0.05, 1.7, 0.9), mat(0xBFA060, 0.9))  # woven wall


def scarecrow():
    W = mat(0x8C6A3A, 0.9)
    cyl((0, 0, 0.9), 0.04, 1.8, W)
    cyl((0, 0, 1.35), 0.03, 1.3, W, (PI / 2, 0, 0))
    box((0, 0, 1.15), (0.3, 0.7, 0.55), mat(0x2E5C8A, 0.9))
    sph((0, 0, 1.62), 0.16, mat(0xE8D8A0, 0.9))
    cone((0, 0, 1.82), 0.42, 0.22, mat(0xC9A85A, 0.9))   # caping
    for y in (0.62, -0.62):
        cone((0, y, 1.3), 0.08, 0.25, mat(0xD8C070, 0.9), (PI, 0, 0))  # straw hands
    box((0.12, 0, 0.8), (0.05, 0.35, 0.3), mat(0x7A2A1A, 0.9))


def hornbill_totem():
    W, D = mat(0x7A5A3A, 0.8), mat(0x2A1A10, 0.8)
    cyl((0, 0, 1.2), 0.18, 2.4, W, verts=10)
    for z in (0.4, 0.9, 1.4, 1.9):
        tor((0, 0, z), 0.19, 0.035, D, seg=10)
        sph((0.17, 0, z + 0.2), (0.04, 0.12, 0.08), mat(WHITE, 0.6))
    sph((0, 0, 2.55), (0.3, 0.2, 0.22), mat(BLACK, 0.6))           # hornbill body
    sph((0.28, 0, 2.7), 0.12, mat(BLACK, 0.6))
    cone((0.55, 0, 2.72), 0.08, 0.45, mat(0xF2A020, 0.4), (0, PI / 2 + 0.2, 0))
    box((0.4, 0, 2.86), (0.25, 0.06, 0.1), mat(0xE0322B, 0.5))      # casque
    for y in (0.22, -0.22):
        box((-0.05, y, 2.55), (0.45, 0.04, 0.3), mat(BLACK, 0.6), (0, 0, 0.2 * (1 if y > 0 else -1)))
    box((-0.35, 0, 2.5), (0.4, 0.12, 0.05), mat(WHITE, 0.6))


def sesajen():
    box((0, 0, 0.04), (0.6, 0.6, 0.08), mat(0x9A7A3A, 0.9))  # woven tray
    for i, c in enumerate((0xE0322B, 0xF6C21B, WHITE, 0xFF7AB0, 0x7A3AC0)):
        sph((0.18 * math.cos(i * 1.25), 0.18 * math.sin(i * 1.25), 0.11), 0.06, mat(c, 0.6))
    sph((0, 0, 0.14), (0.12, 0.12, 0.1), mat(0xF3E8C0, 0.8))  # rice cone
    cone((0, 0, 0.26), 0.1, 0.2, mat(0xF0E0A0, 0.8))
    for i in range(3):
        cyl((0.2, -0.2 + i * 0.05, 0.3), 0.006, 0.45, mat(0x7A2A1A, 0.8))
        sph((0.2, -0.2 + i * 0.05, 0.53), 0.012, mat(0xFF6020, 0.3, 0, 6))
    cyl((-0.18, 0.18, 0.12), 0.06, 0.14, mat(0xC9A060, 0.8))  # coconut water


def canang():
    box((0, 0, 0.03), (0.3, 0.3, 0.06), mat(0x8AB040, 0.8))
    for i, c in enumerate((0xE0322B, WHITE, 0xF6C21B, 0x3A60D0)):
        a = i * PI / 2 + PI / 4
        sph((0.08 * math.cos(a), 0.08 * math.sin(a), 0.08), (0.06, 0.06, 0.03), mat(c, 0.6))
    cyl((0, 0, 0.2), 0.005, 0.3, mat(0x7A2A1A, 0.8))
    sph((0, 0, 0.35), 0.01, mat(0xFF6020, 0.3, 0, 6))


def bale_kulkul():
    Bk, S, T = mat(0xB0502A, 0.85), mat(0x9A9488, 0.9), mat(0x3A2A1A, 0.95)
    box((0, 0, 0.9), (1.8, 1.8, 1.8), Bk)
    for z in (0.2, 1.7):
        box((0, 0, z), (1.95, 1.95, 0.15), S)
    for x in (-0.7, 0.7):
        for y in (-0.7, 0.7):
            cyl((x, y, 2.6), 0.07, 1.6, mat(TEAK, 0.7))
    box((0, 0, 1.85), (1.7, 1.7, 0.1), mat(TEAK, 0.7))
    cone((0, 0, 3.9), 1.45, 1.1, T, verts=4, rot=(0, 0, PI / 4))
    cyl((0, 0, 2.7), 0.16, 1.0, mat(0x8C5A2B, 0.7))   # the kulkul drum
    box((0, 0, 1.0), (0.4, 0.02, 0.9), mat(0x2A1A10, 0.8))
    box((0, 0.91, 1.0), (0.4, 0.02, 0.9), mat(0x2A1A10, 0.8))
    box((0, 0.92, 1.4), (1.2, 0.04, 0.12), mat(GOLD, 0.4, 0.5))


def sasando():
    cyl((0, 0, 0.5), 0.06, 0.9, mat(0xC9A060, 0.6))
    for i in range(10):
        a = i * PI / 5
        cyl((0.06 * math.cos(a), 0.06 * math.sin(a), 0.5), 0.003, 0.88, mat(0xD0D0D0, 0.3, 0.9))
    for i in range(9):
        a = -PI / 2 + i * PI / 8
        box((0.22 * math.cos(a), 0.22 * math.sin(a), 0.55), (0.05, 0.22, 0.8), mat(0xC8A048, 0.85), (0, 0, a))
    box((0, 0, 0.05), (0.5, 0.5, 0.1), mat(DARKWOOD, 0.7))


def mic_podium():
    W = mat(TEAK, 0.45)
    box((0, 0, 0.55), (0.6, 0.8, 1.1), W, None)
    box((0.05, 0, 1.14), (0.75, 0.95, 0.06), W, (0, -0.25, 0))
    box((0.31, 0, 0.6), (0.01, 0.5, 0.35), mat(GOLD, 0.3, 0.8))
    sph((0.31, 0, 0.62), (0.01, 0.12, 0.12), mat(RED, 0.4))
    for y in (-0.15, 0, 0.15):
        cyl((0.15, y, 1.35), 0.008, 0.35, mat(0x222222, 0.4, 0.6), (0, -0.6, 0))
        sph((0.25, y, 1.5), 0.03, mat(0x333333, 0.5, 0.5))


def candle_lantern():
    S = mat(STONE, 0.95)
    box((0, 0, 0.08), (0.35, 0.35, 0.16), S)
    cyl((0, 0, 0.3), 0.07, 0.3, S, verts=8)
    box((0, 0, 0.55), (0.3, 0.3, 0.22), S)
    box((0, 0, 0.55), (0.32, 0.18, 0.12), mat(0xFFB040, 0.3, 0, 6))
    box((0, 0, 0.55), (0.18, 0.32, 0.12), mat(0xFFB040, 0.3, 0, 6))
    cone((0, 0, 0.78), 0.3, 0.22, S, verts=4, rot=(0, 0, PI / 4))
    sph((0, 0, 0.92), 0.05, S)


def water_hose():
    tor((0, 0, 0.12), 0.35, 0.05, mat(0xE0322B, 0.5), seg=32)
    tor((0, 0, 0.22), 0.3, 0.05, mat(0xE0322B, 0.5), seg=32)
    tor((0, 0, 0.32), 0.25, 0.05, mat(0xE0322B, 0.5), seg=32)
    cyl((0.42, 0, 0.1), 0.05, 0.4, mat(0xC8A030, 0.3, 0.8), (0, PI / 2, 0))


def tire():
    for i in range(4):
        tor((random.uniform(-0.05, 0.05), random.uniform(-0.05, 0.05), 0.12 + i * 0.22), 0.32, 0.12, mat(0x141414, 0.9), seg=24)


def litter():
    random.seed(4)
    for i in range(6):
        x, y = random.uniform(-0.4, 0.4), random.uniform(-0.4, 0.4)
        c = random.choice((0x7ACCF0, 0x40A060, WHITE, 0xE0322B))
        cyl((x, y, 0.035), 0.035, 0.22, mat(c, 0.2, 0, 0, 0.8), (PI / 2, 0, random.uniform(0, PI)))
    for i in range(3):
        x, y = random.uniform(-0.4, 0.4), random.uniform(-0.4, 0.4)
        box((x, y, 0.005), (0.18, 0.12, 0.01), mat(random.choice((WHITE, 0xF6C21B, 0xC0C0C0)), 0.9), (0, 0, random.uniform(0, PI)))
    cyl((0.1, 0.25, 0.05), 0.04, 0.1, mat(WHITE, 0.6), r2=0.05)


def seashell():
    random.seed(9)
    for i in range(5):
        x, y = random.uniform(-0.3, 0.3), random.uniform(-0.3, 0.3)
        c = random.choice((0xF3E0D0, 0xE8B090, 0xF0D0A0, 0xD89070))
        cone((x, y, 0.03), 0.05, 0.12, mat(c, 0.5), (0, PI / 2 - 0.2, random.uniform(0, PI * 2)), verts=8)
    for i in range(3):
        box((0.1, -0.1, 0.01), (0.2, 0.04, 0.015), mat(0xE8702A, 0.6), (0, 0, i * PI / 3 + 0.2))  # starfish
    sph((-0.15, 0.15, 0.03), (0.08, 0.06, 0.03), mat(0xF0E8E0, 0.4))


def garuda():
    G, Gd = mat(GOLD, 0.3, 0.8), mat(0xA8862A, 0.4, 0.7)
    sph((0, 0, 1.0), (0.22, 0.12, 0.38), G)                  # body
    sph((0, 0, 1.5), (0.13, 0.11, 0.14), G)                   # head
    cone((0, -0.16, 1.48), 0.05, 0.14, Gd, (PI / 2, 0, 0))    # beak
    box((0, 0, 1.65), (0.06, 0.05, 0.14), Gd)                 # crest
    for side in (1, -1):
        for i in range(8):
            a = 0.25 + i * 0.12
            box((side * (0.35 + i * 0.05), 0.02, 1.05 + i * 0.07), (0.07, 0.04, 0.55 - i * 0.03), G, (0, side * a, 0))
    for i in range(7):
        box(((i - 3) * 0.06, 0.02, 0.45), (0.06, 0.04, 0.4), G, (0, (i - 3) * 0.12, 0))  # tail
    box((0, -0.14, 0.98), (0.36, 0.04, 0.42), mat(BLACK, 0.4))      # shield
    box((-0.09, -0.165, 1.08), (0.17, 0.02, 0.2), mat(RED, 0.4))
    box((0.09, -0.165, 1.08), (0.17, 0.02, 0.2), mat(WHITE, 0.4))
    box((-0.09, -0.165, 0.88), (0.17, 0.02, 0.2), mat(WHITE, 0.4))
    box((0.09, -0.165, 0.88), (0.17, 0.02, 0.2), mat(RED, 0.4))
    sph((0, -0.18, 0.98), (0.06, 0.02, 0.06), mat(GOLD, 0.3, 0.8, 1.5))
    box((0, -0.05, 0.25), (0.7, 0.05, 0.1), mat(WHITE, 0.5), (0, 0, 0))  # ribbon


def judge_bench():
    W, D = mat(TEAK, 0.45), mat(DARKWOOD, 0.5)
    box((0, 0, 0.55), (1.1, 4.8, 1.1), W)
    box((0.02, 0, 1.12), (1.25, 5.0, 0.08), D)
    for y in (-1.6, 0, 1.6):
        box((-0.56, y, 0.55), (0.04, 1.4, 0.8), D)
        box((-0.6, y, 0.6), (0.02, 1.0, 0.5), mat(0x5A2A14, 0.5))
    box((-0.58, 0, 0.95), (0.04, 0.5, 0.2), mat(GOLD, 0.3, 0.8))
    for y in (-1.5, 0, 1.5):
        box((0.75, y, 1.0), (0.12, 0.7, 2.0), mat(0x5A1A1A, 0.6))   # high chair backs
        box((0.75, y, 2.05), (0.14, 0.75, 0.12), mat(GOLD, 0.3, 0.8))
    cyl((0.1, -0.5, 1.2), 0.05, 0.08, D)                          # gavel block
    cyl((0.1, -0.35, 1.25), 0.03, 0.2, D, (PI / 2, 0, 0))


def gallery_bench():
    W = mat(TEAK, 0.5)
    box((0, 0, 0.45), (0.45, 3.2, 0.06), W)
    box((0.22, 0, 0.8), (0.05, 3.2, 0.6), W)
    for y in (-1.5, 0, 1.5):
        box((0, y, 0.22), (0.45, 0.06, 0.45), W)
        box((0.22, y, 0.5), (0.05, 0.06, 1.0), W)
    box((0.25, 0, 1.12), (0.08, 3.25, 0.06), mat(DARKWOOD, 0.5))


def potted_palm():
    cyl((0, 0, 0.3), 0.32, 0.6, mat(0x8A4A2A, 0.8), r2=0.24)
    cyl((0, 0, 0.6), 0.3, 0.04, mat(0x3A2A1A, 0.95))
    for i in range(3):
        cyl((0.03 * i, 0, 0.95), 0.04, 0.8, mat(0x7A6A3A, 0.85))
    random.seed(2)
    for i in range(10):
        a = i * PI / 5
        box((0.4 * math.cos(a), 0.4 * math.sin(a), 1.35), (0.75, 0.16, 0.02), mat(random.choice((0x3E7A2E, 0x4A8A3A, 0x2E6A2A)), 0.6), (0, -0.55, a))


def frangipani():
    T = mat(0x7A6A5A, 0.8)
    cyl((0, 0, 0.9), 0.12, 1.8, T, r2=0.09)
    random.seed(5)
    for i in range(5):
        a = i * 2 * PI / 5
        cyl((0.35 * math.cos(a), 0.35 * math.sin(a), 2.0), 0.05, 0.9, T, (0.5 * math.sin(a), -0.5 * math.cos(a), 0))
        cx, cy = 0.7 * math.cos(a), 0.7 * math.sin(a)
        sph((cx, cy, 2.45), (0.4, 0.4, 0.22), mat(0x3E6A2E, 0.7))
        for k in range(5):
            sph((cx + random.uniform(-0.3, 0.3), cy + random.uniform(-0.3, 0.3), 2.6 + random.uniform(0, 0.08)), (0.08, 0.08, 0.03), mat(WHITE, 0.5))
            sph((cx + random.uniform(-0.3, 0.3), cy + random.uniform(-0.3, 0.3), 2.62), 0.025, mat(0xF6C21B, 0.5))


def _gable(length, base, z0, z1, m):
    """A triangular prism along X: the gable wall between the wall top z0 and the ridge z1."""
    import bmesh
    me = bpy.data.meshes.new("gable")
    bm = bmesh.new()
    vs = []
    for x in (-length / 2, length / 2):
        vs += [bm.verts.new((x, -base / 2, z0)), bm.verts.new((x, base / 2, z0)), bm.verts.new((x, 0, z1))]
    for f in ((0, 1, 2), (5, 4, 3), (0, 3, 4, 1), (1, 4, 5, 2), (2, 5, 3, 0)):
        bm.faces.new([vs[i] for i in f])
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new("gable", me)
    bpy.context.scene.collection.objects.link(o)
    o.data.materials.append(m)
    return o


def _kampung_house(wall, trim, roof_col, seed):
    """A kampung house facade 5 m wide: zinc roof, porch, shuttered windows, door, potted plants,
    a clothesline and an AC unit. Built front-facing (-Y)."""
    random.seed(seed)
    W, T, Rf = mat(wall, 0.85), mat(trim, 0.6), mat(roof_col, 0.45, 0.6)
    box((0, 0, 0.15), (5.2, 3.2, 0.3), mat(0x8A8478, 0.9))            # foundation
    box((0, 0, 1.9), (5.0, 3.0, 3.2), W)                               # walls
    box((0, -1.51, 0.45), (5.0, 0.03, 0.3), T)                         # skirting
    for side in (1, -1):
        box((0, side * 0.82, 3.88), (5.6, 1.95, 0.08), Rf, (-side * 0.45, 0, 0))   # gable roof
    box((0, 0, 4.28), (5.7, 0.14, 0.1), mat(0x8A8A8A, 0.4, 0.8))
    _gable(4.96, 3.0, 3.5, 4.22, W)                                     # gable ends
    box((0, -1.6, 0.31), (0.95, 0.06, 0.02), mat(0x5A3A24, 0.8))       # step
    box((0.6, -1.52, 1.35), (0.9, 0.05, 1.9), mat(0x6B3F22, 0.6))       # door
    box((0.6, -1.53, 2.38), (1.05, 0.04, 0.12), T)
    sph((0.95, -1.56, 1.35), 0.03, mat(GOLD, 0.3, 0.8))
    for x in (-1.6, 2.0):
        box((x, -1.52, 2.0), (0.9, 0.04, 0.9), mat(0x2A3A4A, 0.15, 0.4))  # window glass
        box((x, -1.54, 2.0), (1.0, 0.03, 0.07), T)
        box((x, -1.54, 2.0), (0.07, 0.03, 1.0), T)
        for sx in (-0.62, 0.62):
            box((x + sx, -1.55, 2.0), (0.32, 0.03, 1.0), T)                 # shutters
        box((x, -1.6, 1.5), (1.1, 0.2, 0.08), T)                            # sill
    box((0, -2.1, 2.9), (5.2, 1.2, 0.06), Rf, (-0.18, 0, 0))               # porch roof
    for x in (-2.4, 2.4):
        cyl((x, -2.6, 1.45), 0.06, 2.9, T)
    for x in (-2.0, -1.2, 1.5):
        cyl((x, -1.85, 0.5), 0.16, 0.36, mat(random.choice((0xB04A2A, 0x3A6AA0, 0xE0E0E0)), 0.7), r2=0.12)
        sph((x, -1.85, 0.85), (0.28, 0.28, 0.3), mat(random.choice((0x3E7A2E, 0x2E6A2A, 0x5A8A3A)), 0.8))
    box((-2.0, -1.6, 2.9), (0.6, 0.25, 0.4), mat(0xE8E8E8, 0.5))          # AC unit
    cyl((0.0, -2.55, 2.45), 0.008, 4.8, mat(0x9A9A9A, 0.4, 0.6), (0, PI / 2, 0))  # clothesline
    for i in range(5):
        box((-1.8 + i * 0.85, -2.55, 2.15), (0.45, 0.02, 0.55), mat(random.choice((0xE0322B, 0xF6C21B, 0x1F4FB0, WHITE, 0x2E8B57)), 0.9))
    box((-0.8, -1.53, 3.1), (1.4, 0.03, 0.35), mat(trim, 0.5))              # name board


def house_ambon_a(): _kampung_house(0x5FB8C8, WHITE, 0x9AA0A8, 1)
def house_ambon_b(): _kampung_house(0xF09AB8, 0x2E5C8A, 0xB04A2A, 2)
def house_ambon_c(): _kampung_house(0xF2D060, 0x2E8B57, 0x8A8A8A, 3)


BUILDERS = {
    "house-ambon-a": house_ambon_a, "house-ambon-b": house_ambon_b, "house-ambon-c": house_ambon_c,
    "traffic-cone": traffic_cone,
    "fire-truck": fire_truck, "water-cannon": water_cannon, "jeep": jeep, "ontel": ontel,
    "colonial-lamp": colonial_lamp, "gubuk": gubuk, "scarecrow": scarecrow, "hornbill-totem": hornbill_totem,
    "sesajen": sesajen, "canang": canang, "bale-kulkul": bale_kulkul, "sasando": sasando,
    "mic-podium": mic_podium, "candle-lantern": candle_lantern, "water-hose": water_hose, "tire": tire,
    "litter": litter, "seashell": seashell, "garuda": garuda, "judge-bench": judge_bench,
    "gallery-bench": gallery_bench, "potted-palm": potted_palm, "frangipani": frangipani,
}


def build(name):
    clear()
    BUILDERS[name]()
    return export(name)


def build_all():
    for name in BUILDERS:
        build(name)
