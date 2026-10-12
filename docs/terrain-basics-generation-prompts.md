# 基础地形素材生成提示词

生成方式：内置 imagegen。参考图仅用于风格参照，原文件保持不变。

参考素材：

- `Assets/Art/Sprites/Props/Storage/wooden_crate_8view_sheet.png`
- `Assets/Art/Sprites/Props/Kitchen/stone_water_basins_sheet.png`
- `Assets/Art/Sprites/Props/Camp/hide_tent.png`

以下保留实际提交给生成工具的提示词。提示词中的尺寸、颜色数和无缝要求是生成目标；输出实际为 1254 × 1254，未证明严格像素网格、颜色数或无缝性。

## cave_earth_dry

```text
Use case: stylized-concept. Asset type: production pixel-art albedo texture for a Unity HD-2D game with real 3D terrain and tiny pixel sprite characters. Input images are STYLE REFERENCES ONLY, not edit targets: match their restrained old-school pixel clusters, muted brown/slate palette, readable dark crevices, no smooth painting. Generate a single square 1024x1024 fully opaque texture, a coherent low-resolution 128x128 pixel design enlarged with exact nearest-neighbor hard square pixels (8x8 blocks), limited palette 12-18 colors. Flat orthographic material surface filling entire canvas; NO perspective, objects, border, grid lines, labels, text, background margin, ambient occlusion vignette, directional cast shadows, baked light glow or photographic noise. Seamless wrap on both axes: left/right and top/bottom should connect naturally. Surface variation subdued so tiny character sprites remain legible. Subject: compacted dry cave earth, dusty desaturated umber and taupe soil, sparse small gray-brown embedded pebbles, subtle angular cracks and packed clay clusters. Low contrast, not orange, not paved stones, not a floor tileset atlas. A single repeating ground material suitable for village paths and cave main floor.
```

## cave_rock_face

```text
Use case: stylized-concept. Asset type: production pixel-art albedo texture for a Unity HD-2D game with real 3D terrain and tiny pixel sprite characters. Input images are STYLE REFERENCES ONLY, not edit targets: match their restrained old-school pixel clusters, muted brown/slate palette, readable dark crevices, no smooth painting. Generate a single square 1024x1024 fully opaque texture, a coherent low-resolution 128x128 pixel design enlarged with exact nearest-neighbor hard square pixels (8x8 blocks), limited palette 12-18 colors. Flat orthographic material surface filling entire canvas; NO perspective, objects, border, grid lines, labels, text, background margin, ambient occlusion vignette, directional cast shadows, baked light glow or photographic noise. Seamless wrap on both axes: left/right and top/bottom should connect naturally. Surface variation subdued so tiny character sprites remain legible. Subject: natural cave cliff rock face, irregular interlocking fractured slate limestone strata, medium muted gray with warm taupe inclusions and darker gray cracks. Broad angular rock clusters and sparse small chips. No manufactured brick wall, no distinct isolated boulders. A single repeating cliff material for vertical 3D walls and ledges.
```

## cave_moss_soil

```text
Use case: stylized-concept. Asset type: production pixel-art albedo texture for a Unity HD-2D game with real 3D terrain and tiny pixel sprite characters. Input images are STYLE REFERENCES ONLY, not edit targets: match their restrained old-school pixel clusters, muted brown/slate palette, readable dark crevices, no smooth painting. Generate a single square 1024x1024 fully opaque texture, a coherent low-resolution 128x128 pixel design enlarged with exact nearest-neighbor hard square pixels (8x8 blocks), limited palette 12-18 colors. Flat orthographic material surface filling entire canvas; NO perspective, objects, border, grid lines, labels, text, background margin, ambient occlusion vignette, directional cast shadows, baked light glow or photographic noise. Seamless wrap on both axes: left/right and top/bottom should connect naturally. Surface variation subdued so tiny character sprites remain legible. Subject: damp mossy cave soil for riverbanks and cultivation areas. Muted olive and gray-green moss patches over dark umber earth, occasional tiny gray pebbles, low-growing dense moss clusters. Natural uneven distribution, no bright emerald lawn, no flowers, no large plants. A single repeating horizontal ground material.
```

## cave_water_calm

```text
Use case: stylized-concept. Asset type: production pixel-art albedo texture for a Unity HD-2D game with real 3D terrain and tiny pixel sprite characters. Input images are STYLE REFERENCES ONLY, not edit targets: match their restrained old-school pixel clusters, muted brown/slate palette, readable dark crevices, no smooth painting. Generate a single square 1024x1024 fully opaque texture, a coherent low-resolution 128x128 pixel design enlarged with exact nearest-neighbor hard square pixels (8x8 blocks), limited palette 12-18 colors. Flat orthographic material surface filling entire canvas; NO perspective, objects, border, grid lines, labels, text, background margin, ambient occlusion vignette, directional cast shadows, baked light glow or photographic noise. Seamless wrap on both axes: left/right and top/bottom should connect naturally. Surface variation subdued so tiny character sprites remain legible. Subject: calm shallow underground river water surface, restrained dark desaturated teal blue-green, scattered short stepped pixel ripple strokes in gray teal, subtle dark blue patches suggesting depth. Nearly uniform calm surface, no rocks, no shore, no foam border, no reflection of objects, no bright white sparkles. Static base-color texture for a water plane.
```

## cave_wood_planks

```text
Use case: stylized-concept. Asset type: production pixel-art albedo texture for a Unity HD-2D game with real 3D terrain and tiny pixel sprite characters. Input images are STYLE REFERENCES ONLY, not edit targets: match their restrained old-school pixel clusters, muted brown/slate palette, readable dark crevices, no smooth painting. Generate a single square 1024x1024 fully opaque texture, a coherent low-resolution 128x128 pixel design enlarged with exact nearest-neighbor hard square pixels (8x8 blocks), limited palette 12-18 colors. Flat orthographic material surface filling entire canvas; NO perspective, objects, border, grid lines, labels, text, background margin, ambient occlusion vignette, directional cast shadows, baked light glow or photographic noise. Seamless wrap on both axes: left/right and top/bottom should connect naturally. Surface variation subdued so tiny character sprites remain legible. Subject: weathered rough wood planks for a goblin cave settlement bridge and raised wooden floor. Long parallel timber boards running vertically through entire texture, irregular staggered seams, dark thin gaps, sparse knots and broad angular grain clusters. Muted warm brown and dark umber palette matching reference crate, moderate contrast. No metal hardware, no perspective or isolated bridge illustration.
```

## cave_rocks_stalagmites_sheet

```text
Use case: stylized-concept. Asset type: transparent pixel-art terrain detail sprite sheet for a Unity HD-2D cave game. The three input images are STYLE REFERENCES ONLY: match their tiny crisp retro hand-clustered pixels, muted gray stone, brown accents and dark silhouettes. Create one square sheet of SIX isolated stone terrain sprites arranged in exactly 3 columns by 2 rows of equal cells. Top row: low small rounded rock; medium fractured boulder; flat clustered riverbank stones. Bottom row: single short stalagmite; cluster of three tall stalagmites; moss-covered broken boulder. Each fits entirely in its own cell with generous transparent gutters, baseline near bottom of its cell; no overlap, no cutoff. Three-quarter front view with slight visibility of tops, suitable for billboard sprites beside 3D terrain. Each sprite should look like approximately 32x40 to 48x64 source pixels enlarged in exact nearest-neighbor blocks, hard staircase edges, controlled 8-12 color palette per sprite, upper-left highlights subtle like the stone basin reference. Real transparent alpha background, no ground plane, no cast shadow, no checkerboard baked into image, no labels, no text, no border, no glow, no blur or smooth gradients. Gray taupe stone and subdued olive moss.
```
