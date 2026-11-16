"""Compose English Nexus media from recorded game screenshots; no image generation."""
import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
BG = "#0b111b"
WHITE = "#f0f4ef"
MUTED = "#b4c1cd"
MINT = "#82e4c6"
FONTS = Path("C:/Windows/Fonts")

def font(size, bold=False, mono=False):
    return ImageFont.truetype(str(FONTS / ("consola.ttf" if mono else "arialbd.ttf" if bold else "arial.ttf")), size)

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--images", type=Path, default=ROOT / "artifacts/nexus-0.2.0/images")
    args = parser.parse_args()
    folder = args.images.resolve()
    patrol_path, treatment_path = folder / "01-patrol.png", folder / "02-treatment.png"
    patrol = Image.open(patrol_path).convert("RGB")
    treatment = Image.open(treatment_path).convert("RGB")
    if patrol.size != (1280, 720) or treatment.size != (1280, 720):
        raise SystemExit("Expected the recorded 1280x720 captures; review crops for other sizes.")
    cover = Image.new("RGB", (1920, 1080), BG)
    d = ImageDraw.Draw(cover)
    d.rectangle((0, 0, 1920, 12), fill=MINT)
    d.text((68, 64), "CASUALTIES: UNKNOWN", font=font(29, mono=True), fill=MINT)
    d.text((68, 190), "GUARDIAN", font=font(124, bold=True), fill=WHITE)
    d.text((68, 322), "DRONE", font=font(124, bold=True), fill=WHITE)
    d.text((73, 505), "COMBAT + MEDICAL COMPANION", font=font(29, bold=True), fill=MINT)
    d.text((73, 567), "An escort for the descent.", font=font(37), fill=WHITE)
    d.text((73, 634), "Patrol support. Lane-aware fire.", font=font(27), fill=MUTED)
    d.text((73, 678), "External wound repair. No resource costs.", font=font(27), fill=MUTED)
    for x, key, label in [(73, "F6", "DEPLOY"), (330, "F7", "CEASEFIRE"), (615, "F8", "MEDICAL")]:
        d.rectangle((x, 791, x + 68, 846), outline=MINT, width=2)
        d.text((x + 13, 801), key, font=font(28, mono=True), fill=WHITE)
        d.text((x + 84, 810), label, font=font(20, bold=True), fill=MUTED)
    d.rectangle((936, 108, 1860, 914), fill="#121e2c", outline="#344958", width=2)
    d.text((967, 132), "ACTUAL GAMEPLAY", font=font(24, mono=True), fill=MINT)
    crop = (460, 90, 890, 425)
    cover.paste(patrol.crop(crop).resize((860, 670), Image.Resampling.NEAREST), (968, 187))
    d.text((968, 878), "Screenshot detail / staged test arena", font=font(20), fill=MUTED)
    d.line((68, 963, 1854, 963), fill="#344958", width=2)
    d.text((73, 995), "0.2.0  /  WINDOWS  /  SINGLE-PLAYER BETA", font=font(25, mono=True), fill=WHITE)
    d.text((1215, 998), "Requires BepInEx 5 + CUCoreLib 1.1", font=font(23), fill=MUTED)
    outputs = []
    cover_path = folder / "cover-screenshots-en.png"
    cover.save(cover_path)
    outputs.append(cover_path)
    gallery = [
        (patrol, (300, 90, 1000, 455), 2, "PATROL SUPPORT", "01-patrol-en.png", "Staged in-game test arena. Original game lighting. Screenshot cropped and enlarged."),
        (treatment, (420, 190, 820, 408), 3, "EXTERNAL WOUND CARE", "02-treatment-en.png", "Prepared external wound. Bleeding, skin and muscle repair; lost blood is not restored."),
    ]
    for source, box, scale, title, filename, caption in gallery:
        img = Image.new("RGB", (1600, 1000), BG)
        draw = ImageDraw.Draw(img)
        draw.rectangle((0, 0, 1600, 8), fill=MINT)
        draw.text((66, 36), title, font=font(43, bold=True), fill=WHITE)
        draw.text((1130, 48), "GUARDIAN DRONE / 0.2.0", font=font(21, mono=True), fill=MINT)
        detail = source.crop(box)
        detail = detail.resize((detail.width * scale, detail.height * scale), Image.Resampling.NEAREST)
        img.paste(detail, ((1600 - detail.width) // 2, 142))
        draw.text((66, 927), caption, font=font(24), fill=MUTED)
        path = folder / filename
        img.save(path)
        outputs.append(path)
    manifest = {
        "method": "Actual game captures, crop, integer nearest-neighbour enlargement, flat typography and layout. No image-generation model, reconstruction or inpainting.",
        "inputs": {patrol_path.name: digest(patrol_path), treatment_path.name: digest(treatment_path)},
        "cover_crop": list(crop), "cover_scale": 2,
        "outputs": {p.name:{"sha256":digest(p),"size":list(Image.open(p).size)} for p in outputs},
        "notice": "Game art belongs to its respective authors. The arena and wound were prepared by a test fixture. UI outside the crops is omitted; screenshot pixels inside the crops are preserved.",
    }
    (folder / "media-provenance.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(manifest, indent=2))

if __name__ == "__main__":
    main()
