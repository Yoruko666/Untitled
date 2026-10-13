# 哥布林祭祀大厅 demo

场景：`Assets/Scenes/Props/GoblinRitualHallDemo.unity`。Unity 2022.3.62f3c1 / URP 14.0.12。

## 打开与验收

1. 打开场景，或使用 `Untitled > Ritual Hall > Open Scene Demo`，在 Game 视图查看全景。
2. 在 Scene 视图选中 `03_NorthAltar_AncestorIdol` 和 `05_SecretStairwell_BetweenAltarAndOfferings`，按 F 检查祭坛、贡品台与半开石盖下的暗道。
3. 进入 Play Mode 查看两列火炬、油灯与祭坛烛火的轻微明暗变化。

这是可编辑的场景美术 demo；房间已有基础环境碰撞，当前没有接入玩家控制、献祭交互、长老 AI 或跨场景暗道传送。

## 布局与气氛

内部约 26 × 26 世界单位，XZ 为地面，Y 为高度，整房 prefab 原点是地面中心。以新版 `CaveHD2DLocal.unity` 的祭祀大厅区域和用户提供的两张参考图为依据，先制作方形模块供独立预览；正式融合时再微调地形、房间边界、入口位置和高差。

- 北侧祭坛：四级登台石阶、厚石祭台、骨饰与符文嵌条、暗红垂布、贡品碗、蜡烛及带獠牙、骨角的哥布林先祖雕像。
- 围绕祭坛的贡品台：十段实体石台组成 U 形围台，左右各三段、前方四段，中央留口通向祭坛。贡品分为蘑菇根茎与陶罐、兽骨与蜡烛、玉石与金币三组；贡品与石台分别保存为 prefab，方便替换。
- 长老跪拜：围台正前方的灰色石席与灰绿编织跪拜垫、坐垫和香炉；从南入口到跪拜席的暗红旧织毯标明仪式中轴。
- 两列火炬：左右各五个独立石座骨饰火炬，保持参考图的排列关系；入口、供品储藏与暗道补充油灯和墙火盆。
- 暗道入口：在祭坛东侧与贡品台之间留出约 1.8 × 2.8 的真实地面开口，补充八级下行石阶、石衬壁、半开的镶条石盖、拉环与滑动痕迹，底部暖色微光引向东侧地下通道。`FutureConnection_ToElderRoom_UnderfloorEast` 标出之后连接长老私室的预留点。
- 仪式陈设：六根刻符文石柱、两幅先祖符号垂旗、仪式鼓、悬挂兽骨铃架、两侧候拜垫与骨堆；后侧放供品箱垛、粮袋、蘑菇木盆和陶罐，入口两侧放药瓶、卷轴、储物箱与燃料。
- 使用痕迹：墙根碎石与苔藓、火炬座烧灰、祭坛附近蜡滴和磨损石板填充边缘，中央保持宽阔仪式路线。

火光采用暖橙色，低强度玉石光强调先祖雕像；冷灰绿环境反射光维持洞穴层次。南侧及东侧使用低切面墙，便于俯视和新版地图的低俯角镜头阅读。展示相机、补光与全局后处理单独放在场景中，不包含在整房模块中。

## 资源位置

| 内容 | 位置 |
| --- | --- |
| 独立场景 | `Assets/Scenes/Props/GoblinRitualHallDemo.unity` |
| 整房模块 | `Assets/Prefabs/Props/RitualHall/goblin_ritual_hall_room.prefab` |
| 原创可复用道具 | `Assets/Prefabs/Props/RitualHall` |
| 保存的实体网格 | `Assets/Art/Models/Props/RitualHall` |
| 材质与纹理 | `Assets/Art/Materials/Props/RitualHall`、`Assets/Art/Textures/Props/RitualHall` |
| 用户提供的平面草图和补充图 | `Assets/Art/References/RitualHall` |
| 展示后处理 | `Assets/Settings/GoblinRitualHallPresentation.asset` |
| 离线建模与组装代码 | `Assets/Editor/RitualHall` |

新增 13 件实体道具：祭台、先祖雕像、贡品台模块、三组贡品、石座骨饰火炬、符文石柱、先祖垂旗、仪式鼓、香炉、跪拜垫、骨铃架。网格通过项目内 Editor 离线建模保存，使用倒角、分面石材、器皿内壁、金属箍、骨饰、缠绳和织物纹理补充细节。其余坐垫、权杖、骨堆、油灯、墙火盆、箱桶、食材、药瓶、卷轴与柴束复用现有素材库，没有下载第三方素材。

场景和 prefab 是摆放的权威来源，运行时不重建。`Build` 拒绝覆盖已有 demo；后续直接编辑保存的 prefab，或将整房模块拖入新地图再调整。本次独立 demo 不改写 `CaveHD2DLocal.unity`。

## 预览与检查

[全景](images/ritual-hall/ritual_hall_overview.png) · [祭坛与贡品台](images/ritual-hall/ritual_hall_altar.png) · [暗道石阶](images/ritual-hall/ritual_hall_secret_stair.png) · [新版地图低俯角](images/ritual-hall/ritual_hall_map_camera12.png)。

低俯角预览采用新版地图当前启用的 Cinemachine 设置：12.09° 俯角、34° FOV、24.6 距离。相机焦点为大厅内部局部区域；全景相机只用于房间模块展示。暗道到长老私室的真实长度、高差及通行净高需要在正式融合时结合地形调整。

2026-10-13：已通过 Unity C# 编译、场景和整房 prefab 保存及重新载入、模型与材质引用检查、十处火炬和十段贡品台数量检查，以及全景、祭坛、暗道和新版地图低俯角的实际相机渲染。最终确认 20 处局部光源；暗道射线命中下行石阶而非地面或横穿的侧墙。根据首版预览修正了通道侧墙穿过石阶的问题、加深下行通道并略微提亮环境补光。尚未进行玩家通行、仪式交互、跨区连接或整包构建验证。
