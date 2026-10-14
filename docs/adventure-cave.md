# Adventure：自然化哥布林洞穴接入

场景：`Assets/Scenes/Adventure.unity`。

本次导入自然化后的 `goblin_cave_natural.fbx`，保留 15 个区域、中心 Empty、原高度和 A/B/C 路线。地图通过 ModelImporter 转换为 Unity 的 XZ 地面、Y 高度，1 单位为 1 米。模型与材质分别位于 `Assets/Art/Models/GoblinCave/`、`Assets/Art/Materials/GoblinCave/`。

## 场景组装

- `Adventure_GoblinCave` 是 FBX 实例，151 个 Mesh 中除水面外的 150 个使用静态、非 Convex MeshCollider。URP Lit 材质已 remap，避免标准材质变粉。
- `Adventure_CaveRuntime` 持有 Bootstrap、现有 GoblinPlayer 实例及 AdventureCaveSession。保留现有 InputManager、PlayerEntity、像素角色动画和 F1 调试框架。
- Player 使用 Height=1.8、Radius=0.35、Center Y=0.9、Step Offset=0.28、Slope Limit=45、Skin Width=0.04、Speed=5。启用重力，从 01 的有效平台出生。
- Main Camera 使用现有 CameraController，正交跟随、俯角 55°、Yaw=30°、Distance=22、Orthographic Size=10。移动方向相对相机；其他场景没有绑定 movementCamera 时沿用原世界 XZ 移动。
- 像素视觉子物体保持竖直 Billboard，避免与胶囊一起倾斜。Camera 与 Player 均通过序列化引用连接。
- 玩家低于 Y=-5.5 时回到入口并重置垂直速度，避免落入深渊后无法继续测试。尚未接入正式死亡、任务或撤离规则。

## 调试操作

1. 打开 Adventure 并 Play，用原有 WASD/方向键输入移动，检查相机跟随和角色动画。
2. F1 打开原有开发者调试面板，调整移速和镜头参数。
3. 左下角 `Cave Debug` 打开区域面板，可查看坐标/落地状态并跳转 15 个区域；01 按钮返回入口。

## 已执行检查

- Unity 2022.3.62f3c1 编译通过。
- 15 个标记、22 米最高层高差、单位和相机引用检查通过。
- 沿导入模型的实际 MeshCollider 执行 1,684 个地面碰撞采样，全部通过。结果在 `output/adventure-cave/integration.json`。
- 一次针对性的 Play Mode 检查通过现有 PlayerEntity/InputManager 移动链路，实际走完 A、B、C，并从 09 进入 10。三条路线分别约 30、48、54 秒，未卡死或触发跌落回收。结果在 `output/adventure-cave/play_routes.json`。
- 运行相机画面见 `docs/adventure-cave/adventure_cave_player.png`。

本次没有烘焙 NavMesh、添加守卫/潜行感知，也没有执行完整构建；跌落回收和调试面板仍需人工体验。试玩感受与画面参数可通过上述调试面板调整。

## Editor 操作

`Untitled/Adventure/Import Natural Goblin Cave` 按明确操作导入并组装场景，只替换本工具拥有的两个根对象。重新导入会重置这两个根对象内的手工摆放，因此调整后先保存场景备份；工具支持 Undo。

`Untitled/Adventure/Validate Cave Integration` 检查已保存的接入。

`Untitled/Adventure/Check A B C Routes in Play Mode` 启动针对性的三路检查，并自动退出 Play Mode。它使用真实玩家输入链路，测试期间不要手动干预移动。

FBX 单位、轴转换及 CharacterController 参数依据 Unity 2022.3 的 [ModelImporter](https://docs.unity3d.com/ja/2022.3/ScriptReference/ModelImporter.html) 与 [Character Controller](https://docs.unity3d.com/cn/2022.3/Manual/class-CharacterController.html) 文档设置；本场景的可通行性以实际 Play Mode 检查为依据。
