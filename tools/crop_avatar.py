from PIL import Image

img_path = r'C:\Users\SANTHOSH\.gemini\antigravity\brain\96a4b36e-4a22-4b1d-99ed-485b65db487f\.user_uploaded\media_1792602305135_6897fad3.png'
im = Image.open(img_path).convert('RGBA')

# Crop the avatar circle:
# The circle is around center (894, 167), radius approx 56
cx, cy, r = 894, 167, 56
avatar_crop = im.crop((cx - r, cy - r, cx + r, cy + r))
avatar_crop.save(r'src\GrumphySwarm\Resources\avatar_default.png')
print("Saved avatar_default.png successfully! Size:", avatar_crop.size)
