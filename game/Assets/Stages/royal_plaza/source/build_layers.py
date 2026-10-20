"""Cuts reference/royal_plaza_key_art_v2.png into the stage's floor texture and cards (ADR-0030 §3.3) and prints
where each card stands.

Run from the repo root: python game/Assets/Stages/royal_plaza/source/build_layers.py
Needs Pillow and numpy only. Hidden parts behind each layer are filled with a smooth push-pull fill: good enough for
the strips the camera's travel reveals; replace them by inpainting with the masks this writes to source/.

Fit: the key art was made from the camera guide (Retexture), so it is the guide's frame: the stage_projection
defaults, horizon on the camera's eye level. Its painted floor is projected onto the 3D floor; the balustrade base
(row BASE_ROW) stands on the floor's far edge, which fixes the Middle card's distance. Near layers slide furthest as
the camera travels, so the floor and Middle are widened by mirroring the painting's outer edges (EXTEND_*): a
placeholder until the key art is panned wider.
"""
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

STAGE = Path(__file__).resolve().parent.parent
KEY_ART = STAGE / "reference" / "royal_plaza_key_art_v2.png"

# The round-start camera (stage_projection defaults; StageProjectionTests keep them in step with MatchCamera).
CAMERA_Z = 7.346
EYE = 1.2377
ART_BOTTOM = -0.3909
ART_HEIGHT = 3.2571

BASE_ROW = 862  # the balustrade base, where the floor ends
FLOOR_TOP_ROW = 840  # the floor texture starts a little above it, so the projection never runs out
MIDDLE_ROWS = (0, 880)
BACK_DISTANCE = 52.0  # from the camera, in metres (z = -45)
SKY_DISTANCE = 157.0  # z = -150
SKY_LAST_ROW = 560  # the sky ends above the far hills

# Pixels mirrored onto each side: the floor's near edge needs plane x out to ±10.7 m at a wall, Middle ±5.4 m.
EXTEND_FLOOR = 2500
EXTEND_MIDDLE = 450

# Out to the image edges: the mirrored widening continues whatever stands there.
LEFT_COLONNADE = [(0, 15), (760, 15), (800, 60), (990, 240), (990, 300), (905, 310), (905, 880), (0, 880)]
RIGHT_COLONNADE = [(2630, 15), (3616, 15), (3616, 895), (2735, 895), (2735, 300), (2630, 290)]
BALUSTRADE = [(0, 750), (3616, 750), (3616, 880), (0, 880)]


def mask(size, *polygons):
    image = Image.new("L", size, 0)
    draw = ImageDraw.Draw(image)
    for polygon in polygons:
        draw.polygon(polygon, fill=255)
    return np.asarray(image) > 127


def box_mean(values, radius):
    """Mean over a (2r+1)² box, edges clamped."""
    padded = np.pad(values, radius, mode="edge")
    summed = padded.cumsum(0).cumsum(1)
    summed = np.pad(summed, ((1, 0), (1, 0)))
    size = (2 * radius) + 1
    h, w = values.shape
    window = summed[size:size + h, size:size + w] - summed[:h, size:size + w] - summed[size:size + h, :w] + summed[:h, :w]
    return window / (size * size)


def sky_mask(rgb):
    """Smooth, light, blue-to-turquoise areas above the hills: the sky and aurora, not the carved stone."""
    luminance = rgb.mean(2)
    detail = np.sqrt(np.maximum(box_mean(luminance ** 2, 4) - (box_mean(luminance, 4) ** 2), 0))
    blue = (rgb[..., 2] > rgb[..., 0] + 25) & (luminance > 120)
    sky = blue & (detail < 6)
    sky[SKY_LAST_ROW:] = False
    # Close small holes and drop specks: grow, shrink, shrink, grow.
    def grow(m, r):
        return box_mean(m.astype(np.float32), r) > 0.01

    def shrink(m, r):
        return box_mean(m.astype(np.float32), r) > 0.99
    return grow(shrink(shrink(grow(sky, 3), 3), 3), 3)


def upsample(rgb, shape):
    channels = [Image.fromarray(np.ascontiguousarray(rgb[..., i]), "F").resize((shape[1], shape[0]), Image.BILINEAR)
                for i in range(3)]
    return np.dstack([np.asarray(c) for c in channels])


def push_pull_fill(rgb, known):
    """Fills unknown pixels with a smooth blend of the known ones around them."""
    # Each level holds the known pixels' mean colour and how much of the pixel is known (0..1).
    levels = [(rgb, known.astype(np.float32))]
    while max(levels[-1][1].shape) > 1:
        colour, weight = levels[-1]
        h, w = weight.shape
        h2, w2 = (h + 1) // 2, (w + 1) // 2
        pc = np.zeros((h2 * 2, w2 * 2, 3), np.float32)
        pw = np.zeros((h2 * 2, w2 * 2), np.float32)
        pc[:h, :w], pw[:h, :w] = colour * weight[..., None], weight
        summed = pc.reshape(h2, 2, w2, 2, 3).sum((1, 3))
        total = pw.reshape(h2, 2, w2, 2).sum((1, 3))
        levels.append((summed / np.maximum(total, 1e-6)[..., None], np.minimum(total, 1.0)))
    filled = levels[-1][0]
    for colour, weight in reversed(levels[:-1]):
        up = upsample(filled, weight.shape)
        filled = (colour * weight[..., None]) + (up * (1 - weight[..., None]))
    out = rgb.copy()
    out[~known] = filled[~known]
    return out


def mirror_extend(image, pixels):
    return np.concatenate([image[:, pixels:0:-1], image, image[:, -2:-pixels - 2:-1]], axis=1)


def save(path, rgb, alpha=None):
    channels = [rgb.clip(0, 255)] + ([alpha.astype(np.float32) * 255] if alpha is not None else [])
    image = np.dstack(channels).astype(np.uint8)
    Image.fromarray(image, "RGBA" if alpha is not None else "RGB").save(path, optimize=True)


def card(name, distance, rows, width_px):
    """World centre and size of a card cut to rows, scaled so it looks like the painting at round start."""
    pixels_per_metre = HEIGHT_PX / ART_HEIGHT
    scale = distance / CAMERA_Z
    top = EYE + ((ART_BOTTOM + ((HEIGHT_PX - rows[0]) / pixels_per_metre) - EYE) * scale)
    bottom = EYE + ((ART_BOTTOM + ((HEIGHT_PX - rows[1]) / pixels_per_metre) - EYE) * scale)
    print(f"{name}: z = {CAMERA_Z - distance:.3f}, centre y = {(top + bottom) / 2:.3f}, "
          f"size = ({width_px / pixels_per_metre * scale:.3f}, {top - bottom:.3f})")


art = np.asarray(Image.open(KEY_ART).convert("RGB")).astype(np.float32)
HEIGHT_PX, WIDTH_PX = art.shape[:2]
size = (WIDTH_PX, HEIGHT_PX)
rows = np.arange(HEIGHT_PX)[:, None] * np.ones((1, WIDTH_PX), bool)

middle = mask(size, LEFT_COLONNADE, RIGHT_COLONNADE, BALUSTRADE)
sky = sky_mask(art) & ~middle
back = ~middle & ~sky & (rows < MIDDLE_ROWS[1])

source = STAGE / "source"
Image.fromarray((middle * 255).astype(np.uint8)).save(source / "mask_middle.png")
Image.fromarray((sky * 255).astype(np.uint8)).save(source / "mask_sky.png")

floor = mirror_extend(art[FLOOR_TOP_ROW:], EXTEND_FLOOR)
save(STAGE / "floor" / "floor_key_art.png", floor)
middle_rows = slice(*MIDDLE_ROWS)
save(STAGE / "middle" / "middle_colonnades.png", mirror_extend(art[middle_rows], EXTEND_MIDDLE),
     mirror_extend(middle[middle_rows], EXTEND_MIDDLE))
save(STAGE / "back" / "back_palace.png", push_pull_fill(art, back | middle)[middle_rows], ~sky[middle_rows])
save(STAGE / "sky" / "sky_aurora.png", push_pull_fill(art, sky)[:SKY_LAST_ROW + 200])

pixels_per_metre = HEIGHT_PX / ART_HEIGHT
base_on_plane = ART_BOTTOM + ((HEIGHT_PX - BASE_ROW) / pixels_per_metre)
middle_distance = CAMERA_Z / (1 - (base_on_plane / EYE))
card("Middle", middle_distance, MIDDLE_ROWS, WIDTH_PX + (2 * EXTEND_MIDDLE))
card("Back", BACK_DISTANCE, MIDDLE_ROWS, WIDTH_PX)
card("Sky", SKY_DISTANCE, (0, SKY_LAST_ROW + 200), WIDTH_PX)
floor_height = (HEIGHT_PX - FLOOR_TOP_ROW) / pixels_per_metre
print(f"floor: far edge z = {CAMERA_Z - middle_distance:.3f}; projection art_height = {floor_height:.4f} "
      f"(art_bottom stays {ART_BOTTOM})")
