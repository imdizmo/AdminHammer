#!/usr/bin/env python3
"""Draw the Thunderstore icon (256x256): a gold hammer on a dark tile with a brass edge. Own art."""
from PIL import Image, ImageDraw, ImageFilter
S = 4; W = 256 * S                       # draw at 4x, downsample for clean edges
img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
d = ImageDraw.Draw(img)
d.rounded_rectangle([8*S, 8*S, 248*S, 248*S], radius=36*S, fill=(28, 22, 18, 255), outline=(201, 160, 86, 255), width=6*S)
d.rounded_rectangle([20*S, 20*S, 236*S, 236*S], radius=28*S, outline=(214, 178, 110, 70), width=2*S)
glow = Image.new("RGBA", (W, W), (0, 0, 0, 0)); g = ImageDraw.Draw(glow)
g.ellipse([60*S, 50*S, 200*S, 190*S], fill=(230, 190, 110, 60)); glow = glow.filter(ImageFilter.GaussianBlur(28*S))
img = Image.alpha_composite(img, glow); d = ImageDraw.Draw(img)
hammer = Image.new("RGBA", (W, W), (0, 0, 0, 0)); h = ImageDraw.Draw(hammer)
h.rounded_rectangle([120*S, 92*S, 138*S, 222*S], radius=6*S, fill=(122, 84, 50, 255), outline=(60, 40, 24, 255), width=3*S)     # handle
for y in (150, 172, 194): h.line([122*S, y*S, 136*S, (y+6)*S], fill=(70, 46, 28, 255), width=3*S)                                # grip wraps
h.rounded_rectangle([66*S, 54*S, 192*S, 104*S], radius=10*S, fill=(232, 186, 92, 255), outline=(110, 78, 30, 255), width=4*S)    # gold head
h.rectangle([72*S, 60*S, 186*S, 70*S], fill=(250, 220, 150, 255))                                                                 # highlight
h.rounded_rectangle([60*S, 50*S, 80*S, 108*S], radius=6*S, fill=(200, 150, 64, 255), outline=(110, 78, 30, 255), width=4*S)      # faces
h.rounded_rectangle([178*S, 50*S, 198*S, 108*S], radius=6*S, fill=(200, 150, 64, 255), outline=(110, 78, 30, 255), width=4*S)
hammer = hammer.rotate(-28, center=(128*S, 128*S), resample=Image.BICUBIC)
img = Image.alpha_composite(img, hammer)
img.resize((256, 256), Image.LANCZOS).save(__import__("sys").argv[1])
