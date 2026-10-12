# 哥布林地下训练场场景 demo

场景：`Assets/Scenes/Props/GoblinTrainingDemo.unity`。Unity 2022.3.62f3c1 / URP 14.0.12。

## 打开与查看

1. 打开场景或使用 `Untitled > Training > Open Scene Demo`，在 Game 视图查看全景。
2. 在 Scene 视图选择 `03_ThreeIronCagesAndGuardLane` 或 `04_CommandersStrategyOffice`，按 F 查看铁笼与战略桌。
3. 展开 `GoblinTraining_Module_36x28` 的分组调整布局；独立道具及整房 prefab 位于 `Assets/Prefabs/Props/Training`。

这是场景美术 demo，没有放置 NPC，也没有接入战斗、训练、囚犯或玩家移动逻辑。Play Mode 只运行现有灯具火光组件。

## 布局与氛围

按提供的俯瞰草图安排左侧囚笼区、相邻的左上战略办公室及右侧主训练区。内部约 36 × 28 世界单位，XZ 为地面，Y 为高度。这个 footprint 服务独立 demo，不将草图格子直接换算为新版地图的 2 单位格子。

- 左侧连续三个约 4.7 × 4.25、高 4.0 的实体铁笼，门朝东侧巡查通道；门框、细铁栏、顶部横栏、合页、铆钉、锁孔、交错链环、镣铐、笼内木板及草屑均为实体网格。三个笼位带木牌刻号，周边有箱子、木凳及油灯。
- 办公室用石隔墙围出，东侧保留敞开的门洞。战略桌位于房间左上，约 4.05 × 2.7、高 1.52；桌上有手绘洞穴地图、两色旗棋、卷轴、账册和墨瓶，旁边放油灯。骨冠椅、访客凳、战术黑板、符文旗、盾饰、装备小桌、箱垛与绿毯补全军队首领的工作空间。
- 主区复用现有八方向假人 prefab，共八个，分上下两排，每处有磨损地垫和练习站位线。中央为沙土搏斗圈；东侧有两个插箭草靶，西侧补充环形训练器械与两道木障碍。
- 边缘安排长矛和盾架、头盔及斧头维修台、缝线沙袋、补给木箱、木桶、柴束、长凳、水槽和兽骨图腾。少量草屑及碎石集中在墙边；中央圈和房间入口保留空隙。
- 壁挂火盆、地面火盆、桌面油灯与两盏铁吊灯形成局部琥珀色光池；冷绿灰补光维持地下石室的暗部层次。局部火光复用 `HybridFireLight`。展示镜头的全局补光与后处理单独保存。

## 素材与项目位置

| 内容 | 路径 |
| --- | --- |
| demo 场景 | `Assets/Scenes/Props/GoblinTrainingDemo.unity` |
| 整房 prefab | `Assets/Prefabs/Props/Training/goblin_training_room.prefab` |
| 9 件专用道具 prefab | `Assets/Prefabs/Props/Training` |
| 实体模型 | `Assets/Art/Models/Props/Training` |
| 材质 | `Assets/Art/Materials/Props/Training` |
| 石粒、编织纹与手绘战略图 | `Assets/Art/Textures/Props/Training` |
| 展示后处理 | `Assets/Settings/GoblinTrainingPresentation.asset` |
| 离线制作源码 | `Assets/Editor/Training` |

专用道具：`riveted_iron_cage_3d`、`war_strategy_table_3d`、`spears_and_shields_rack_3d`、`straw_archery_target_3d`、`worn_dummy_training_pad_3d`、`stitched_sandbag_stack_3d`、`chalk_tactics_board_3d`、`armour_and_axe_workbench_3d`、`crossed_axes_training_sign_3d`。

这些模型与新贴图为程序化原创建模；假人、符文旗、盾饰、环形器械、兽骨图腾、油灯、火盆、吊灯、木箱、木桶、水槽、长凳、木凳、草床、柴束、木纹和骨冠椅直接引用现有素材，保留原 GUID。没有引入外部模型。

场景与 prefab 保存实际模型、材质、局部灯光和摆放，运行时不生成房间。菜单优先打开保存场景，`Build` 拒绝覆盖已存在的 demo，后续调整直接编辑场景或 prefab。

## 后续融入新版地图

当前交付独立房间模块；`CaveHD2DLocal.unity` 本轮未修改。整房 prefab 以地面中心为原点，地表约 Y=0；后续根据地图军营／训练区域的地面高度、实际轮廓和门口位置调整尺寸及出口。

`DemoPresentation_CamerasAndFill` 中的相机、全局 Volume 和展示补光不包含在房间 prefab 中；实体灯具及局部 Light 随房间保留。融入地图时使用地图原有相机和全局后处理，并调整局部火光强度。

目前采用无屋顶剖面，南侧外墙与办公室南墙降低，以便检查内部。边界及隔墙仍有完整高度的碰撞；南侧主入口、东侧出口、笼区巡查通道入口及办公室东门为实际墙体空缺。铁笼以完整 BoxCollider 阻挡，笼门尚不具备交互。最终屋顶、遮挡处理及角色通行需在融合阶段微调。

## 预览与检查

[全景](images/training/training_overview.png) · [铁笼细节](images/training/training_iron_cages.png) · [战略办公室](images/training/training_strategy_office.png) · [新版地图低俯角局部](images/training/training_map_camera12.png)。

低俯角图使用新版 `CaveHD2DLocal` 当前的 12.09°、34° FOV、24.6 相机距离；拍摄于独立 demo，供观察假人、立体家具及灯具的遮挡关系。

2026-10-12：Unity C# 编译通过，场景、整房与 9 件道具 prefab 保存成功；重新加载已保存场景并完成全景、铁笼、办公室和 12.09° 低俯角渲染检查，确认 3 座铁笼与 8 个假人，并核对资源 GUID 引用及 meta 完整。首轮检查后改善铁笼暗部可读性，并将地砖的规律色差改为不规则分布。未运行玩家通行、战斗、NPC 交互或游戏包构建。
