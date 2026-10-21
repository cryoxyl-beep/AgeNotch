from PIL import Image
import numpy as np

img_path = r'C:\Users\SANTHOSH\.gemini\antigravity\brain\96a4b36e-4a22-4b1d-99ed-485b65db487f\.user_uploaded\media_1792602305135_6897fad3.png'
im = Image.open(img_path).convert('RGB')
w, h = im.size
print(f"Image size: {w}x{h}")

arr = np.array(im)

# Let's find pixels that are near black (e.g. brightness < 15)
# Note that the background around the notch is cyan/turquoise (#28A59D or similar)
# Let's find for each row y from 0 to h-1, the leftmost and rightmost black pixel
is_black = (arr[:, :, 0] < 20) & (arr[:, :, 1] < 20) & (arr[:, :, 2] < 20)

row_spans = []
for y in range(h):
    black_xs = np.where(is_black[y, :])[0]
    if len(black_xs) > 50: # valid row of notch
        min_x = black_xs[0]
        max_x = black_xs[-1]
        row_spans.append((y, min_x, max_x, max_x - min_x + 1))

print(f"Top row with black: y={row_spans[0][0]}, span={row_spans[0][1]}..{row_spans[0][2]} (w={row_spans[0][3]})")
print(f"Bottom row with black: y={row_spans[-1][0]}, span={row_spans[-1][1]}..{row_spans[-1][2]} (w={row_spans[-1][3]})")

# Print profile every 10 rows
print("\nProfile (every 10 rows):")
for y, min_x, max_x, width in row_spans[::10]:
    print(f"y={y:3d}: x={min_x:3d}..{max_x:3d}, width={width:3d}")

# Let's see bottom 15 rows to see corner rounding
print("\nBottom 15 rows:")
for y, min_x, max_x, width in row_spans[-15:]:
    print(f"y={y:3d}: x={min_x:3d}..{max_x:3d}, width={width:3d}")

# Let's see top 25 rows to see flare
print("\nTop 25 rows:")
for y, min_x, max_x, width in row_spans[:25]:
    print(f"y={y:3d}: x={min_x:3d}..{max_x:3d}, width={width:3d}")
