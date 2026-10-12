# 洞穴岩壁美术资源

## 风格基准

以用户提供的[哥布林角色图](images/goblin_art_style_reference.png)为角色美术基准，以[洞穴草图](images/cave_settlement_reference.png)为布局、自然岩层和冷暖关系参考。角色使用土棕、橄榄绿、灰褐色与克制的像素笔触；岩壁使用低饱和灰褐色，纹理对比低于人物，火光交给 Unity 灯光。

角色图片分辨率不决定地图世界尺寸。地面与岩壁需要协调可见像素大小，不能只把所有图片设成同一个 PPU。3D 材质的颗粒尺寸由纹理内容和世界平铺尺寸共同决定。

## 当前交付

- `Assets/Prefabs/Terrain/cliff_straight_01.prefab`：可摆放的封闭岩壁，宽 4、高约 4.12、厚约 1.72 世界单位，底部中心 pivot，正面朝本地 -Z，带静态 MeshCollider。
- `Assets/Art/Textures/CaveHD2D/cave_cliff_rock_candidate.png`：按角色与洞穴草图生成的岩壁颜色纹理候选，实际 1254×1254、完全不透明。保留工具原始输出，未缩小或量化成严格像素图。
- `Assets/Art/Materials/CaveHD2D/cliff_module_rock.mat`：接入该纹理，使用项目现有 Cave Terrain Shader 的世界坐标投影；平铺尺寸约 4 世界单位，纹理强度 0.8，粗糙无金属。

图片导入设置为 Default、Point、Repeat、无压缩、关闭 mipmaps、不自动缩放非二次幂尺寸。低俯角下远处是否闪烁、是否需要 mipmaps，按最终镜头人工判断。

已查看单图和 3×3 平铺预览。纹理有大块岩片与细裂隙，存在重复图案；不保证裂隙跨边缘严格连续，也不保证满足提示词要求的统一像素网格与色数。当前可用于场景试贴，不能视为已经通过美术验收的最终素材。未运行 Unity 导入、Shader 编译或 Play Mode 验证。

Map 场景中的样例已经引用该岩壁材质；如果 Unity 当前仍显示旧颜色，先等待导入，再重新加载保存的场景。Map 场景及用户 Terrain 仍属于未提交工作区内容。

## 接下来需要的资源

| 优先级 | 资源 | 类型与规格建议 | 用途和制作方式 |
| --- | --- | --- | --- |
| 当前 | 岩壁颜色纹理 | 不透明、无透视、可平铺；严格像素版建议原生 128×128 | 已生成候选并接入。若需严格像素输出，可由用户生成后提供原图 |
| 下一步 | 崖顶岩土纹理 | 原生 128×128，不透明、顶视、低对比，沿用同一灰褐色系 | 岩顶与 Terrain 泥土交界；可以继续生成，需单独接入顶面材质 |
| 下一步 | 外转角、内转角和第二种直壁 | 真正的 3D Mesh，端点与 4 单位模块匹配 | 可以直接建模和组装；AI 生成的二维转角图不能代替模型 |
| 下一步 | 崖脚碎石、大岩块 | 3～5 种简单 3D Mesh，共用岩石材质 | 遮住接缝和打破重复；可以直接制作，不要求用户生成图片 |
| 后续 | 苔藓与湿岩变体 | 同母版的纹理，灰绿、面积克制 | 河岸与低地局部使用；可以继续生成 |
| 后续 | 岩土过渡边缘 | 不规则边缘 Mesh，或专门的混合材质 | 连接崖顶、泥土与岩壁。仅生成 PNG 不会自动实现材质混合 |

现有厨房、营地、火把、骨饰、酒馆与仓储像素图可作为后续候选，应先在相同镜头中统一尺寸、颜色与颗粒密度。当前岩壁阶段不用同时生成整套建筑和道具。

## 用户自行生成时的要求

每次附上哥布林角色图与选定岩石母版。岩壁和崖顶输出完整、不透明的材质方图；不要输出带透视的墙体、包含边框的图集或渲染好的场景。要求平铺，输出原文件，并一起提供实际分辨率。128×128 是本阶段建议，不是八方旅人的官方标准。

岩壁简短提示词：

> 可无缝平铺的像素岩石材质，128×128。参考附图哥布林的土棕、橄榄灰和克制像素笔触。低饱和灰褐色天然岩层，大块面、少量短裂隙、低对比。完整不透明方图，正面平铺材质，无透视、无墙体轮廓、无边框；中性光照，不画投影和高光；不要砖墙和干裂泥土。

崖顶简短提示词：

> 可无缝平铺的像素岩土材质，128×128，正上方视图。沿用附图岩石母版的灰褐色调与像素颗粒大小，平整岩面夹少量踩实细土，安静低对比，无大面积裂纹。完整不透明方图，中性光照，无投影、无高光、无透视、无边框、无植物和孤立物件。

## 本次 GPT 生成记录

使用 imagegen 技能的内置 image_gen 工具，角色与洞穴草图作为风格参考，生成新纹理；不是对原角色图进行编辑。选用收到用户角色图之后生成的版本；更早的占位人物参考版本未导入项目。

最终完整提示词：

```text
Asset: a single seamless tileable flat albedo ROCK MATERIAL texture for a 3D HD-2D goblin cave cliff in Unity. Reference image 1 is the definitive character art: hand-crafted fine pixel clusters, subdued earthy browns, olive grays, warm beige and charcoal. Match its mature rough pixel-art language and restrained palette, NOT chibi or toy-like block art. Do not draw goblins or crates. Reference image 2 is the cave background composition, use only its natural gray-brown geology, not its painted illustration style or lighting. Produce one opaque square, edge-to-edge, flat material tile. Muted warm gray-brown rock, 8-12 discrete subdued colors, low contrast, several broad irregular geological areas and sparse short angular fissures. Slight layer breaks, not masonry or repeated bricks, no mud crack network. Crisp deliberate pixel clusters on a uniform logical 128x128 grid; if native output is larger, visually enlarge solid square pixel cells with nearest-neighbor appearance. No subpixel speckle, antialiasing, gradients or photographic detail. Flat neutral illumination; no baked shine, shading gradients, cast shadows or ambient black cavities, Unity will light the 3D mesh. No top ledge, wall outline, perspective, labels, border, vegetation or isolated objects. Perfect periodic left-right and top-bottom continuity for repeating across a large cliff. Quiet background detail to support the expressive goblin character. Single standalone texture, not a sprite sheet, not a scene, not a repeated preview.
```
