from PIL import Image
import numpy as np

img = Image.open(r'scratch\header.png').convert('L')
arr = np.array(img)
# print bounding box of white pixels before x=80
bright = np.where(arr > 100)
for y, x in zip(*bright):
    if x < 60:
        pass
# Find x range of first white cluster
xs = [x for y, x in zip(*bright) if x < 60]
ys = [y for y, x in zip(*bright) if x < 60]
print(f"Icon: x in [{min(xs)}..{max(xs)}], y in [{min(ys)}..{max(ys)}]")

for y in range(min(ys), max(ys)+1):
    line = "".join('#' if arr[y, x] > 90 else ' ' for x in range(min(xs), max(xs)+1))
    print(line)
