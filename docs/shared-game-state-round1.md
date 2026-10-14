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
