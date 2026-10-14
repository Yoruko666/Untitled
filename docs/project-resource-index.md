# 提交、场景与美术资源索引

整理日期：2026-10-14。这里只记录仓库中的资源与工具，不据此认定玩法已完成或场景切换已接通。

## 提交整理

同步前 `main` 比 `origin/main` 多 11 个提交，远端没有新增提交。保留这些提交的顺序与历史，不重写已发布提交。本次新增内容按第三方资源、tile 目录、细化地图资源、场景与配置、文档分别提交。

| 提交 | 内容 |
| --- | --- |
| `41184337` | feat(art): remake goblin cave tile textures at 64px |
| `825b3b60` | feat(art): add pixel cave riverbank bridge and elevation connector tiles |
| `250dfaa8` | feat: build pixel cave TileWorld settlement map |
| `4c7f6fae` | feat(art): match cave tile colors to terraced terrain map |
| `8b4aee79` | feat(art): add cave rock walls entrances and hollow tunnel tiles |
| `e0df9379` | feat(art): add connected stratified rock tiles and assembled preview |
| `3b4f60a2` | feat(art): create pixel tile library using CaveTileWorld palette |
| `52b7ff60` | chore: install MCP for Unity 10.3.0 |
| `4201c017` | feat: import goblin cave into playable standalone scene |
| `3d1c1c39` | refactor(editor): share demo procedural mesh geometry |
| `fb7b282f` | docs: record architecture review and geometry compatibility checks |

## 场景入口

Build Settings 当前仅启用 `Develop.unity`。其余场景需要在 Editor 中单独打开。

四个主场景按用途放在 `Scenes/Development` 与 `Scenes/Gameplay`，移动时保留原 GUID。其他实验、演示与预览场景保持原位置。

主场景以 `SceneRoot` 为唯一根对象，直属分组按 `Systems`、`Cameras`、`World`、`Lighting`、`UI` 排序，仅保留有内容的分组。系统和场景绑定归 `Systems`；独立相机归 `Cameras`；地图、环境和角色归 `World`；独立灯光及 Volume 归 `Lighting`；Canvas 和 EventSystem 归 `UI`。

已有 Prefab 内部结构保持不变：玩家内部相机、环境内部灯光不单独移出。`Adventure_CaveRuntime` 整体放在 `Systems`，其玩家子对象仍由洞穴导入工具管理。分组使用单位 Transform，重挂对象保留世界变换和现有组件引用。相关 Editor 导入、组装及布局工具使用新路径，并支持在分组内查找禁用对象。

| 场景 | 用途 / 状态 |
| --- | --- |
| `Assets/Scenes/Gameplay/Adventure.unity` | 冒险玩法场景 |
| `Assets/Scenes/Gameplay/Bussiness.unity` | 经营玩法场景 |
| `Assets/Scenes/Gameplay/Cave.unity` | 洞穴地图入口 |
| `Assets/Scenes/CaveHD2D.unity` | 洞穴方案 / 地形实验场景 |
| `Assets/Scenes/CaveHD2DLocal.unity` | 洞穴方案 / 地形实验场景 |
| `Assets/Scenes/CaveStealthPlan.unity` | 洞穴方案 / 地形实验场景 |
| `Assets/Scenes/CaveTerracedTerrain.unity` | 洞穴方案 / 地形实验场景 |
| `Assets/Scenes/CaveTerracedTerrainV2.unity` | 洞穴方案 / 地形实验场景 |
| `Assets/Scenes/CaveTileWorld.unity` | 已保存的 TileWorld 洞穴地图，本次保存其配置状态 |
| `Assets/Scenes/CaveTileWorldHD2D.unity` | 洞穴方案 / 地形实验场景 |
| `Assets/Scenes/CaveTileWorldPixelTilePreview.unity` | 素材 / 道具展示场景 |
| `Assets/Scenes/Development/Develop.unity` | 开发测试入口 |
| `Assets/Scenes/GoblinCave200.unity` | 已保存的独立洞穴场景，见此前导入提交 |
| `Assets/Scenes/PixelCaveConnectedRockPreview.unity` | 素材 / 道具展示场景 |
| `Assets/Scenes/PixelCaveConnectorPreview.unity` | 素材 / 道具展示场景 |
| `Assets/Scenes/PixelCaveTilePreview.unity` | 素材 / 道具展示场景 |
| `Assets/Scenes/PixelCaveTileWorld.unity` | 像素 tile 组装地图 |
| `Assets/Scenes/PixelCaveWallPreview.unity` | 素材 / 道具展示场景 |
| `Assets/Scenes/Props/CaveDetailGallery.unity` | 素材 / 道具展示场景 |
| `Assets/Scenes/Props/CaveModuleGallery.unity` | 素材 / 道具展示场景 |
| `Assets/Scenes/Props/ContainerComparison.unity` | 素材 / 道具展示场景 |
| `Assets/Scenes/Props/GoblinCasinoDemo.unity` | 素材 / 道具展示场景 |
| `Assets/Scenes/Props/GoblinElderRoomDemo.unity` | 素材 / 道具展示场景 |
| `Assets/Scenes/Props/GoblinKitchenDemo.unity` | 素材 / 道具展示场景 |
| `Assets/Scenes/Props/GoblinRitualHallDemo.unity` | 素材 / 道具展示场景 |
| `Assets/Scenes/Props/GoblinTavernDemo.unity` | 素材 / 道具展示场景 |
| `Assets/Scenes/Props/GoblinTrainingDemo.unity` | 素材 / 道具展示场景 |
| `Assets/Scenes/Props/HybridPropGallery.unity` | 素材 / 道具展示场景 |
| `Assets/Scenes/tile.unity` | 手工 TileWorld 测试场景，引用下表中的根目录迁入配置 |

`Map.unity` 与 `WhiteBox.unity` 沿用本次整理前的删除状态，相关历史说明仍保留。`CaveTileWorldRefined.unity` 当前不存在；细化版资源与创建菜单见 [细化版说明](cave-tileworld-refined.md)。

## 美术与配置

| 位置 | 内容 |
| --- | --- |
| `Assets/Art/Textures/` | 地形、道具与像素洞穴贴图 |
| `Assets/Art/Models/`、`Assets/Art/Materials/` | 地形 / 道具模型、合并 mesh 与对应材质 |
| `Assets/Art/References/` | 场景与美术参考图 |
| `Assets/Prefabs/Terrain/PixelCaveTiles/` | 基础地面与 dual-grid 台地模块 |
| `Assets/Prefabs/Terrain/PixelCaveConnectors/` | 坡道、桥面、河岸和高差连接模块 |
| `Assets/Prefabs/Terrain/PixelCaveWalls/` | 岩壁、入口与隧道模块 |
| `Assets/Prefabs/Terrain/PixelCaveConnectedRocks/` | 连续层状岩体模块 |
| `Assets/Configs/PixelCaveTilesets/` | TileWorldCreator 素材目录与筛选文件 |
| `Assets/Art/Previews/PixelCaveTilesets/` | 素材缩略图及 `pixel_cave_tilesets_overview.png` 总览 |
| `Assets/Art/Models/CaveTileWorldRefined/`、`Assets/Art/Materials/CaveTileWorldRefined/`、`Assets/Prefabs/Terrain/CaveTileWorldRefined/`、`Assets/Configs/CaveTileWorldRefined/` | 细化地图生成物；场景尚未保存 |
| `Assets/Configs/CaveTileWorld/Map_Manual.asset` | 从 Assets 根目录迁入的手工地图配置，保留 GUID |
| `Assets/Configs/CaveTileWorld/TileWorldCreatorConfiguration.asset` | 从 Assets 根目录迁入的测试场景配置，保留 GUID |
| `docs/images/` | 文档截图与布局参考，不作为运行时地图数据 |

素材目录刷新入口：`Untitled/Cave/Refresh Pixel Cave Tilesets and Previews`。离线总览拼图工具：`tools/build_pixel_cave_tileset_contact_sheet.py`。整理操作没有执行地图重建。

## 第三方资源与检查范围

TileWorldCreator 与 StylizedWater2 保留供应商目录，本次将此前未提交的材质、模型、预制体、示例和说明纳入版本管理，不升级依赖。`Samples_URP.unitypackage` 及其 `.meta` 为本地归档，不提交。Octopath 本地参考库继续忽略。

已检查新增 Unity 资产的 `.meta` 配对、GUID 重复、资源引用及文件大小；URP 等 Package 引用结合 `Library/PackageCache` 判断。自有新增资源未发现无法解析的外部 GUID。第三方示例仍存在导入时遗留的缺失引用：StylizedWater2 的 WaterRipple 材质、ParticleShowcase / RiverDemo，以及 TileWorldCreator 的部分 2.5D、Cliff、Water 材质。这些示例资源尚不能视为完成验证；本次保留原内容，不猜测替换贴图或 shader。

本次未执行 Unity 编译、Play Mode 或完整构建。建议人工打开 `tile.unity` 检查配置迁移后的引用，打开 `CaveTileWorld.unity` 检查地图，并在 TileWorldCreator 中查看像素 tile 目录和缩略图。
