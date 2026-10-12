# HD-2D 基础地形素材

九张表面贴图已参考 Props 的柴堆与 CaveBasics 的地表／岩石素材重绘为粗颗粒像素风。最终 PNG 为实际 64 × 64、每张最多 8 色；材质引用与资源 GUID 保持原样。尚未完成本轮 Unity Editor 验收。

## 资源

| 文件 | 用途 |
| --- | --- |
| `Textures/TerrainBasic/cave_earth_dry.png` | 洞穴主地面、踩实泥土道路 |
| `Textures/TerrainBasic/cave_rock_face.png` | 岩壁、台地侧面；纹理已有较强块面明暗，需试用确认受光 |
| `Textures/TerrainBasic/cave_moss_soil.png` | 潮湿河岸、种植区附近苔藓地面 |
| `Textures/TerrainBasic/cave_water_calm.png` | 地下河静态水面基础颜色 |
| `Textures/TerrainBasic/cave_wood_planks.png` | 木桥桥面、木平台 |
| `Textures/TerrainBasic/cave_earth_wet.png` | 潮湿河岸、洞穴积湿泥地 |
| `Textures/TerrainBasic/cave_river_gravel.png` | 河床碎石、低岸碎石地 |
| `Textures/TerrainBasic/cave_stone_paving.png` | 聚落石板路、祭坛前场 |
| `Textures/TerrainBasic/cave_farm_soil.png` | 种植床、耕作区裸土 |
| `Sprites/Terrain/CaveBasics/cave_rocks_stalagmites_sheet.png` | 六个透明细节：小石堆、碎裂巨石、河岸石、单石笋、石笋群、苔藓巨石 |
| `Sprites/Terrain/CaveBasics/cave_grass_ferns_sheet.png` | 六种植被：草丛、蕨类、河岸长草、枯草、低矮叶丛、苔藓杂草 |
| `Sprites/Terrain/CaveBasics/cave_mushrooms_sheet.png` | 六种洞穴菌类：黄褐菌、棕菌、蓝灰菌、层状菌、青菌、浅色小菌群 |
| `Sprites/Terrain/CaveBasics/cave_roots_ore_sheet.png` | 三种根须与铁矿石、晶簇岩石、碎岩堆 |
| `Sprites/Terrain/CaveBasics/cave_ground_patches_sheet.png` | 六种地表覆盖：苔藓斑、苔藓条、湿泥、碎石、干土、散落石板 |

以上路径相对于 `Assets/Art/`。共九张表面贴图、五张图集（30 个细节图案）。九张表面贴图各有同名材质，位于 `Materials/TerrainBasic/`，沿用项目现有 URP Lit Shader，白色 Tint、低 Smoothness，已绑定 Base Map。水面材质为不透明静态试用材质，不含流动、折射或岸线效果。

## 导入与使用

- 九张表面贴图实际为 64 × 64，经 nearest-neighbor 缩小与 8 色收敛，不使用 dithering。岩石／石笋图集仍为 1254 × 1254，四张补充图集仍为 1536 × 1024，本轮未修改图集。
- 表面贴图：Default、sRGB、Point、Repeat、关闭 mipmaps、无压缩、关闭 NPOT 缩放。用于具有 UV 的 3D 地面与墙体；场景碰撞、高度和厚度仍由原有几何负责。
- 透明图集：RGBA、Sprite Single、Point、Clamp、无压缩、关闭 mipmaps、PPU 150、底部中心 pivot，沿用 Props 导入习惯。尚未切割，也未制作 Prefab。切分前将 Sprite Mode 改为 Multiple，再在 Sprite Editor 中按六个完整轮廓手工切分并 Apply；顶部一排为三种岩石，底部一排为单石笋、石笋群与苔藓巨石。
- 图集经过放大，切分后的 PPU 150 仅是初始设置；放入场景时按角色比例调整，不能从 PNG 尺寸推定实际世界尺寸。岩石 Sprite 适合侧边细节；大型围岩与遮挡继续使用 3D 网格。
- 材质初始 Tiling 为 1 × 1。按地形 UV 和世界尺寸调整；地表与道具应在实际游戏镜头下比较像素密度，再统一 Tiling。
- 所有贴图以可重复铺设为生成目标；尚未确认边缘严格无缝。岩壁和木板对比偏强，先在小样块上检查，避免直接覆盖整张地图。

## 搭配建议

- 入口与通行地面以干土为主，聚落前场少量使用石板；种植床使用耕土，附近零散点缀菌类。
- 河岸使用湿泥／碎石，加入少量草丛或蕨类；深处洞穴以菌类、根须、矿岩为主。绿色植物优先放在入口或有光处。
- 地表覆盖图集为俯视图。切分后用于朝上的 XZ 面片，略抬离地面以避免重叠闪烁；它们不是自动拼接的岸线或 autotile，也不自动混合地形材质。使用支持透明裁切的 3D 材质，不能直接使用本套不透明地表材质。
- 植被、菌类与矿岩为斜视图，可按现有 HD-2D 道具方式制作受光面片；悬挂根须需把 pivot 调至悬挂位置。所有图集按三列两排组织，切分时保留零散碎石与叶尖。
- 已检查四张新图集的 RGBA 与透明区域。生成预览中的背景 RGB 可能保留颜色；以 alpha 通道和 Unity 实际透明显示为准。透明边缘与比例仍需场景验收。

## 验收

1. 在测试地面与墙体上分别拖入同名材质，确认能正确显示且保留硬边像素。
2. 调整 Tiling 为 2 × 2 或 4 × 4，从实际镜头检查接缝、重复感和与现有 Props 的比例。
3. 切分图集，放一块岩石和一组石笋在现有角色旁，检查透明边缘、遮挡与受光。

生成方式为内置 imagegen；本轮表面贴图完整提示词见 [64px 重绘记录](../../../../docs/cave-textures-64px-prompts.md)，图集原提示词见 [初版生成记录](../../../../docs/terrain-basics-generation-prompts.md)。已检查本轮 PNG 尺寸、色数、不透明性及 `.meta` 未变；未运行 Editor、Play Mode 或构建。
