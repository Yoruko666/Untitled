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

## 补充素材

继续使用内置 imagegen 与同一组 Props 风格参考。四张新增表面贴图实际为 1254 × 1254，四张新增 RGBA 图集为 1536 × 1024；生成要求与实际尺寸可能不同。植物与蘑菇各执行了一次透明背景清理，最终交付清理后的版本。

### cave_earth_wet

```text
Use case: stylized-concept. Asset type: production pixel-art albedo texture for a Unity HD-2D game with real 3D terrain and tiny pixel sprite characters. Input images are STYLE REFERENCES ONLY, not edit targets: match their restrained old-school pixel clusters, muted brown/slate palette, readable dark crevices, no smooth painting. Generate a single square 1024x1024 fully opaque texture, a coherent low-resolution 128x128 pixel design enlarged with exact nearest-neighbor hard square pixels (8x8 blocks), limited palette 12-18 colors. Flat orthographic material surface filling entire canvas; NO perspective, objects, border, grid lines, labels, text, background margin, ambient occlusion vignette, directional cast shadows, baked light glow or photographic noise. Seamless wrap on both axes: left/right and top/bottom should connect naturally. Surface variation subdued so tiny character sprites remain legible. Subject: compacted damp riverbank cave mud in dark muted gray-brown umber. Broad irregular wet clay clusters, sparse flat gray pebbles, tiny dull cool patches. Not a puddle, no mirror reflection, no deep fissures. Low contrast horizontal muddy ground, matching the existing dry soil texture but visibly darker and damper.
```

### cave_river_gravel

```text
Use case: stylized-concept. Asset type: production pixel-art albedo texture for a Unity HD-2D game with real 3D terrain and tiny pixel sprite characters. Input images are STYLE REFERENCES ONLY, not edit targets: match their restrained old-school pixel clusters, muted brown/slate palette, readable dark crevices, no smooth painting. Generate a single square 1024x1024 fully opaque texture, a coherent low-resolution 128x128 pixel design enlarged with exact nearest-neighbor hard square pixels (8x8 blocks), limited palette 12-18 colors. Flat orthographic material surface filling entire canvas; NO perspective, objects, border, grid lines, labels, text, background margin, ambient occlusion vignette, directional cast shadows, baked light glow or photographic noise. Seamless wrap on both axes: left/right and top/bottom should connect naturally. Surface variation subdued so tiny character sprites remain legible. Subject: flat underground riverbed gravel. Dense small rounded pebbles of varied subdued slate gray, taupe, gray-blue and dark brown, embedded in sediment. Stones mostly flat and low contrast with a few angular fragments, no water, no large boulders, no directional illumination.
```

### cave_stone_paving

```text
Use case: stylized-concept. Asset type: production pixel-art albedo texture for a Unity HD-2D game with real 3D terrain and tiny pixel sprite characters. Input images are STYLE REFERENCES ONLY, not edit targets: match their restrained old-school pixel clusters, muted brown/slate palette, readable dark crevices, no smooth painting. Generate a single square 1024x1024 fully opaque texture, a coherent low-resolution 128x128 pixel design enlarged with exact nearest-neighbor hard square pixels (8x8 blocks), limited palette 12-18 colors. Flat orthographic material surface filling entire canvas; NO perspective, objects, border, grid lines, labels, text, background margin, ambient occlusion vignette, directional cast shadows, baked light glow or photographic noise. Seamless wrap on both axes: left/right and top/bottom should connect naturally. Surface variation subdued so tiny character sprites remain legible. Subject: rough manually laid cave village stone paving, medium slate gray irregular broad flagstones with small dark joints, occasional worn chipped edges. Sparse earthy dust, flat and weathered. Stones should form an unobtrusive walkable path under pixel characters, no boulders, no relief pillars.
```

### cave_farm_soil

```text
Use case: stylized-concept. Asset type: production pixel-art albedo texture for a Unity HD-2D game with real 3D terrain and tiny pixel sprite characters. Input images are STYLE REFERENCES ONLY, not edit targets: match their restrained old-school pixel clusters, muted brown/slate palette, readable dark crevices, no smooth painting. Generate a single square 1024x1024 fully opaque texture, a coherent low-resolution 128x128 pixel design enlarged with exact nearest-neighbor hard square pixels (8x8 blocks), limited palette 12-18 colors. Flat orthographic material surface filling entire canvas; NO perspective, objects, border, grid lines, labels, text, background margin, ambient occlusion vignette, directional cast shadows, baked light glow or photographic noise. Seamless wrap on both axes: left/right and top/bottom should connect naturally. Surface variation subdued so tiny character sprites remain legible. Subject: freshly worked dark brown cultivation bed soil. Loose umber earth clumps in gently broken parallel short furrows, tiny organic specks, subdued dark soil. No crops, no plants, no borders, no farm scene. Surface usable as a repeating cultivated soil material in cave settlement planting beds.
```

### cave_grass_ferns_sheet

```text
Use case: stylized-concept. Asset type: usable transparent pixel-art terrain sprite sheet for Unity HD-2D 3D cave village. Input images are STYLE REFERENCES ONLY, not editing targets: emulate tiny crisp retro pixel clusters, hard stepped edges, muted brown/slate colors and sparse restrained highlights of existing props. Square canvas. Exactly SIX separate sprites in 3 columns by 2 rows, equal-size cells, generous fully transparent gutters. One complete standalone sprite centered inside each cell, no overlaps, no clipping. Approximately 32 to 64 logical pixels per object visibly enlarged with nearest-neighbor, no soft painterly detail, no smooth gradient or antialiasing. All cells genuinely transparent alpha, no ground plane or cast shadows, no checkerboard baked in, no text, no numbers, no borders or UI. Subject: sparse entrance and damp riverbank vegetation; NOT a dense sunlit forest. Top row left to right: short olive-green grass tuft, medium broad-leaf fern clump, taller narrow river reed clump without fluffy cattails. Bottom row: dry brown grass tuft, low muted green leaf clump, small mossy weed clump. Three-quarter front view and visible plant silhouettes rooted at bottom, matching reference props, no pots, no flowers. Dark desaturated greens, ochre dry blades, modest cream highlights.
```

透明清理提示词：

```text
Use case: background-extraction. Edit ONLY transparency and background of this pixel-art sprite sheet. Preserve all six objects, their exact pixel art shapes, palette, positions and 3 by 2 layout. Completely REMOVE every soft colored halo, lighting bloom, black backdrop, haze and shadow surrounding the sprites. Absolutely empty transparent alpha outside each hard sprite silhouette, including spaces between leaves or stems. Object body pixels should be fully opaque, surrounding empty pixels fully transparent. Preserve hard stepped pixel edges. Do not add lighting, redraw objects, introduce text, or change composition. Output a clean usable game sprite atlas with actual transparent background.
```

### cave_mushrooms_sheet

```text
Use case: stylized-concept. Asset type: usable transparent pixel-art terrain sprite sheet for Unity HD-2D 3D cave village. Input images are STYLE REFERENCES ONLY, not editing targets: emulate tiny crisp retro pixel clusters, hard stepped edges, muted brown/slate colors and sparse restrained highlights of existing props. Square canvas. Exactly SIX separate sprites in 3 columns by 2 rows, equal-size cells, generous fully transparent gutters. One complete standalone sprite centered inside each cell, no overlaps, no clipping. Approximately 32 to 64 logical pixels per object visibly enlarged with nearest-neighbor, no soft painterly detail, no smooth gradient or antialiasing. All cells genuinely transparent alpha, no ground plane or cast shadows, no checkerboard baked in, no text, no numbers, no borders or UI. Subject: cave fungi and small mushrooms for cave cultivation and damp rock edges. Top row: cluster of three small ochre-capped mushrooms; medium brown mushrooms with cream stems; broad slate-blue cave fungus cluster. Bottom row: flat pale bracket fungi on a small stone; two muted teal mushrooms without glow; short dense tiny cream mushroom patch. Three-quarter front view with slight top visibility, stems rooted near bottom of cell. Restrained ochre, cream, dark brown, gray blue and teal, no neon glowing light, no decorative fantasy particles.
```

透明清理提示词：

```text
Use case: background-extraction. Edit ONLY transparency and background of this pixel-art sprite sheet. Preserve all six objects, their exact pixel art shapes, palette, positions and 3 by 2 layout. Completely REMOVE every soft colored halo, lighting bloom, black backdrop, haze and shadow surrounding the sprites. Absolutely empty transparent alpha outside each hard sprite silhouette, including spaces between leaves or stems. Object body pixels should be fully opaque, surrounding empty pixels fully transparent. Preserve hard stepped pixel edges. Do not add lighting, redraw objects, introduce text, or change composition. Output a clean usable game sprite atlas with actual transparent background.
```

### cave_roots_ore_sheet

```text
Use case: stylized-concept. Asset type: game-ready transparent pixel-art atlas. Match style of input reference props ONLY, keep references unchanged. Exactly SIX isolated complete sprites in a 3-column 2-row layout, with generous transparent gutters. Clear hard square pixels, limited muted brown/gray/olive palette, no smooth gradients or antialiasing. Every object has hard opaque pixels and crisp silhouette on absolutely empty transparent background: no glows, no colored haze, no blurred halos, no cast shadows, no ground plane, no labels, no text, no borders. Cave terrain accents. Top row: tangled exposed brown roots; dangling long root cluster; twisted root emerging from a small stone. Bottom row: gray stone with a few rust iron veins; fractured rock with a few small subdued gray-teal crystal points; scattered chipped slate rubble pile. Three-quarter front view, restrained upper-left highlights, muted colors, crystal surfaces solid and not luminous.
```

### cave_ground_patches_sheet

```text
Use case: stylized-concept. Asset type: game-ready transparent pixel-art atlas. Match style of input reference props ONLY, keep references unchanged. Exactly SIX isolated complete sprites in a 3-column 2-row layout, with generous transparent gutters. Clear hard square pixels, limited muted brown/gray/olive palette, no smooth gradients or antialiasing. Every object has hard opaque pixels and crisp silhouette on absolutely empty transparent background: no glows, no colored haze, no blurred halos, no cast shadows, no ground plane, no labels, no text, no borders. Flat terrain surface patches. Pure orthographic TOP-DOWN view, no perspective thickness or standing objects. Top row: low olive moss patch; elongated irregular moss strip; dark brown damp mud patch. Bottom row: sparse small gray-brown gravel scatter; dry taupe dirt patch; 4 to 6 flat slate stepping stones. Centers solid textured pixels, irregular fragmented pixel boundaries to full transparency, no square backgrounds. These are six standalone flat ground decorations, not autotiles. No lighting halo.
```
