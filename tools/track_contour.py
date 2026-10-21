from PIL import Image
import numpy as np

img_path = r'C:\Users\SANTHOSH\.gemini\antigravity\brain\96a4b36e-4a22-4b1d-99ed-485b65db487f\.user_uploaded\media_1792602305135_6897fad3.png'
im = Image.open(img_path).convert('RGB')
arr = np.array(im)

h, w, _ = arr.shape

# The desktop is turquoise: R < 60, G in [120..190], B in [130..190]
# The notch is black/dark: R < 35, G < 35, B < 35

contour = []
for y in range(40, h):
    # Scan from x=0 to right to find notch outer boundary
    left_x = None
    for x in range(0, w // 2):
        r, g, b = arr[y, x]
        # if this pixel is dark (notch)
        if r < 35 and g < 35 and b < 35:
            # confirm next 5 pixels are also dark
            if all(arr[y, x+i, 1] < 35 for i in range(1, 6)):
                left_x = x
                break

    # Scan from x=w-1 to left to find notch outer boundary
    right_x = None
    for x in range(w - 1, w // 2, -1):
        r, g, b = arr[y, x]
        if r < 35 and g < 35 and b < 35:
            if all(arr[y, x-i, 1] < 35 for i in range(1, 6)):
                right_x = x
                break

    if left_x is not None and right_x is not None:
        contour.append((y, left_x, right_x, right_x - left_x + 1))

print("Contour sampled:")
for y, l, r, width in contour[::5]:
    print(f"y={y:3d}: left={l:3d}, right={r:3d}, width={width:3d}")

print("\nBottom of notch:")
for y, l, r, width in contour[-15:]:
    print(f"y={y:3d}: left={l:3d}, right={r:3d}, width={width:3d}")
