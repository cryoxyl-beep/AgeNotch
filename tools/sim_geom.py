import math
from PIL import Image, ImageDraw

width = 680
height = 148
flareRadius = 28
bottomRadius = 32

im = Image.new('RGBA', (width, height), (0, 0, 0, 0))
draw = ImageDraw.Draw(im)

# Let's trace the boundary points:
points = [(0, 0)]

# 1. Top-Left flare: arc from (0, 0) to (flareRadius, flareRadius), center (0, flareRadius)
# angle from -90 to 0 (in standard math with y down: angle from -pi/2 to 0)
for a in range(-90, 1):
    rad = math.radians(a)
    x = 0 + flareRadius * math.cos(rad)
    y = flareRadius + flareRadius * math.sin(rad)
    points.append((x, y))

# 2. Left vertical wall to (flareRadius, height - bottomRadius)
points.append((flareRadius, height - bottomRadius))

# 3. Bottom-Left corner: center (flareRadius + bottomRadius, height - bottomRadius)
# arc from 180 to 90
for a in range(180, 89, -1):
    rad = math.radians(a)
    x = flareRadius + bottomRadius + bottomRadius * math.cos(rad)
    y = height - bottomRadius + bottomRadius * math.sin(rad)
    points.append((x, y))

# 4. Bottom edge to (width - flareRadius - bottomRadius, height)
points.append((width - flareRadius - bottomRadius, height))

# 5. Bottom-Right corner: center (width - flareRadius - bottomRadius, height - bottomRadius)
# arc from 90 to 0
for a in range(90, -1, -1):
    rad = math.radians(a)
    x = width - flareRadius - bottomRadius + bottomRadius * math.cos(rad)
    y = height - bottomRadius + bottomRadius * math.sin(rad)
    points.append((x, y))

# 6. Right vertical wall to (width - flareRadius, flareRadius)
points.append((width - flareRadius, flareRadius))

# 7. Top-Right flare: center (width, flareRadius), arc from 180 to 270 (or in math: -180 to -90)
for a in range(180, 271):
    rad = math.radians(a)
    x = width + flareRadius * math.cos(rad)
    y = flareRadius + flareRadius * math.sin(rad)
    points.append((x, y))

# 8. Back to (0, 0)
points.append((0, 0))

draw.polygon(points, fill=(0, 0, 0, 255))

# Draw onto a turquoise background (like the reference) to check the silhouette
bg = Image.new('RGB', (width + 60, height + 40), (40, 165, 157))
bg.paste(im, (30, 0), im)
bg.save(r'scratch\simulated_silhouette.png')
print("Saved simulated_silhouette.png")
