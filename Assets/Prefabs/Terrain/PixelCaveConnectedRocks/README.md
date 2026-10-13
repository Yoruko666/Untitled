# 连续分层岩体

直接沿用 `PixelCaveTilePreview` 的岩石边界和分层轮廓，仅调整网格高度。不是用独立方块近似岩石连接。

## 内容

- 2.4 m、4.8 m 两套岩体，每套有 fill、edge、outer_corner、inner_corner、double_corner 五种 dual-grid 模块。
- 4.8 m 自然岩壁 straight、corner、end 三个模块。
- TileWorldCreator dual-grid presets：`Assets/Configs/PixelCaveConnectedRocks/pixel_rock_cliffs_240.asset` 和 `pixel_rock_cliffs_480.asset`。
- `Assets/Scenes/PixelCaveConnectedRockPreview.unity`：两片真正拼接的岩体、一段连续 U 形岩壁，以及单件模块展示。

所有模块带 MeshCollider，复用已经匹配地图配色的 64×64、16 色岩石材质。网格的水平尺寸、边界顶点、分层连接关系与原预览一致，shader 的世界像素密度保持不变。

## 摆放

岩体的原点在顶部，网格向下延伸 2.4 m 或 4.8 m。要让岩体从 Y=0 升起，就把它放到 Y=2.4 或 Y=4.8；使用 TileWorldCreator 时格子尺寸为 4 m，选择对应 preset，并设置目标顶部高度。同一片岩体使用相同高度的完整模块组。

自然岩壁原点在底部，高 4.8 m。直段沿 X 每 4 m 拼接，转角和端头按各自端面对齐。预览场景已经提供实际摆放。岩壁厚度约 1.2 m，与 `PixelCaveWalls` 中较厚的洞口不同，接洞口时用凸岩处理厚度变化。

现有地图未自动改动。预览中的对象为可编辑的保存实例，网格与材质引用对应这套资源；要在地图中新放模块，从本目录拖入 prefab。

## 验证

检查了预览里 125 对相邻边界的各层高度与端面范围，以及连续岩壁的直段、转角连接。两套 preset 的 prefab 引用已核对。Unity 实际贴图、阴影和角色通行仍需人工确认。

`python tools/build_pixel_cave_connected_rocks.py` 可重建这些网格、prefab 和 preset，并保留已有 GUID。原始 PixelCaveTiles 和地图场景不会被重写。
