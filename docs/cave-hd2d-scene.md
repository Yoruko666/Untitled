# CaveHD2DLocal 地图制作记录

本轮日期：2026-10-12。阶段：整图空间改造与 Edit Mode 静态构图检查。

**整图空间改造已制作，并完成 Edit Mode 静态构图检查；未经试玩、通行、感知或性能验证。** 三个代表位置的静态画面不等于整图通行或潜行验收。

## 场景与保存

- `Assets/Scenes/CaveHD2DLocal.unity`：本地正式地形、功能建筑占位、多路线空间与 Cinemachine 固定方向相机。
- `CaveHD2D.unity` 和 `Cave.unity`：本轮不修改，继续保留各自布局与相机。
- `LayoutGrid` 的 Floor/River Tilemaps 与 `CaveElevation` 保存正式布局、高度与地面分区；`Geometry` 按本地场景 GUID 保存派生网格。
- `Landform/MapProduction` 保存建筑、场地及空间标记，其中 `15_SpatialConnections` 为本轮新增岩肩、短围栏和方向标识；`StealthBlockout` 保留五处住宿棚屋与部分掩体。
- 旧 `Architecture` 和 `PlacedProps` inactive，连同其 Collider 一起禁用。

每格为 2 世界单位，角色尺度保持原设置。低岸 `-4`、主层 `0`、住区 `2.4 / 4.8`、大厅和长老台地 `7.2`；水面 `-6.2`、河床 `-7.4` 保持原数据。桥头过渡按正式四角高度延长，没有整体替换台地。

普通保存和当前场景的 `Rebuild Scene Geometry` 更新派生地形并保留手工摆放。`Replace With Terraced Settlement Layout` 会替换布局，不能当作刷新；`Build HD2D Scene` 与 `Apply HD2D Scene Lighting` 面向远端场景。

## 本轮改动

酒馆及外部桌组移到火塘西侧，赌场移到住区坡口外侧；炉体移向供给桥方向，烟囱放在靠桥一侧。仓库货垛、公共区掩体、入口侧岩和栅栏让开坡口、观察位置及建筑门口。

重新描出主层换路、仓库返回入口、住区前街与上坡连接、工坊供给路线和大厅两入口接近方向。少量岩肩与短围栏形成区域边界，桥头登坡增加平缓过渡，闲置外沿局部收边。五处棚屋、四块种植床、两座桥和全部功能种类保留。

普通门洞净开口 `2.4`、大厅 `3.2` 世界单位，入口柱框移到开口外侧。通巷、落脚面和暴露段按 [整图方案](cave-map-redesign-plan.md) 的几何约束摆放，尚未做角色通行测试。

东侧主路经过火塘、住区和折返坡到大厅正面；西侧供给路经过工坊、两座桥和厨房后侧到大厅侧面。主层换路、住区小环路和仓库返回入口连接保留，撤退不必经过大厅。没有新增守卫、感知、导航、任务、战斗或 runtime API。

## 固定方向相机

保留 Cinemachine `2.10.7`、Pitch `30°`、Yaw/Roll `0°`、FOV `34°`、Camera Distance `32`。Follow 玩家根节点，Tracked Object Offset `(0,1.6,0)`，Screen X/Y `0.5/0.6`。

Body 为 Framing Transposer，Aim 为 Do Nothing，Look At 留空；三轴 Damping `0.25`，Dead Zone Width/Height `0.05`、Depth `0`，Soft Zone Width/Height `0.8`、Bias `0`。Brain 两项更新方式均为 Late Update。

旧 `CameraController` 保持禁用；URP、Volume、`CaveWallReveal` 引用和角色 upright Billboard 保留。不新增旋转、变焦、震动、Confiner 或相机碰撞绕行，灯光、水面、后处理设置不变。

## 检查与后续

Unity Edit Mode 制作保存并输出三组 `16:9` 静态画面：入口与火塘、供给桥东端与厨房方向、上坡与大厅/长老支路。检查后恢复玩家出生位置，截图位置未写入正式场景。画面与定位记录见 [整图方案的静态检查](cave-map-redesign-plan.md#5-静态检查记录)。

核对保存、资源引用和 diff；没有运行 Play Mode、完整构建、通行、感知或性能验证。历史 V6 的检查只属于旧布局和 42° 镜头，不能作为本轮验收结果。

当前打开的 Unity 场景需重新加载保存结果。后续根据人工反馈单独安排路线、碰撞、移动手感和玩法验证。
