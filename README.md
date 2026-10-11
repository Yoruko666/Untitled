# Untitled

Unity 2.5D 经营 / 冒险 Demo

玩法循环：**白天经营→ 经营结算 → 夜晚搜打撤→ 战利品结算 → 进入下一天**。


---

## 环境要求

| 项 | 版本 |
| --- | --- |
| Unity | 2022.3.62f3c1 |
| 渲染管线 | URP 14.0.12 |
| 输入 | Input System 1.14.2 |
| 资源加载 | Addressables 1.22.3 |
| 文本 | TextMeshPro 3.0.7 |

---

## 目录结构

```
Untitled/
├─ Assets/
│  ├─ AddressableAssetsData/          Addressables 配置（UI 面板在 Panel 组，地址 = 面板名）
│  ├─ Art/                            Audio / Materials / Models / Sprites / Textures
│  ├─ Configs/Cave/CaveLayout.asset   洞穴布局配置（CaveMapLayout）
│  ├─ Editor/
│  │  ├─ Cave/                        CaveMapBuilder / CaveMapEditor / CavePresentationEditor
│  │  └─ HybridProps/                 HybridPropWindow（道具素材浏览器）
│  ├─ Input/InputActions.inputactions 输入定义（Player / UI）
│  ├─ Prefabs/
│  │  ├─ UI/Menu.prefab               主菜单面板
│  │  └─ Props/                       可放置道具（Camp / Kitchen / Lighting / TotemsBones）
│  ├─ Scenes/
│  │  ├─ Develop.unity                开发 / 测试场景
│  │  ├─ Cave.unity                   洞穴地图
│  │  ├─ Adventure.unity              夜晚冒险
│  │  ├─ Bussiness.unity              白天经营
│  │  └─ Props/                       HybridPropGallery / ContainerComparison 展厅
│  ├─ Scripts/
│  │  ├─ Core/                        Boot / Manager / Event / Camera / Rendering
│  │  ├─ Data/                        纯 C# 数据（GameData、Inventory、Package、Items、Cave）
│  │  ├─ Debug/                       HybridGalleryViewer
│  │  ├─ Gameplay/                    Entity（Player / Collection）、Adventure、Bussiness
│  │  ├─ Levels/Cave/                 CaveMap / CaveCoordinates
│  │  └─ UI/                          UIManager / PanelBase / UIPanel / MenuPanel
│  ├─ Settings/                       URP 配置
│  └─ Shaders/                        CaveWater、hd2d_pixel_prop
├─ docs/                              道具清单等文档
├─ tools/export_cave_layout.gd        洞穴布局导出脚本（离线）
├─ Packages/
└─ ProjectSettings/
```

---

## 快速入门

### 1. 管理器（Manager）

纯 C# 管理器继承 `ManagerBase<T>`，由 `Bootstrap` 统一注册，之后通过 `Instance` 单例访问：

```7:13:Assets/Scripts/Core/Boot/Bootstrap.cs
private void Awake()
{
    RegisterManager(new GameManager());
    RegisterManager(new InputManager());
    RegisterManager(new UIManager());
    RegisterManager(new InteractManager());
}
```

### 2. 事件中心（EventCenter）

静态事件总线，按 `GameEventType` 订阅，支持 0~3 个参数：

```csharp
EventCenter.Subscribe(GameEventType.Interact, OnInteract);
EventCenter.Subscribe<int>(GameEventType.ScrollSelect, OnScrollSelect);
EventCenter.Unsubscribe(GameEventType.Interact, OnInteract);
EventCenter.Invoke(GameEventType.Interact);
```

### 3. 数据（Data）

纯 C# 数据类：

- `GameData` 聚合 `Money`、`InventoryData`（仓库）、`PackageData`（冒险背包）。
- `InventoryData` / `PackageData` 提供 `AddItem` / `ConsumeItem` / `GetItemCount` / `HasItem` / `Clear`。
- `ItemData`（id + 数量）、`ItemConfig`（名称 / 类型 / 描述 / 图标）。
- 冒险结束时把背包物品结算进仓库，下一轮冒险开始时清空背包。

### 4. UI

使用`UIManager`统一管理，添加UI面板后，于 `UIPanel` 中统一注册：

```csharp
UIManager.Instance.Open(UIPanel.Menu); 
UIManager.Instance.Close(UIPanel.Menu);
UIManager.Instance.Unload(UIPanel.Menu);
```

- 面板脚本继承 `PanelBase`，并重写 `OnInit` / `OnShow` / `OnHide`。

### 5. 交互

可交互对象（可拾取物品、道具、NPC等）实现接口 `IInteractable`。

```csharp
public class Collection : EntityBase, IInteractable
{
    public void Interact() {}
}
```

### 6. 实体与镜头

- `EntityBase`：`Coord` 映射 XZ 平面与世界坐标。
- `CharacterBase`：封装胶囊碰撞体与移动逻辑。
- `PlayerEntity`：封装玩家输入。

### 7. 洞穴地图

- HD-2D 场景分别保留为 `CaveHD2DLocal.unity`（本地基础地形 V4）和 `CaveHD2D.unity`（远端布局与道具摆放）；两份可独立打开编辑。用途、Editor 菜单作用范围与验收说明见 [洞穴场景说明](docs/cave-hd2d-scene.md)。
- `CaveMapLayout`（`Assets/Configs/Cave/CaveLayout.asset`）：格子尺寸、原点、地面 / 墙体 / 河水材质、出生点。
- `CaveMap`：按布局生成地图，提供 `CellToWorld` / `WorldToCell` / `IsWalkable` / `SpawnPosition`。
- `CaveCoordinates`：像素、格子与本地坐标互转。
- Editor 工具在 `Assets/Editor/Cave/`，布局源文件由 `tools/export_cave_layout.gd` 离线导出。


## 可放置道具素材

41 个预制体已按厨房、营地、图腾骨饰、照明整理到 `Assets/Prefabs/Props`，模型与材质位于 `Assets/Art` 对应分类。保留水缸、锅的 3D 版本，并提供对应 HD-2D 版本。

Unity 菜单 **Untitled → Props → 素材浏览器** 可预览、调节大小与放入场景；完整展厅与容器对照场景在 `Assets/Scenes/Props`。详见 [可放置道具说明与清单](docs/placeable_props.md)。

HD-2D 道具由 `HybridProp` 驱动，保持固定摆放朝向；八方向素材通过 `Direction Frame` 选择显示帧，默认 `0`（正面），摄像机移动不会改变朝向或图片帧。照明道具可配合 `HybridFireLight` 提供火光。

---

## 其他

开发约定、目录职责与提交规范见 [AGENTS.md](AGENTS.md)。
