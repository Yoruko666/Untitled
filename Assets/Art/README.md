# 美术资源

## 目录

| 目录 | 内容 |
| --- | --- |
| `Sprites/Player.gif` | 玩家图像 |
| `Sprites/goblin.png` | 沿用 Godot 项目的哥布林像素原图，第一步测试房间使用 |
| `Sprites/Terrain/Cave` | 洞穴地面、墙面与地形图集 |
| `Sprites/Props/Kitchen` | 铁锅、柴火、食材、水槽、工作台、晾肉架 |
| `Sprites/Props/Camp` | 帐篷、草床、木刺、训练假人 |
| `Sprites/Props/TotemsBones` | 旗帜、兽骨、仪式木架、木盾 |
| `Sprites/Props/Lighting` | 支架火把、火盆、壁挂火把、铁提灯 |
| `Sprites/Props/Storage` | 钱袋堆、木箱八视角图集 |
| `Sprites/Props/Treasure` | 金币堆、金条堆、金宝石堆、财宝堆 |
| `Sprites/Props/Tavern` | 酒箱、吧台、酒桶、酒桌、木凳、啤酒杯 |
| `References/Kitchen` | 厨房道具参考图 |
| `Materials` / `Models` / `Textures` / `Audio` | 材质、模型、纹理与音频 |

## 命名

使用小写英文 `snake_case`，文件名描述物件内容。

- `_sheet`：多个物件或视角的图集；`_8view_sheet`：八视角图集。
- `_tileset`：地形组合图。
- `_crop`：裁剪素材，可能包含其他视图或边缘残片。
- `_reference`：参考图。
- `_01`、`_02`：同类物件的不同变体。

## 使用注意

- PNG Sprite 当前为 `Single`、`Point` 过滤、关闭 mipmaps、无压缩，PPU 为 `150`，pivot 为底部居中，mesh 为 `Full Rect`。
- `goblin.png` 为例外：PPU 为 `32`，pivot 位于底部中央的实际脚底（Y = 1/26）。`Models/GoblinBillboard.asset` 与其像素尺寸对应，`Materials/FirstRoom/GoblinLit.mat` 使用 URP Lit / Alpha Clipping 提供 3D 受光、透明裁切和深度遮挡。
- 图集尚未切割。需要独立物件或视角时，使用 Sprite Editor 切割，并按实际内容确认裁切范围与方向顺序。
- 仓储、财宝、酒馆与 `Lighting/iron_lantern.png` 共 14 张新增透明 PNG；三个八视角图集为 272 × 34，`ale_crate_front_back_sheet.png` 为正反面图集。它们可作为 Sprite 使用，尚未制作可放置道具 Prefab。
- 部分小图带有边缘残片；放入场景前检查显示范围，按物件需要调整 PPU 与 pivot。
- `References` 中的 JPG 保留白底，作为参考图使用。


## 可放置道具

- 预制体位于 `Assets/Prefabs/Props/{Kitchen,Camp,TotemsBones,Lighting}`，共 41 个。
- 原生网格位于 `Models/Props`，材质位于 `Materials/Props`，按同一类别组织。`_3d` 为立体容器，`_hd2d` 为受光像素面片。
- 复用现有 30 张像素原图与厨房参考图，PNG 的现有 Sprite 导入设置保持不变。JPG 白底仍保留，HD-2D 材质通过 Shader 裁切使用完整轮廓。
- Unity 菜单 `Untitled/Props/素材浏览器` 可看原图、预览角度、调大小和放入场景；使用说明与逐项索引见 [可放置道具](../../docs/placeable_props.md)。
