# 哥布林奶茶店 UI · 第一轮

## 范围与项目检查

本轮交付主题系统、三种按钮、通用面板、订单卡片、任务追踪和独立 Showcase。背包、对话、奖励弹窗及完整日夜 HUD 留待第一轮人工验收后继续。没有改动现有场景、摄像机、玩家控制、关卡、输入配表或游戏状态。

- Unity 2022.3.62f3c1 / URP 14.0.12 / Input System 1.14.2。
- 复用 uGUI 1.0.0、TextMeshPro 3.0.7，未引入第二套 UI 框架。
- 已有 `Untitled.UIManager`、`PanelBase` 和 Addressables 面板地址保留。新增视觉容器位于 `Untitled.UI` 命名空间，避免与旧 `Untitled.UIPanel` 地址常量冲突。
- `GameData.Money` 已存在，但没有余额变化事件；经营、冒险入口尚有未实现部分；未发现正式订单、任务系统。因此 Showcase 使用独立 `UIMockDataSource`，不冒充正式玩法。
- 现有 Liberation Sans 不覆盖中文。新增 Noto Sans CJK SC Regular 2.004，使用 SIL OFL 1.1，保留原始 OTF、版权说明、授权文本和动态多图集 SDF。
- 首次编译发现原有 MCP 10.3.0 包缺失 `Editor/Tools/Build` 源码，被通用 Build 忽略规则漏掉。恢复该版本原始文件及 meta；本地 `ManageBuild.cs` 与上游 v10.3.0 核对一致。没有升级包。

## 打开与验收

在 Unity 菜单选择 **Untitled → UI → Open Showcase**，或打开 `Assets/Scenes/UI/UIShowcase.unity`，进入 Play Mode。

1. 点击切换主题，或按 T：日夜视觉变化，当前订单与任务进度保留。Tab / Shift+Tab / 方向键移动按钮焦点，Enter / 空格确认；不可用按钮不能响应。
2. 点击刷新数据，或按 R：三组 Mock 快照循环更新订单进度、金币和任务。达到第三组时订单动作可用；它只报告“制作台请求”，不修改余额或完成任务。
3. 点击收起订单，或按 O，再重新打开；以 1920×1080 与 1280×720 检查排版。任务列表可滚轮滚动，Showcase 支持 PgUp / PgDn。

已在 Unity Editor Play Mode 中通过鼠标射线点击、T/Tab 和 Enter/空格确认键盘输入、禁用按钮、六次关闭/重开、动态快照、正式金币只读适配、主题保留状态和场景名称映射检查。1920×1080 与 1280×720 的日夜文字溢出/屏幕边界检查通过，运行检查记录错误数为 0；四张 PNG 来自实际 Play Mode 的 Game View 渲染目标，并处理了 Metal 纹理的纵向方向。

场景映射检查验证的是主题绑定回调，不等同于加载完整 Bussiness/Adventure 场景试玩；长文案、完整模态输入协调与 Player 构建仍待正式接入时验收。

实际检查结果和运行截图见 `docs/images/ui/verification.txt` 与同目录 PNG。不要把静态资源检查等同于 Play Mode 测试。正式经营/冒险场景的整体 HUD 与玩家输入协调尚未接入。

## 交付位置

沿用本项目 AGENTS.md 的目录约定：

| 内容 | 位置 |
| --- | --- |
| 运行时代码 | `Assets/Scripts/UI/Goblin/` |
| 编辑器制作与验收 | `Assets/Editor/GoblinUI/` |
| DayTheme / NightTheme | `Assets/Configs/UI/Goblin/` |
| 七个 Prefab | `Assets/Prefabs/UI/Goblin/` |
| 像素 Sprite / Sprite Atlas | `Assets/Art/UI/Goblin/` |
| 中文字体与授权 | `Assets/Art/UI/Goblin/Fonts/` |
| 独立场景 | `Assets/Scenes/UI/UIShowcase.unity` |

Prefab：`UIButton_Primary`、`UIButton_Secondary`、`UIButton_Danger`、`UIPanel`、`UIOrderCard`、`UIQuestRow`、`UIQuestTracker`。场景实例保留 Prefab 关联，文字、图标、填充条、边框、按钮均可独立编辑。

## 使用主题和组件

Canvas 设置 Screen Space Overlay、GraphicRaycaster 和 Canvas Scaler：Scale With Screen Size，参考 1920×1080，Match 0.5。使用 InputSystemUIInputModule，已有 EventSystem 时复用现有对象，避免重复创建。

Canvas 根节点挂 `UIThemeContext`，配置 DayTheme 和 NightTheme。组件放在它的子树中；组件启用时订阅主题事件，禁用时退订，重新启用时应用当前主题。无静态主题状态。调用 `ApplyDay()` / `ApplyNight()` / `Toggle()` 切换，不触碰数据源。

需要自动对应场景时在 Inspector 开启 `followGameplayScenes`：Bussiness → Day；Adventure / Cave → Night；其他场景不改变主题。该绑定是可选的，Showcase 默认关闭。不要用新 UI 替换游戏场景中的摄像机或玩家根节点。

`UISafeArea` 保留屏幕安全区域与主题 SafeMargin，默认 64 参考像素。日夜主题分别配置文字/背景/强调/危险颜色、字体、9-slice 面板/边框/按钮、图标和通用 Padding / Spacing。美术修改应直接编辑主题与已生成资源；生成工具不会覆盖已有 Showcase。

- UIButton：`SetLabel`、`SetIcon`、`SetInteractable`、`SetVisible`。`Clicked` 是可在 Inspector 绑定的 UnityEvent。Hover / Selected / Pressed / Disabled 采用可见的颜色变化，uGUI Button 处理鼠标和 Enter 确认；UIButton 补充聚焦时的空格确认，宿主已绑定空格 Submit 时会跳过补充处理，避免重复事件。
- UIPanel：`SetTitle`、`SetIcon`、`Show` / `Hide`；`Content` 可放 LayoutGroup，主题应用 Padding 与 Spacing。它是视觉容器；模态暂停/ESC 仍由既有 PanelBase/UIManager 决定。
- UIOrderCard：`Bind(UIOrderData)`；标题、顾客要求、配方、图标、状态、进度、动作可用性来自快照。`ActionRequested(string orderId)` 只发出输入意图。空订单显示等待状态，禁用动作。
- UIQuestTracker：`Bind(IReadOnlyList<UIQuestData>)`；按需复用行对象，多任务通过 ScrollRect 访问，空列表隐藏所有行。任务完成与计数由生产者指定，UI 不自行判断。

正文使用清晰的中文 SDF，边框和图标使用 Point 过滤、无 Mipmap、无纹理压缩。过长文本按预设最小字号调整，仍过长则省略，不无限缩小文字；正式数据接入时须用真实最长配方/任务文案再验收，长道具说明放下一轮详情界面。

## 数据接入

`UIOrderData` / `UIQuestData` / `UISnapshot` 为只读显示快照，输入和显示分离。

```csharp
// 在玩法主机完成 Bootstrap 初始化后：
source.Initialize(GameManager.Instance.GameData); // UIGameDataSource
presenter.SetSource(source);                      // UIHUDPresenter
// 订单/任务/经济系统发生变化后，由玩法主机提供显示快照：
source.Publish(currentDay, orderDisplayData, questDisplayData);
```

`UIGameDataSource` 只读 GameData.Money；Publish 触发 Changed。现有经济系统没有事件，因此由调用方在正式业务完成后通知，不轮询，也不在 UI 中调用 AddMoney / TrySpendMoney。将 `ActionRequested` 连接正式订单系统的“打开制作台”或其他输入接口，业务系统再决定结果并发布新快照。

若已有正式聚合事件源，直接实现 `IUIReadModel`，通过 Inspector（MonoBehaviour）或 `SetSource` 注入。绑定器启用订阅、禁用退订，切换源会先退订旧源；重新打开恢复最新快照。快照由外部玩法层持有，销毁 UI 不应销毁游戏状态。

为了保持现有 Addressables 面板栈，可在现有 PanelBase 子类引用本轮视觉 Prefab，并在 OnShow(data) 中 Bind；不要把纯视觉 UIPanel 直接作为 UIManager.Open 的面板根替换，因为旧管理器要求 PanelBase。

## 资源授权与占位说明

`Assets/Art/UI/Goblin/Sprites/*_placeholder.png` 全部为本轮用 C# 程序绘制的占位素材：日夜木框（64px / 14px slice）、按钮（64×32 / 12-10-12-10 slice）、奶茶、金币、任务卷轴、太阳、月亮（32px）、纯白填充块。是独立小素材，没有使用整张参考概念图或从中裁切背景。

参考图只用于提炼暖木/羊皮纸/抹茶绿及深木/蓝灰/金币黄的风格与层级，不作为交付素材或背景。图标主体保持一致，日夜切换改变环境色与主按钮色，减少识别成本。

字体：[上游字体](https://github.com/notofonts/noto-cjk/blob/main/Sans/OTF/SimplifiedChinese/NotoSansCJKsc-Regular.otf)、[上游授权](https://github.com/notofonts/noto-cjk/blob/main/Sans/LICENSE)。版权与分发要求详见 Fonts/SOURCE.md、OFL.txt；发行游戏时须保留这两份说明。SDF 使用未修改的字体作为动态源，可在构建中添加新的简体中文字符。

需要继续制作的正式美术：

- 手绘像素木纹/羊皮纸纹理与内外阴影，保留现有切片边距。
- 哥布林店徽、藤蔓、杯印、夜间铁钉等可独立替换的角饰。
- 奶茶配方与原料全套图标、任务分类/完成图标。
- 按钮独立的 hover / focus / pressed / disabled 图像；本轮采用颜色反馈。
- 夜间警戒符号、血量/资源图标；后续 HUD 所需素材。

Noto 2.004 的源字体检查覆盖 U+4E00–U+9FEF、英文数字与常用标点；U+9FF0–U+9FFF 这 16 个后来加入的罕用字不在该版本中。若正式剧情需要这些字，必须补充有明确授权的 fallback 字体，不能宣称覆盖所有 Unicode 汉字。

## 编辑器工具与验证边界

`Generate First Round Assets` 在首次缺资源时创建主题、字体 SDF、Sprite、Atlas、Prefab 和场景；已有场景时拒绝覆盖，避免丢失手工制作。资源和 `.meta` 一起提交。

`Validate First Round Assets` 检查主题依赖、Prefab 缺失脚本、切片边框、动态字体源及中文/英文/常用符号的源字体字形覆盖，不依赖操作系统字体。

`GoblinUIPlayModeVerification.Run` 是本轮专用的显式编辑器验证入口：进入独立场景的 Play Mode，使用临时虚拟 Mouse/Keyboard 经过 Input System 和 UI 射线点击，验证禁用按钮、重复打开、一次性事件、数据/主题保留，并以两种 Game View 分辨率采集截图。运行结束移除设备并退出编辑器。该入口不在正常游戏中运行，不做 Player 构建测试，也不宣称完整经营/探索场景已接通。
