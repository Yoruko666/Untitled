# Unity HD-2D 地图构建顺序

整理日期：2026-10-12。适用环境：Unity 2022.3.62f3c1 / URP 14.0.12。

本文记录地图从草图到可试玩场景的推荐构建顺序，用于安排后续工作，不代表各阶段已经完成。顺序是结合开发者访谈、Unity 官方文档和本项目规范整理的实施建议；原作访谈没有公开一套可直接照搬的 Unity 制作流程。

## 1. 总体顺序

**地图草图 → 尺寸与镜头 → 可走的白盒地图 → 遮挡检查 → 小范围美术样板 → 模块化铺图 → 完整灯光 → 后处理与性能检查。**

《八方旅人》开发者说明，角色采用 2D 像素图，背景几乎全部由 3D 渲染；探索时使用固定镜头，并尽量把让玩家想去的地方安排在同一屏幕内。因此，本项目建议在地图早期就让镜头参与设计。[八方旅人 II 开发者访谈](https://www.unrealengine.com/developer-interviews/octopath-traveler-ii-builds-a-bigger-bolder-world-in-its-stunning-hd-2d-style?lang=en-US)

| 阶段 | 工作内容 | 阶段结果 |
| --- | --- | --- |
| 1. 地图草图 | 标出入口、出口、主路、支路、房间、资源点和视觉地标 | 能解释玩家从哪里来、往哪里走、为什么探索 |
| 2. 尺寸与镜头 | 用角色作比例尺，确定格子尺寸、通道宽度、墙高、镜头倾角与可视范围 | 人物、门和道路比例协调，屏幕内能看清目标 |
| 3. 白盒地图 | 用简单几何体搭地面、墙体、台阶，加入 Collider、出生点和移动 | 玩家能走完整条路线，不能穿墙或进入虚空 |
| 4. 遮挡检查 | 从游戏镜头检查墙、柱子、屋顶是否挡住角色和入口 | 道路可辨认，重要交互点不会长期被遮住 |
| 5. 美术样板 | 只替换一个房间或路口的地面、墙、角色材质，并加入基础光照 | 确认像素角色与 3D 环境能够融合 |
| 6. 模块化铺图 | 把地面、直墙、转角、门框、岩石等做成 Prefab，替换其余白盒 | 地图风格统一，后续容易编辑 |
| 7. 完整灯光 | 调整环境亮度、主光、火把、阴影及必要的烘焙 | 道路清楚，空间层次和氛围成立 |
| 8. 后处理与性能 | 调色、适量 Bloom、按需加景深，检查实际运行表现 | 画面效果不破坏角色轮廓和地图可读性 |

这些阶段允许小范围往返：例如遮挡检查发现通道过窄，就回到白盒修改；美术样板发现比例不协调，就调整相关尺寸。先解决影响后续铺图的基础问题，再批量制作。

## 2. 尺寸与镜头一起确定

世界移动使用 XZ 平面，Y 表示高度。先统一格子尺寸、地图原点和坐标映射，再确定角色、碰撞体、门洞、通道与墙体的比例。扩图时优先增加布局范围，避免靠整体缩放地图根节点改变这些比例。

固定镜头可以跟随角色移动，重点是探索时保持稳定的观察方向和构图规则。主要从 Game View 判断道路和遮挡；Scene View 的编辑视角不等于玩家看到的画面。

Unity 的 `Perspective` 保留近大远小的透视关系，`Orthographic` 不随距离改变物体的显示大小。两种投影都可以用于类似风格；本项目建议在白盒阶段选定，因为它们会影响道路、墙体和角色的占屏比例。[Unity 2022.3 Camera 文档](https://docs.unity3d.com/2022.3/Documentation/Manual/class-Camera.html)

## 3. 白盒阶段就要能玩

`Blockout` 指用简单几何体验证空间布局。此时主要检查路线、尺寸、碰撞和可见性。

最简单的起点是在 `GameObject → 3D Object → Plane / Cube` 创建地面和墙体。形状复杂后可以考虑 ProBuilder；其官方文档说明它支持关卡设计、原型制作、碰撞 Mesh 和试玩，不必为了第一个小房间先引入新工具。[ProBuilder 文档](https://docs.unity3d.com/Packages/com.unity.probuilder@5.0/manual/index.html)

初期沿用 `CharacterController` 与 3D Collider。`CharacterController.Move()` 接收本次移动的位移量，并受碰撞约束；它本身不会自动施加重力，需要按玩法另外处理重力或高度约束。[Unity 2022.3 Move API](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/CharacterController.Move.html)

显示与通行规则分开：看起来像地面的区域不一定能走，装饰物也不一定需要阻挡。出生点必须有效，墙体、边界和虚空要有明确的通行限制。

## 4. 先处理遮挡，再批量制作美术

从实际游戏镜头检查入口、转角、墙后和交互点。洞穴可以按“切开顶盖的模型”组织：保留后方和两侧的岩壁体量，根据镜头降低、分段或隐藏近侧遮挡。具体处理属于本项目建议，须通过画面和试玩确认。

先做一个样板区域：**一个角色＋一块地面＋一段墙＋一盏灯＋稳定镜头。** 用它验证尺度、像素清晰度、接地感、光照和遮挡，确认后再扩展到整张地图。

如果使用 Quad 承载角色贴图，可以尝试 URP `Lit + Alpha Clipping`。`Alpha Clipping` 会裁掉低于阈值的 alpha 区域，让透明背景消失并保留清楚的轮廓。具体材质应结合现有角色的渲染方式选择，不能直接假定普通 Sprite 材质会接受 3D 光照。[URP 14 Lit 文档](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/lit-shader.html)

Billboard 只调整视觉子对象，角色根节点和碰撞体保持正常的世界方向。环境使用 3D 地面和墙体提供厚度、遮挡与碰撞，并统一贴图的像素密度、配色和细节等级。

## 5. 模块化铺图与布局保存

把样板中可重复使用的地面、直墙、转角、门框、岩石等保存为 Prefab，再替换其余白盒。Scene 保存关卡摆放与组装，Prefab 保存可复用对象配置。

地图布局保持一个权威来源。已有地图应沿用其保存的布局和明确的 Editor 重建操作；runtime 读取保存结果，避免自动覆盖手工编辑内容。替换视觉模型时复查受影响的通道、门洞与 Collider，不因外观改变而无意改变可通行范围。

大地图按区块组织，避免默认给每格添加 Update。是否需要合并 Mesh、调整加载或其他优化，由实际瓶颈决定。

## 6. 基础光照早加，精细灯光与后处理后做

样板阶段加入基础光照，用于判断材质和角色是否协调。完整布局稳定后，再细调环境亮度、主光、火把、阴影和必要的烘焙。

《八方旅人》初代访谈提到，团队在战斗特效中同时加入 Point Light，让角色阴影落到环境上，加强像素角色与 3D 环境的视觉联系。这是原作经验；Unity 中仍需验证所选 Renderer、shader 与阴影设置能否达到目标。[八方旅人初代开发者访谈](https://www.unrealengine.com/spotlights/octopath-traveler-s-hd-2d-art-style-and-story-make-for-a-jrpg-dream-come-true)

后处理在地图和灯光基本稳定后加入：先调色，再尝试适量 Bloom，景深按需启用并验证角色与交互点的清晰度。关闭后处理时，地图仍应能读懂。

URP 的后处理使用 `Volume` framework，无需额外安装 Post Processing Stack v2，且 URP 与该旧包不兼容。新 Scene 的基本配置是：Camera 勾选 `Post Processing`，创建 `GameObject → Volume → Global Volume`，创建 Profile 并添加所需的 Override；Camera 的 Volume Mask 必须包含该 Volume 所在的 Layer。[URP 14 后处理文档](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/integration-with-post-processing.html)

## 7. 洞穴地图的最小验收

新地图的第一步可以只完成 **入口 → 通道 → 一个房间 → 出口**。已有大地图可选同等规模的一段路线进行检查，不必为验证流程重新搭建整图。

1. 角色能走完整条路线，墙和边界确实阻挡。
2. 游戏镜头下始终能辨认角色、道路和出口。
3. 一个样板房间里的像素角色、3D 墙体和灯光看起来协调。

达到这些条件后，再扩大铺图范围。一般画面、灯光、镜头和移动手感由人工试玩确认；性能优化依据实际运行检查安排。

## 延伸资料

- [HD-2D 场景开发经验：八方旅人与 Unity 实践](hd2d-scene-development.md)：项目已有的原始资料、画面取舍与实践说明。
- [URP 14 Depth of Field](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/post-processing-depth-of-field.html)：需要景深时查对应参数与模式。
