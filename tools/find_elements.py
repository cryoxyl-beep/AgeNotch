from PIL import Image
import numpy as np

img_path = r'C:\Users\SANTHOSH\.gemini\antigravity\brain\96a4b36e-4a22-4b1d-99ed-485b65db487f\.user_uploaded\media_1792602305135_6897fad3.png'
im = Image.open(img_path).convert('RGB')
arr = np.array(im)

# Relative to top of notch y0=45, left of body x0=158
# Let's inspect:
# 1. Nook pill: look at region x in [160..300], y in [45..90]
# Find pixels with color of pill background (not pure black, e.g. R in [30..60])
pill_mask = (arr[45:90, 160:320, 0] > 30) & (arr[45:90, 160:320, 0] < 70)
py, px = np.where(pill_mask)
if len(py) > 0:
    print(f"Nook pill: y={45+py.min()}..{45+py.max()} (h={py.max()-py.min()+1}), x={160+px.min()}..{160+px.max()} (w={px.max()-px.min()+1})")

# 2. Camera lens: look at x in [500..600], y in [45..80]
lens_mask = (arr[45:80, 500:600, 2] > 60) # cyan/blue accent in lens
ly, lx = np.where(lens_mask)
if len(ly) > 0:
    print(f"Camera lens: y={45+ly.min()}..{45+ly.max()}, x={500+lx.min()}..{500+lx.max()}")

# 3. Gear icon: look at x in [900..960], y in [50..85]
gear_mask = (arr[50:85, 900:960, 0] > 100)
gy, gx = np.where(gear_mask)
if len(gy) > 0:
    print(f"Gear icon: y={50+gy.min()}..{50+gy.max()} (h={gy.max()-gy.min()+1}), x={900+gx.min()}..{900+gx.max()} (w={gx.max()-gx.min()+1})")

# 4. Vertical dividers: in notch interior, look for subtle vertical lines (R in [20..40])
# Let's search columns between x=350..550 and x=750..880
# where pixels are slightly brighter than black (e.g. 25-45) continuously from y=100 to y=200
for x in range(350, 550):
    col = arr[110:210, x, 0]
    if np.mean(col) > 18 and np.std(col) < 8:
        print(f"Possible divider 1 at x={x}: mean={np.mean(col):.1f}")
        break

for x in range(750, 880):
    col = arr[110:210, x, 0]
    if np.mean(col) > 18 and np.std(col) < 8:
        print(f"Possible divider 2 at x={x}: mean={np.mean(col):.1f}")
        break

# 5. Album art: non-black image in left section x in [165..320], y in [95..225]
art_mask = (arr[95:225, 165:320, 0] > 30) | (arr[95:225, 165:320, 1] > 30) | (arr[95:225, 165:320, 2] > 30)
ay, ax = np.where(art_mask)
if len(ay) > 0:
    print(f"Album art area: y={95+ay.min()}..{95+ay.max()} (h={ay.max()-ay.min()+1}), x={165+ax.min()}..{165+ax.max()} (w={ax.max()-ax.min()+1})")

# 6. Apple Music badge (red badge): R > 180, G < 80, B < 80
badge_mask = (arr[150:230, 240:320, 0] > 180) & (arr[150:230, 240:320, 1] < 80)
by, bx = np.where(badge_mask)
if len(by) > 0:
    print(f"Music badge: y={150+by.min()}..{150+by.max()} (h={by.max()-by.min()+1}), x={240+bx.min()}..{240+bx.max()} (w={bx.max()-bx.min()+1})")

# 7. Avatar (circle): x in [800..950], y in [95..225]
avatar_mask = (arr[95:225, 820:950, 0] > 30) | (arr[95:225, 820:950, 1] > 30)
vy, vx = np.where(avatar_mask)
if len(vy) > 0:
    print(f"Avatar: y={95+vy.min()}..{95+vy.max()} (h={vy.max()-vy.min()+1}), x={820+vx.min()}..{820+vx.max()} (w={vx.max()-vx.min()+1})")

# 8. Calendar 'Aug' text: white text around x in [450..550], y in [100..150]
aug_mask = (arr[100:150, 450:550, 0] > 200) & (arr[100:150, 450:550, 1] > 200)
uy, ux = np.where(aug_mask)
if len(uy) > 0:
    print(f"Aug text: y={100+uy.min()}..{100+uy.max()} (h={uy.max()-uy.min()+1}), x={450+ux.min()}..{450+ux.max()} (w={ux.max()-ux.min()+1})")

# 9. Blue active date '05' or 'MON': B > 180, R < 100
blue_mask = (arr[95:160, 600:750, 2] > 180) & (arr[95:160, 600:750, 0] < 100)
dy, dx = np.where(blue_mask)
if len(dy) > 0:
    print(f"Blue active date: y={95+dy.min()}..{95+dy.max()} (h={dy.max()-dy.min()+1}), x={600+dx.min()}..{600+dx.max()} (w={dx.max()-dx.min()+1})")
