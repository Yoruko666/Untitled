# 哥布林奶茶店 UI · 第二轮

## 已交付

复用第一轮 uGUI / TMP / 日夜主题，本轮新增物品格子、背包、NPC 对话、道具说明和奖励弹窗。创建独立 `UIShowcaseExtended.unity`，第一轮 Showcase 保留。

| Prefab | 作用 |
| --- | --- |
| UIItemSlot | 动态图标、名称、数量、选中标记、鼠标/键盘预览及操作请求 |
| UIInventoryPanel | 全部/材料/任务分类、空格、可滚动长列表、快照更新 |
| UIItemTooltip | 完整可滚动说明、固定详情和屏幕边界内的浮动提示 |
| UIDialogueBox | NPC 肖像、可滚动台词、动态选项、禁用选项、继续/关闭请求 |
| UIDialogueChoice | 可复用的对话选项按钮 |
| UIRewardPopup | 遮挡背景的奖励弹窗、只读奖励列表、确认已阅、Esc 关闭与焦点恢复 |
| UI_Dialogue | 连接既有 DialoguePanel / UIManager / 对话配表的正式面板根 |

运行时代码位于 `Assets/Scripts/UI/Goblin/`，Prefab 位于 `Assets/Prefabs/UI/Goblin/`，制作入口位于 `Assets/Editor/GoblinUI/`。本轮小图标位于 `Assets/Art/UI/Goblin/Sprites/*_extended_placeholder.png`。

## 三步验收

Unity 菜单 **Untitled → UI → Open Extended Showcase**，或打开 `Assets/Scenes/UI/UIShowcaseExtended.unity` 后进入 Play Mode。

1. 悬停/选择物品查看详情，点击分类；Tab / 方向键选择，Enter / 空格确认。按 R 两次查看超过 12 格的长列表，PgUp/PgDn 翻页；按 B 关闭和重开背包，按 T 切换主题。
2. 点击对话选项，查看回复并继续结束对话，再点击“重新对话”。禁用的欠条选项不能触发。长台词支持滚轮；焦点在对话框内时 PgUp/PgDn 可滚动台词，键盘选择较下面的选项时列表自动跟随。
3. 打开“查看奖励”：背景不能点击，Tab 与方向键在关闭/确认按钮之间移动；Enter/空格确认或 Esc 关闭，焦点恢复到原按钮。奖励已经由数据源提供，确认不发放金币或物品。奖励过多时滚轮/PgUp/PgDn 可翻页。

在 1920×1080 和 1280×720 下检查以上操作。实际截图与验收记录位于 `docs/images/ui_extended/`。

## 数据和生命周期

显示快照 `UIItemData`、`UIInventorySnapshot`、`UIDialogueData`、`UIRewardData` 为只读对象，集合在创建时复制为只读列表。视图保持当前快照，主题切换只改视觉。数据更新、筛选和显示/隐藏不会消耗物品、增加金币或决定任务状态。

- `UIItemSlot.Bind(item)` 填充内容；`Bind(null)` 显示不可操作空格。`Previewed` 通知预览，`ActionRequested(id)` 发出操作意图。`CanRequest=false` 的任务物品仍可用鼠标/键盘查看，不能发送操作请求。
- `UIInventoryPanel.Bind(snapshot)` 更新并复用格子；至少展示 12 个格子，不将超过 12 个的物品截掉。`SetFilter(category)` 仅筛选显示。`ItemRequested(id)` 由正式物品系统消费；`PreviewChanged(item)` 可接到 tooltip。
- `UIInventoryPresenter.SetSource(IUIInventoryReadModel)` 注入来源，启用时订阅，禁用/更换来源时退订。关闭的背包也可接收最新快照，重新打开显示最新数据。
- `UIItemTooltip.Show(item)` 展示可交互的固定详情；`ShowAt(item, screenPoint)` 创建非阻挡的浮动提示并限制在父 RectTransform 边界内。浮动模式不截获鼠标，调用方可通过 `ScrollDescription(delta)` 路由滚动。说明使用完整文本和 ScrollRect，没有将整段描述烘焙到图片中。
- `UIDialogueBox.Bind(data)` 显示台词与选项；`ChoiceRequested(id)`、`ContinueRequested`、`CloseRequested` 通知宿主。视图不读取配表或选择下一节点。`FocusDefault()` 将焦点放到首个可用选项或继续/关闭按钮。
- `UIRewardPopup.Open(data)` 打开只读奖励展示，接管焦点并拦截背景点击。`Acknowledged(id)` 每次打开最多发出一次；确认后关闭。再次展示同一奖励是重新查看，不是领取第二次。业务层应在弹窗显示前完成发放。`CloseRequested` 是关闭意图，视图随后隐藏；本轮弹窗不修改 Time.timeScale 或玩家状态。
- `UIScrollFollowSelection` 在焦点变化时将选中格子/选项滚入视野。UIButton 新增 `panelSurface` 配置用于物品格子，原有三类按钮行为保持。

组件在 OnEnable/OnDisable 成对处理输入事件，不在每次 Bind 时重复添加点击监听。列表按需创建、后续复用；旧数量、旧选项及空数据都明确清理。

## 正式仓库 / 探索包裹接入

既有 `InventoryData.Items` 和 `PackageData.Items` 可注入同一个只读适配器。先在 Bootstrap 完成数据库初始化后配置：

```csharp
inventorySource.Initialize(
    GameManager.Instance.GameData.InventoryData.Items,
    DatabaseManager.Instance.ItemDatabase,
    address => cachedIcons.TryGetValue(address, out var sprite) ? sprite : null,
    "仓库");
inventoryPresenter.SetSource(inventorySource);
inventorySource.Publish();
```

探索包裹用 `GameManager.Instance.GameData.PackageData.Items` 替换第一个参数。物品增减由正式系统完成，然后调用 `Publish()`。适配器不轮询、不调用 AddItem / ConsumeItem；从真实 ItemDatabase 获取名称、类型、描述和图标地址。图标解析函数读取宿主的缓存，资源异步加载与释放仍由宿主管理；图标缺失时显示主题的占位钱袋，不依赖临时系统字体。

当前配表只定义 Ingredient 类型，因此正式类型映射为“材料”或“其他”。Mock 的“任务”分类仅演示显示行为，后续正式任务物品类型应由物品配置与数据源提供，不由 UI 推断。

## 正式对话接入

本轮在现有 Addressables `Panels` 组注册 `UI_Dialogue`，没有替换已有 UI_Menu。新正式 Prefab 根节点仍含 `Untitled.DialoguePanel : PanelBase`；新增可选 UIDialogueBox 引用。旧面板若未设置该引用，继续使用原有 TMP/Button 字段。

原有调用方式可直接复用：

```csharp
UIManager.Instance.Open(Untitled.UIPanel.Dialogue, new DialogRequest(startId));
```

`DialoguePanel` 仍读取 DialogDatabase / DialogOptionDatabase，维护选项 ID 到 NextId 的映射并推进/结束节点；新 UIDialogueBox 只提供显示和输入。原有 UIManager 负责面板栈与暂停。关闭时清理节点映射，销毁时解除事件监听。

正式 Canvas 需要配置第一轮 `UIThemeContext`、Canvas Scaler 和 InputSystemUIInputModule，并让 UIManager.Root 指向对应 Canvas。日夜主题应用继续复用第一轮方案；本轮没有更换已有游戏场景、摄像机或玩家控制。真实 NPC 触发流程与完整日夜 HUD 的整体场景试玩仍在后续阶段。

## 资源与制作入口

新增七个程序绘制的 32px 占位 Sprite：牛奶、茶叶、红糖木块、珍珠、水晶、钱袋、哥布林肖像。试喝样品复用奶茶杯图标，配方卷轴复用卷轴。全部独立可替换、Point 过滤、无 Mipmap、无压缩，纳入现有 Sprite Atlas 文件夹。

日夜主题增加 `ItemFallbackIcon` 与 `NPCPortrait`，只在原字段为空时填入。字体沿用 Noto Sans CJK SC / SIL OFL 1.1，版权、授权文本和罕用字范围见第一轮文档及 `Assets/Art/UI/Goblin/Fonts/SOURCE.md`。

**Untitled → UI → Generate Second Round Assets** 只用于首次制作；已有扩展场景时拒绝覆盖。正式美术仍需替换：完整原料/装备/任务图标、各 NPC 独立头像、背包标签与奖励角饰。现有布局和逻辑不依赖概念图背景。

## 已做验证与边界

Unity 2022.3.62f3c1 Editor 编译通过。完整 Play Mode 检查通过：格子点击/空格一次性事件、分类、只读任务物品、空背包、20 个物品不丢失、四次关闭/重开、主题保留、tooltip 边界、禁用对话选项、对话切换与重开、奖励遮挡/焦点限制/Esc/三次确认、真实 ItemDatabase 与 UIManager + UI_Dialogue 读取真实对话节点、暂停恢复。

截图复核修正了底部提示贴框和窄关闭按钮。后续只复验受影响的布局、关闭符号、键盘选中最后一格的可见性、长台词/九项奖励翻页，以及空奖励关闭；没有重复运行已通过的完整业务接入检查。最终 UI 运行错误为 0，8 张截图来自实际 Play Mode 的固定分辨率 Game View 渲染目标，并处理 Metal 纵向方向。

没有运行 Player 构建，也没有宣称所有真实 NPC/关卡/玩家输入流程已验收。下一阶段是轻量日夜 HUD、交互提示、血量/资源条和暂停菜单；完整七日业务规则不属于 UI 开发范围。
