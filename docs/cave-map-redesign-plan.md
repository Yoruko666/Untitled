# 哥布林洞穴地图制作方案：地形、建筑占位与潜行空间

整理日期：2026-10-12。版本：V4。阶段：地图制作阶段。

实施对象为 `Assets/Scenes/CaveHD2DLocal.unity`。本轮完成地图与 Cinemachine 相机初始配置并保存；不运行 Play Mode，不进行试玩、通行、感知、构图或性能验证。**地图与相机初始配置已制作，未经验证。**

## 1. 地图骨架与保存方式

保留左侧河谷、中央生活主层、东侧住区、后方首领高台和右后方长老台地。原 2D 图只提供功能种类与区域关系，不逐格复刻矩形房间。

Grid 每格 2 个世界单位，角色尺度不变。低岸 `Y=-4`、主层 `0`、住区 `+2.4 / +4.8`、大厅和长老台地 `+7.2`，水面与河床沿用原高度。沿用已有短退台，不整体替换地形。

`LayoutGrid` 的 Floor/River Tilemaps 与 `CaveElevation` 是正式布局及四角高度的权威来源；保存时由既有机制更新本地场景的派生网格。建筑与功能物件作为普通场景对象保存于 `CaveMap/Landform/MapProduction`，五处住宿棚屋及部分掩体继续保存在 `StealthBlockout`，可手工编辑。

旧 `CaveMap/Architecture` 与 `LevelRoot/PlacedProps` 保持隐藏，连同其 Collider 一起禁用。不要整体启用旧草稿，也不要将 `Replace With Terraced Settlement Layout` 当作刷新操作；该菜单会替换正式布局。普通保存或当前场景的 `Rebuild Scene Geometry` 才是日常派生更新入口。

## 2. 功能区与建筑占位

占位使用简单体块、棚顶、柱框和少量功能物件，设置明确名称与简单 Collider。独立建筑以外围墙体与入口缺口表达边界，不制作完整室内。地面场地标识不额外叠加阻挡 Collider。

| 功能 | 当前安排 | 占位内容 |
| --- | --- | --- |
| 洞外撤离、入口前厅 | 南侧洞口及前场 | 入口侧岩、两侧栅栏、撤离方向标记 |
| 军队区域 | 入口东侧 | 军营棚屋、训练空地和训练柱 |
| 休息区／住宿 | 东侧中层住区 | 复用五处棚屋，错开前组北棚屋，保留前后巷 |
| 厨房备料 | 西北主层、供给路线旁 | 厨房、炉体及烟囱、备料台和货垛 |
| 聚餐 | 厨房东侧公共主层 | 独立宴会场地、四组桌凳 |
| 酒馆 | 公共区南侧 | 独立棚屋及外部桌组和停留空间 |
| 赌场 | 公共区东侧、住区下坡口旁 | 独立棚屋、桌体与东侧绕行方向；入口朝南 |
| 祭坛大厅 | 后方首领高台 | 大厅墙框、棚顶、祭坛、骨旗，正面与西侧入口 |
| 长老私库 | 右后方台地 | 独立私库、前场和货物 |
| 种植区 | 左岸低地 | 四块带边框的种植床 |
| 仓库 | 东南主层 | 仓库和三组外围货垛 |
| 卸货庭院 | 仓库西南外侧、面向入口 | 开放庭院和集中卸货占位 |

公共火塘独立设置石圈、柴堆与局部长凳，作为生活区地标，本轮不新增灯光。河岸工坊保留原墙体、棚顶与局部货物，补充工作台和木料。两座桥保持原位置和结构；工坊与火塘不替代厨房、酒馆或赌场。

## 3. 路线、地形与潜行空间

- 东侧主路：入口 → 火塘外围 → 居住前街 → 折返坡 → 大厅正面。
- 西侧供给路：桥头 → 左岸工坊 → 供给桥 → 厨房后侧 → 侧坡 → 大厅西侧。
- 主层以正式地面材质整理厨房前场、火塘边缘和住区坡口之间的横向连接；仓库外沿接回入口。
- 住区前后巷保留返回环路。大厅两入口分别接入，移除入口庭院中的旧实心分隔块。

本轮局部展开东侧折返坡外肩，保留原坡面高度，并将相邻正式格子的共享角点同步保存。降低南侧可见岩肩，保留后方岩体与已有退台。厨房炉体及货垛、仓库货垛和大厅入口掩体从入口及落脚面旁错开摆放。

入口侧岩、仓库外围货垛、厨房炉体、棚屋侧墙及大厅岩肩提供分散遮挡；中间保留暴露段，不铺连续掩体墙。桥中央保持开阔，桥头、坡口和转角留观察位置。`14_ObservationAndRetreatConnections` 的空对象标记设计位置，只用于后续人工定位，不表示安全状态或已经验证的通行结果。

本步没有守卫、巡逻、感知、导航、任务或战斗系统，不判定发现概率、穿越时间窗口或掩体安全性。后续再根据实际移动与镜头效果检查空间，再单独安排玩法接入。

## 4. 固定方向相机

仅本地洞穴场景接入 Cinemachine `2.10.7`。该版本适用于 [Unity 2022.3](https://docs.unity3d.com/2022.3/Documentation/Manual/com.unity.cinemachine.html)，[Framing Transposer](https://docs.unity3d.com/Packages/com.unity.cinemachine@2.10/manual/CinemachineBodyFramingTransposer.html) 控制跟随和屏幕位置。

| 项目 | 保存的初始配置 |
| --- | --- |
| 输出相机 | 原 Main Camera + Cinemachine Brain |
| Virtual Camera | `CaveExplorationCamera` |
| 投影与旋转 | Perspective；Pitch `30°`、Yaw `0°`、Roll `0°` |
| Lens | FOV `34°`，沿用原裁剪面 |
| Body / Aim | Framing Transposer / Do Nothing |
| Follow / Look At | 玩家根节点 / 留空 |
| Tracked Object Offset | `(0, 1.6, 0)` |
| Camera Distance | `32` |
| Screen Position | X `0.5`、Y `0.6` |
| Damping | X、Y、Z 均为 `0.25` |
| Dead Zone | Width、Height `0.05`，Depth `0` |
| Soft Zone | Width、Height `0.8`，Bias X、Y `0` |
| Lookahead / Noise / Group Framing | 关闭 |
| Brain 更新 | Update Method 与 Blend Update Method 均为 Late Update |

Main Camera 上旧 `CameraController` 禁用，保留目标引用供现有 Editor 工具读取，并将其俯角记录同步为 `30°`。URP、Volume、`CaveWallReveal` 引用保留，角色继续使用 upright Billboard，建筑固定朝向。其他场景保持原相机。

不启用 Confiner、自动旋转、自动变焦、震动或碰撞绕行。透视模式下 Confiner 不能保证整个画面边缘留在地图内，参见 [官方能力说明](https://docs.unity3d.com/Packages/com.unity.cinemachine@2.10/manual/CinemachineConfiner.html)。上述数值属于本项目未经试玩的初始配置，不是《八方旅人》的原作参数，也不照搬旧脚本的距离和 damping 数值关系。

## 5. 交付记录与后续

本轮仅核对 Unity 编译、保存内容、资源引用、diff 与提交范围。不新增 runtime API，不重构 Editor 工具，不调整角色、灯光、水面或后处理。依赖仅新增 Cinemachine，其他 Package 保持原版本。

地图、相机及必要派生资源与方案文档分别创建本地 commit，不自动 push 或创建 PR。使用隔离临时项目的 Unity Edit Mode 完成制作保存，同步正式场景及本地派生资源；当前已打开的编辑器需要重新加载场景才能看到磁盘上的结果。

**地图与相机初始配置已制作，未经验证。** V6 的旧验证记录不适用于本轮新布局和 30° 相机。试玩、通行、感知、构图与性能验证以后单独安排。

原参考图继续保留：

- [三级台地概念图](images/cave_settlement_reference.png)
- [2D 功能平面图](images/cave_functional_plan_reference.png)
- [早期构图草案](images/cave_composition_proposal.png)，仅用于美术意图参考，不作为尺寸、参数或验证依据。
