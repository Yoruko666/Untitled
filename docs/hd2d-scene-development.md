# HD-2D 场景开发经验：八方旅人与 Unity 实践

整理日期：2026-10-11。Unity 实践部分面向本项目的 Unity 2022.3.62f3c1 / URP 14.0.12。

本文收集开发者讲稿、访谈和实践文章，重点回答：怎样让像素角色、3D 环境、水面与光照组成协调的场景。标为“开发者经验”的内容来自对应资料；标为“本项目建议”的内容是根据这些经验和当前代码整理的实现建议。

## 1. 先理解画面目标

八方旅人开发团队在 Epic 的访谈中说明，HD-2D 将像素角色与 3D 环境结合；他们还让战斗特效同时产生 Point Light，使角色阴影落到环境上，增强二者的联系。它的关键是让不同表现形式发生视觉联系。[开发者访谈：Octopath Traveler，2019](https://www.unrealengine.com/spotlights/octopath-traveler-s-hd-2d-art-style-and-story-make-for-a-jrpg-dream-come-true?lang=en-US)

**本项目建议：**从实际 Game Camera 判断画面，先关注四件事：

- **尺度：**角色、门洞、墙高、河宽之间的比例是否协调。
- **空间：**前景、中景、远景是否分开；墙顶、侧面和地面是否能辨认。
- **联系：**角色能否受到环境光影响，脚底是否有可信的接地感。
- **可读性：**玩家能否看清自己、可走路线和下一处目标。

Bloom、景深和调色可以强化这些关系。调整它们之前，先保证关掉后处理时仍能读懂地图。

## 2. 最值得读的原始资料

| 资料 | 类型与日期 | 值得关注的内容 |
| --- | --- | --- |
| [Acquire：Nintendo Switch《OCTOPATH TRAVELER》开发讲稿](https://www.docswell.com/s/EpicGamesJapan/KXPWY5-UE4_UFE2018_ACQUIRE_OCTOPATHTRAVELER) | Epic Games Japan 官方账号发布；讲演于 2018-10-14 | 尺度、材质、后处理、地图工具和室内表现，含制作过程对照图 |
| [八方旅人初代开发者访谈](https://www.unrealengine.com/spotlights/octopath-traveler-s-hd-2d-art-style-and-story-make-for-a-jrpg-dream-come-true?lang=en-US) | Epic 采访 Square Enix / Acquire；2019-10-25 | 2D 与 3D 的融合、特效与灯光的联系 |
| [八方旅人 II 开发者访谈](https://www.unrealengine.com/developer-interviews/octopath-traveler-ii-builds-a-bigger-bolder-world-in-its-stunning-hd-2d-style?lang=en-US) | Epic 采访 Square Enix / Acquire；2023-08-22 | 固定探索镜头、地图构图、昼夜光照、平面角色与 3D 背景的协调 |
| [GAME Watch：高桥真志与宫内继介访谈](https://game.watch.impress.co.jp/docs/interview/1128111.html) | 开发者直接访谈；2018-06-18 | 为什么水面采用更接近写实、但经过调色的表现 |
| [TW0CATS：Perfecting Unity’s Billboard Shader，Part 1](https://tw0catsgames.com/update/2023/11/06/perfecting-unitys_billboard_shader_for_hd2d_01.html) | Spiral Fantasy 开发者实践；2023-11-06 | Billboard 的俯视变薄、阴影和 shader 问题；属于 Unity 方案，不代表八方旅人源码 |

Unreal Fest 讲稿可以优先看以下页码。右栏是本项目的应用建议，UE4 的具体实现需按 Unity 的坐标与渲染管线重新设计。

| 页码 | 开发者经验摘要 | 本项目建议 |
| --- | --- | --- |
| 14–16 | 广角、建筑比例和高低差会影响尺度感 | 镜头与角色、墙体尺寸一起确定 |
| 31–34 | 使用主材质派生与 World UV | 统一地表材质规则，减少旋转后的像素方向混乱 |
| 35–37 | 以角色屏幕位置为中心处理前景透明 | 为遮挡严重的位置预留可见性处理 |
| 45–52 | 使用调色、Bloom、Bokeh 等后处理，部分参数按地图调整 | 不同场景使用适合自己的 Volume 配置 |
| 54–63 | 格子地图工具有块状、数据量和制作工时问题；结合模块地形 | 格子负责布局，视觉轮廓另外处理 |
| 66–67 | 室内会调整尺度、灯光、后处理和相机距离等 | 室内外分别评估构图与参数 |

以上摘要依据[原始讲稿](https://www.docswell.com/s/EpicGamesJapan/KXPWY5-UE4_UFE2018_ACQUIRE_OCTOPATHTRAVELER)。讲稿的尺度与制作阶段对照图，比单独复制一组后处理数值更值得参考。

## 3. 镜头与尺度要一起设计

**开发者经验：**八方旅人 II 在探索时使用固定镜头方向，团队会尽量把让玩家想去的地方安排在同一屏幕内。他们也指出，2D 角色像薄纸，如何与 3D 背景协调，以及镜头能转到什么程度，都需要专门处理。[八方旅人 II 开发者访谈](https://www.unrealengine.com/developer-interviews/octopath-traveler-ii-builds-a-bigger-bolder-world-in-its-stunning-hd-2d-style?lang=en-US)

**本项目建议：**把“固定”理解为探索镜头的方向和构图规则稳定；相机仍可跟随玩家。决定一条洞穴通道是否好看时，应同时看通道入口、近侧墙、角色与远处目标，不能只看 Scene View 的俯视平面图。

| 参数 | 主要影响 | 调整方向 |
| --- | --- | --- |
| Pitch，俯角 | 地面可见面积、侧墙高度、竖直角色的视觉压缩 | 降低俯角通常更能看见立面，但近侧遮挡也更明显 |
| Perspective / Orthographic | 远近缩放、透视关系 | 先按目标画面选择；两种投影都能用于类似风格 |
| FOV | 在相同机位下的视野范围与透视夸张程度 | 使用较窄 FOV 时，同步调整距离与画面容纳范围 |
| 相机距离与 Focus Offset | 角色占屏比例、玩家前方能看见多少内容 | 给即将进入的通道和目标留空间 |
| 墙高、门宽、平台层级 | 人物与环境的比例、遮挡和空间层次 | 用角色站在旁边判断，避免只按“真实米数”决定 |

先确定一个参考机位，再摆一段通道、转角、河岸和一个角色。主要路线保持可读，远处可以露出一处亮点或入口，让地图有值得继续探索的方向。

当前 `Cave.unity` 保存的参数为 Pitch `29.293°`、FOV `34°`、跟随距离 `20.5`、Focus Offset `(0, 1.4, 0)`。这是本项目当前起点，**不是八方旅人公开的标准参数**。调整地图时先沿用它，避免镜头与地形同时大幅变化。

本地整图改造使用 `CaveHD2DLocal.unity`，与上述旧 `Cave.unity` 分开。当前本地镜头为 Cinemachine `2.10.7`：Pitch 约 `12.09°`、FOV `34°`、Camera Distance `24.6`、Tracked Object Offset `(0, 1.6, 0)`、Screen X/Y `0.5/0.6`。角色根节点缩放为 `0.9`。旧 `CameraController` 已禁用，其 `30° / 32` 字段不代表当前镜头；不要将其距离与 Focus Offset 直接套入 Framing Transposer。三段低俯角地图修订见 [整图改造方案](cave-map-redesign-plan.md)，尚未重新进行画面、试玩或通行检查；保留的三张 Edit Mode 图片属于上一轮 `30° / 32` 镜头。

本地制作采用分段显露目标：入口以火塘暖色占位提供方向，供给桥沿横向连接接近厨房烟囱，上坡通过大厅宽前场与私库小屋轮廓区分目标。低俯角下，优先退让前景岩肩、错开掩体和建筑、把部分屋顶收至后半部；角色附近的遮挡裁切不能替代前方路线的可读性。建筑入口柱框放在门洞净开口外侧。是否达到连续显露的构图意图，需要按当前镜头人工查看。

## 4. 格子布局与自然地形分开处理

**本项目建议：**Grid / Tilemap 保存地板、河道等布局，派生 Mesh 保存立体显示。成员绘制并保存的布局始终是正式来源；外部数据只通过显式导入进入编辑流程。[Unity 2022.3：Tilemap](https://docs.unity3d.com/2022.3/Documentation/Manual/class-Tilemap.html)

通行区域可以规整，洞穴外观仍然可以有不规则的轮廓：

- 保持通道宽度和边界碰撞稳定，在边界外侧加入高低变化、凹凸轮廓和不同尺度的岩面。
- 墙顶、墙侧和地面分别考虑材质与明暗，避免所有面都像同一种方砖。
- 长直墙和长河岸分成几段视觉变化；转角、通道交汇与较大的洞室适合安排明显的轮廓差异。
- 接缝处使用一致的贴图密度、UV 和水面相位，避免玩家看出区块边界。

对当前洞穴，格子生成的墙体可以先作为结构底盘。自然岩面、边缘装饰等模块由相应资产负责人提供，地图侧确定布局和摆放需求。**视觉凹凸不能无意改变窄路宽度和碰撞边界。**

当前 `CaveMapBuilder` 按 `16 × 16` 格分块，仍以格子 Box 为主要几何形状。分块解决了组织方式；自然洞穴的轮廓与变化还需要进一步设计。

## 5. 像素密度：看世界尺寸，也看最终占屏尺寸

**Unity 的规则：**PPU 表示 Sprite 中多少像素对应一个世界单位。Point 过滤保留贴图的块状采样；mipmaps 和过滤方式影响缩小、远处与斜视时的显示。[Unity 2022.3：Sprite Import Settings](https://docs.unity3d.com/2022.3/Documentation/Manual/texture-type-sprite.html)

**本项目建议：**统一的是视觉规则，不必强制所有贴图采用同一个 PPU。角色的有效像素高度、贴图透明留白、地面重复频率与相机距离，都影响最后看起来有多“粗”。

对 SpriteRenderer 或按 PPU 生成的 Sprite Mesh：

```text
世界高度 = 像素高度 / PPU × Transform 的纵向缩放

例如：32 像素 / 32 PPU × 1 = 1 世界单位
```

**手工 Quad 或 MeshFilter 的尺寸由 Mesh 和 Transform 决定。修改 Texture 的 PPU，不会自动缩放已经制作的角色 Quad 或地面 Mesh。**地面的像素密度还取决于 UV、贴图重复次数和所选图集区域。

建议为每类资源固定一个起点，再按 Game View 调整：

| 资源 | 起点 | 重点观察 |
| --- | --- | --- |
| 像素角色 | Point、先关闭有损压缩、脚底 pivot；mipmaps 按距离需求决定 | 脸与轮廓可读、透明边缘干净、动画不跳动 |
| 倾斜地面与墙面 | 先统一 texel density（每世界单位对应的贴图像素数），再比较 mipmaps 和过滤方案 | 远处是否闪烁、斜视是否变成噪点、区块是否接缝 |
| 水与雾 | 根据整体风格控制细节频率 | 运动与光照协调，避免满屏细碎高光 |

不要一次性把所有环境贴图的 mipmaps 关闭。透视镜头下的远处地面需要另行观察。若角色变糊，先检查导入、占屏大小、抗锯齿和景深分别造成了什么影响。

URP 14 的 FXAA、SMAA、TAA 与 MSAA 解决的问题不同；TAA 使用历史帧，可能出现运动拖影，MSAA 则不能解决贴图或高光的全部 aliasing。本项目建议从关闭后处理抗锯齿的基线开始，按实际闪烁与边缘问题选择。[URP 14：Anti-aliasing](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/anti-aliasing.html)

## 6. 角色受光、Billboard 与阴影要分别考虑

**Unity 方案：**像素角色平面可使用 URP Lit，并开启 Alpha Clipping，让低于 Threshold 的透明部分不参与显示。Render Face 可按需要设为 Both。[URP 14：Lit Shader](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/lit-shader.html)

**开发者经验：**TW0CATS 的 Unity 实践指出，竖直 Billboard 从高处观看会变薄；平面在某些光源方向下的投影也会变窄，自身阴影还可能影响角色明暗。文章讨论了通过自定义 shader 改善表现。这些问题值得观察，但其修改代码依赖当时的管线版本。[Billboard 开发实践](https://tw0catsgames.com/update/2023/11/06/perfecting-unitys_billboard_shader_for_hd2d_01.html)

**本项目建议：**把下面三种需求分开判断：

| 需求 | 起点 | 常见问题 |
| --- | --- | --- |
| 角色看起来面对镜头 | Billboard 仅作用于 Visual；根节点与碰撞体保持直立 | 全方向朝向镜头时，角色平面可能显得向后躺 |
| 角色受到环境光影响 | Lit / 自定义受光 shader | 平面的真实法线不等于人物脸部、身体的立体法线 |
| 角色有接地与投影 | 接触阴影、简化脚底影或实际 ShadowCaster，按需求选择 | 顶光下投影很细；补光后角色仍有不合理的自阴影 |

先让角色脚底稳定、亮暗协调，再考虑绘制 normal map、专用 ShadowCaster 或更复杂的 Billboard shader。简化脚底影可以提供接地感，但不会自动替代实际投影的遮挡关系。

本项目已有 `SpriteBillboard.keepUpright`：开启时仅跟随相机 yaw；关闭时使用相机完整旋转。它是 Transform 层面的 Billboard，尚不是处理了各渲染 Pass 的专用角色 shader。

## 7. 洞穴灯光：让空间能读懂，再突出火光

**本项目建议：**以三层光照组织洞穴：

1. **环境底光：**保证暗部还有必要的轮廓和路径信息，避免整张洞穴完全依赖火把照亮。
2. **主光与空间明暗：**让墙顶、侧面、地面产生可辨认的差异；安排亮区与暗区形成层次。
3. **局部光：**火把、入口或其他光源突出局部目标，明暗范围不要平均铺满整个地图。

可以用偏冷、低饱和的环境与较暖的火光形成色彩区分。具体颜色应从同一套参考画面决定，避免所有灯都采用很强的橙色、所有暗部都变成浓蓝色。

火焰“自身亮”和“照亮周围”是两个需求：Emission 提供发光表面，场景照明还需相应 Light 或经过设计的烘焙方案。根据需要分别设置发光强度、灯的 range、阴影和闪烁幅度。

材质也应区分石材、水和金属的反光程度。地面普遍很亮时，先检查 Smoothness、光源与材质，而不是直接压低全场曝光。需要增强接触层次时，可再评估 SSAO 的效果与代价。[URP 14：SSAO](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/post-processing-ssao.html)

## 8. 水面经验：让水有自己的表面行为

**开发者经验：**宫内继介在 GAME Watch 的访谈中解释，他们尝试过像素水，但效果过度、显得不自然；最终采用更接近写实并经过调色的水面。这说明水、角色与地形可以采用不同的表现强度，只要画面整体协调。[开发者水面访谈](https://game.watch.impress.co.jp/docs/interview/1128111.html)

**本项目建议：**水面应有与石材不同的运动、反光和岸边关系。可以按以下顺序完善：

| 层次 | 作用 | Unity 中可采用的方案 |
| --- | --- | --- |
| 水面与河床分离 | 建立表面、深度与岸边高度关系 | 独立水面 Mesh，河床与岸壁另外生成 |
| 连续流动 | 表现流向，避免格子各自抖动 | 使用统一 world-space 坐标或连续 UV 控制流动采样 |
| 变化的法线与高光 | 让水面对光照作出动态响应 | 滚动 normal map（法线贴图，改变表面受光方向），或程序化波纹法线 |
| 岸边过渡 | 让水与地形连接 | 岸边 mask、深浅色过渡；需要时加入克制的泡沫 |
| 环境反射 | 提供周围环境的视觉联系 | 根据需求评估 Reflection Probe 或专门的反射方案 |
| 透明与折射 | 看见河床并产生扰动 | 在确有需要时增加，并处理透明排序与深度问题 |

Reflection Probe 是一种环境反射方案；它不是水面平面反射或折射的同义词。使用自定义水 shader 时，还要核对 shader 是否实际采样反射信息。[URP 14：Reflection Probes](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/lighting/reflection-probes.html)

静止洞穴水未必需要大幅波浪，窄河也未必有明显泡沫。先让流动方向、反光频率和岸边过渡可信，再增加复杂效果。

### 当前项目的水面边界

`CaveWater.shader` 当前已有世界坐标采样、程序化波纹法线、受光高光和岸边颜色变化。它使用 Opaque 渲染，**尚未实现透明、折射，也没有单独的实时平面倒影方案**；当前的深浅色主要来自岸边距离 mask，不是读取真实水深。

其波纹采样还做了像素量化，因此外观可能比八方旅人的水更粗、更程式化。若希望接近后者，可以先比较“连续采样”与“量化采样”的差别，再决定是否保留量化；这些属于下一步实验建议，本文没有修改 shader。

## 9. 后处理按职责调整

**Unity 的规则：**URP 14 的 Gaussian DOF 只处理远景模糊，成本较低；Bokeh DOF 能处理近、远景，质量更高但较慢。Bloom 的 Threshold 控制哪些亮度进入辉光，Intensity 和 Scatter 分别影响强度与扩散。[URP 14：Depth of Field](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/post-processing-depth-of-field.html)、[Bloom](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/post-processing-bloom.html)

**本项目建议：**

- **调色：**统一石材、角色与水面的色彩关系，保留必要的明度差；先解决全局曝光，再调整局部材质。
- **Bloom：**突出火焰和少数高亮区域，避免墙面、地板与角色都蒙上一层光。
- **DOF：**主活动区域保持清晰，让近景与远景承担适量虚化；不要靠模糊掩盖地图轮廓问题。
- **Vignette：**轻微收拢视线即可，避免把边缘通道压暗到难以辨认。

对跟随相机，焦点距离可从玩家附近的目标点沿相机前向的距离取得起点：

```text
focusDistance = dot(目标点世界坐标 - 相机世界坐标, 相机 forward)
```

这是一种 Unity 实现建议，不是八方旅人公开的焦点算法。遇到升降平台、角色高度变化或相机演出时，还需要根据画面调整。

Point 过滤与 DOF 同时存在并不矛盾：前者决定贴图怎么采样，后者决定画面中哪些深度清晰。应让角色所在的主要活动区域保留像素轮廓。

## 10. 为洞穴安排可读的遮挡

**本项目建议：**从当前探索镜头观看，把洞穴理解成可看见内部的空间：

- 后侧墙提供高度与封闭感；近侧墙可以降低视觉高度、分段隐藏或采用角色附近的遮挡处理。
- 河道转弯、墙体缺口与较亮的入口可以帮助玩家识别方向。
- 前景暗部可以增加纵深，但要保留角色经过窄路时的可见性。
- 视觉墙与阻挡可以分开表达：降低显示高度时，原有通行边界仍由正确的 Collider 保持。

实现透明或镂空时，同步考虑 color、depth 与 shadow pass 的行为；只改变最终颜色，可能仍留下不符合预期的深度和阴影。具体 pass 应按当前 URP shader 核对。

## 11. 当前项目的使用方式与制作顺序

本文根据当前项目文件整理了已有基础，不把它们等同于已经完成美术验收：

| 位置 | 当前职责 |
| --- | --- |
| `Assets/Scenes/Development/Develop.unity` | 小房间开发测试 |
| `Assets/Scenes/Gameplay/Cave.unity` | 洞穴地图与当前透视镜头 |
| `Assets/Scenes/CaveHD2DLocal.unity` | 本地河谷台地聚落、整图功能占位、多路线空间与 Cinemachine 固定方向镜头 |
| `Assets/Scripts/Levels/Cave/CaveMap.cs` | 场景 Tilemap 引用、坐标查询与出生点 |
| `Assets/Editor/Cave/CaveMapBuilder.cs` | 分块地面、墙体、水面与 Collider 的派生生成 |
| `Assets/Editor/Cave/CaveMapSceneSaving.cs` | 保存、进入 Play Mode 前同步派生几何 |
| `Assets/Scripts/Core/Rendering/SpriteBillboard.cs` | 角色视觉朝向 |
| `Assets/Shaders/CaveWater.shader` | 当前水面表现 |
| `Assets/Settings/CavePresentation.asset` | 洞穴 Volume 配置 |

**建议制作顺序：**

1. 选一个通道交汇的小区域作为样板，固定镜头，确定角色、墙高、通道与河岸比例。
2. 完成地形轮廓和材质分区，保持 Tilemap 通行规则与视觉变化独立。
3. 在同一区域确定环境底光、局部光与水面表现。
4. 最后调整后处理，把这套尺度、材质与构图规则扩展到整张地图。

程序负责保存与派生生成，地图负责人决定轮廓、路线、视线与区域节奏，资产负责人提供符合约定尺度和材质规则的模块。协作前先约定 pivot、世界尺寸、朝向、受光方式与是否自带 Collider。

不要为美术调整反复改通行数据；也不要为每种视觉变化增加逐格 Update。新增动态灯光、水、透明效果或后处理后，有实际性能问题再用 Profiler 判断主要成本。

本轮文档的人工参考检查可以只选三项：

1. **可读性：**玩家走到转角、近侧墙和河岸时，是否还能看清角色与路线。
2. **协调性：**同一画面中的石材、像素角色与水，是否共享合理的比例、明暗和细节频率。
3. **稳定性：**移动相机时是否出现明显贴图闪烁、水面接缝、阴影跳变或难以辨认的虚化。

## 12. 遇到问题时先查哪里

| 现象 | 优先检查 |
| --- | --- |
| 像铺满方砖，缺少洞穴感 | 外轮廓、墙顶与侧面、长直段的变化、主路线与环境细节的区分 |
| 角色像漂在地面上 | 脚底 pivot、Visual 高度、Mesh 尺寸、接地阴影与碰撞体偏移 |
| 人物缩成薄片或像向后倒 | 镜头俯角、Billboard 是否跟随 pitch、是否误旋转玩家根节点 |
| 水像蓝色石头 | 水与河床是否分离、运动、法线、高光、岸边过渡与所需反射是否存在 |
| 像素看不清 | 占屏尺寸、PPU / Mesh 尺寸、压缩、采样、AA 与 DOF 各自的影响 |
| 画面过亮、发白 | 材质反光、曝光、发光范围与 Bloom，而非只加深整体颜色 |
| 暗部看不清路 | 环境底光、局部明度差、阴影、雾与暗角的叠加 |
| 画面好看但路线难判断 | Game Camera 下的近侧遮挡、目标可见性、地面与边界的明度差 |

## 延伸阅读

- [Unreal Fest Europe 2019：The Fusion of Nostalgia and Novelty in the Development of Octopath Traveler](https://www.unrealengine.com/events/unreal-fest-europe-2019/the-fusion-of-nostalgia-and-novelty-in-the-development-of-octopath-traveler?lang=en-US)：讲演入口，可继续了解初代的视觉发展过程。
- [Unity 2022.3：Grid](https://docs.unity3d.com/2022.3/Documentation/Manual/class-Grid.html)、[Tile Palette](https://docs.unity3d.com/2022.3/Documentation/Manual/Tilemap-Palette.html)：地图编辑与坐标布局的官方说明。

阅读时先看开发者制作过程与取舍，再对照本项目的 Renderer、材质和镜头验证具体用法。文中的经验用于确定下一步工作方向；画面参数仍由实际试玩与人工美术反馈决定。
