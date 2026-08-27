#!/usr/bin/env python3
"""Draw the Turkish drone sprites.

    ./tcd-drone-sprites.py temperat.pal out/

Writes 16 facings for each of the three drones as 8 bit indexed PNGs, ready for
`./utility.sh ra --png-to-shp`. The index in each pixel is a palette entry, so the
game's remap of entries 80-95 to the player's colour applies to the fuselage
stripe without anything further being done to it.

Extract the palette first, once:

    ./utility.sh ra --extract temperat.pal

The frames are generated rather than painted so that a change to a wingspan or a
shading band is one number here rather than forty-eight edits by hand. The art is
deliberately plain: clean silhouettes in the game's own palette, not an attempt
to imitate Westwood's dithering.
"""

import os
import sys
from collections import Counter
from PIL import Image, ImageDraw

SUPERSAMPLE = 8
CANVAS = 56          # matches the engine's own aircraft sprites
FACINGS = 16

# Aircraft are seen at a tilt rather than straight down, so the sprite is
# squashed along the screen's vertical axis after rotating.
TILT = 0.88


def load_palette(path):
    data = open(path, 'rb').read()
    if len(data) != 768:
        raise SystemExit(f'{path} is {len(data)} bytes; a palette is 768')
    return [(data[i * 3] * 255 // 63, data[i * 3 + 1] * 255 // 63, data[i * 3 + 2] * 255 // 63)
            for i in range(256)]


def nearest(pal, rgb):
    # Entry 0 is transparent, so never hand it back as a colour.
    return min(range(1, 256), key=lambda i: sum((a - b) ** 2 for a, b in zip(pal[i], rgb)))


def build(pal, kind):
    remap = [81, 84, 88]                       # the player-colour ramp
    grey = [nearest(pal, c) for c in ((176, 176, 176), (146, 146, 146),
                                      (117, 117, 117), (88, 88, 88), (60, 60, 60))]
    glass = nearest(pal, (125, 134, 174))
    red = nearest(pal, (190, 40, 40))
    white = nearest(pal, (236, 236, 236))
    warn = nearest(pal, (178, 80, 64))

    if kind == 'recon':
        span, body, wing, fw = 21, 18, 4.0, 2.6
    elif kind == 'strike':
        span, body, wing, fw = 26, 23, 4.8, 3.0
    elif kind == 'kamikaze':
        span, body, wing, fw = 17, 14, 3.4, 2.4
    else:
        raise SystemExit(f'unknown drone `{kind}`')

    s = SUPERSAMPLE
    c = CANVAS * s / 2
    im = Image.new('RGBA', (CANVAS * s, CANVAS * s), (0, 0, 0, 0))
    g = ImageDraw.Draw(im)

    def box(x0, y0, x1, y1, i):
        g.rectangle([c + x0 * s, c + y0 * s, c + x1 * s, c + y1 * s], fill=pal[i])

    # Wing, in four bands. One flat band reads as a wireframe at this size.
    wy = -1.0
    box(-span / 2, wy, span / 2, wy + wing * 0.30, grey[0])
    box(-span / 2, wy + wing * 0.30, span / 2, wy + wing * 0.62, grey[1])
    box(-span / 2, wy + wing * 0.62, span / 2, wy + wing * 0.86, grey[2])
    box(-span / 2, wy + wing * 0.86, span / 2, wy + wing, grey[3])
    for side in (-1, 1):
        box(side * span / 2 - 1.0, wy, side * span / 2, wy + wing, grey[3])

    # Fuselage, lit from the left.
    box(-fw, -body / 2 + 1.6, fw, body / 2, grey[1])
    box(-fw, -body / 2 + 1.6, -fw + 1.2, body / 2, grey[0])
    box(fw - 1.2, -body / 2 + 1.6, fw, body / 2, grey[3])
    g.polygon([(c - fw * s, c + (-body / 2 + 2.2) * s), (c, c - body / 2 * s),
               (c + fw * s, c + (-body / 2 + 2.2) * s)], fill=pal[grey[1]])

    # Sensor dome. The one detail that survives at twenty-four pixels.
    g.ellipse([c - 1.8 * s, c - (body / 2 - 1.4) * s,
               c + 1.8 * s, c - (body / 2 - 4.6) * s], fill=pal[glass])

    # A flat tailplane. A V-tail turns to noise once it is this small.
    box(-span * 0.22, body / 2 - 1.4, span * 0.22, body / 2 + 0.6, grey[1])
    box(-span * 0.22, body / 2 + 0.0, span * 0.22, body / 2 + 0.6, grey[3])

    # Team stripe, drawn in the remap range so it takes the player's colour.
    box(-fw, -1.4, fw, 2.4, remap[1])
    box(-fw, -1.4, -fw + 1.2, 2.4, remap[0])
    box(fw - 1.2, -1.4, fw, 2.4, remap[2])

    # Roundel on the port wing. Four pixels will not hold a crescent, so it is a
    # red marking with a white bite taken out of it.
    mx = -span * 0.30
    box(mx - 1.6, wy + 0.5, mx + 1.6, wy + wing - 0.6, red)
    box(mx - 0.2, wy + 1.2, mx + 0.9, wy + wing - 1.4, white)
    box(mx + 0.4, wy + 1.2, mx + 0.9, wy + wing - 1.4, red)

    if kind == 'strike':
        for side in (-1, 1):
            box(side * span * 0.36 - 1.1, wy + wing, side * span * 0.36 + 1.1, wy + wing + 4.8, grey[2])
            box(side * span * 0.36 - 1.1, wy + wing + 3.9, side * span * 0.36 + 1.1, wy + wing + 4.8, grey[4])

    if kind == 'kamikaze':
        box(-fw + 0.5, -body / 2, fw - 0.5, -body / 2 + 3.4, warn)

    return im


def facings(pal, kind):
    base = build(pal, kind)
    s, n = SUPERSAMPLE, CANVAS
    c = n * s / 2

    used = {}
    for y in range(0, n * s, 4):
        for x in range(0, n * s, 4):
            p = base.getpixel((x, y))
            if p[3]:
                used.setdefault(p[:3], None)
    lookup = {rgb: pal.index(rgb) for rgb in used}

    out = []
    for f in range(FACINGS):
        # Facings run anticlockwise on screen.
        rot = base.rotate(f * 360 / FACINGS, resample=Image.BICUBIC, center=(c, c))
        rot = rot.resize((n * s, int(n * s * TILT)), Image.BICUBIC)

        canvas = Image.new('RGBA', (n * s, n * s), (0, 0, 0, 0))
        canvas.alpha_composite(rot, (0, (n * s - rot.height) // 2))
        src = canvas.load()

        indices = bytearray(n * n)
        for y in range(n):
            for x in range(n):
                # The most common colour in the block, not the average of them:
                # averaging invents tones the palette does not have and the edges
                # turn to mush.
                votes = Counter()
                for yy in range(y * s, (y + 1) * s):
                    for xx in range(x * s, (x + 1) * s):
                        p = src[xx, yy]
                        if p[3] > 128:
                            rgb = min(lookup, key=lambda k: sum((a - b) ** 2 for a, b in zip(k, p[:3])))
                            votes[lookup[rgb]] += 1

                if votes and sum(votes.values()) >= s * s * 0.34:
                    indices[y * n + x] = votes.most_common(1)[0][0]

        frame = Image.frombytes('P', (n, n), bytes(indices))
        flat = []
        for rgb in pal:
            flat += list(rgb)
        frame.putpalette(flat)
        out.append(frame)

    return out


def main():
    if len(sys.argv) != 3:
        raise SystemExit(__doc__)

    pal = load_palette(sys.argv[1])
    out_dir = sys.argv[2]
    os.makedirs(out_dir, exist_ok=True)

    for kind in ('recon', 'strike', 'kamikaze'):
        for i, frame in enumerate(facings(pal, kind)):
            frame.save(os.path.join(out_dir, f'{kind}-{i:04d}.png'), transparency=0)
        print(f'{kind}: {FACINGS} frames')


if __name__ == '__main__':
    main()
