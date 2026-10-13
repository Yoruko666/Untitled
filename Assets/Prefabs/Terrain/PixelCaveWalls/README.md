# 洞穴岩壁、洞口与通道 tile

17 个带厚度及非 Convex MeshCollider 的 3D 模块。复用 `PixelCaveTiles` 已匹配 `CaveTerracedTerrainV2` 配色的 rock、rock_shadow、rock_light 和 moss 材质；纹理为实际 64×64、16 色，Point、Repeat、无 mipmap、无压缩。

打开 `Assets/Scenes/PixelCaveWallPreview.unity` 查看整套模块，再把所需 prefab 拖入地图。地图布局不会自动替换。

| 模块 | 尺寸或净空 | 用途 |
| --- | --- | --- |
| `tile_wall_straight_480` | 宽 4 m、厚约 2 m、高 4.8 m | 分层起伏直岩壁 |
| `tile_wall_moss_480` | 同直岩壁 | 顶部带苔藓的变化段 |
| `tile_wall_broken_480` | 宽 4 m、端部高 4.8 m | 岩顶破损变化段 |
| `tile_wall_low_240` | 宽 4 m、高 2.4 m | 低岩壁 |
| `tile_wall_inner_corner` | 4 × 4 m、高 4.8 m | L 形内转角 |
| `tile_wall_outer_corner` | 4 × 4 m、高 4.8 m | 分面弧形外转角 |
| `tile_wall_end` | 2 × 2 m、高 4.8 m | 岩壁倒角收尾 |
| `tile_wall_buttress` | 高 3.6 m | 岩壁支撑凸岩 |
| `tile_entrance_small` | 外宽 4 m、高 4.8 m、深 2 m；净宽 2.6 m、中央净高 3.7 m | 窄拱形洞口 |
| `tile_entrance_wide` | 外宽 8 m、高 6.4 m、深 2 m；净宽 5.6 m、中央净高 5.2 m | 主洞口或大型通道入口 |
| `tile_entrance_blocked` | 同窄洞口，带实体碎岩封堵 | 不可通行洞口 |
| `tile_wall_alcove` | 同窄洞口，背后封闭 | 岩壁凹室，可摆火炬或祭品 |
| `tile_tunnel_straight` | 4 × 4 m、高 4.8 m；通行净宽 2.6 m | 拱形直通道 |
| `tile_tunnel_turn` | 8 × 8 m、高 4.8 m；通道净宽 2.6 m、净高 4.4 m | 从南侧转向东侧的 L 通道 |
| `tile_roof_480` | 4 × 4 m，底部 Y=4.4、顶部 Y=4.8 | 可选岩顶，室内编辑时可隐藏 |
| `tile_rock_pillar_480` | 底部直径 1.8 m、高 4.8 m | 分面岩柱 |
| `tile_rock_overhang` | 宽 4 m，底部 Y=3.2 | 岩壁挑檐和洞口上方凸岩 |

## 摆放

XZ 是地面，Y 是高度，原点位于地面高度；保持 Scale 为 1。直岩壁沿 X 延伸，面朝 -Z，中心依次相隔 4 m 可连续拼接；可绕 Y 旋转适应洞壁方向。内外转角以端面手工对齐，尚未接入 TileWorldCreator 自动邻接 preset。

洞口沿 Z 穿过，MeshCollider 只覆盖岩石外壳，不封住可通行洞口。宽洞口高度为 6.4 m，拼到 4.8 m 岩壁时需用岩顶或凸岩处理顶部高度变化。凹室背面和封堵洞口有实体碰撞。

转弯通道南端开口中心为局部 `(-2, 0, -4)`，东端为 `(4, 0, -2)`；与直通道拼接时按开口中心对齐并旋转方向。通道不带地面，底下铺已有 floor/path tile。岩顶与挑檐原点仍是地面，实际网格悬在对应高度。

现有 shader 对这些模块沿用世界坐标贴图。岩顶会参与遮挡，可在 HD-2D 切开视角中隐藏；未新增自动隐藏行为。

## 验证和重建

检查了有限顶点、非退化三角形、法线方向、封闭体积、洞口和转弯路径的采样空腔，以及封堵洞口/凹室的阻挡。Unity Editor 的贴图效果、碰撞烘焙和实际角色通过仍需人工确认。

可运行 `python tools/build_pixel_cave_walls.py` 重建网格及 prefab，保留已有 GUID；仅在需要重建资源时运行。
