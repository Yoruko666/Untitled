# 可放置道具素材

已整理 41 个可放置、统一缩放的预制体：5 个连续封闭 3D 容器与 36 个 HD-2D 像素面片。包含保留的两款铁锅、三款石水缸 3D 模型，以及它们对应的五个 HD-2D 版本。

## 目录

| 类型 | 目录 |
| --- | --- |
| 可放置预制体 | `Assets/Prefabs/Props/{Kitchen,Camp,TotemsBones,Lighting}` |
| Unity 原生网格 | `Assets/Art/Models/Props/`，按相同类别分区 |
| 材质 | `Assets/Art/Materials/Props/`，`Preview` 存放展厅台座材质 |
| 共用像素材质 Shader | `Assets/Shaders/hd2d_pixel_prop.shader` |
| 物件渲染、火光组件 | `Assets/Scripts/Core/Rendering/HybridProp.cs`、`HybridFireLight.cs` |
| 展厅运行控件 | `Assets/Scripts/Debug/HybridGalleryViewer.cs` |
| 素材浏览器 | `Assets/Editor/HybridProps/HybridPropWindow.cs` |
| 全部 41 个物件展厅 | `Assets/Scenes/Props/HybridPropGallery.unity` |
| 水缸、锅新旧版本对照 | `Assets/Scenes/Props/ContainerComparison.unity` |

30 张原图已经存在于本项目：复用 `Art/Sprites/Props` 与 `Art/References/Kitchen` 的纹理，通过原始字节 SHA-256 匹配并更新引用；不再复制第二套原图。Prefab、网格、材质和场景沿用原 `.meta` GUID，材质和参考图引用改为本项目已有纹理的 GUID。资源与脚本随项目克隆即可使用，无需外部 demo 目录或本机绝对路径。

美术文件使用小写 snake_case；`_3d` 是真实立体容器，`_hd2d` 是受光像素面片。网格与贴图采样、完整轮廓裁切、八方向帧和墙面间距沿用已确认的素材实现。`placeable_props_inventory.json` 是资源索引，不是重建或覆盖 Prefab 的生成配置。

## 使用与验收

1. 用 Unity 2022.3.62f3c1 打开本项目，选择 **Untitled → Props → 素材浏览器**。分类查看、拖动预览改变角度，比较原图与受光效果。
2. 点击 **容器对照**，左列是原有 3D、右列是 HD-2D。根节点 `Transform / Scale` 或 Inspector 的“整体大小”统一缩放；浏览器也可先调“预览与放置大小”再放入场景。
3. 从 `Assets/Prefabs/Props` 拖入场景，或使用浏览器“放入当前场景”（支持 Undo）。根节点放在地面或墙面接触点，壁炬手动调整高度；进入展厅 Play 后可用屏幕按钮和滑杆检查角度、大小。

## 使用边界

- 物件位于 XZ 地面，Y 是高度；根节点采用底部中心支点。BoxCollider 用作摆放占位与三维碰撞。
- HD-2D 面片始终沿竖直轴朝向相机，只旋转 `Visual` 子对象；请通过根节点调整摆放，不要手动旋转 `Visual`。
- 八方向素材在 `HybridProp` Inspector 用 `Direction Frame`（0–7）选择固定显示的图片帧，六套素材默认均为 `0`（正面）。移动或旋转相机、旋转根节点都不会自动切换图片帧；只有一面的素材始终展示同一完整图案。原 `Direction Offset` 的已保存选择会自动沿用。
- HD-2D 面片适合倾斜镜头，并非任意俯仰角的完整立体模型。
- 像素面片参与 URP 3D 光照、透明裁切、阴影与深度遮挡；水缸 JPG 的白底与中性压缩边缘由材质裁切，原图文件不修改。
- 展厅使用 IMGUI 按钮与滑杆，不修改游戏的 Input Actions、Input System 配置或现有玩法场景。编辑器退出 Play 后再摆放与编辑。
- 5 个 3D 容器的原生网格直接复用，没有重新拼接或重新建模。多个物件之间是否相交仍取决于地图摆放位置。

## 素材索引

### 厨房（22）

| 素材 | 类型 | 预制体 |
| --- | --- | --- |
| 铁锅·铭牌 | 3D 网格 | `Assets/Prefabs/Props/Kitchen/cauldron_a_3d.prefab` |
| 铁锅·素面 | 3D 网格 | `Assets/Prefabs/Props/Kitchen/cauldron_b_3d.prefab` |
| 石水缸·窄 | 3D 网格 | `Assets/Prefabs/Props/Kitchen/stone_cistern_narrow_3d.prefab` |
| 石水缸·标准 | 3D 网格 | `Assets/Prefabs/Props/Kitchen/stone_cistern_standard_3d.prefab` |
| 石水缸·宽 | 3D 网格 | `Assets/Prefabs/Props/Kitchen/stone_cistern_wide_3d.prefab` |
| 柴捆·粗 | HD-2D | `Assets/Prefabs/Props/Kitchen/firewood_bundle_hd2d.prefab` |
| 柴捆·细 | HD-2D | `Assets/Prefabs/Props/Kitchen/firewood_slim_hd2d.prefab` |
| 柴捆·叠放 | HD-2D | `Assets/Prefabs/Props/Kitchen/firewood_stack_hd2d.prefab` |
| 散落木柴 | HD-2D | `Assets/Prefabs/Props/Kitchen/firewood_scattered_hd2d.prefab` |
| 像素蔬菜堆 | HD-2D | `Assets/Prefabs/Props/Kitchen/produce_pixel_hd2d.prefab` |
| 蔬菜堆·混合 | HD-2D | `Assets/Prefabs/Props/Kitchen/produce_garden_hd2d.prefab` |
| 蔬菜堆·根茎 | HD-2D | `Assets/Prefabs/Props/Kitchen/produce_roots_hd2d.prefab` |
| 蔬菜堆·叶菜 | HD-2D | `Assets/Prefabs/Props/Kitchen/produce_leaves_hd2d.prefab` |
| 蔬菜堆·葱蒜 | HD-2D | `Assets/Prefabs/Props/Kitchen/produce_onions_hd2d.prefab` |
| 砧板桌·八方向 | HD-2D | `Assets/Prefabs/Props/Kitchen/chopping_table_hd2d.prefab` |
| 石灶与锅 | HD-2D | `Assets/Prefabs/Props/Kitchen/cooking_stove_hd2d.prefab` |
| 晾肉架 | HD-2D | `Assets/Prefabs/Props/Kitchen/meat_rack_hd2d.prefab` |
| 铁锅·铭牌 / HD-2D | HD-2D | `Assets/Prefabs/Props/Kitchen/cauldron_a_hd2d.prefab` |
| 铁锅·素面 / HD-2D | HD-2D | `Assets/Prefabs/Props/Kitchen/cauldron_b_hd2d.prefab` |
| 石水缸·窄 / HD-2D | HD-2D | `Assets/Prefabs/Props/Kitchen/stone_cistern_narrow_hd2d.prefab` |
| 石水缸·标准 / HD-2D | HD-2D | `Assets/Prefabs/Props/Kitchen/stone_cistern_standard_hd2d.prefab` |
| 石水缸·宽 / HD-2D | HD-2D | `Assets/Prefabs/Props/Kitchen/stone_cistern_wide_hd2d.prefab` |

### 营地（5）

| 素材 | 类型 | 预制体 |
| --- | --- | --- |
| 兽皮帐篷 | HD-2D | `Assets/Prefabs/Props/Camp/hide_tent_hd2d.prefab` |
| 草铺·八方向 | HD-2D | `Assets/Prefabs/Props/Camp/straw_bed_hd2d.prefab` |
| 木桩路障 | HD-2D | `Assets/Prefabs/Props/Camp/spike_barrier_hd2d.prefab` |
| 木床·八方向 | HD-2D | `Assets/Prefabs/Props/Camp/wooden_bed_hd2d.prefab` |
| 草人·八方向 | HD-2D | `Assets/Prefabs/Props/Camp/training_dummy_hd2d.prefab` |

### 图腾骨饰（5）

| 素材 | 类型 | 预制体 |
| --- | --- | --- |
| 符文旗 | HD-2D | `Assets/Prefabs/Props/TotemsBones/rune_banner_hd2d.prefab` |
| 兽骨图腾 | HD-2D | `Assets/Prefabs/Props/TotemsBones/beast_totem_hd2d.prefab` |
| 吊环练习架 | HD-2D | `Assets/Prefabs/Props/TotemsBones/ring_training_rack_hd2d.prefab` |
| 骨堆 | HD-2D | `Assets/Prefabs/Props/TotemsBones/bone_pile_hd2d.prefab` |
| 符文盾 | HD-2D | `Assets/Prefabs/Props/TotemsBones/rune_shield_hd2d.prefab` |

### 照明（9）

| 素材 | 类型 | 预制体 |
| --- | --- | --- |
| 篝火 | HD-2D | `Assets/Prefabs/Props/Lighting/campfire_hd2d.prefab` |
| 木三脚架·未点燃 | HD-2D | `Assets/Prefabs/Props/Lighting/wood_tripod_unlit_hd2d.prefab` |
| 木三脚火架 | HD-2D | `Assets/Prefabs/Props/Lighting/wood_tripod_lit_hd2d.prefab` |
| 铁笼火架 | HD-2D | `Assets/Prefabs/Props/Lighting/iron_cage_fire_hd2d.prefab` |
| 铁盆火架 | HD-2D | `Assets/Prefabs/Props/Lighting/iron_bowl_fire_hd2d.prefab` |
| 木座火盆 | HD-2D | `Assets/Prefabs/Props/Lighting/timber_brazier_hd2d.prefab` |
| 骨制火架 | HD-2D | `Assets/Prefabs/Props/Lighting/bone_fire_stand_hd2d.prefab` |
| 木座壁炬·八方向 | HD-2D | `Assets/Prefabs/Props/Lighting/wall_torch_wood_hd2d.prefab` |
| 石座壁炬·八方向 | HD-2D | `Assets/Prefabs/Props/Lighting/wall_torch_stone_hd2d.prefab` |

## 本次整理检查

- Unity 2022.3.62f3c1 编译通过；41 个预制体的脚本、网格、材质、纹理、方向帧和两个预览场景引用检查通过。
- 复用 30 张项目内原图；41 个网格的几何与 UV 内容保持一致，无缺失或重复 GUID。资源和 `.meta` 配对归档。
- 本次未运行 Play Mode 或构建游戏包；场景的视觉和交互按上方三步人工检查。
