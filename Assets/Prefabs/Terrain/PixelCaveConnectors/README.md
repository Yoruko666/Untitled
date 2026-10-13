# 哥布林洞穴连接 tile

针对 `CaveTerracedTerrainV2` 的河岸、桥头和高差连接补充模块。纹理沿用 PixelCaveTiles 的哥布林洞穴配色，新增木板、河岸碎石和踩实土路三张实际 64×64、16 色纹理，Point、Repeat、无 mipmap、无压缩。预览场景：`Assets/Scenes/PixelCaveConnectorPreview.unity`。

| Prefab | 尺寸（X × Z） | 用途 |
| --- | --- | --- |
| `tile_floor_gravel` | 4 × 4 m | 河岸碎石地面 |
| `tile_floor_path` | 4 × 4 m | 踩实土路 |
| `tile_bank_straight` | 4 × 2 m | 河岸直段，向下厚 0.65 m |
| `tile_bank_outer_corner` | 4 × 4 m | 半径 4 m 的河岸外转角，向下厚 0.65 m |
| `tile_bank_inner_corner` | 4 × 4 m | L 形河岸内转角，向下厚 0.65 m |
| `tile_ramp_045` | 4 × 4 m | 上升 0.45 m，住宅/仓库入口 |
| `tile_ramp_120` | 4 × 4 m | 上升 1.2 m，短平台连接 |
| `tile_ramp_240` | 4 × 8 m | 上升 2.4 m；两段接 4.8 m，三段接 7.2 m |
| `tile_steps_120` | 4 × 4 m | 上升 1.2 m，八级台阶，每级高 0.15 m |
| `tile_bridge_deck` | 4 × 4 m | 木桥连续桥面，底部支撑梁 |
| `tile_bridge_landing` | 4 × 4 m | 木桥面与土路的同高连接 |
| `tile_retaining_edge_120` | 4 × 1 m | 平台石挡边，向下厚 1.2 m |

XZ 是地面，Y 是高度，保持 Transform Scale 为 1。坡道原点的 Y 是低端高度，沿 +Z 上升；下一段放在上一段末端并提高对应的 rise。例：2.4 m 坡道中心 Z 分别为 0、8，Y 分别为 2.4、4.8，可接到 7.2 m 高；第三段中心 Z=16、Y=7.2，可接到 9.6 m 高。

河岸原点是岸顶。地图河岸为 -2.4 m、水面为 -3.05 m，0.65 m 厚度对应这段水线高差。直段与转角按端面位置手工对齐；这些是摆放模块，未注册成 TileWorldCreator 自动邻接 preset。桥头 -Z 半段是木板，+Z 半段是土路。

每个模块带 MeshCollider。几何检查覆盖三角形、法线、坡道全宽连续高度、桥头同高和台阶高差；Unity Editor 的材质观感及角色移动还需人工验收。现有地图未自动替换，直接把所需 prefab 拖入场景，避免与原地形同高重叠。

网格可用 `python tools/build_pixel_cave_connectors.py` 重建，保留已有 GUID。只在需要重建这些资源时运行。
