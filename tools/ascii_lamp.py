from PIL import Image
import numpy as np

img = Image.open(r'scratch\header.png').convert('L')
arr = np.array(img)
# The lamp icon is to the left of "Nook" in header.png
# Let's print ASCII art of the icon!
crop = arr[4:25, 10:35]
for row in crop:
    print("".join('#' if p > 80 else ' ' for p in row))
