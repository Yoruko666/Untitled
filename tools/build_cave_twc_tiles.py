"""Blender-authored 4 m cave paving modules for the standard TWC grid.

Run: blender --background --python tools/build_cave_twc_tiles.py
The irregular perimeter and ramps remain separate transition pieces.
"""
from pathlib import Path
import bpy

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/Art/Models/CaveTileWorld"
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
earth = bpy.data.materials.new("earth")
earth.diffuse_color = (.52, .43, .31, 1)
rock = bpy.data.materials.new("rock")
rock.diffuse_color = (.30, .28, .25, 1)
roles = ["dead_end", "single", "fill", "corner_way", "corner_fill",
         "interior_corner", "double_corner", "edge_way", "edge_fill",
         "three_way", "three_way_fill", "edge_corner_fill", "three_corner", "four_way"]
for index, role in enumerate(roles):
    # A shared full-square footprint deliberately joins the curved transition mesh.
    # All tops are exactly planar; variation is triangulation, not raised thresholds.
    center = (-.45 + (index % 4) * .3, -.35 + (index % 3) * .35, 0)
    verts = [(-2, -2, 0), (2, -2, 0), (2, 2, 0), (-2, 2, 0), center,
             (-2, -2, -.35), (2, -2, -.35), (2, 2, -.35), (-2, 2, -.35)]
    faces = [(0, 1, 4), (1, 2, 4), (2, 3, 4), (3, 0, 4),
             (0, 5, 6, 1), (1, 6, 7, 2), (2, 7, 8, 3), (3, 8, 5, 0), (5, 8, 7, 6)]
    data = bpy.data.meshes.new("cave_tile_" + role)
    data.from_pydata(verts, [], faces)
    data.materials.append(earth)
    data.materials.append(rock)
    data.update()
    uv = data.uv_layers.new(name="UVMap")
    for poly in data.polygons:
        poly.material_index = 0 if poly.index < 4 else 1
        for loop in poly.loop_indices:
            co = data.vertices[data.loops[loop].vertex_index].co
            uv.data[loop].uv = ((co.x + 2) / 4, (co.y + 2) / 4)
    obj = bpy.data.objects.new("cave_tile_" + role, data)
    bpy.context.collection.objects.link(obj)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.fbx(filepath=str(OUT / "cave_floor_tiles.fbx"), use_selection=True,
                         object_types={"MESH"}, axis_forward="-Z", axis_up="Y",
                         apply_unit_scale=True, bake_anim=False, add_leaf_bones=False)
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / "tools/art/cave_twc_tiles.blend"))
print("CAVE_TWC_TILES_EXPORTED: 14 modules, 4 m footprint, planar tops")
