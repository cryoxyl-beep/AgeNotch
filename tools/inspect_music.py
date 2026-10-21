from PIL import Image
import numpy as np

img_path = r'C:\Users\SANTHOSH\.gemini\antigravity\brain\96a4b36e-4a22-4b1d-99ed-485b65db487f\.user_uploaded\media_1792602305135_6897fad3.png'
im = Image.open(img_path).convert('RGB')
arr = np.array(im)

# Album art square is between x=170..300 and y=105..225
# Let's check coordinates where album art starts and ends
# In album art, the borders have corner radius
for y in [105, 110, 150, 215, 220]:
    row = arr[y, 160:320, 0]
    # find where row > 20
    xs = np.where(row > 20)[0] + 160
    if len(xs) > 0:
        print(f"y={y}: album art xs from {xs[0]} to {xs[-1]} (w={xs[-1]-xs[0]+1})")

# Let's check text to the right of album art: x in [300..470], y in [105..225]
# Let's find vertical lines or non-black pixels in x in [300..475]
title_mask = (arr[105:140, 300:460, 0] > 150)
ty, tx = np.where(title_mask)
print(f"Track title: y={105+ty.min()}..{105+ty.max()} (h={ty.max()-ty.min()+1}), x={300+tx.min()}..{300+tx.max()} (w={tx.max()-tx.min()+1})")

album_mask = (arr[135:160, 300:460, 0] > 100) & (arr[135:160, 300:460, 0] < 200)
my, mx = np.where(album_mask)
print(f"Album title: y={135+my.min()}..{135+my.max()} (h={my.max()-my.min()+1}), x={300+mx.min()}..{300+mx.max()} (w={mx.max()-mx.min()+1})")

artist_mask = (arr[155:180, 300:460, 0] > 100) & (arr[155:180, 300:460, 0] < 200)
ry, rx = np.where(artist_mask)
print(f"Artist text: y={155+ry.min()}..{155+ry.max()} (h={ry.max()-ry.min()+1}), x={300+rx.min()}..{300+rx.max()} (w={rx.max()-rx.min()+1})")

ctrls_mask = (arr[180:215, 300:420, 0] > 150)
cy, cx = np.where(ctrls_mask)
print(f"Transport controls: y={180+cy.min()}..{180+cy.max()} (h={cy.max()-cy.min()+1}), x={300+cx.min()}..{300+cx.max()} (w={cx.max()-cx.min()+1})")

# Divider 1: between music and calendar
for x in range(450, 485):
    col = arr[110:210, x, 0]
    if 10 < np.mean(col) < 50:
        print(f"Divider 1 column: x={x}, mean brightness={np.mean(col):.1f}")

# Divider 2: between calendar and avatar
for x in range(815, 840):
    col = arr[110:210, x, 0]
    if 10 < np.mean(col) < 50:
        print(f"Divider 2 column: x={x}, mean brightness={np.mean(col):.1f}")
