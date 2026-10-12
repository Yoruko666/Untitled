# HD-2D 基础地形素材

参考 Props 中木箱、石槽和帐篷的硬边像素、土褐／灰色配色，结合洞穴聚落、河岸、台地与木桥需求生成。此套为首版可试用素材；尚未放入场景或完成 Unity Editor 验收。

## 资源

| 文件 | 用途 |
| --- | --- |
| `Textures/TerrainBasic/cave_earth_dry.png` | 洞穴主地面、踩实泥土道路 |
| `Textures/TerrainBasic/cave_rock_face.png` | 岩壁、台地侧面；纹理已有较强块面明暗，需试用确认受光 |
| `Textures/TerrainBasic/cave_moss_soil.png` | 潮湿河岸、种植区附近苔藓地面 |
| `Textures/TerrainBasic/cave_water_calm.png` | 地下河静态水面基础颜色 |
| `Textures/TerrainBasic/cave_wood_planks.png` | 木桥桥面、木平台 |
| `Sprites/Terrain/CaveBasics/cave_rocks_stalagmites_sheet.png` | 六个透明细节：小石堆、碎裂巨石、河岸石、单石笋、石笋群、苔藓巨石 |

以上路径相对于 `Assets/Art/`。五张表面贴图各有同名材质，位于 `Materials/TerrainBasic/`，沿用项目现有 URP Lit Shader，白色 Tint、低 Smoothness，已绑定 Base Map。水面材质为不透明静态试用材质，不含流动、折射或岸线效果。

## 导入与使用

- 所有 PNG 实际为 1254 × 1254；保留生成原图，未二次缩放。生成图具有像素外观，但不等同于原 Props 的原生低分辨率像素网格。
- 表面贴图：Default、sRGB、Point、Repeat、关闭 mipmaps、无压缩、关闭 NPOT 缩放。用于具有 UV 的 3D 地面与墙体；场景碰撞、高度和厚度仍由原有几何负责。
- 透明图集：RGBA、Sprite Single、Point、Clamp、无压缩、关闭 mipmaps、PPU 150、底部中心 pivot，沿用 Props 导入习惯。尚未切割，也未制作 Prefab。切分前将 Sprite Mode 改为 Multiple，再在 Sprite Editor 中按六个完整轮廓手工切分并 Apply；顶部一排为三种岩石，底部一排为单石笋、石笋群与苔藓巨石。
- 图集经过放大，切分后的 PPU 150 仅是初始设置；放入场景时按角色比例调整，不能从 PNG 尺寸推定实际世界尺寸。岩石 Sprite 适合侧边细节；大型围岩与遮挡继续使用 3D 网格。
- 材质初始 Tiling 为 1 × 1。按地形 UV 和世界尺寸调整；地表与道具应在实际游戏镜头下比较像素密度，再统一 Tiling。
- 所有贴图以可重复铺设为生成目标；尚未确认边缘严格无缝。岩壁和木板对比偏强，先在小样块上检查，避免直接覆盖整张地图。

## 验收

1. 在测试地面与墙体上分别拖入同名材质，确认能正确显示且保留硬边像素。
2. 调整 Tiling 为 2 × 2 或 4 × 4，从实际镜头检查接缝、重复感和与现有 Props 的比例。
3. 切分图集，放一块岩石和一组石笋在现有角色旁，检查透明边缘、遮挡与受光。

生成方式为内置 imagegen；完整提示词见 [生成记录](../../../../docs/terrain-basics-generation-prompts.md)。已检查 PNG 可读取、透明通道、导入设置和材质引用；未运行 Editor、Play Mode 或构建。
