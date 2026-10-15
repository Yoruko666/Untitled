# 哥布林奶茶店 Developer Debug Toolkit：项目检查与第一阶段交付

检查开始于 2026-10-14，交付记录于 2026-10-15。以实际脚本、场景配置与本次运行结果为准；本轮只实施第一阶段。

## 项目检查

- Unity **2022.3.62f3c1**；URP 14.0.12、Input System 1.14.2、Addressables 1.22.3、Test Framework 1.1.33。未升级依赖。
- 自研 runtime 为 `Assets/Scripts/{Core,Data,Gameplay,UI,Debug,Levels}`；Editor 为 `Assets/Editor/<Feature>`，使用既有 Assembly-CSharp / Assembly-CSharp-Editor。未为工具拆分整个项目程序集。
- 没有独立 GameSession 类型。Bootstrap 按 Database → Game → Input → UI → Interact → Debug 初始化纯 C# Manager；GameManager.GameData 是共享会话状态。ManagerBase.TryGet 不会自动创建实例。SubsystemRegistration 重置静态状态，同一会话重复 Bootstrap 不重建已初始化的 GameManager。
- GameData 已有 Day、Phase、Money、InventoryData、PackageData、订单实例 ID 列表、能力与剧情标记；这些是共享数据，尚不是完整七日剧情或日夜流程。
- InventoryData 已校验物品目录、正数量、溢出、堆叠与容量；PackageData 复用它。ItemDatabase 的 protobuf 整数主键是权威 ID，ItemCatalog 复制配表与可选 SO 堆叠配置。
- GameSaveStore 提供 JSON 及临时文件替换；GameManager.Initialize 自动读取普通路径 `Application.persistentDataPath/shared-game-v1.json`，显式 TrySave/TryLoad 原地更新状态。DTO 当前版本为 1，没有保存时间或专用测试槽位；无效物品恢复采用跳过策略。调试工具没有借 TryRestore 修改单个字段，也没有修改存档完整性规则。
- UIGameplayReadModel 订阅 GameData.StateChanged，HUD 天数/金币能跟随共享状态。UIInventoryDataSource 订阅 InventoryData.Changed；UIInventoryWindow 与已打开的物品格子能跟随修改。旧 UIGameDataSource 需要业务手动 Publish，不假设所有 Showcase 的 Mock 都能随真实数据更新。
- ShopSceneBindings/ShopStation 提供第一人称玩家、出生点和站点请求；AdventureCaveSession 提供地下玩家、镜头与区域定位。两者可验证当前场景入口，但不是通用场景加载或日夜切换服务。
- BussinessManager/AdventureManager 的日夜结束流程仍有占位；AdventureManager.EndAdventure 重复调用可能重复结算，不在本轮调用或修改。
- 现有 DebugManager/DebugOverlay 是 F1 移速与镜头面板，尚不具备共享状态命令、模态输入焦点或测试场景加载。新工具没有增加第二个 F1 监听者。
- 存在 Develop、Bussiness、MilkTeaShop、Adventure 及地下/展示场景；**Build Settings 只启用了 Develop**。Editor 的场景验证加载不代表 Player 已注册这些场景。
- 检查开始时 Git main 干净。未修改 Scene、Prefab、物品配表、地下玩法或美术资源。

## 可复用接口与缺失接口

| 模块 | 可复用 | 缺失/限制 |
| --- | --- | --- |
| 会话 | GameManager.TryGet、GameData、Bootstrap、ManagerBase.IsInitialized | 无独立 GameSession；工具不得另造会话 |
| 金币 | TryAddMoney、TrySpendMoney、MoneyChanged、StateChanged | 无 SetMoney；工具按差额调用已有加/扣 API |
| 背包 | TryAddItem、ConsumeItem、Clear、HasItem、Items、Changed、Capacity/UsedSlots | 没有需要临时复制的背包业务 |
| 物品 | ItemDatabase.TryGet、ItemCatalog、InventorySettings | ID 来源只使用现有配表 |
| 天数 | GameData.SetDay、StateChanged | 无 Day 1–7 必要剧情/订单依赖与就绪验证服务 |
| 阶段 | GameData.SetPhase、GamePhase | 无完整阶段转换、结算入口和场景切换协调器 |
| 剧情 | StoryFlags、SetStoryFlag | 无剧情标记目录及每天初始化规则；本轮只显示已有标记 |
| 订单 | AcceptedOrders、CompletedOrders、TryAcceptOrder/TryCompleteOrder | 仅实例 ID 集合；没有正式配方、生成、交付、奖励服务 |
| 任务/探索结果 | UIQuestData 等显示模型、PackageData | 无任务进度服务、探索结果协议或防重复结算服务 |
| 存档 | GameSaveStore、GameStateSaveData v1、GameManager.TrySave/TryLoad | 无专用测试槽位、时间字段和完整文件检查工具 |
| UI | UIGameplayReadModel、UIInventoryDataSource、UIInventoryWindow | Mock 显示快照不能当作真实订单/任务状态 |

订单、任务、探索结果在窗口明确显示“功能不可用”，没有使用 ID 操作或 UI 显示快照伪造完整业务。

## 第一阶段实现

`DebugCommandService.Current` 是统一入口，不创建 GameManager、游戏对象或第二份可变状态。请求包含命令及明确参数，结果包含状态、原因、时间与独立只读快照。失败、不可用、需要确认及成功均进入最近 100 条执行日志；阻止游戏事件或命令结果回调重入命令。

EditorWindow 只构造请求、读取服务快照并绘制日志，不直接写 GameData、InventoryData 或存档 DTO。运行时服务不引用 UnityEditor。能力范围：

| 命令 | 行为 |
| --- | --- |
| GetState / GetInventory | 读取当前会话的 Day、Phase、金币、仓库格数/数量、场景及已有订单 ID/剧情标记 |
| AddMoney / SetMoney | 验证正增量/非负目标与整数溢出，调用现有金币 API，沿用 UI 事件 |
| GiveItem / RemoveItem | 验证 ID/数量、现有表与库存，调用背包校验；失败保持数量不变 |
| ClearInventory | 服务要求 Confirmed，窗口提供确认对话框；只清空当前仓库，不清探索包裹 |
| SetDay | 验证 1–7，然后明确报告缺少七日就绪服务；保持状态不变 |
| SetDayMetadata | 需确认，验证 1–7，调用已有 SetDay；结果明确说明仅共享字段/HUD 更新，剧情、订单、任务尚未准备 |
| SetPhase | 验证枚举、当前已加载场景中唯一且启用的店铺/地下入口与玩家；只对齐当前场景阶段。目标场景不符或缺少结算入口时失败，不执行加载/结算 |

阶段测试通过手动打开场景或 Editor 专属验证加载场景，再执行 SetPhase。没有宣称单改阶段就完成地下进入/返回流程。现有玩家、镜头与店铺服务保持原实现。

## 安全与编译范围

工具运行时类型及原有 DebugManager/DebugOverlay 使用统一条件：

```csharp
#if UNITY_EDITOR || (DEVELOPMENT_BUILD && UNTITLED_DEVELOPER_TOOLS)
```

Editor 内可用；Player 必须同时是 Development Build 且显式设置 `UNTITLED_DEVELOPER_TOOLS` 才包含这些类型。仅设置符号而构建 Release 仍不包含它们。Bootstrap 相关注册/重置和 PlayerEntity 调试移速引用同步受条件保护；普通移动保持基础 Speed。不依赖隐藏按钮保护命令。

服务拒绝 Edit Mode、未初始化 GameManager/GameData、空/未知命令及无效参数。只读/拒绝/取消操作不标记调试会话；背包原子添加被容量/溢出拒绝时恢复之前的保存锁状态。

首次状态修改**先**标记 GameManager.DeveloperStateModified，再调用游戏 API 发事件。标记后 GameManager.TrySave 在进行任何 IO 前明确失败，即使 StateChanged/MoneyChanged 回调或场景切换后的代码尝试普通保存，也不能覆盖玩家存档。该会话锁在普通读档后仍保留；本轮没有提供解锁命令。停止 Play Mode 再开始新会话，原 Bootstrap 重建 Manager 后解除。第三阶段实现专用槽位时必须通过独立路径与同样的完整性校验保存测试数据。

本轮未增加测试保存/加载按钮或文件写入命令。验证程序只操作明确隔离的 `Library/DeveloperToolsVerification/ordinary-save-sentinel.json`，不使用玩家路径。

## 使用与验收

工具入口：**Untitled → Developer Tools → Debug Toolkit**。

1. 在 Editor 打开 `Assets/Scenes/Bussiness.unity`，进入 Play Mode 后打开工具窗口；增加金币，检查原 HUD 金币；输入现有表的物品 ID 发放道具，打开仓库再修改数量，检查原格子刷新。
2. 选择 Day 7：“准备指定 Day”应报告缺少服务且不改变状态；“仅修改 Day 数据…”取消时不变，确认后 HUD 变为 Day 7，但结果提示剧情/订单未准备。地下阶段在店铺内应失败；当前店铺阶段可对齐，结算不可用。
3. 用无效物品 ID/非正数量尝试操作，应得到明确失败且保持库存；清空需确认。修改后窗口显示普通保存锁住，停止 Play Mode 再进入恢复新会话。展开执行日志查看参数与原因。

F1：**本轮没有实现 InGameDebugPanel**。原有 F1 面板仍只调移速/镜头，与本次 Editor 命令窗口不同；新状态命令、输入焦点处理与允许命令列表按第二阶段接入统一服务，不通过现有镜头面板冒充完成。

DebugScenario 及 FreshGame / ShopOrderTest / ShopDeliveryTest / DungeonReturnTest / Day7Test 五个资产：**本轮未创建**，按第三阶段实施。创建时要分别报告缺少的真实依赖，不预先创建看似完成但无法准备业务状态的资产。

自动检查入口：**Untitled → Developer Tools → Development Tests** → Run Edit Mode Guards / Run Phase 1 Checks。后者会临时加载店铺与地下测试场景，应先保存当前场景；结束后在交互 Editor 中恢复已保存场景列表。未保存的非空场景被拒绝，避免丢失编辑内容。

编译开关检查：先让 Unity 导入/编译项目，再在项目根运行 `python3 tools/check_developer_tools_compilation.py`。使用当前 Unity 自带 Roslyn 和缓存 runtime 引用，移除 Editor 引用/宏，在 Library 下输出四组独立程序集并以 Cecil 检查实际类型与直接程序集引用。**这不是完整 Player 构建或最终包验证**，缓存的 Package 引用仍来自 Editor 编译产物。

## 测试结果

| 检查 | 本轮结果 |
| --- | --- |
| runtime / Editor C# 编译 | 项目 Unity 版本实际编译通过，无 error CS |
| Edit Mode/空命令/未知命令保护 | RunEditModeChecks 通过，进程正常退出 0 |
| 缺少会话 | Play Mode 空场景下命令失败，无静默创建或成功 |
| 金币与原 HUD | 加金币、减少目标、目标 0、溢出拒绝；真实 UIGameplayReadModel 同步更新 |
| 背包与原 UI | 真实仓库窗口及物品格子随发放/删除更新；缺失 ID、非正数量、不足库存、容量不足安全拒绝；确认清空有效；先前只读快照保持独立 |
| Day 就绪/确认 | 0/8 拒绝，完整 Day 7 准备报告不可用且保持 Day；元数据修改确认后更新 HUD；未确认不变 |
| 阶段一致性 | 空场景/店铺内地下阶段拒绝；真实店铺、真实地下中对齐相应阶段成功，原日夜 HUD 对应场景；结算拒绝 |
| 重入/执行日志 | MoneyChanged 回调重入命令被拒绝并保留日志；成功、失败、不可用和确认结果可查看 |
| 场景切换唯一性 | Bussiness → Adventure → Bussiness，GameManager 与 GameData 引用保持，Day/金币保持，未创建持久对象 |
| 普通存档保护 | 首次修改的 MoneyChanged 回调内 TrySave 已失败；跨场景锁仍保持；隔离普通存档哨兵文件字节完全不变 |
| Release 编译隔离 | Release、Release + opt-in 符号、默认 Development 三组均不含 DebugCommandService/请求/结果与原 DebugManager/DebugOverlay 类型；显式 opt-in Development 存在这些类型，四组实际编译与元数据检查通过，无直接 Editor 程序集引用 |
| F1/输入焦点 | 新面板尚未实施，未验证新 F1 开关或焦点；保留原面板，不宣称第二阶段已完成 |
| 预设/专用测试存档/无效场景跳转 | 尚未实施，未验证 |
| 完整 Release Player 包 | 尚未构建，不能以宏编译检查代替最终包验收 |

实际输出：`DEVELOPER_TOOLS_EDIT_GUARDS_PASSED`、`DEVELOPER_TOOLS_PHASE1_CHECKS_COMPLETE`、`DEVELOPER_TOOLS_PHASE1_PASSED`、`DEVELOPER_TOOLS_COMPILATION_GATES_PASSED`。Play Mode 结果 `Library/DeveloperToolsVerification/phase1.txt` 为 PASSED，Error/Exception/Assert 计数 0。原始日志 `/private/tmp/developer-tools-edit.log`、`/private/tmp/developer-tools-play.log`；编译与元数据结果在 `Library/DeveloperToolsVerification/Compilation`。

最初沙箱启动 Unity 不能连接授权客户端，停止本任务进程后正常权限重试完成。Play Mode 批处理已完成全部检查并退出 Play，但 Unity 退出时停在 `Leftover web requests after shut down`，清理了本任务进程；不能记为该批处理进程正常退出 0。这是已有环境行为，测试 PASSED 与退出清理分别记录。

未进行图形窗口布局截图、手工键鼠试玩或完整 Player 构建；上述三个人工验收步骤仍有用。本轮没有修改原 UI 展示主题或 Mock 的行为。

## 实际文件清单

| 文件（相对 Unity 项目根） | 新增/修改 |
| --- | --- |
| Assets/Scripts/Debug/DeveloperTools/DebugCommand.cs | 新增：请求、结果、状态及独立只读快照 |
| Assets/Scripts/Debug/DeveloperTools/DebugCommandService.cs | 新增：统一命令、校验、就绪限制、保存保护标记与日志 |
| Assets/Editor/DeveloperTools/DeveloperToolWindow.cs | 新增：Editor 操作与状态窗口 |
| Assets/Editor/DeveloperTools/DebugLogPanel.cs | 新增：命令结果日志视图 |
| Assets/Editor/DeveloperTools/DeveloperToolsVerification.cs | 新增：当前阶段专属 Edit/Play Mode 验证 |
| Assets/Scripts/Core/Manager/GameManager.cs | 修改：调试会话普通保存保护，保留原路径及完整性服务 |
| Assets/Scripts/Core/Boot/Bootstrap.cs | 修改：调试服务注册/重置编译开关 |
| Assets/Scripts/Debug/DebugManager.cs | 修改：原面板编译开关 |
| Assets/Scripts/Debug/DebugOverlay.cs | 修改：原 F1 面板编译开关 |
| Assets/Scripts/Gameplay/Entity/Player/PlayerEntity.cs | 修改：调试移速引用编译开关，默认基础速度 |
| tools/check_developer_tools_compilation.py | 新增：当前阶段编译门槛与元数据验证，不改 PlayerSettings |
| docs/developer-debug-toolkit-phase1.md | 新增：实际检查、接口、使用、限制与验证记录 |

新增 Unity 脚本与目录均附 .meta，原 GUID 保留。Library、临时日志和编译产物不提交。

## 后续边界与已知问题

- 第二阶段：InGameDebugPanel 接入服务、F1 与模态输入焦点/暂停/光标恢复；合并原有 F1 入口，避免两个面板同时响应。场景加载须验证存在及 Build Settings 注册，选择有效出生点并复用现有会话；真实订单/任务未落地时继续显示不可用。
- 第三阶段：DebugScenario、五个预设及每个依赖报告、专用测试槽位与版本/天数/时间/文件验证、跨场景恢复；补充预设/存档/F1 与实际 Player Release 构建验证。
- 当前可改 Day 元数据，不能准备任意可玩的剧情天数；当前可对齐已有场景阶段，不能创建结算业务或自动转换场景。
- 原镜头 F1 面板暂未统一到新命令服务，也没有获得新面板的输入焦点能力；本轮的新共享状态 UI 全部通过统一服务，原镜头面板合并属于第二阶段。
- 调试服务以 Unity 主线程调用为前提，不提供后台线程并发执行。游戏事件抛异常时返回明确失败并保留保存锁，但现有游戏 API 的事件异常可能发生在状态修改后，结果提示查看当前状态，不伪造事务回滚。
- 普通保存锁只保护本工具通过 GameManager 共享会话产生的修改；项目代码直接实例化 GameSaveStore 并自行写玩家路径的行为不由本工具接管。当前 runtime 未发现这类普通保存调用。
