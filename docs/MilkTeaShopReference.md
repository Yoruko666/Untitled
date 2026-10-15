# 奶茶店参考场景

打开 `Assets/Scenes/MilkTeaShopReference.unity`，或使用菜单 **Untitled → Milk Tea Shop → Open Reference Interior**。场景已经保存在 Unity 中，当前处于编辑模式。

WASD 移动，鼠标观察，E 交互，Esc 释放或恢复鼠标。前台左侧留有员工通道，可绕过服务台进入中间走道。

![Unity 场景摄像机预览](milk-tea-reference/preview.png)

预览由 Unity 场景摄像机渲染，运行时使用现有的准星与交互提示。

## 布局与资源

- 室内约 3 × 7 米，净高 3.2 米；中央工作走道约 1.6 米宽。工作台高约 0.9 米，前台高约 1.05 米。
- 左侧为三联饮品菜单、嵌入式十二格小料台、茶桶、糖浆瓶和清洗区；右侧为封杯机、透明杯、成品、包装袋与冰箱。木梁、暖光吊灯、双层收纳架、盆栽和垂藤参考了提供的图片。
- 后方保留石拱洞口，补建岩壁、阶梯、扶手与灯笼，并使用匹配拱形的墙面封边避免漏出背景。
- 玩家胶囊沿用现有配置；本场景眼位为脚底以上 1.55 米，透视 FOV 为 64°。未修改共享玩家 Prefab、输入定义或其他场景。

使用用户提供的 `D:/download/MilkTeaShop_Package_v2/MilkTeaShop_V2/MilkTea_Assets_49.blend`，原文件保持原样。交付包含 33 种模型：23 种直接复用，2 种改造，8 种补建。

| 文件 | 用途 |
|---|---|
| `tools/art/milk_tea_reference.blend` | 可编辑的 Blender 资产库，保留分件模型和资产预览实例 |
| `tools/build_milk_tea_reference.py` | 从原资产库导出并补建本场景模型的脚本，使用 Blender 后台模式执行 |
| `Assets/Art/Models/MilkTeaReference/` | 米制 FBX、来源与材质清单 |
| `Assets/Art/Materials/MilkTeaReference/` | 本场景独立的 URP 材质 |
| `Assets/Prefabs/MilkTeaReference/` | 模型 Prefab 与完整房间 Prefab |
| `Assets/Settings/MilkTeaReferencePresentation.asset` | 暖色灯光表现使用的后处理配置 |
| `Assets/Editor/MilkTeaShop/MilkTeaReferenceBuilder.cs` | 显式创建新场景的入口；已有场景需直接编辑，不自动覆盖 |

房间 Prefab 按建筑、点位、工作站、收纳装饰、灯光分组。运行时读取保存的摆放，没有自动重建布局。

## 检查与范围

- Blender 导出时将直接复用模型与原包标注的尺寸对照；Unity 导入后检查 FBX 的米制尺寸与材质映射。
- Unity 脚本编译通过。出生点、前台左侧至洞口的四段胶囊通道检查，以及八个工作站的射线识别检查通过；未发现缺失脚本或材质。
- 进入过 Play Mode，Console 未发现错误或警告，结束后退出并保存场景。未进行完整构建，也未模拟完整的经营流程。
- 复用 `ShopFirstPersonController`、`ShopRaycastInteractor`、`ShopStation` 和 `ShopSceneBindings`。备料、菜单、清洗、封杯、出杯、仓储、洞口和前台均有可编辑的交互引用。
- 交互仍是现有的提示、反馈和事件接口；订单、扣料、金币与洞穴场景切换没有在本次实现。

## 人工验收

1. Play 后从前台左侧绕入工作区，再走向洞口，确认通行与转身手感。
2. 对准小料台、封杯机、冰箱等位置按 E，确认提示与反馈。
3. 检查默认视角、暖光与道具密度；需要调整时直接编辑房间 Prefab。
