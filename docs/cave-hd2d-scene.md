# CaveHD2DLocal 地图制作记录

本轮日期：2026-10-12。阶段：低俯角下的三段地图修订。

**本轮修改已写入场景文件，未重新打开 Unity 场景或进行画面、通行、感知与性能验证。** 文档中的三张静态图属于上一轮 `30°` 镜头。

## 场景与保存

- `Assets/Scenes/CaveHD2DLocal.unity`：本地正式地形、功能建筑占位、多路线空间与 Cinemachine 固定方向相机。
- `CaveHD2D.unity` 和 `Cave.unity`：本轮不修改，继续保留各自布局与相机。
- `LayoutGrid` 的 Floor/River Tilemaps 与 `CaveElevation` 保存正式布局、高度与地面分区；`Geometry` 按本地场景 GUID 保存派生网格。
- `Landform/MapProduction` 保存建筑、场地及地标占位，其中 `15_SpatialConnections` 保存岩肩、短围栏和方向标识；`StealthBlockout` 保留五处住宿棚屋，并增加供给桥两岸的侧边货垛。
- 旧 `Architecture` 和 `PlacedProps` inactive，连同其 Collider 一起禁用。

每格为 2 世界单位，角色根节点缩放保持 `0.9`。低岸 `-4`、主层 `0`、住区 `2.4 / 4.8`、大厅和长老台地 `7.2`；水面 `-6.2`、河床 `-7.4` 保持原数据。本轮只改手工对象，Floor/River Tilemaps、`CaveElevation` 与派生地形保持原保存结果。

普通保存和当前场景的 `Rebuild Scene Geometry` 更新派生地形并保留手工摆放。`Replace With Terraced Settlement Layout` 会替换布局，不能当作刷新；`Build HD2D Scene` 与 `Apply HD2D Scene Lighting` 面向远端场景。

## 本轮改动

入口岩肩降低，火塘增加暖色火焰形占位和浅色石圈；公共区掩体向侧边退让。军营、酒馆和赌场门楣使用少量色彩区分用途。

供给桥两岸增加侧边货垛；东岸岩肩移回地面，工坊河侧岩肩降低。厨房炉体收窄、烟囱加高，前墙降低、屋顶退到后半部，侧后货垛错开接近方向。

前排住宿棚屋左右错开并收短屋顶，保留五处住宿实体及原环路。大厅前墙降低、屋顶收为后半部，祭坛前移，前场两侧增加低石块和高旗架；前场掩体退向侧后方。私库缩小屋顶并使用青色占位，与大厅拉开轮廓差异。种植区、两座桥、仓库连接和全部功能种类保留。

普通门洞净开口 `2.4`、大厅 `3.2` 世界单位，入口柱框移到开口外侧。通巷、落脚面和暴露段按 [整图方案](cave-map-redesign-plan.md) 的几何约束摆放，尚未做角色通行测试。

东侧主路经过火塘、住区和折返坡到大厅正面；西侧供给路经过工坊、两座桥和厨房后侧到大厅侧面。主层换路、住区小环路和仓库返回入口连接保留，撤退不必经过大厅。没有新增守卫、感知、导航、任务、战斗或 runtime API。

## 固定方向相机

保留任务开始时的最新 Cinemachine `2.10.7` 参数：Pitch 约 `12.09°`、Yaw/Roll `0°`、FOV `34°`、Camera Distance `24.6`。Follow 玩家根节点，Tracked Object Offset `(0,1.6,0)`，Screen X/Y `0.5/0.6`。

Body 为 Framing Transposer，Aim 为 Do Nothing，Look At 留空；三轴 Damping `0.25`，Dead Zone Width/Height `0.05`、Depth `0`，Soft Zone Width/Height `0.8`、Bias `0`。Brain 两项更新方式均为 Late Update。

旧 `CameraController` 保持禁用，其 `30° / 32` 字段不是当前镜头参数；URP、Volume、`CaveWallReveal` 引用和角色 upright Billboard 保留。灯光、水面、后处理设置不变，火塘自发光仅来自新增占位材质。

## 检查与后续

本轮核对场景对象、资源引用和 diff，不进行 Unity 画面检查或动态验证。新增装饰占位不带 Collider；桥头货垛与大厅前场低石块带 BoxCollider。详细摆放见 [整图方案](cave-map-redesign-plan.md)。

原三张 `16:9` 静态图记录的是上一轮 `30° / 32` 镜头；更早的 `42°` 检查也不属于当前布局。二者均不能作为最新低俯角的验收结果。

当前打开的 Unity 场景需重新加载保存结果。后续根据人工反馈单独安排路线、碰撞、移动手感和玩法验证。
