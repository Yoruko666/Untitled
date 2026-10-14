# 原地图的 tile 完善版

> 2026-10-14 整理状态：生成脚本、配置、材质、Prefab 与合并 mesh 已保存，但仓库当前没有 `Assets/Scenes/CaveTileWorldRefined.unity`。下文描述设计与创建方式，不代表已交付可打开的细化场景。当前已保存地图参见 [资源索引](project-resource-index.md)。

场景：`Assets/Scenes/CaveTileWorldRefined.unity`。以 `CaveTileWorld.unity` 为布局与配色参考，地面、台地、岩体、岩壁、坡道、桥面与水面全部用指定的四套 tile 重新铺设。原角色、摄像机和 debug 设置继承；建筑位置保留为空占位。

地面与台地使用 `PixelCaveTilePreview` 的 dual-grid 模块，连续岩体使用 `PixelCaveConnectedRockPreview`，岩壁使用 `PixelCaveWallPreview`，弯曲坡道与桥面使用 `PixelCaveConnectorPreview`。水面使用 wet tile 配合原图水色。新 prefab variant 沿用原地图材质，四套源素材不被改写。

新场景移除了原地形 mesh、原岩体、原桥面、原房间与旧装饰模型。TileWorldCreator 的 paint layer 根据原图覆盖范围铺设，弯曲坡道用连接 tile 分段拼接；以模块尺寸近似曲线。wet 是河谷的主要地面，moss 集中在原种植与潮湿局部。

`BuildingReservations | original 15 districts` 保留各分区的建筑占位，可在 Scene 中选中对应对象查看尺寸。这里的 BoxCollider 是禁用的编辑轮廓，不阻挡角色。新增岩体和岩壁避开占位与原通道。

打开新场景后，按原地图的方式进入 Play Mode。角色、摄像机和 debug 的操作沿用原场景。

地形配置位于 `Assets/Configs/CaveTileWorldRefined/cave_full_tile_configuration.asset`，与原地图配置独立。场景对象和保存的 TWC paint layer 是后续编辑的布局来源，普通加载与 Play 不自动重建。连接 tile 与建筑占位也要随布局修改保持对应。

创建菜单为 `Untitled/Cave/Create Refined TileWorld Cave`，只创建尚不存在的新场景。场景不增加新玩法或 debug 功能。
