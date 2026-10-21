import math
from PIL import Image, ImageDraw, ImageFont

w = 660
h = 140
flare = 26
corner = 30

# Background canvas
canvas = Image.new('RGB', (w + 80, h + 40), (40, 165, 157))

# Build notch mask
mask = Image.new('L', (w, h), 0)
draw = ImageDraw.Draw(mask)

pts = [(0, 0)]
# top-left flare
for a in range(-90, 1):
    rad = math.radians(a)
    pts.append((0 + flare * math.cos(rad), flare + flare * math.sin(rad)))
pts.append((flare, h - corner))
# bottom-left corner
for a in range(180, 89, -1):
    rad = math.radians(a)
    pts.append((flare + corner + corner * math.cos(rad), h - corner + corner * math.sin(rad)))
pts.append((w - flare - corner, h))
# bottom-right corner
for a in range(90, -1, -1):
    rad = math.radians(a)
    pts.append((w - flare - corner + corner * math.cos(rad), h - corner + corner * math.sin(rad)))
pts.append((w - flare, flare))
# top-right flare
for a in range(180, 271):
    rad = math.radians(a)
    pts.append((w + flare * math.cos(rad), flare + flare * math.sin(rad)))
pts.append((0, 0))

draw.polygon(pts, fill=255)

notch = Image.new('RGB', (w, h), (0, 0, 0))
ndraw = ImageDraw.Draw(notch)

# Draw Header
# Nook pill: x=flare+14, y=6, w=72, h=24, r=12
nx, ny, nw, nh = flare + 14, 6, 72, 24
ndraw.rounded_rectangle([nx, ny, nx+nw, ny+nh], radius=12, fill=(38, 38, 44))
ndraw.text((nx+26, ny+5), "Nook", fill=(255, 255, 255))
# Tray tab
ndraw.text((nx+nw+28, ny+5), "Tray", fill=(160, 160, 170))

# Camera lens
cx = w // 2
ndraw.ellipse([cx-4, 3, cx+4, 11], fill=(13, 13, 18), outline=(30, 30, 40))
ndraw.ellipse([cx-1, 6, cx+1, 8], fill=(10, 132, 255))

# Gear
ndraw.text((w - flare - 28, ny+4), "*", fill=(160, 160, 170))

# Content Row: y = 36 to 132 (h = 96)
# Left: Media
art = Image.open(r'src\GrumphySwarm\Resources\album_default.png').resize((64, 64))
# round corners of art
art_mask = Image.new('L', (64, 64), 0)
ImageDraw.Draw(art_mask).rounded_rectangle([0, 0, 64, 64], radius=12, fill=255)
notch.paste(art, (flare + 14, 46), art_mask)

# Badge
ndraw.rounded_rectangle([flare + 14 + 48, 46 + 48, flare + 14 + 68, 46 + 68], radius=5, fill=(239, 68, 68), outline=(0, 0, 0), width=1)

# Media text
tx = flare + 14 + 74
ndraw.text((tx, 48), "Dibi Dibi Rek", fill=(255, 255, 255))
ndraw.text((tx, 66), "Best Of", fill=(160, 160, 170))
ndraw.text((tx, 82), "Ismaël Lô", fill=(113, 113, 122))
ndraw.text((tx, 98), "|<<   ||   >>|", fill=(220, 220, 230))

# Divider 1
div1_x = flare + 14 + 235
ndraw.line([(div1_x, 44), (div1_x, 118)], fill=(30, 30, 36), width=1)

# Center: Calendar
cal_x = div1_x + 16
ndraw.text((cal_x, 56), "Aug", fill=(255, 255, 255))

# Week strip
strip_x = cal_x + 55
days = [("T", "02", False), ("F", "03", False), ("S", "04", False), ("MON", "05", True), ("T", "06", False), ("W", "07", False), ("T", "08", False)]
for i, (wd, dn, is_today) in enumerate(days):
    dx = strip_x + i * 26
    c_hdr = (10, 132, 255) if is_today else (100, 100, 100)
    c_num = (10, 132, 255) if is_today else (170, 170, 170)
    ndraw.text((dx, 52), wd, fill=c_hdr)
    ndraw.text((dx, 68), dn, fill=c_num)

# Calendar subtitle
ndraw.text((cal_x + 40, 96), "[cal] Nothing for today", fill=(130, 130, 140))

# Divider 2
div2_x = div1_x + 275
ndraw.line([(div2_x, 44), (div2_x, 118)], fill=(30, 30, 36), width=1)

# Avatar
av = Image.open(r'src\GrumphySwarm\Resources\avatar_default.png').resize((64, 64))
av_mask = Image.new('L', (64, 64), 0)
ImageDraw.Draw(av_mask).ellipse([0, 0, 64, 64], fill=255)
av_x = div2_x + (w - flare - div2_x - 64) // 2
notch.paste(av, (av_x, 46), av_mask)

# Paste notch onto background
canvas.paste(notch, (40, 0), mask)
canvas.save(r'scratch\full_simulated_notch.png')
print("Saved full_simulated_notch.png successfully!")
