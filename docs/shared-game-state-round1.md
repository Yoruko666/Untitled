# 共享游戏系统：第一轮检查与实施计划

检查时间：2026-10-14。检查对象为实际代码、场景引用、Package 清单和 Git 工作区。

## 项目检查

- Unity 2022.3.62f3c1，URP 14.0.12，Input System 1.14.2，Addressables 1.22.3，Test Framework 1.1.33；不升级依赖。
- 自研 runtime 位于 Assets/Scripts/{Core,Data,Gameplay,UI,Debug,Levels}，Editor 位于 Assets/Editor。自研使用 Assembly-CSharp / Assembly-CSharp-Editor；第三方水体、TileWorldCreator 有独立 asmdef。本轮不拆分程序集。
- Bootstrap 按 Database → Game → Input → UI → Interact → Debug 初始化。ManagerBase 的会话实例已在 SubsystemRegistration 重置；同一会话重复 Bootstrap 不重建 GameData。这可以复用，不需要新的单例或 DontDestroyOnLoad 游戏状态对象。
- GameData 已有金币、MoneyChanged、仓库 InventoryData、探索 PackageData。金币仍是公共字段；背包 Items/Id/Num 可外部修改。仓库、包裹两套算法相同，未校验负数、零、整数溢出、无效 ID，无事件、容量或恢复能力。
- ItemDatabase 从 Config/Table/Item.xlsx 导出 Protobuf 后由 Addressables 加载。整数主键是现有稳定 ID。生成代码不能手改；新增 SO 不复制名称、描述或另建 ID 权威来源。
- UI/Goblin 已提供只读数据模型、InventoryPresenter、InventoryPanel、InventoryWindow 和 HUD。UIGameplayReadModel 初始化 InventoryData.Items，打开仓库时手动 Publish；InventoryWindow 只绑定一次快照，已打开窗口不会跟随数量变化。UI 没有直接扣材料，保留该职责边界。
- ShopSceneBindings/ShopStation、ShopRaycastInteractor、ShopFirstPersonController 已完成站点请求和第一人称交互。UIManager 管理 Addressables 窗口、模态暂停和换场景清理；不重写这些系统。
- 正式订单业务不存在。UIOrderData、UIOrderCard、WorkRequested 是显示/请求接口；Mock 与正式来源分离。BussinessManager 和 AdventureManager 的自动日夜流程仍是占位。AdventureManager 重复 EndAdventure 会重复结算包裹，本轮记录而不修改地下逻辑。
- 无当前天数/阶段的权威状态、订单历史、能力/剧情标记、存档。HUD 静态 sessionDisplay 只是显示缓存，不能当存档。
- Build Settings 仅启用 Develop；Bussiness、MilkTeaShop、Adventure 和多种洞穴/展示场景已存在。现有验证使用 EditorSceneManager 在 Play Mode 载入场景，不代表 Player 场景切换已经接通。
- 检查开始时 Git main 干净。地下相关路径有近期提交，docs/cave-map-redesign-plan.md 记录地图调整。Git 干净不能证明其他成员没有在其他 checkout 或未保存 Editor 中编辑；本轮不操作 Assets/Scripts/Levels/Cave、Assets/Editor/Cave、AdventureManager、地下资源或任何场景。

## 第一轮实施范围

1. 扩展现有 GameData：天数、阶段、金币、仓库、包裹、接受/完成订单实例 ID、能力 ID、剧情标记；通过方法修改并发事件。订单生成/配方/交付规则留待后续。
2. InventoryData 统一添加/消耗/查询/清空/恢复算法。Items 与 ItemData 对外只读，保留现有 AddItem/ConsumeItem/GetItemCount/HasItem 入口；PackageData 复用该算法。
3. 增加 ItemDefinition（SO，现有表主键 + 每堆上限）与 InventorySettings（SO，容量可禁用）。物品表仍权威；生产背包以表校验 ID。默认无限容量、单 ID 累加行为与之前一致。UI 显示按 ID 汇总的数量，容量按实际需要的堆叠格计算。
4. 增加纯可序列化存档 DTO 和 JSON 文件服务；只保存 ID、数量、数值、字符串列表，不保存 GameObject/Sprite/ScriptableObject/Dictionary。恢复原地更新 GameData/InventoryData，保持既有 UI 引用。无效物品记录跳过，读档错误不改变当前状态。
5. GameManager 暴露保存/加载，Bootstrap 注入已有数据库和可选设置；启动加载已有存档。显式保存，不在 Editor 停止试玩时自动写存档。
6. InventoryDataSource 订阅背包事件，InventoryWindow 可绑定只读模型并对称退订；保留旧快照和列表接口供已有 Showcase 验证使用。正式 HUD 天数读取 GameData。
7. 沿用现有 Editor 自动化验证方式增加任务专属测试，不引入新测试框架、不移动所有 runtime 到新 asmdef。验证结果在完成后补入本文件。

## 建议修改文件

修改：Data/GameData.cs、Data/Inventory/InventoryData.cs、Data/Items/ItemData.cs、Data/Package/PackageData.cs、Core/Manager/GameManager.cs、Core/Boot/Bootstrap.cs、UI/Goblin/UIInventoryDataSource.cs、UI/Goblin/UIInventoryWindow.cs、UI/Goblin/UIGameplayReadModel.cs，以及旧 HUD 验证的天数注入行。

新增：Data/Items/ItemDefinition.cs、ItemCatalog.cs、Data/Inventory/InventorySettings.cs、Data/Save/GameStateSaveData.cs、Core/Save/GameSaveStore.cs、Editor/SharedGame/SharedGameVerification.cs（及所需 Play Mode 验证）。新增 Unity 文件附带 meta。

## 下一轮边界

地下协议应接收任务所需材料 ID/数量和独立探索会话 ID，返回获得物品与必要结果 ID；共享服务校验后统一结算，防止重复领取和容量失败导致丢失。协议不引用店铺 UI、制作台、玩家 Transform 或地下地图对象。以明显标记的 Editor 开发工具测试进入/收集/返回；当前轮不实现协议、模拟器、地下地形、AI 或战斗。


## 第一轮完成结果

共享状态仍由已有 GameManager 持有。GameData 的 Day、Phase、Money、InventoryData、PackageData 和进度列表对外只读，修改入口触发事件；金币保留 MoneyChanged，其余共享进度触发 StateChanged。库存变化使用各容器的 Changed，不需要逐帧轮询。

订单保存的是实例 ID：AcceptedOrders 为尚未完成的已接受订单；TryCompleteOrder 将其转入 CompletedOrders。没有增加顾客生成、制作、配方、奖励或 UI 标签的业务规则。订单展示快照仍由现有业务发布接口提供，展示文字不进入存档。阶段由后续流程服务显式 SetPhase，不通过 UI 主题或场景名称反推；正式 HUD 的天数/金币已读取共享权威状态。

InventoryData 使用同一份列表/索引；Items 为只读集合，ItemData 的 Id/Num 不允许外部赋值。TryAddItem 为原子单物品添加，容量不足、未知 ID、非正数量或溢出时整个操作失败、不发送事件。ConsumeItem 校验数量，归零移除。PackageData 继承同一数据容器，继续保留原有调用入口，不改变地下探索/结算脚本。

ItemDefinition 是对已有物品表的堆叠配置扩展 SO：ItemId 使用表的主键，MaxStack 表示每堆上限。ItemCatalog 将表和可选 SO 设置复制为只读会话目录；无效/重复 SO 引用跳过并警告，避免第二份 ID/名称权威源。未配置的有效物品默认可累计至 int.MaxValue，保持既有行为。InventorySettings.Capacity 为 0 时无限制，否则按堆叠所需格数计容量；UI 仍按 ID 汇总显示数量，不新增拖拽/拆堆玩法。降低容量后读档仍保留有效物品，消耗可以腾出格子。

GameStateSaveData 是可序列化 DTO，保存版本、天数、阶段、金币、两容器 ID/数量及进度字符串列表。GameSaveStore 先完整写入临时文件，再替换旧文件。GameManager.TryLoad 原地恢复状态，不替换 GameData 或背包引用，已打开的 UI 自动刷新。错误版本、负金币、无效天数/阶段及损坏文件拒绝加载；物品记录逐条过滤，合并重复 ID，返回跳过计数。未知物品被跳过，后续恢复定义不会自动找回已跳过记录；不自动覆盖原存档。

参数为空的 InventoryData/GameData 构造保留旧的独立调用方式，只验证正整数 ID；生产 GameManager 始终注入 ItemCatalog，并以物品表校验。新业务请注入目录，不把无目录实例作为正式状态来源。

## 接入与验收入口

1. 若需要容量/堆叠限制，通过 Assets → Create → Untitled → Inventory 创建 Item Stack Definition 和 Inventory Settings，填入现有表 ID，将设置绑定到启动场景 Bootstrap 的 inventorySettings。SO 设置在会话启动时复制；后续场景 Bootstrap 不覆盖已初始化的服务。现有场景未修改，默认无限容量。
2. 新业务使用 game.InventoryData.TryAddItem(itemId, count) 并检查返回值；消耗使用 ConsumeItem。用 SetDay、SetPhase、TryAcceptOrder、TryCompleteOrder、UnlockAbility、SetStoryFlag 修改进度。不要修改 UI 快照或以 UI 文本推断物品数量。
3. 已有 GameManager.TrySave(out error) 显式保存，TryLoad(out error) 显式恢复。启动自动加载 Application.persistentDataPath/shared-game-v1.json；不自动在停止 Editor Play 时保存。正式保存按钮及日夜流程接入留待业务阶段。

开发检查菜单位于 **Untitled → Shared Game → Development Tests**：Run Data Checks、Run Shop Integration Checks、Verify Saved Session After Restart。使用前保存当前场景；场景集成检查临时进入空场景再载入已保存的 Bussiness/Adventure，不写回场景，结束 Play Mode。验证工具只存在 Editor 程序集，名称明确标记 DEVELOPMENT ONLY；没有将其挂入正式场景或 Prefab。

## 实际验证结果

使用项目版本 Unity 2022.3.62f3c1，在真实项目执行编译与无图形批处理检查。

| 验收项 | 结果与证据 |
| --- | --- |
| C# 编译 | 全项目 runtime/Editor 编译通过，无 error CS；新增 SO 和脚本由 Unity 正常导入。 |
| 添加材料后 UI 更新 | 真实 Bussiness 仓库窗口打开后从 4 增至 6，窗口快照和实际格子绑定均更新。 |
| 消耗数量/UI | 从 6 消耗至 3，窗口自动刷新；零/负数/不足数量被拒绝，归零移除。 |
| 地下模拟结果入库 | **本轮未实现/未运行**，按用户第一轮范围留到协议与开发模拟器阶段；没有以直接 AddItem 冒充地下结算验证。 |
| 跨场景状态 | 真实 Bussiness → Adventure → Bussiness，GameManager、GameData 引用、材料数量、金币、天数、阶段、订单 ID、能力和剧情标记保持；日/夜 HUD 原绑定正常。 |
| 保存并重启 | 先验证 Manager 显式保存/加载及已打开 UI 刷新；另启 Unity 进程读取之前的 DTO 文件通过，再另启 Play Mode 进程验证 GameManager.Initialize 自动加载及店铺 HUD 的第 4 天/37 金币。 |
| 无效物品/损坏数据 | 未知/零/负 ID 添加拒绝；读档跳过 null、未知 ID、非正数量及溢出重复记录，不崩溃；坏 JSON、缺失文件/字段、错误版本不会修改当前状态。 |
| 已有店铺/UI 回归 | 仓库站点打开/关闭与重新绑定、制作台订单快照、暂停菜单、第一人称模态输入阻断通过；关闭后恢复控制。 |
| 额外数据检查 | 5 组自动化检查覆盖堆叠格计算、可禁用容量、超容量恢复、事件次数、整数溢出、订单/能力/剧情去重、快照隔离、文件替换、空状态保护；实际 SO 配置与运行时目录隔离也在 Play Mode 验证。 |

测试期间 runtime Error/Exception/Assert 为 0。主要输出标记：SHARED_GAME_DATA_PASSED、SHARED_GAME_RESTART_PASSED、SHARED_GAME_SHOP_INTEGRATION_PASSED、SHARED_GAME_MANAGER_RESTART_PASSED。

测试存档和结果位于忽略的 Library/SharedGameVerification，未覆盖正式存档。批处理原始日志在 /private/tmp/shared-game-data.log、shared-game-play.log、shared-game-restart-data.log、shared-game-restart-play.log。开始时沙箱启动不能连接授权客户端，正常权限重试后测试执行成功；两个 Play Mode 批处理在已输出 PASSED、结束 Play、关闭系统后停留于退出阶段的“Leftover web requests”，清理了本任务启动的进程。该退出问题不记为正常进程退出；数据检查批处理进程正常退出 0。

未执行 Player 构建、图形渲染/截图检查、完整键鼠试玩或全部 Showcase 回归；上述运行测试不能替代这些验证。Git diff 确认没有任何 Scene、Prefab、地下关卡、地下逻辑、第三方或生成代码修改。

## 修改文件清单

| 文件（相对项目根） | 改动 |
| --- | --- |
| Assets/Scripts/Data/GameData.cs | 共享状态、只读进度、事件、快照和恢复。 |
| Assets/Scripts/Data/Inventory/InventoryData.cs | 校验、堆叠/容量、只读容器、变化事件、存档恢复。 |
| Assets/Scripts/Data/Items/ItemData.cs | 对外只读 ID/数量。 |
| Assets/Scripts/Data/Package/PackageData.cs | 复用 InventoryData，保留旧 API。 |
| Assets/Scripts/Core/Manager/GameManager.cs | 配置注入、独立存取接口、启动恢复。 |
| Assets/Scripts/Core/Boot/Bootstrap.cs | 注入可选 InventorySettings。 |
| Assets/Scripts/UI/Goblin/UIInventoryDataSource.cs | 订阅/退订库存事件，保留旧列表接口。 |
| Assets/Scripts/UI/Goblin/UIInventoryWindow.cs | 绑定实时只读模型，关闭/销毁退订，保留快照入口。 |
| Assets/Scripts/UI/Goblin/UIGameplayReadModel.cs | 天数/金币读共享数据，仓库窗口绑定实时来源。 |
| Assets/Editor/GoblinUI/GoblinUIHUDVerification.cs | 原验证第 4 天改为通过 GameData.SetDay 注入。 |
| Assets/Scripts/Data/Items/ItemDefinition.cs（新增） | SO 堆叠定义，沿用表 ID。 |
| Assets/Scripts/Data/Items/ItemCatalog.cs（新增） | 可注入目录接口、表与 SO 的会话复制。 |
| Assets/Scripts/Data/Inventory/InventorySettings.cs（新增） | SO 容量开关及堆叠配置。 |
| Assets/Scripts/Data/Save/GameStateSaveData.cs（新增） | 阶段枚举与可序列化 DTO。 |
| Assets/Scripts/Core/Save/GameSaveStore.cs（新增） | JSON 文件存取。 |
| Assets/Editor/SharedGame/SharedGameVerification.cs（新增） | 数据及独立进程恢复自动检查。 |
| Assets/Editor/SharedGame/SharedGamePlayVerification.cs（新增） | 实际店铺/场景/Manager 恢复检查。 |
| docs/shared-game-state-round1.md（新增） | 检查、计划、接入、文件清单和验收记录。 |

新增脚本及文件夹对应的 .meta 一同提交；原文件 GUID 保留。

## 下一阶段建议

先与地下成员确认“任务目标材料 + 探索会话 ID + 获得物品 + 结果标记”的最小协议。共享结算要防重复提交，并以事务检查全部奖励容量，避免部分领取和旧 AdventureManager 重复 EndAdventure 发放问题。随后实现只存在 Editor 的进入/收集/返回模拟器，补齐本轮明确推迟的验收项。任务材料应来自正式订单/配方 SO 配置，不能从 UIOrderData.Recipe 的显示字符串解析。再把保存按钮、游戏阶段和日夜场景切换接入正式业务；已有地图设计与店铺表现保持独立。
