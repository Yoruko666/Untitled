# 洞穴地面纹理候选

当前三张 `Assets/Art/Textures/CaveHD2D/` 贴图已重绘并替换为实际 64 × 64、每张最多 8 色的不透明 PNG。文件名、GUID 和材质绑定沿用原资源；未进行本轮 Unity Editor 验收。完整提示词见 [64px 重绘记录](cave-textures-64px-prompts.md)。以下为初版生成记录。

两张素材由 GPT 内置 imagegen 技能生成，以用户素材包中的 `06_image-06.png`（64×64 细颗粒泥土）为风格参考。

| 素材 | 用途 | 实际尺寸 |
| --- | --- | --- |
| `Assets/Art/Textures/CaveHD2D/cave_ground_path_candidate.png` | 平整、略亮的踩实泥土，供通路试铺 | 1254×1254 |
| `Assets/Art/Textures/CaveHD2D/cave_ground_wet_candidate.png` | 较暗泥土与稀疏灰绿斑点，供湿岸试铺 | 1254×1254 |

两张 PNG 完全不透明，保留生成原图。它们是高清像素风候选，**不是原生 64×64 像素纹理**；工具未满足提示词中的小尺寸、严格像素网格要求。未通过降采样把它们冒充原生像素素材。

已查看单图和 3×3 平铺预览，未见明显边框接缝；重复图案、与母版的颗粒尺寸和实际低俯角表现仍需场景中确认，不声明严格无缝。导入为 Default Texture、Point、Repeat、无压缩、关闭 mipmaps、NPOT 不缩放。正式铺贴时按实际镜头判断 mipmaps，并以材质的世界重复尺寸统一颗粒大小。

本次只新增候选文件，没有替换地面材质、修改场景或运行 Unity。64×64 母版与这两张高清图不能仅按相同的文件重复尺寸假定具有相同像素密度。

## 实际生成提示词：踩实土路

```text
Generate one production candidate for a Unity HD-2D game terrain ALBEDO texture, using the attached 64x64 image only as a style and material reference. A single square, completely opaque, edge-to-edge, seamless tileable PIXEL ART material. Retain the reference's restrained fine-grained earth surface, subdued warm brown colors, low contrast and simple discrete pixel clusters. Deliver at an actual 64x64 pixel resolution if supported; if the native tool only supports larger images, render a visually strict 64x64 pixel design as an exact integer nearest-neighbor enlargement and report/save the actual output resolution honestly. All pixel cells must align to a uniform grid; no antialiasing, no gradients. Perfect left/right and top/bottom periodic continuity. No borders, margins, labels, isolated objects, perspective, large stone plates, cracks, diagonal scratches, lighting gradients, cast shadows, scene illumination or baked glare. Flat neutral illumination; future lighting is supplied by Unity. Texture-only single tile, not a sheet, a map or a repeated preview. Material variant: WALKED COMPACTED EARTH. Slightly smoother and a little lighter than the reference; fewer tiny grains and tiny soft worn patches. Keep brown muted with a slight gray undertone. The tile is uniform walked-on earth material across the entire square, NOT a strip or drawing of a road; no directional ruts. Quiet enough for large areas and pixel characters.
```

## 实际生成提示词：潮湿岸土

```text
Generate one production candidate for a Unity HD-2D game terrain ALBEDO texture, using the attached 64x64 image only as a style and material reference. A single square, completely opaque, edge-to-edge, seamless tileable PIXEL ART material. Retain the reference's restrained fine-grained earth surface, subdued warm brown colors, low contrast and simple discrete pixel clusters. Deliver at an actual 64x64 pixel resolution if supported; if the native tool only supports larger images, render a visually strict 64x64 pixel design as an exact integer nearest-neighbor enlargement and report/save the actual output resolution honestly. All pixel cells must align to a uniform grid; no antialiasing, no gradients. Perfect left/right and top/bottom periodic continuity. No borders, margins, labels, isolated objects, perspective, large stone plates, cracks, diagonal scratches, lighting gradients, cast shadows, scene illumination or baked glare. Flat neutral illumination; future lighting is supplied by Unity. Texture-only single tile, not a sheet, a map or a repeated preview. Material variant: DAMP RIVERBANK EARTH. Same earth grains and scale as the reference, subtly darker and slightly cooler gray-brown. Sparse tiny muted gray-green moss flecks, less than 10 percent of surface, softly distributed. Avoid dense vegetation, rocks and visible water/puddles. Matte wet soil with no painted shine or reflections. Maintain low contrast and the restrained common palette of the reference.
```
