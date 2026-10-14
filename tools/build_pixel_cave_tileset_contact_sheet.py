"""Assemble the Unity-rendered catalog thumbnails into a labeled reference sheet."""
from pathlib import Path
import math
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
IMAGES = ROOT / "Assets/Art/Previews/PixelCaveTilesets"


def build():
    font = ImageFont.truetype("C:/Windows/Fonts/consola.ttf", 16)
    title_font = ImageFont.truetype("C:/Windows/Fonts/consolab.ttf", 24)
    groups = [
        ("AUTO EDGES | Dual Grid | 4m cells", sorted(IMAGES.glob("pixel_cliffs_*.png"))
         + sorted(IMAGES.glob("pixel_rock_cliffs_*.png"))),
        ("FLOORS | Standard Grid | 4m cells", sorted(IMAGES.glob("pixel_cave_floor_*.png"))),
        ("WALLS & ENTRANCES | Standard Grid stamps", sorted(IMAGES.glob("pixel_cave_stamp_pixelcavewalls_*.png"))),
        ("CONNECTORS | Standard Grid stamps", sorted(IMAGES.glob("pixel_cave_stamp_pixelcaveconnectors_*.png"))),
        ("BASIC MODULES | Standard Grid stamps", sorted(IMAGES.glob("pixel_cave_stamp_pixelcavetiles_*.png"))),
        ("ROCK WALL MODULES | Standard Grid stamps", sorted(IMAGES.glob("pixel_cave_stamp_pixelcaveconnectedrocks_*.png"))),
    ]
    columns, cell_width, cell_height = 6, 256, 292
    height = 88 + sum(48 + math.ceil(len(paths) / columns) * cell_height for _, paths in groups)
    sheet = Image.new("RGB", (columns * cell_width, height), (25, 31, 39))
    draw = ImageDraw.Draw(sheet)
    draw.text((20, 16), "PIXEL CAVE | TILESET CATALOG", font=title_font, fill=(232, 230, 214))
    draw.text((20, 52), "47 presets / 63 prefabs - actual Unity renders - original model proportions", font=font, fill=(169, 181, 192))
    y = 88
    for heading, paths in groups:
        draw.rectangle((0, y, sheet.width, y + 40), fill=(43, 56, 66))
        draw.text((16, y + 8), heading, font=title_font, fill=(201, 218, 178))
        y += 48
        for index, path in enumerate(paths):
            x = index % columns * cell_width
            top = y + index // columns * cell_height
            with Image.open(path) as thumbnail:
                sheet.paste(thumbnail.convert("RGB"), (x, top))
            label = path.stem
            for prefix in ("pixel_cave_stamp_pixelcaveconnectedrocks_", "pixel_cave_stamp_pixelcaveconnectors_",
                           "pixel_cave_stamp_pixelcavewalls_", "pixel_cave_stamp_pixelcavetiles_",
                           "pixel_cave_floor_pixelcaveconnectors_", "pixel_cave_floor_pixelcavetiles_"):
                label = label.removeprefix(prefix)
            draw.text((x + 10, top + 260), label, font=font, fill=(221, 224, 225))
        y += math.ceil(len(paths) / columns) * cell_height
    output = IMAGES / "pixel_cave_tilesets_overview.png"
    sheet.save(output)
    print(output)


if __name__ == "__main__":
    build()
