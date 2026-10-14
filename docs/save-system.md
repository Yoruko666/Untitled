# 模块化存档系统

## 架构检查与计划

审查时工作区干净。项目版本为 Unity 2022.3.62f3c1、URP 14.0.12、Input System 1.14.2、Unity Test Framework 1.1.33。

| 模块 | 当前实现 | 本次处理 |
| --- | --- | --- |
| GameManager / Bootstrap | 纯 C# ManagerBase 单例；Bootstrap 按依赖顺序初始化并在 Play Mode 重置静态状态 | 保留，增加 Session 和 SaveManager 注册 |
| GameData | Money、MoneyChanged、InventoryData、PackageData | 复用金币、物品状态；恢复保持引用不变 |
| 仓库 / 探索包裹 | int 物品 ID、数量、列表与运行时索引 | 复用 InventoryData；PackageData 不持久化 |
| 静态物品配置 | Excel → protobuf → Addressables → ItemDatabase | 保持原链路；本项目现有静态数据并非全为 ScriptableObject，不迁移配置 |
| 订单 / 七日剧情 | UIOrderData、HUD 显示缓存；无正式订单或剧情服务 | 新增独立 ID / 进度 / 完成标志和剧情标记数据模块，不编写订单玩法 |
| 场景 | Bussiness、Adventure、Cave 各有 Bootstrap；经营结束 / 冒险结束仅占位 | 提供可注入阶段入口，不接通日夜经营流程 |
| 已有存档 | 未发现 gameplay 存档；关卡 Editor 保存是地图资产工具 | 不改地图、地形或地图工具 |
| 测试 | 已安装 UTF，未发现项目存档测试 | 新增 Editor NUnit 测试，仅使用唯一临时目录 |

第一阶段：SaveData、GameSession 适配、SaveManager、SaveStorage、单槽持久化、数据恢复和自动化测试。测试通过后才进入第二阶段的地下检查点与版本迁移；第一阶段不会把未知旧版本自动当作新游戏。

第二阶段计划（本次未实现）：进入地下前先持久化已验证的检查点，成功后创建带唯一 runId 的临时 RunState；探索产出留在 RunState 中。撤离构造新的完整快照并附结算凭证，持久化成功后才发布金币 / 物品，重复 runId 不得再次提交。失败必须传入明确的结算规则；中途退出直接恢复进入前检查点，不能借用失败惩罚。开发接口模拟奖励与失败，不依赖地下实体或地形。版本迁移采用逐版本转换接口，保留旧文件，转换后通过完整验证才恢复；未知未来版本继续拒绝。

架构为 `GameSession → SaveManager → SaveData → SaveStorage`。存档只包含可序列化的值与 ID，配置资产、Scene、GameObject、MonoBehaviour、Transform、UI 显示对象均不进入 JSON。

DTO 使用 JsonUtility 支持的独立类及列表；不直接保存 Dictionary。参考 [Unity 2022.3 JSON 序列化](https://docs.unity3d.com/2022.3/Documentation/Manual/JSONSerialization.html)。

## 第一阶段使用

Bootstrap 初始化 GameManager 后注册 SaveManager。默认单槽位为 `Application.persistentDataPath/Saves/slot_0.json`，备份为 `.bak`。启动不自动加载、不自动保存、不覆盖已有槽位；新游戏只重置运行时状态。

```csharp
var saves = SaveManager.Instance;
SaveResult result = await saves.NewGameAsync();
// 明确修改运行状态；实际订单与剧情系统后续接入这些接口。
var session = GameManager.Instance.Session;
session.SetProgress(2, GamePhase.Day);
session.GameData.AddMoney(100);
session.GameData.InventoryData.AddItem(101, 3);
session.SetOrder("day2.order.01", 1f, true);
session.SetStoryFlag("intro.finished", true);
result = saves.Save();
result = await saves.LoadAsync();
// 调用方必须处理 result.Success / Error / Message；读取备份会标记 RecoveredFromBackup。
```

默认阶段入口为白天 `Bussiness`、夜晚 `Adventure`，已加入 Build Settings，保留 Develop 为首场景。需要使用其他场景时注入 `new GamePhaseRestorer("YourDayScene", "YourNightScene")`，或实现 `IGamePhaseRestorer`。存档内不保存场景路径。新增场景系统可实现 `ISessionSceneSystem.RestoreSession`，在数据已恢复、UI 发布前初始化自身；该接口不应在初始化时改写存档状态。

加载：读取 / 校验 → 验证版本 → 验证阶段入口 → 恢复共享状态（IsReady=false）→ 进入场景 / 初始化场景系统 → IsReady=true / Changed → HUD、背包与打开的订单面板刷新。新场景 Bootstrap 复用已有 Manager；HUD 在未 ready 时不发布默认值。失败返回明确错误，恢复原运行状态；异步场景加载失败后 Unity 场景本身未必能回滚，调用方应根据错误返回主菜单或重试。

临时文件与主文件同目录，写完 Flush(true) 并重新读取校验后才执行 File.Replace；不支持原子替换的平台返回 Io，不退化为先删除主文件。正常替换把上一份有效主文件保存为备份。主文件损坏时读备份，读取不会修改任何文件；下一次保存保留这份有效备份。双方均损坏时拒绝覆盖，留给用户显式处理。校验和用于检测意外损坏，不用于反作弊。

schemaVersion 初值 1。第一阶段拒绝未知旧版本和未来版本，拒绝覆盖它们，也不会偷偷退回较旧备份。验证 1–7 天、阶段、非负金币、正数量、稳定唯一 ID、订单进度与完成状态、必要模块和大小上限。未知物品 ID 保留，以兼容配置内容调整；已有 UI 会显示未知物品。

## 验证与边界

在 Unity Test Runner 的 EditMode 运行 `Untitled.Tests.SaveSystemTests`。测试覆盖新游戏、往返恢复、隔离 Preview Scene 的生命周期切换、金币和物品索引、Day 1 → Day 2、订单完成、剧情、引用与快照隔离、恢复顺序、加载并发、损坏 / 缺失主文件、备份恢复、未知版本策略、非法数据、路径不可写、主文件 / 备份锁定及有效副本保留。测试不会读取真实 persistentDataPath，不加载或保存项目场景资产。

实际结果：Unity 2022.3.62f3c1 Editor 编译成功；最终 EditMode 20 / 20 通过，0 失败、0 跳过，测试用例耗时约 0.52 秒。`git diff --check` 通过。没有执行生产场景的异步加载试玩、完整 UI 画面验收、Play Mode 测试或完整构建。初始跨场景测试不兼容 EditMode 的建场景限制，已改为隔离 Preview Scene 并通过。

文件修改列表：

- 新增 `Assets/Scripts/Data/Save/SaveData.cs`：独立模块 DTO。
- 新增 `Assets/Scripts/Core/Save/GameSession.cs`：运行状态与快照适配。
- 新增 `Assets/Scripts/Core/Save/SaveManager.cs`：收集、恢复、并发保护及错误返回。
- 新增 `Assets/Scripts/Core/Save/SaveStorage.cs`：JSON 文件、校验和、临时写入、原子替换与备份。
- 新增 `Assets/Scripts/Core/Save/SaveValidator.cs`、`SaveResult.cs`：合法性检查与统一结果。
- 新增 `Assets/Scripts/Core/Save/GamePhaseRestorer.cs`：阶段入口与场景恢复接口。
- 修改 `Assets/Scripts/Core/Boot/Bootstrap.cs`、`Core/Manager/GameManager.cs`：注册与生命周期。
- 修改 `Assets/Scripts/Data/GameData.cs`：保持金币事件的内部恢复接口。
- 修改 `Assets/Scripts/UI/Goblin/UIGameplayReadModel.cs`：以 Session 为权威源、等待恢复完成后重新发布。
- 修改 `ProjectSettings/EditorBuildSettings.asset`：启用已有 Bussiness / Adventure 场景，Develop 仍为首场景。
- 新增 `Assets/Tests/Editor/SaveSystemTests.cs`：20 个用例；新增资源及目录同时提交 `.meta`。
- 新增本文：审查、计划、调用方法、测试结果和限制。

第一阶段没有地下 RunState、退出 / 失败结算、奖励提交、版本迁移转换器，也没有保存按钮、自动保存或自动推进日期。现有 AdventureManager.SettleItem 仍是原有直接加入仓库的占位流程，不能作为防重复地下结算使用。阶段切换与 UI 画面需要人工试玩，未执行完整构建。

人工验收最多三项：在 Bussiness 调用保存并改变金币后加载；验证 Day 2 和已完成订单显示；从夜晚存档加载确认进入 Adventure 且背包保持一致。
