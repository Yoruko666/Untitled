# Untitled 开发规范

本规范约定 Untitled 项目的 Unity 开发、资源管理与交付方式。

## 项目

- Unity / C# / URP，目标是 3D 环境、像素角色与倾斜镜头的 HD-2D 经营 / 冒险游戏。区分目标玩法与已实现功能。
- 当前版本：Unity `2022.3.62f3c1`、URP `14.0.12`、Input System `1.14.2`；以 `ProjectSettings/ProjectVersion.txt` 和 `Packages` 清单为准。
- `Develop` 用于开发测试，`Cave` 用于洞穴地图；`Adventure`、`Bussiness` 用于对应玩法，不假定场景切换已接通。
- API 或 Package 行为不确定时查对应版本的官方文档，已核实的用法可复用：[Unity 2022.3](https://docs.unity3d.com/2022.3/Documentation/Manual/index.html)、[Scripting API](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/index.html)、[URP 14](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/index.html)。

## 工作方式

- 小步完成当前需求：实现 → 必要检查 → 本地 commit → 简短交付，后续根据人工反馈修改。
- 改动前检查 Git 状态，只阅读相关文件，保留用户已有修改；不顺带重构、升级 Package、调整玩法或打磨画面。
- 优先交付可编辑、可试玩的小场景。达到本步目标就收尾，不为简单任务搭建通用工具或临时验证框架。
- 用户要求只分析、不写代码、不运行或分步确认时，严格按该范围执行。

## 目录与代码

- `Assets/Scripts/`：`Core` 放启动、Manager、事件、相机、公共渲染与配表框架；`Gameplay` 放实体和玩法；`Data` 放纯 C# 数据，`Data/Generated` 放 protoc 生成的配表类型；`UI`、`Debug`、`Utils` 按职责使用。
- `Assets/Scenes/` 放场景，`Prefabs/` 放可复用对象，`Configs/` 放配置（`Configs/Database` 放配表 `.bytes`），`Art/` 放美术，`Shaders/` 和 `Settings/` 放渲染相关内容，`Input/` 放输入定义；`Config/Table/` 与 `Config/Proto/` 放 Assets 外的配表源表与结构定义。
- Editor 工具放 `Assets/Editor/<Feature>/`；runtime 不依赖 `UnityEditor`。关卡专属脚本、测试、`docs/` 和离线 `tools/` 按需创建，不预建空目录。
- 沿用 `Untitled` namespace 与已有命名，包括 `Bussiness`。C# 类型、文件及公开成员使用 PascalCase，局部变量和私有字段使用 camelCase，四空格缩进；美术文件使用小写 snake_case。
- 新增或修改文本使用 UTF-8、LF、一个末尾换行，无行尾空格；不批量格式化无关文件。

## Scene、Component 与资源

- Scene 保存摆放与组装，Prefab 保存可复用对象，MonoBehaviour 处理行为，纯 C# 类型处理数据或无场景依赖的服务。
- 跨模块使用 Inspector 引用、公开接口、event 或入口注入；避免依赖其他模块内部的 Transform 路径，以及逐帧查找对象或组件。
- 纯 C# Manager 沿用 `ManagerBase<T>`，由 `Bootstrap` 按依赖顺序初始化；访问 `Instance` 前保证初始化，不依赖不同对象的 `Awake` 顺序。
- 事件订阅与退订配对，明确 Input Actions 的 Enable、Disable、Dispose 拥有者；跨 Scene 和重复 Play Mode 时避免重复初始化、残留订阅与静态状态。
- 配置资产与运行状态分开，避免 Play Mode 污染共享配置。需要保存的字段遵循 Unity 序列化规则，不假定 Dictionary、HashSet 可直接保存。
- 资源与 `.meta` 一起处理和提交，移动或重命名保留 GUID；修改序列化字段时保留已有值和引用，必要时使用 `FormerlySerializedAs`。
- 输入修改使用 `Assets/Input/InputActions.inputactions`，由 importer 生成 wrapper，不手改 `InputActions.cs`。

## 配表与数据库

- 配表链路为 Excel 源表 → protobuf 二进制 → Addressables → `DatabaseManager` 内存表，不使用数据库引擎；唯一权威来源是 `Config/Table/*.xlsx`。
- 源表每个 sheet：第 1 行为字段名（须与 proto 一致）、第 2 行类型说明、第 3 行中文注释，第 4 行起为数据，第一列为主键。
- proto 放 `Config/Proto/*.proto`，`csharp_namespace` 使用 `Untitled.Config`；`.bytes` 必须落在 `Assets/Configs/Database`（Addressables 只能加载 Assets 内资产），由 `Configs` 组加载、地址与文件名一致。
- protoc 生成的 C# 位于 `Assets/Scripts/Data/Generated`，不手改；字段改动先改 proto 再重新导出。
- 离线导出用 `tools/export_config.py`，新增表需在脚本 `TABLES` 注册；导出依赖 `protoc` 与 `openpyxl`，脚本会把生成物换行统一为 LF。
- 运行框架在 `Assets/Scripts/Core/Database/`：表类继承 `DatabaseBase<TRow>` 并重写 `KeyOf`、`Parse`，Addressables 地址常量集中在 `DatabasePath`，新表在 `DatabaseManager.Initialize` 注册。
- 配表只读，运行时不写回；通过 `DatabaseManager.Instance.XxxDatabase.Get(id)` / `TryGet` 查询，不逐帧查找。`.bytes` 与生成的 C# 作为导出产物一起提交。
- Protobuf 运行时来自 `Assets/Plugins` 下的 DLL，不通过 Package Manager 安装，不随任务替换版本。

## HD-2D 与地图

- 世界移动使用 XZ 平面，Y 表示高度；`EntityBase.Coord` 对应 XZ。统一格子尺寸、地图原点和坐标映射；移动速度使用世界单位/s，角色尺寸与碰撞体按统一比例设置。
- 初期使用 `CharacterController` 与 3D Collider。Billboard 只作用于视觉子对象；`Move()` 接收本次位移，不自动施加重力。
- 环境使用 3D 地面和墙体提供厚度、遮挡与碰撞；角色材质明确是否参与 3D 光照。先完成基础尺寸和画面，再按需增加后处理。
- 地图布局保持一个权威来源；runtime 读取保存的布局，不自动覆盖手工编辑内容。导入与重建通过明确的 Editor 操作完成。
- 显示与通行规则分开，出生点必须有效，边界和虚空要有阻挡。地图 Editor 修改支持 Undo/Redo 与正确保存。
- 大地图按区块组织，避免默认每格带 Update；有实际瓶颈再优化。

## 验证与交付

- 检查范围与改动规模匹配：文档检查内容与 diff；代码检查编译和相关逻辑；必要时使用已有测试或一次针对性的试玩。已知错误先修复。
- 画面、灯光、镜头、移动手感及一般场景交互可以交给用户人工试玩。交付时给出最多三项简短验收操作，收到反馈后再修改；人工验收不作为本地 commit 的前置条件。
- 交付前不反复验证、截图或重跑已通过的检查。只有相关改动、失败或明确未解决的问题才复测受影响部分，不追求穷尽所有边界。
- 不默认新增自动化验证工具或执行完整构建；测试和构建只用于当前任务确需覆盖的重要逻辑、风险或构建问题。
- Editor 操作失败时做一次有针对性的恢复，仍失败就换简单方式或交给人工验证，不围绕工具问题反复尝试。修改脚本前退出 Play Mode，编译完成后再运行。
- 汇报只说明改动、已做检查和需要人工确认的内容；未运行的 Editor、Play Mode 或构建验证如实说明。

## Git

- 每个任务完成必要检查后创建清楚的 `feat`、`fix`、`refactor`、`docs` 或 `chore` commit；用户明确要求不提交时除外。文档与玩法实现分开提交。
- 提交前核对 diff 和暂存范围，只提交当前任务文件；不混入无关 Editor 配置，不提交 Library、Temp、Logs、构建产物或自动生成的 IDE 工程。
- 本地 commit 不自动意味着 push 或 PR，按用户授权范围执行。
