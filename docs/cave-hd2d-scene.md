# CaveHD2DLocal 地图制作记录

本轮日期：2026-10-12。阶段：功能区换位与长老住处／私库连接修订。

**整图扩容与彩色分区已制作，并完成 Edit Mode 静态构图检查；未经试玩、通行、感知或性能验证。**

## 本轮结果

`Assets/Scenes/CaveHD2DLocal.unity` 的正式地形外框由 `136×124` 扩至 `204×186` 世界单位，每格仍为 2。Floor 为 `102×93` 格包络、7199 个单元，其中 River 为 754 个单元。外框面积为原来的 2.25 倍，新增场地增加实际填充面积；Floor 包含河流，不能作为可通行面积。

原 2D 图的撤离、前厅、军队、住宿、厨房备料、聚餐、酒馆、赌场、祭坛大厅、长老私库、种植区、仓库与卸货庭院全部保留。公共火塘和河岸工坊各有独立分区，五处棚屋、四块种植床、两座桥保留。

03 军营迁至原 04 的东侧台地，04 住宿迁至原 03 的入口东南场地；12 仓库迁至西北原厨房场地，05 厨房连炉体和烟囱迁至东南原仓库场地。编号与颜色跟随功能，建筑与相关货物一起调整，住区模型没有缩小。

通过扩地形和移动手工物件完成，不缩放地图根节点、角色或整批建筑。角色根节点 `0.9` 缩放与出生位置保留；住宿现位于入口侧主层，军营使用原住区台地；低岸 `-4`、主层 `0`、东侧台地 `2.4/4.8`（现军营）、后方高台 `7.2` 的高度体系不变。桥面宽 `5.4`，两侧限制间预留超过 `4.8` 净宽。

主路按 `8`、供给路及区域连接按 `6`、住区巷道按 `4` 世界单位制作。大厅正面与西侧入口、祭坛—长老住处小通道、主层换路、住区环路和仓库返回入口分别保留。掩体在侧边，空地留作观察、作业、转向和撤退。

## 编辑对象与保存

- `LayoutGrid` 的 Floor/River Tilemaps 与 `CaveElevation` 保存正式布局、高度和材质分区，`Geometry` 由原保存机制更新。
- `Landform/MapProduction` 与 `StealthBlockout` 保存既有建筑、场地与掩体；岩肩逐件移动或隐藏，没有执行整体布局替换。
- 长老住处为 `MapProduction/08_ElderPrivateStoreAndCourt/ElderResidence`，内部 `ElderPrivateStore` 用隔断与 `2.4` 门洞表达约 `4×3.5` 的小房间。新增祭坛东侧开口和 `3.2` 小通道，接住处西侧门；原公共坡道直入长老台地的支路改为岩界隔开，前侧 `4.8` 短退台写入正式高度。
- `Landform/ZoneVisualization` 默认开启，包含 15 个区域网格和 1 个灰色道路网格。每区独立命名、使用独立 Unlit 材质，贴合地形并抬高约 `0.03`，无 Collider、无阴影。

关闭 `ZoneVisualization` 即恢复原地面。编号仅用于整图截图，没有保存为场景标识或增加 runtime 切换。后续改地形需同步更新受影响的彩色网格，原地形保存机制不自动生成展示分区。

普通保存与 `Rebuild Scene Geometry` 保留手工对象；不要用 `Replace With Terraced Settlement Layout` 刷新，它会替换正式布局。旧 `Architecture`、`PlacedProps` 连同 Collider 继续隐藏，其他洞穴场景未修改。

## 相机与检查

Cinemachine `2.10.7` 保持 Pitch 约 `12.09°`、FOV `34°`、距离 `24.6`、Follow 玩家根节点、Tracked Object Offset `(0,1.6,0)`、Screen X/Y `0.5/0.6`。Framing Transposer、Do Nothing、三轴 Damping `0.25`、Dead Zone `0.05/0.05/0`、Soft Zone `0.8/0.8` 与 Brain Late Update 沿用原值。

旧 `CameraController` 保持禁用，其 `30°/32` 字段不是当前镜头参数。URP、Volume、CaveWallReveal 引用保留，灯光、水面、后处理及 Package 不变。没有新增守卫、感知、导航、任务、战斗、runtime API 或通用 Editor 工具。

完成 Edit Mode 整图、分区关闭对照和私库房间俯视图，复查入口、供给桥与仓库、东南厨房及大厅接近。长老小通道的低俯角静态图仍被近侧岩体遮挡，记录为后续人工反馈项；本步没有运行 runtime 的墙体显隐，不能据此判断 Play Mode 可见性。临时玩家与相机在检查后恢复。根据物件变换和 BoxCollider 尺寸核对道路占位，没有运行物理模拟。

![完整分区](images/cave_sites_overview.png)

![中文图例](images/cave_sites_legend.png)

各区坐标、尺寸与局部图见 [扩图方案和记录](cave-map-redesign-plan.md)。旧 `30°/32` 图保留作历史资料，不作为本轮验收。

地图资源与文档分别本地提交，不 push；已有亮洞口材质修改和其他场景未跟踪派生资源保留。当前打开的 Unity 场景需重新加载保存结果，动态通行与潜行留待后续验证。
