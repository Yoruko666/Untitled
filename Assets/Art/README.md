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

## 赌场 demo 实体素材

- `Models/Props/Casino`、`Materials/Props/Casino`、`Textures/Props/Casino` 保存原创石赌桌、编织草席、骨骰、筹码、卡牌、符文轮、轮锁保险箱、金条、钱袋、老板桌椅、兑换柜台与招牌，共 13 件独立道具。
- 对应 prefab 及整屋模块位于 `Assets/Prefabs/Props/Casino`，展示场景为 `Assets/Scenes/Props/GoblinCasinoDemo.unity`；油灯、火盆、箱桶、哥布林像素角色等引用现有库。
- 模型和布置都已保存，运行时不生成房间。详见 [赌场 demo 说明](../../docs/goblin-casino-demo.md)。

## 训练场 demo 实体素材

- `Models/Props/Training`、`Materials/Props/Training`、`Textures/Props/Training` 保存原创铁笼、战略桌及手绘地图、兵器架、草靶、地垫、沙袋、战术板、装备维修台和训练场门牌，共 9 件独立道具。
- 对应 prefab 与整房模块位于 `Assets/Prefabs/Props/Training`，展示场景为 `Assets/Scenes/Props/GoblinTrainingDemo.unity`。假人、灯具、箱桶、旗饰等直接引用项目素材库。详见 [训练场 demo 说明](../../docs/goblin-training-demo.md)。

## 长老私房 demo 实体素材

- 原创书架、手稿书桌、兽骨祭祀台、衣柜、床头柜、书卷药瓶、骨质权杖、圆形织毯与冥想软垫共 10 件道具；网格、材质和纹理位于 `Art/{Models,Materials,Textures}/Props/ElderRoom`。
- 独立道具与整房 prefab 位于 `Assets/Prefabs/Props/ElderRoom`；场景为 `Assets/Scenes/Props/GoblinElderRoomDemo.unity`。木床放大 2.15 倍；保险箱、灯具和储物陈设复用现有库，东北角保留书柜半遮挡的暗道入口。
- 布局和模型已保存，运行时不重建。详见 [长老私房 demo 说明](../../docs/goblin-elder-room-demo.md)。

## 厨房 demo 实体素材

- 新增转轴炭火烤架、储粮架、粮袋、陶罐、叠碗、厨具挂杆、出餐台、食材木盆、蒜串香草、锅炉石圈和备料推车，共 11 件实体道具；网格、材质与纹理位于 `Art/{Models,Materials,Textures}/Props/Kitchen`。
- 整房模块为 `Assets/Prefabs/Props/Kitchen/goblin_kitchen_room.prefab`，独立场景为 `Assets/Scenes/Props/GoblinKitchenDemo.unity`；现有砧板桌、大锅、吊肉架、水槽、柴堆、箱桶和灯具直接复用素材库。
- 布局与模型均已保存，运行时不重建；详见 [厨房 demo 说明](../../docs/goblin-kitchen-demo.md)。

## 祭祀大厅 demo 实体素材

- 原创祭台、先祖雕像、贡品台模块、三组贡品、火炬、符文石柱、垂旗、仪式鼓、香炉、跪拜垫和骨铃架共 13 件实体道具，网格、材质和纹理位于 `Art/{Models,Materials,Textures}/Props/RitualHall`。
- 整房模块为 `Assets/Prefabs/Props/RitualHall/goblin_ritual_hall_room.prefab`，独立场景为 `Assets/Scenes/Props/GoblinRitualHallDemo.unity`；其余箱桶、油灯、坐垫、供品食材、药瓶与兽骨直接复用素材库。
- 当前 demo 的火炬、旗帜、壁炬、图腾、仪式挂架、贡品、侧边草席及箱桶提灯共 49 处实例已换为项目像素素材；原制作模型保留为备选。箱桶、铁提灯与金币宝石复用原图封装为 `RitualHall/*_hd2d.prefab`。
- 按用户草图保留北侧祭坛、U 形贡品台、灰色长老跪拜席和左右各五处火炬；祭坛与贡品台之间的真实开口、下行石阶、半开石盖和地下通道预留表达连接长老私室的暗道。
- 所有模型与摆放均已保存，运行时不重建。参考图在 `Art/References/RitualHall`，详见 [祭祀大厅 demo 说明](../../docs/goblin-ritual-hall-demo.md)。

## 本地参考资源

- 八方旅人资源库仅供本地参考，位于 `Models/Octopath`，目录及其 `.meta` 已加入 Git 忽略规则，不作为项目共享资源或运行依赖。
