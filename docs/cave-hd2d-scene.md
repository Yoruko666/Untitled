# CaveHD2DLocal 地图制作记录

本轮为地图制作阶段，实施日期 2026-10-12。**地图与相机初始配置已制作，未经验证。** 未运行 Play Mode、试玩、通行、感知、构图或性能验证。

## 场景与编辑

- `Assets/Scenes/CaveHD2DLocal.unity`：本轮正式地形、功能建筑占位、分散掩体与 Cinemachine 30° 固定方向相机。
- `CaveHD2D.unity` 与 `Cave.unity`：保持原布局和相机，本轮不修改。
- `LayoutGrid` 的 Floor/River Tilemaps 与 `CaveElevation` 保存正式布局、高度及材质；`Geometry` 按本地场景 GUID 保存派生资源。
- `Landform/MapProduction` 保存新功能建筑、场地及设计位置标记；`Landform/StealthBlockout` 保留五处住宿棚屋和部分实体掩体。
- 旧 `Architecture` 与 `PlacedProps` inactive，其 Collider 同时禁用。不要整体启用旧草稿。

保持每格 2 世界单位和现有角色尺寸，低岸 `-4`、主层 `0`、住区 `2.4 / 4.8`、大厅和长老高台 `7.2`，水面 `-6.2`、河床 `-7.4`。

普通保存与 `Rebuild Scene Geometry` 根据当前布局更新派生地形，保留手工摆放。`Replace With Terraced Settlement Layout` 会替换正式布局，不能当作刷新操作；`Build HD2D Scene` 与 `Apply HD2D Scene Lighting` 面向远端场景，不用于本地版日常更新。

## 本轮制作内容

补齐入口与撤离前场、军营训练区、厨房备料、宴会、酒馆、赌场、祭坛大厅、长老私库、四块种植床、仓库及卸货庭院。五处住区棚屋继续复用，公共火塘、河岸工坊和两座桥各自承担独立功能。

建筑以简单墙体、入口缺口、棚顶、柱框与功能物件表达，不制作完整室内。地面场地为薄占位，墙体及物件保存简单 Collider。具体区域、占位内容与相机参数见 [地图制作方案](cave-map-redesign-plan.md)。

保留东侧入口—火塘—住区—折返坡—大厅正面和西侧桥头—工坊—供给桥—厨房后侧—侧坡—大厅侧面两条路线。整理主层横向连接及仓库返回入口的地面分区，局部展开折返坡外肩、降低南侧岩肩，错开坡口与入口附近的货物和棚屋。住区前后巷保留小环路，大厅庭院不再保留旧实心分隔块。

观察位置空对象只记录设计位置。掩体、暴露段、绕行与撤退连接尚未进行空间验证，不标记安全位置。当前没有守卫、巡逻、感知、导航、任务或战斗系统；已有角色显露和移动边界不等于感知遮挡规则。

## 相机与保留项

Cinemachine `2.10.7` 只在本地场景接入，Main Camera 添加 Brain，旧 `CameraController` 禁用并保留目标引用。Virtual Camera 的 Body 为 Framing Transposer，Aim 为 Do Nothing，Follow 玩家根节点，Look At 留空。

Pitch `30°`、Yaw/Roll `0°`、FOV `34°`、Camera Distance `32`、Tracked Object Offset `(0, 1.6, 0)`、Screen X/Y `0.5/0.6`、三轴 Damping `0.25`；Dead Zone Width/Height `0.05`、Depth `0`，Soft Zone Width/Height `0.8`、Bias `0`。Brain 两项更新方式均为 Late Update。

关闭 Lookahead、Noise 和 Group Framing，不添加 Confiner 或相机碰撞绕行。URP、Volume、`CaveWallReveal` 引用、角色 upright Billboard、灯光、水面和后处理保持原设置。相机参数是本项目未经试玩的初始配置。

## 检查状态

本轮只核对 Unity 编译、制作保存、资源引用和 diff，不执行完整构建或空间验证。隔离临时项目保存后，将正式场景与本地派生资源同步回来；已打开的 Unity 场景需重新加载。

历史 V6 曾进行路线、河岸与掩体检查，但该记录只属于旧布局与 42° 相机，不能作为本轮验收结果。后续再单独安排试玩、通行、构图等验证和玩法接入。
