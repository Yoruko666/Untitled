# 哥布林厨房 demo

场景：`Assets/Scenes/Props/GoblinKitchenDemo.unity`。Unity 2022.3.62f3c1 / URP 14.0.12。

## 打开与验收

1. 打开场景，或使用 `Untitled > Kitchen > Open Scene Demo`，在 Game 视图查看全景。
2. 在 Scene 视图选择 `02_LongChoppingTable_ExistingHD2D` 或 `03_CauldronSpitRoastAndMeatRack`，按 F 检查备菜台、大锅和烤肉架。
3. 进入 Play Mode 查看油灯、墙火盆和灶火的轻微明暗变化。

这是可编辑的场景美术 demo，没有接入玩家移动、厨师 AI、烹饪交互或出餐玩法。

## 布局与气氛

内部约 16 × 16 世界单位，地面使用 XZ，Y 为高度。按参考图 05 厨房的大致关系，将最长的砧板桌作为中央偏北的备菜工作台，大锅紧邻其东侧。西侧和南侧各留一个入口，便于之后接入大地图道路。这个方形 footprint 用于独立预览，融合时按房间边界微调。

- 备菜：保留 `Kitchen/chopping_table_hd2d` 的原始八方向素材与 prefab 引用，以非等比缩放延长桌面；当前正面朝南。旁边放食材木盆、粮袋、木凳和带木支架的油灯。
- 炖煮与烤肉：复用 `Kitchen/cauldron_b_hd2d`，放大为 2.65 倍，底下补充石圈炭火；北侧设置实体转轴烤肉架、烤肉、摇柄、铁架、炉箅和炭床。吊肉架、小灶、柴堆和散落木柴直接复用素材库。
- 储粮：三层粮架放置粮袋、陶罐和调料标签；背墙悬挂蒜串、洞穴香草及菜刀、铁锅、长柄勺。西南角设置收货箱垛、根茎菜、洋葱和蘑菇木盆。
- 清洗：西侧石水槽、备用木桶、木水桶、用过的碗和水渍石板。
- 出餐：南侧粗木台放叠碗、炖菜、调料罐和油灯，下层继续储放器皿；东侧增加木桶、长凳和储物箱；中部偏西设置带轮备料推车，放置生肉、菜刀、炖菜、油灯及下层储物箱。
- 地面与墙边：砧板桌操作垫、推车及出餐台工作垫、炉边煤灰、碎石、苔藓与散落草屑，形成使用痕迹；中央保留工作与搬运空间。

十二处局部光源包括大锅灶火、烤架炭火、小灶自带火光、三处墙火盆和六盏油灯。火光集中在烹饪与工作节点，冷灰绿环境补光维持地下洞穴层次。展示用补光、相机与后处理不包含在整房 prefab 中。

## 资源位置

| 内容 | 位置 |
| --- | --- |
| 独立场景 | `Assets/Scenes/Props/GoblinKitchenDemo.unity` |
| 整房模块 | `Assets/Prefabs/Props/Kitchen/goblin_kitchen_room.prefab` |
| 原创可复用道具 | `Assets/Prefabs/Props/Kitchen` |
| 保存的网格 | `Assets/Art/Models/Props/Kitchen` |
| 材质、石材与织物纹理 | `Assets/Art/Materials/Props/Kitchen`、`Assets/Art/Textures/Props/Kitchen` |
| 展示后处理 | `Assets/Settings/GoblinKitchenPresentation.asset` |
| 离线建模及组装代码 | `Assets/Editor/Kitchen` |

新增 11 件实体道具：转轴炭火烤肉架、粮架、开口粮袋、带盖调料罐、叠碗与炖菜、厨具挂杆、出餐木台、蔬菜蘑菇木盆、蒜串与香草、铁锅石圈火床、备料推车。网格由项目内 Editor 离线建模生成，直接保存为资源；既有砧板桌、大锅、水槽、吊肉架、柴堆、蔬菜、桌凳、箱桶和灯具继续引用原素材。没有第三方下载素材。

整房 prefab 原点为房间地面中心，分区保留独立层级。场景和 prefab 是摆放的权威来源，运行或加载时不会重建；`Build` 拒绝覆盖已有 demo。后续可以直接编辑保存的 prefab 或将它拖入新地图，再调整房间边界和出入口。

当前阶段以 `CaveHD2DLocal.unity` 的 05 区域为设计参考，独立制作房间模块。额外渲染采用该地图启用的 Cinemachine 镜头设置：12.09° 俯角、34° FOV、24.6 距离。正式融合时仍需调整实际地形、遮挡、通行和整体灯光。

## 预览与检查

[全景](images/kitchen/kitchen_overview.png) · [备菜与烤肉](images/kitchen/kitchen_cooking_line.png) · [储粮和清洗](images/kitchen/kitchen_pantry_and_washing.png) · [新版地图低俯角](images/kitchen/kitchen_map_camera12.png)。

2026-10-12：已通过 Unity C# 编译、场景及 prefab 保存、重新载入的网格与材质引用检查，以及全景、烹饪区、储粮清洗区和新版地图低俯角的实际相机渲染检查。根据第一版预览补充了中央备料推车及工作灯，最终重新载入确认 12 处局部光源及原有砧板桌、大锅引用完整。未执行玩家移动、烹饪交互或整包构建验证。
