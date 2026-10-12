# 哥布林地下酒馆场景 demo

场景：`Assets/Scenes/Props/GoblinTavernDemo.unity`。Unity 2022.3.62f3c1 / URP 14.0.12。

## 打开与查看

1. 打开场景，进入 Game 视图或 Play Mode 查看整间酒馆。场景为美术摆放 demo，尚未接入经营、玩家移动或交互。
2. 在 Scene 视图选中 `04_LBarAndWineRacks` 或某个 `TableSet`，按 F 查看家具与灯具细节。
3. 调整房间时展开 `GoblinTavern_Module_32x32` 的分组；整间房间及独立家具都保存在 `Assets/Prefabs/Props/Tavern`。

也可使用 Unity 菜单 `Untitled > Tavern > Open Scene Demo`。该入口只打开已保存的 demo，保留后续手工摆放。

## 草图与陈设

沿用草图的六张大圆桌（后排两张、前排四张）、右上 L 形吧台、北/东侧酒架和吧台半高门。桌面直径 4 世界单位；房间内沿约 32 × 32 世界单位，XZ 为地面、Y 为高度。尺度先服务独立 demo；新地图格子为 2 世界单位，接入时可按实际酒馆 footprint 调整布局，避免将草图的每一小格直接等同于新地图格子。

- 家具为实体网格：圆形木板桌面、铁箍铆钉、桌腿斜撑、四腿木凳、吧台脚踏杆、分层酒架、弧形拼板酒桶、木箱、长凳、半高门。
- 桌面暂不放酒和食物，保留油灯。酒架上的瓶子是酒架库存陈设。
- 北侧石砌火炉、柴薪和靠墙储物，东侧桶组，南侧箱桶与长凳、一个小鼓乐角，补充生活感。
- 中央织物地毯强调横向通道，南入口另有短地毯；家具围绕通行区域布置。
- 墙上符文旗、符文盾、骨饰与麦酒招牌形成地下哥布林酒馆的辨识度。
- 壁挂火盆、桌面/吧台油灯和三盏铁吊灯提供暖色局部光，冷灰环境补光保留洞壁层次。火光复用项目已有 `HybridFireLight`。

## 资源位置与复用

| 内容 | 目录 |
| --- | --- |
| 场景 | `Assets/Scenes/Props/GoblinTavernDemo.unity` |
| 房间整体预制体 | `Assets/Prefabs/Props/Tavern/goblin_tavern_room.prefab` |
| 独立家具和灯具 | `Assets/Prefabs/Props/Tavern` |
| 实体网格 | `Assets/Art/Models/Props/Tavern` |
| URP 材质 | `Assets/Art/Materials/Props/Tavern` |
| 木纹贴图 | `Assets/Art/Textures/Props/Tavern/tavern_oak_grain.png` |
| 后处理 | `Assets/Settings/GoblinTavernPresentation.asset` |
| 本次离线制作源码 | `Assets/Editor/Tavern` |

新增实体家具和木纹由本次程序化建模原创生成；不含下载的第三方模型。柴堆、柴束、符文旗、符文盾、骨堆直接引用项目原有 prefab 与素材 GUID，没有复制另一套素材。

场景与 prefab 已保存所有摆放；Play Mode 不生成、重建或覆盖房间。离线作者工具不是运行时依赖，`Build` 在场景已存在时会拒绝覆盖。后续直接在 Inspector/Scene 视图修改即可。

## 接入 CaveHD2DLocal

此轮交付独立 demo，不修改 `CaveHD2DLocal.unity` 的地形、布局、地图玩法或 Cinemachine 配置。以后将房间 prefab 放入地图功能区，根节点贴合该区域地面高度；所有家具以底部为支点。

房间模型与展示灯光分开：复用房间 prefab 时不会带入展示相机、全局 Volume 和补光。实体灯具会随房间一起进入地图，届时按地图光照强度调整。

南墙为剖面展示矮墙，但其边界 Collider 仍为完整墙高；西门和南门有真实空缺与门槛。当前无屋顶，为查看内部陈设而设。融入地图时根据镜头遮挡方案重新处理正面墙与屋顶，不把剖面展示方式视为最终建筑结构。

## 检查与预览

2026-10-12：在独立临时工程中使用相同 Unity 版本完成 C# 编译、场景与 prefab 生成；重新载入已保存场景并渲染检查。修正了平面顶面朝向、吧台短段的缩放方式、后处理子资源保存，并调整了灯光。未运行游戏包构建或玩家移动/交互测试。

[酒馆全景](images/tavern/tavern_overview.png) · [吧台近景](images/tavern/tavern_bar.png) · [30° 镜头陈设预览](images/tavern/tavern_cave30.png)。
