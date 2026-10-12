# 哥布林长老私房 demo

场景：`Assets/Scenes/Props/GoblinElderRoomDemo.unity`。Unity 2022.3.62f3c1 / URP 14.0.12。

## 打开与验收

1. 打开场景，或使用 `Untitled > ElderRoom > Open Scene Demo`，在 Game 视图查看全景。
2. 在 Scene 视图选择 `02_NorthEastSecretPassage`，按 F 检查床后方的暗道门洞和移开的书柜。
3. 展开 `04_NorthEastSleepingAlcove/LargeBedReplacementSocket` 检查木床；后续大床素材替换这个节点内的床即可。

这是可编辑的场景美术 demo。Play Mode 可查看现有火光组件；没有接入玩家移动、NPC、保险箱交互或书柜开门动画。

## 布局与氛围

内部约 18 × 18 世界单位，XZ 为地面，Y 为高度。按照草图保留左下主入口、右上寝区以及背后暗道。这个 footprint 用于独立 demo，后续按新版地图房间边界调整。

- 左侧书房：两座实体书架、带手稿的书桌、骨冠椅、访客凳、卷轴和权杖。书架有多色书脊、金属束带、羊皮标签、骨饰与封顶符纹；书桌有抽屉、拉环、金属护角、摊开的手稿、书签和羽毛笔。
- 右上寝区：原有 `Camp/wooden_bed_hd2d` 木床放大为 2.15 倍，独立替换节点保留原素材引用；补充床头柜、油灯、衣柜、软垫和箱子。
- 北侧祭祀台与中央冥想区：兽骨圣物、蜡烛、铜制香碗、符纹台布、圆形织毯和三个软垫。
- 右下金库：直接复用赌场的铆钉保险箱、金条堆和钱袋，安排独立地毯、储物箱和清点卷册。
- 左下会客与药材区：圆桌、木凳、药瓶、书卷和油灯；墙边补充储物架、箱垛、水槽、木桶及长凳。中部保留连接各区的空隙。
- 暖色油灯、墙火盆和一盏小吊灯提供局部光池，冷灰绿补光维持地下石室层次。

## 暗道

东北北墙实际断开，留出约 2.8 单位宽的门洞，并向后延伸约 4.3 单位的短走廊。书柜稍微向西移开、略带角度，遮住门洞西侧，东侧保留约 1.3 单位的入口。浅色磨痕和书柜底部滑动痕迹作为微弱线索，通道内以低强度冷光提示深度。

`ConcealingBookcase_PartiallySlidAside` 是静态演示姿态。走廊末端是 demo 边界，融入大地图时需延长通道并移除末端封墙。入口没有跨洞的墙体碰撞盒；书柜仍有自身碰撞。

## 资源位置

| 内容 | 位置 |
| --- | --- |
| 独立场景 | `Assets/Scenes/Props/GoblinElderRoomDemo.unity` |
| 整房 prefab 与 10 件原创道具 prefab | `Assets/Prefabs/Props/ElderRoom` |
| 实体网格 | `Assets/Art/Models/Props/ElderRoom` |
| URP 材质与石材、织物纹理 | `Assets/Art/Materials/Props/ElderRoom`、`Assets/Art/Textures/Props/ElderRoom` |
| 展示后处理 | `Assets/Settings/GoblinElderRoomPresentation.asset` |
| 离线建模与场景组装源码 | `Assets/Editor/ElderRoom` |

新增模型与纹理由本项目离线程序化建模生成；木床、保险箱、灯具、桌凳、钱袋等直接复用项目资源。没有第三方下载素材。

整房 prefab 以房间地面中心为原点。展示相机、全局补光和 Volume 位于独立的 `DemoPresentation_CamerasAndFill`，不随整房 prefab 带入大地图。保存的场景和 prefab 是布局来源，运行时不会重新搭建；`Build` 拒绝覆盖已存在的 demo。

当前未修改 `CaveHD2DLocal.unity`。已按该文件保存的 30° 相机俯角和 34° FOV 准备局部预览，正式融合时需结合地图遮挡、通行与整体灯光继续微调。

## 预览

[全景](images/elder-room/elder_overview.png) · [书房](images/elder-room/elder_study.png) · [寝区与暗道](images/elder-room/elder_bed_and_secret_passage.png) · [新版地图相机角度](images/elder-room/elder_map_camera30.png)。

2026-10-12：已通过 Unity C# 编译、场景与 prefab 保存、重新载入资源检查以及实际相机渲染检查；未执行玩家移动、交互或整包构建验证。
