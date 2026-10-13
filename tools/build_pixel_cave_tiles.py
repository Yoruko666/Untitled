"""Author the PixelCaveTiles sample library in Blender.

Run: blender --background --python tools/build_pixel_cave_tiles.py
Meshes use a 4 m footprint, with the origin on the walking surface.
The five cliff shapes follow TileWorldCreator V4's dual-grid orientation.
"""
import json
import math
from pathlib import Path

import bpy
from mathutils import Vector
from mathutils.geometry import tessellate_polygon

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/Art/Models/PixelCaveTiles"
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.context.scene.unit_settings.system = "METRIC"
materials = [bpy.data.materials.new(name) for name in
             ("surface", "rock", "rock_shadow", "rock_light", "rim")]
names = []


def mesh_object(name, vertices, faces, slots):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    for material in materials:
        mesh.materials.append(material)
    mesh.update()
    uv = mesh.uv_layers.new(name="UVMap")
    for face, slot in zip(mesh.polygons, slots):
        face.material_index = slot
        face.use_smooth = False
        axis = max(range(3), key=lambda i: abs(face.normal[i]))
        for loop_index in face.loop_indices:
            point = mesh.vertices[mesh.loops[loop_index].vertex_index].co
            coordinates = (point.x, point.y) if axis == 2 else (
                (point.y, point.z) if axis == 0 else (point.x, point.z))
            uv.data[loop_index].uv = (coordinates[0] / 4, coordinates[1] / 4)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    names.append(name)
    return obj


def triangular_cap(vertices, polygon, offset):
    points = [Vector(p) for p in polygon]
    indices = {tuple(p): i + offset for i, p in enumerate(points)}
    return [tuple(offset + p if isinstance(p, int) else indices[tuple(p)] for p in triangle)
            for triangle in tessellate_polygon([points])]


def connection(a, b):
    # Grid borders are mating surfaces, never bevel or distort them.
    return any(abs(a[axis] - sign * 2) < 1e-5 and
               abs(b[axis] - sign * 2) < 1e-5
               for axis in (0, 1) for sign in (-1, 1))


def shelf(name, polygons, depth=3.2, flat=False):
    vertices, faces, slots = [], [], []
    rows = [(0, 0, 0), (-depth, 0, 1)] if flat else [
        (0, 0, 4), (-.16, -.035, 3), (-.36, .025, 1),
        (-1.02, .06, 2), (-1.14, -.025, 1), (-1.86, .035, 3),
        (-2.02, -.045, 1), (-2.98, .025, 2), (-3.2, 0, 1)]
    for polygon in polygons:
        area = sum(a[0] * b[1] - b[0] * a[1]
                   for a, b in zip(polygon, polygon[1:] + polygon[:1]))
        if area < 0:
            polygon = list(reversed(polygon))
        start, count = len(vertices), len(polygon)
        normals = []
        for i, point in enumerate(polygon):
            previous, following = polygon[i-1], polygon[(i+1) % count]
            normal = Vector((0, 0))
            for a, b in ((previous, point), (point, following)):
                if not connection(a, b):
                    normal += Vector((b[1]-a[1], a[0]-b[0])).normalized()
            if normal.length:
                normal.normalize()
            # Keep endpoint positions identical to adjacent tiles at every stratum.
            if abs(point[0]) >= 1.999 or abs(point[1]) >= 1.999:
                normal = Vector((0, 0))
            normals.append(normal)
        for height, displacement, _ in rows:
            vertices.extend((point[0] + normal.x * displacement,
                             point[1] + normal.y * displacement, height)
                            for point, normal in zip(polygon, normals))
        top = triangular_cap(vertices, vertices[start:start+count], start)
        faces.extend(top)
        slots.extend([0] * len(top))
        for row in range(len(rows)-1):
            for i in range(count):
                j = (i+1) % count
                faces.append((start+row*count+i, start+(row+1)*count+i,
                              start+(row+1)*count+j, start+row*count+j))
                slots.append(1 if connection(polygon[i], polygon[j]) else rows[row][2])
        bottom_start = start + (len(rows)-1)*count
        bottom = triangular_cap(vertices, vertices[bottom_start:bottom_start+count], bottom_start)
        faces.extend(tuple(reversed(face)) for face in bottom)
        slots.extend([2] * len(bottom))
    return mesh_object(name, vertices, faces, slots)


square = [(-2, -2), (2, -2), (2, 2), (-2, 2)]
edge = [(-2, 0), (-1, .035), (0, -.065), (1, .035), (2, 0), (2, 2), (-2, 2)]
corner = [(-2, 2), (-2, 0)] + [
    (-2 + 2 * math.cos(angle), 2 + 2 * math.sin(angle))
    for angle in (-math.pi/2, -math.pi*3/8, -math.pi/4, -math.pi/8, 0)]
inside = [(-2, -2), (0, -2)] + [
    (2 + 2 * math.cos(angle), -2 + 2 * math.sin(angle))
    for angle in (math.pi, math.pi*7/8, math.pi*3/4, math.pi*5/8, math.pi/2)] + [(2, 2), (-2, 2)]
# The first point in each arc is already present in the outline.
corner.pop(2)
inside.pop(2)
for family in ("earth", "stone", "moss", "wet"):
    shelf("tile_floor_" + family, [square], .35, flat=True)
for role, polygons in {"fill": [square], "edge": [edge], "outer_corner": [corner],
                       "inner_corner": [inside], "double_corner": [corner,
                       [(-x, -y) for x, y in corner]]}.items():
    shelf("tile_cliff_" + role, polygons)


def ramp(name, steps=0):
    vertices, faces, slots = [], [], []
    segments = steps or 1
    # Each closed wedge/step is level across its full width.
    for i in range(segments):
        near, far = -2 + 4*i/segments, -2 + 4*(i+1)/segments
        low = .0 if steps else 1.6*i/segments
        high = 1.6*(i+1)/segments
        top_near = high if steps else low
        offset = len(vertices)
        vertices.extend([(-2, near, top_near), (2, near, top_near),
                         (2, far, high), (-2, far, high),
                         (-2, near, -.35), (2, near, -.35),
                         (2, far, -.35), (-2, far, -.35)])
        new_faces = [(0, 1, 2, 3), (0, 4, 5, 1), (1, 5, 6, 2),
                     (2, 6, 7, 3), (3, 7, 4, 0), (7, 6, 5, 4)]
        faces.extend(tuple(offset+index for index in face) for face in new_faces)
        slots.extend([0, 3, 1, 1, 1, 2])
    return mesh_object(name, vertices, faces, slots)


ramp("tile_ramp", 0)
ramp("tile_steps", 8)

# Upright wall modules have a matching 4 m span and a ground-level pivot.
for role, polygon in {
    "straight": [(-2, -.55), (-1, -.62), (0, -.5), (1, -.64), (2, -.55), (2, .6), (-2, .6)],
    "corner": [(-2, -.55), (.55, -.55), (.55, 2), (-.6, 2), (-.6, .6), (-2, .6)],
    "end": [(-2, -.55), (1.5, -.55), (2, -.1), (2, .15), (1.5, .6), (-2, .6)]
}.items():
    obj = shelf("tile_wall_" + role, [polygon])
    for vertex in obj.data.vertices:
        vertex.co.z += 3.2
    for face in obj.data.polygons:
        if face.normal.z > .5:
            face.material_index = 3


def rock(name, radius, height):
    vertices, faces, slots = [], [], []
    count = 8
    for row, (z, scale) in enumerate(((0, .85), (.25*height, 1), (.7*height, .8), (height, .38))):
        for i in range(count):
            angle = 2*math.pi*i/count
            variation = 1 + .12*math.sin(i*2.4 + row*.8)
            vertices.append((math.cos(angle)*radius*scale*variation,
                             math.sin(angle)*radius*scale*variation*.8, z))
    for row in range(3):
        for i in range(count):
            faces.append((row*count+i, row*count+(i+1)%count,
                          (row+1)*count+(i+1)%count, (row+1)*count+i))
            slots.append((1, 2, 1)[row] if i % 3 else 3)
    faces.append(tuple(range(24, 32)))
    slots.append(3)
    faces.append(tuple(reversed(range(8))))
    slots.append(2)
    mesh_object(name, vertices, faces, slots)


rock("tile_rock_low", 1.1, .85)
rock("tile_rock_tall", .8, 2.6)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.fbx(filepath=str(OUT / "pixel_cave_tiles.fbx"), use_selection=True,
                         object_types={"MESH"}, axis_forward="-Z", axis_up="Y",
                         apply_unit_scale=True, bake_anim=False, add_leaf_bones=False)
manifest = {"Modules": [{"Name": obj.name,
             "Slots": sorted({face.material_index for face in obj.data.polygons})}
            for obj in bpy.context.scene.objects if obj.type == "MESH"]}
(OUT / "pixel_cave_tiles.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / "tools/art/pixel_cave_tiles.blend"))
print("PIXEL_CAVE_TILES_EXPORTED: " + str(len(names)) + " closed meshes")
