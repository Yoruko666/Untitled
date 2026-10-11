# CaveHD2D 三级台地基础地图

打开 `Assets/Scenes/CaveHD2D.unity`，点击 Play，使用原有移动输入探索。当前是按三级哥布林聚落草图搭建的可编辑基础地图，使用现有道具表现生活分区，尚未增加 NPC、生产、经营或攀爬玩法。原 `Cave` 场景保留。

## 布局与路线

- **左侧河岸低地，Y = -1.2**：弯折的地下河、种植床、取水设施和工坊。河床再下沉 0.42，水面下沉 0.10；河道由岸边岩石阻挡，通过木桥通行。
- **中央生活主层，Y = 0**：入口、公共火塘、厨房与宴会、酒馆、沿右侧岩壁聚集的住区、靠入口的干燥仓库和训练区。两条宽坡连接河岸，生活区不再由独立矩形房间串接。
- **后方高台，Y = +2.4**：首领图腾、石台和长老住处。右侧折返坡道分两段上升，中间有转弯平台；首领台座额外抬高 0.36，三级短阶每级 0.12。

主路线用坡道连接，不需要跳跃或爬梯。角色沿用两倍显示尺寸，场景实例启用原有重力，CharacterController 的 Step Offset 为 0.18。

## 编辑与重建

- `CaveMap/LayoutGrid` 的 Floor/River Tilemaps 保存平面布局；`CaveMap` 上的 `CaveElevation` 保存各格四角高度、地面材质分区和桥面通行标记。坐标仍为 XZ，高度使用 Y，当前每格一个世界单位。
- `CaveMap/Geometry` 保存按 16 格区块生成的地面、岩体、台地立面、河床、水面与碰撞。高度和碰撞使用相同的已保存数据，runtime 不重新生成地图。
- `CaveMap/Architecture` 保存木桥、床台、首领台阶等组装；`LevelRoot/PlacedProps` 按生活区分组，保持 Prefab 关联，支持手工摆放。

普通 `Untitled → Cave → Build HD2D Scene` 及保存场景时的同步只刷新派生地形，保留手工布局、高度和物件。修改高度后重建即可。新增 Floor 格未设置高度时默认落在生活主层；删除 Floor 格后该格保存的高度记录不产生地形。

`Untitled → Cave → Replace With Terraced Settlement Layout` 是明确的布局替换操作：恢复本版初始三级台地，并重新摆放区域道具。已有手工修改时不要使用它。`Apply HD2D Scene Lighting` 会重设场景光照，手工调光后不必重复执行。

场景沿用固定倾斜透视镜头、现有像素道具材质与墙体遮挡透视。地形和资源依据见 [HD-2D 场景开发经验](hd2d-scene-development.md)。Tilemap 批量写入采用 [SetTiles](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Tilemaps.Tilemap.SetTiles.html)；角色移动沿用 [CharacterController.Move](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/CharacterController.Move.html)，重力由原有 PlayerEntity 积分。

## 检查与人工试玩

已检查 Unity 编译、场景保存重载、代表性地面与坡道碰撞高度，并在 Play Mode 检查河岸坡道、木桥和折返上坡的往返通行。未执行完整构建。

人工试玩关注三项：从入口观察火塘与分区；走过河岸坡道、木桥并登上高台；检查镜头遮挡、亮度与角色比例。此版优先确认空间和路线，岩石造型、生活细节与画面效果等待反馈后继续调整。
