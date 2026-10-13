# CaveTileWorld 配色像素 tile

按 `PixelCaveTilePreview` 的模块类型和拼接轮廓制作，配色直接取自 `CaveTileWorld` 当前使用的 `CaveTerracedV2` 地形材质。

贴图为地图原始的实际 64×64、8 色 PNG，分别是 earth、rock、moss、wet、stone；Point、Repeat、无 mipmap、无压缩。没有重新生成近似配色。材质复制地图对应的 tint、TextureStrength、TileScale、PixelDensity 和 PaletteSteps；rock_shadow、rock_light 与 rim 仅在对应地图材质的基础上派生明暗变化。

## 模块

共 21 个 prefab：

- 地面四类：earth、stone、moss、wet。
- 连续悬崖五类：fill、edge、outer_corner、inner_corner、double_corner。
- 苔藓悬崖五个对应变体。
- 坡道、台阶。
- 岩壁三类：straight、corner、end。
- 独立岩石两类：low、tall。

网格复用 `PixelCaveTiles`，保持原预览的 4 m 模数和连接端面，带 MeshCollider。悬崖原点在顶部，向下延伸 3.2 m；坡道与台阶上升 1.6 m。预览保留原场景的两段坡道连接方式。材质和贴图是本套独立资源，不修改原地图或其他 tile 的配色。

## 查看和使用

打开 `Assets/Scenes/CaveTileWorldPixelTilePreview.unity`。默认启用 `Assembly Camera`，Game 视图显示连好的泥土/苔藓平台、坡道与岩壁；查看单件时，关闭这台相机，启用 `Tile Library Camera`。两台相机不要同时启用。

新摆模块从本目录拖入地图。材质位于 `Assets/Art/Materials/CaveTileWorldPixelTiles`，贴图位于 `Assets/Art/Textures/CaveTileWorldPixelTiles`。

TileWorldCreator 的两套 dual-grid preset 和连接示例配置位于 `Assets/Configs/CaveTileWorldPixelTiles`。格子尺寸为 4 m；使用配套 earth/moss preset 时，将平台顶部高度按目标位置设置。这些示例配置与原预览配置分开保存。

预览采用 `CaveTileWorld` 的环境光和雾颜色，但局部展示灯与地图不同，最终明暗需要在地图中查看。文件检查覆盖贴图尺寸/色数、原始贴图一致性、材质参数、prefab 和 preset 引用。未运行 Unity 画面及角色通行验收。

原地图、原预览与用户正在编辑的 CaveTileWorld 配置均未覆盖。`python tools/build_cave_tileworld_pixel_library.py` 只用于首次创建；预览已存在时会拒绝重建，保护保存的修改。
