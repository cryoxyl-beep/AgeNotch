from PIL import Image

img_path = r'C:\Users\SANTHOSH\.gemini\antigravity\brain\96a4b36e-4a22-4b1d-99ed-485b65db487f\.user_uploaded\media_1792602305135_6897fad3.png'
im = Image.open(img_path).convert('RGBA')

# Album art in reference is around x=171..295, y=103..224
# The square artwork (excluding badge) is approx (172, 104, 292, 224)
art_crop = im.crop((172, 104, 292, 224))
art_crop.save(r'src\GrumphySwarm\Resources\album_default.png')
print("Saved album_default.png successfully! Size:", art_crop.size)
