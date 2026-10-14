# Pixel Cave tilesets

四个 PixelCave 预览场景对应的 63 个原有 Prefab 已注册为 47 个 TileWorldCreator TilePreset，每个条目带实际模型缩略图。TWC 的 Tile Presets → Select 中点击 `PixelCave` 分类，或者搜索 `pixel_cave`、`pixel_cliffs`、`pixel_rock_cliffs`。

## 选择和绘画

| 类型 | 数量 | Use Dual Grid | 用法 |
| --- | --- | --- | --- |
| `pixel_cliffs_earth`、`pixel_cliffs_moss` | 2 | 开启 | 完整五种模块，按邻接自动生成悬崖边缘和转角 |
| `pixel_rock_cliffs_240`、`pixel_rock_cliffs_480` | 2 | 开启 | 完整五种模块，连续岩体深度分别为 2.4 m、4.8 m |
| `pixel_cave_floor_*` | 6 | 关闭 | 普通网格铺地，每个格子使用相应地面模块 |
| `pixel_cave_stamp_*` | 37 | 关闭 | 单件墙体、洞口、通道、桥、坡道、台阶和岩石模块 |

在 Tiles Build Layer 中指定 Blueprint Layer，再选择一个 preset。上述模块按世界尺寸制作，基础格子为 4 m，通常关闭 `Scale Tiles To Cell Size`，保持模型原比例。悬崖原点在顶部，Y Offset 决定顶面高度；墙体原点在底部。已有自动拼接 preset 保留原来的资源位置与 GUID，现有引用仍有效。

`stamp` 条目的 standard-grid 形状槽位都引用同一个 Prefab，用于把指定模块加入绘画库，不提供新的转角或交叉口模型。TWC 仍会根据格子邻接旋转它们。坡道、洞口和桥头等方向性模块，建议使用独立 Blueprint Layer、单独绘制，并检查最终方向；需要精确旋转、拼接或高差时直接摆放原 Prefab，或使用 Objects Build Layer。8 m 的长坡道、大洞口及转弯通道不能按 4 m 格子密铺，否则会重叠。勿把这些 stamp 当成完整自动邻接套装。

多个不同 preset 放在同一个 Build Layer 的 Tile Presets 列表中会按权重随机选择。手动控制区域类型时，为每种地面或模块建立独立的 Blueprint / Build Layer。

## 预览和刷新

缩略图与带名称的总览图位于 `Assets/Art/Previews/PixelCaveTilesets/`，总览为 `pixel_cave_tilesets_overview.png`。

更新模型后使用菜单 `Untitled → Cave → Refresh Pixel Cave Tilesets and Previews` 重新注册和渲染缩略图。退出 Play Mode 后执行；工具使用独立预览 Scene，不重建四个源场景或现有地图。执行 `python tools/build_pixel_cave_tileset_contact_sheet.py` 可更新总览图。
