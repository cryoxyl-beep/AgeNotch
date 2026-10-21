from PIL import Image
import numpy as np

img_path = r'C:\Users\SANTHOSH\.gemini\antigravity\brain\96a4b36e-4a22-4b1d-99ed-485b65db487f\.user_uploaded\media_1792602305135_6897fad3.png'
im = Image.open(img_path).convert('RGB')
arr = np.array(im)

# Look at y from 40 to 240
# For each y, find where turquoise transitions to black on left, and where black transitions to turquoise on right
profile = []
for y in range(40, 245):
    # turquoise has green > 80 and blue > 80, black has green < 20 and blue < 20
    is_turquoise = (arr[y, :, 1] > 70) & (arr[y, :, 2] > 70)
    # find black pixels
    # in row y, notch body is black
    is_black = (arr[y, :, 0] < 20) & (arr[y, :, 1] < 20) & (arr[y, :, 2] < 20)
    # find first non-turquoise from left after x=50
    # and first non-turquoise from right before x=950
    t_xs = np.where(is_turquoise)[0]
    left_t = t_xs[t_xs < 500]
    right_t = t_xs[t_xs > 500]
    left_edge = left_t[-1] + 1 if len(left_t) > 0 else 0
    right_edge = right_t[0] - 1 if len(right_t) > 0 else 1001
    profile.append((y, left_edge, right_edge, right_edge - left_edge + 1))

print("Y=40..80:")
for y, l, r, w in profile[:40:2]:
    print(f"y={y:3d}: left={l:3d}, right={r:3d}, width={w:3d}")

print("\nY=180..245:")
for y, l, r, w in profile[140::2]:
    print(f"y={y:3d}: left={l:3d}, right={r:3d}, width={w:3d}")
