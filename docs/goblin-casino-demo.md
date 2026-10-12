# 哥布林地下赌场场景 demo

场景：`Assets/Scenes/Props/GoblinCasinoDemo.unity`。Unity 2022.3.62f3c1 / URP 14.0.12。

## 打开与查看

1. 打开场景，在 Game 视图或 Play Mode 查看整间赌场；也可使用 `Untitled > Casino > Open Scene Demo`。
2. 在 Scene 视图选择 `03_ThreeGamblingCircles` 或 `04_BossOfficeAndGoldVault`，按 F 查看赌桌、草席、保险箱与账桌。
3. 修改布局时展开 `GoblinCasino_Module_28x28` 的分组。整屋和独立家具预制体位于 `Assets/Prefabs/Props/Casino`。

这是场景美术 demo；哥布林为静态像素示意角色，尚未接入赌博、经营、玩家移动或 NPC 交互。

## 草图对应与陈设

保留草图的三个四面围坐赌局：左上骨骰、左下牌局、右下符文幸运轮，每处均有一张低矮石桌和四张双人草席。右上以石隔墙和西侧入口划出老板房，最右上放铁保险箱，其前方放金条托盘和钱袋。

房间内沿约 28 × 28 世界单位，XZ 是地面，Y 是高度；该尺寸服务独立 demo，不将草图每一格直接等同于新版地图的 2 世界单位格子。桌面约 3 × 3、高 0.72，草席约 3.65 × 1.35。

- 石桌有斜切边、包铁角、铆钉、嵌入的绿布桌面和石面刻纹。草席有编织纹、捆边及流苏。
- 桌上有骨骰、骰杯、标记卡牌、符文轮盘、彩色筹码和金钱堆，赌桌旁放钱袋与地面油灯。
- 保险箱有厚门、包边、合页、铆钉、五辐轮锁及带刻度的密码盘；金条带压印，分层堆在木托盘上。
- 老板房有红毯、抽屉账桌、账本、羽笔墨瓶、骨冠扶手椅、油灯、符文旗、盾饰、箱子及钱袋。
- 北侧兑换柜台用铁栅与交易开口表现现金管理，墙边补充箱桶、等候长凳、草绳袋、柴束、骨堆和碎石，门口至中央铺磨损长毯。
- 壁挂火盆、地面/桌面油灯和两盏铁吊灯提供琥珀色暖光；冷绿灰洞穴补光保持石材与暗角层次。火光复用 `HybridFireLight`。

## 资源与复用

| 内容 | 位置 |
| --- | --- |
| 场景 | `Assets/Scenes/Props/GoblinCasinoDemo.unity` |
| 整屋预制体 | `Assets/Prefabs/Props/Casino/goblin_casino_room.prefab` |
| 13 件赌场实体道具 | `Assets/Prefabs/Props/Casino` |
| 实体网格 | `Assets/Art/Models/Props/Casino` |
| URP 材质 | `Assets/Art/Materials/Props/Casino` |
| 石粒与编织贴图 | `Assets/Art/Textures/Props/Casino` |
| 展示后处理 | `Assets/Settings/GoblinCasinoPresentation.asset` |
| 离线制作源码 | `Assets/Editor/Casino` |

赌场专属模型、石粒和编织纹由本次程序化建模原创生成。箱桶、油灯、火盆、吊灯、木凳、长凳、符文旗、盾、骨堆、柴束、木纹以及像素哥布林直接引用项目现有资源，保留原 GUID；没有导入第三方模型或复制另一套素材。

场景和 prefab 保存所有摆放，运行时不会重建或覆盖手工修改。菜单只打开已保存场景，`Build` 在 demo 已存在时拒绝覆盖。

## 接入 CaveHD2DLocal

此轮交付独立小房间。整屋 prefab 以地面中心为原点，各家具按底部支点放置，可在后续按照新版地图赌场区域的实际 footprint、地面高度与镜头方向调整布局。

相机、全局 Volume 和展示补光属于独立的 `DemoPresentation_CamerasAndFill`，不在整屋 prefab 中；实体灯具和局部灯光随房间保留。融入地图时按地图光照调整强度。

为查看内部，当前没有屋顶，南侧外墙和老板房南墙采用剖面矮墙；其碰撞仍是完整边界。西侧赌场入口和老板房西门为真实墙体空缺。最终屋顶、遮挡与门口通行要在地图融合阶段处理。

## 预览与检查

[赌场全景](images/casino/casino_overview.png) · [老板房与金库](images/casino/casino_boss_vault.png) · [骨骰赌局](images/casino/casino_dice_circle.png) · [12.09° 地图镜头局部预览](images/casino/casino_map_camera12.png)。

12.09° 预览使用新版地图当前的俯角、34° FOV，供观察低镜头下石桌、立体家具和角色的遮挡关系；该图仍在独立 demo 中拍摄，不表示已融入大地图。

2026-10-12：在本项目中完成 Unity C# 编译、场景与预制体保存，重新载入保存场景完成最终渲染检查；确认三个赌局、十二张草席和资源 meta 完整。首轮检查后修正了吊灯遮挡和保险箱偏暗问题。未运行玩家通行、NPC 交互测试或游戏包构建。
