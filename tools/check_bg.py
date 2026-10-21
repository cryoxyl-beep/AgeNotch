from PIL import Image
import numpy as np

img_path = r'C:\Users\SANTHOSH\.gemini\antigravity\brain\96a4b36e-4a22-4b1d-99ed-485b65db487f\.user_uploaded\media_1792602305135_6897fad3.png'
im = Image.open(img_path).convert('RGB')
arr = np.array(im)

# Look at column x=50, rows from 0 to 100
for y in range(0, 80, 5):
    print(f"y={y:2d}, x=50: RGB = {arr[y, 50]}")

for y in range(0, 80, 5):
    print(f"y={y:2d}, x=100: RGB = {arr[y, 100]}")
