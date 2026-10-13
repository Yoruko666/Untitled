"""Author real dual-grid rock shelves and prepare selected HD-2D reference meshes.

blender --background --python tools/build_cave_hd2d_modules.py
"""
import json
import math
import random
import shutil
from pathlib import Path
import bpy
from mathutils import Vector, Matrix
from mathutils.geometry import tessellate_polygon

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/Art/Models/CaveTileWorldHD2D"
TEX = ROOT / "Assets/Art/Textures/CaveTileWorldHD2D"
LIB = Path(r"D:/BaiduNetdiskDownload/HD-2D/八方旅人资源库")
OUT.mkdir(parents=True, exist_ok=True)
TEX.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
materials = {}
for name in ("earth", "rock", "moss", "reference_rock", "reference_cave", "reference_moss"):
    materials[name] = bpy.data.materials.new(name)


def shape(name, outlines, depth=3.2):
    verts, faces, slots = [], [], []
    for polygon in outlines:
        start = len(verts)
        n = len(polygon)
        area = sum(polygon[i][0] * polygon[(i+1) % n][1] - polygon[(i+1) % n][0] * polygon[i][1] for i in range(n))
        if area < 0:
            polygon = list(reversed(polygon))
        # The rock profile has a readable top lip and a broken middle stratum.
        for h, inset in ((0, 1), (-.16, .96), (-depth*.48, 1.015), (-depth, .965)):
            verts.extend((x*inset, z*inset, h) for x, z in polygon)
        top = [Vector(v) for v in verts[start:start+n]]
        lookup = {tuple(v): start+i for i, v in enumerate(top)}
        for tri in tessellate_polygon([top]):
            faces.append(tuple(start+v if isinstance(v, int) else lookup[tuple(v)] for v in tri))
            slots.append(0)
        for row in range(3):
            for i in range(n):
                j = (i+1) % n
                faces.append((start+row*n+i, start+(row+1)*n+i, start+(row+1)*n+j, start+row*n+j))
                slots.append(1)
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.materials.append(materials["earth"])
    data.materials.append(materials["rock"])
    data.update()
    uv = data.uv_layers.new(name="UVMap")
    for poly, slot in zip(data.polygons, slots):
        poly.material_index = slot
        for loop in poly.loop_indices:
            co = data.vertices[data.loops[loop].vertex_index].co
            uv.data[loop].uv = (co.x*.25, co.y*.25 if slot == 0 else co.z*.25)
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    return obj


corner = [(-2, 2), (-2, 0)] + [(-2+2*math.cos(a), 2+2*math.sin(a))
          for a in (-math.pi/2, -math.pi*3/8, -math.pi/4, -math.pi/8, 0)]
edge = [(-2, 0), (-.8, -.12), (.7, .09), (2, 0), (2, 2), (-2, 2)]
interior = [(-2, -2), (0, -2)] + [(2+2*math.cos(a), -2+2*math.sin(a))
             for a in (math.pi, math.pi*7/8, math.pi*3/4, math.pi*5/8, math.pi/2)] + [(2, 2), (-2, 2)]
fill = [(-2, -2), (2, -2), (2, 2), (-2, 2)]
for role, outlines in {"corner": [corner], "edge": [edge], "interior": [interior],
                       "double": [corner, [(-x, -z) for x, z in corner]], "fill": [fill]}.items():
    shape("hd2d_shelf_" + role, outlines)

# Tall asymmetric cave ribs replace the old evenly triangulated shell silhouette.
for index in range(3):
    rng = random.Random(430+index)
    count = 8
    vertices = []
    for row, (h, radius) in enumerate(((0, 1.2), (.65, 1.4), (2.5, 1.05), (5.1, .95), (7.5, .4))):
        for i in range(count):
            a = i*2*math.pi/count
            r = radius * (1+rng.uniform(-.18, .18))
            vertices.append((math.cos(a)*r + h*.065, math.sin(a)*r*.8, h))
    faces = []
    for row in range(4):
        for i in range(count):
            j = (i+1) % count
            faces.append((row*count+i, row*count+j, (row+1)*count+j, (row+1)*count+i))
    faces.append(tuple(range(32, 40)))
    mesh = bpy.data.meshes.new("hd2d_cave_rib_" + str(index))
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(materials["rock"])
    mesh.update()
    obj = bpy.data.objects.new(mesh.name, mesh)
    bpy.context.collection.objects.link(obj)

# Resource-library props retain the painted albedo and original UVs. Each gets a
# ground pivot and metres baked into vertices; there is no stretched scene root.
references = [
    ("八方旅人1/石头1/ObjMD_Rock_A_04/ObjMD_Rock_A_04.FBX", "reference_rock", .025, "ref_boulder"),
    ("八方旅人1/石头1/ObjMD_Stone_D_Cave_02/ObjMD_Stone_D_Cave_02.FBX", "reference_cave", .065, "ref_stalagmite"),
    ("八方旅人2/山石_八方旅人2/EnvFldMD_FoliageStone_A_MA/EnvFldMD_FoliageStone_A_MA.FBX", "reference_moss", 3.6, "ref_moss_rock"),
]
records = []
for source, material, factor, prefix in references:
    existing = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(LIB/source))
    imported = set(bpy.context.scene.objects) - existing
    meshes = sorted([o for o in imported if o.type == "MESH"], key=lambda o: o.name)
    for i, obj in enumerate(meshes):
        world = [obj.matrix_world @ v.co for v in obj.data.vertices]
        center = Vector(((min(p.x for p in world)+max(p.x for p in world))/2,
                         (min(p.y for p in world)+max(p.y for p in world))/2,
                         min(p.z for p in world)))
        data = obj.data.copy()
        for vertex, p in zip(data.vertices, world):
            vertex.co = (p-center)*factor
        obj.parent = None
        obj.matrix_world = Matrix.Identity(4)
        obj.data = data
        obj.data.materials.clear()
        obj.data.materials.append(materials[material])
        obj.name = prefix + "_" + str(i).zfill(2)
        for polygon in data.polygons:
            polygon.material_index = 0
        records.append(dict(Name=obj.name, Source=source, MetreFactor=factor))
    for obj in imported:
        if obj.type != "MESH":
            bpy.data.objects.remove(obj, do_unlink=True)

for source, target in [
    ("八方旅人1/石头1/ObjMD_Rock_A_04/ObjTX_Rock_A_01_cl.tga", "reference_rock.tga"),
    ("八方旅人1/石头1/ObjMD_Stone_D_Cave_02/ObjTX_Stone_D_Cave_cl.tga", "reference_cave.tga"),
    ("八方旅人2/山石_八方旅人2/EnvFldMD_FoliageStone_A_MA/EnvFldTX_FoliageStone_A_cl.tga", "reference_moss.tga"),
]:
    shutil.copyfile(LIB/source, TEX/target)

# Native 64 px tiles: clustered mineral planes and broad horizontal strata, with
# low-frequency dust variation rather than independent noisy pixels.
for name, base in {"earth": (.39, .32, .23), "rock": (.35, .35, .31), "moss": (.25, .29, .19), "wet": (.30, .32, .28)}.items():
    rng = random.Random(782+len(name))
    shades = [[rng.randrange(5) for _ in range(8)] for _ in range(8)]
    pixels = []
    for y in range(64):
        for x in range(64):
            value = 1 + .025*math.sin(x*.43+y*.29) + rng.uniform(-.025, .025)
            if name == "rock":
                value += .09*math.sin(x*.14+y*.08)
                seam = (y + int(2*math.sin(x*.18))) % 21
                if seam == 0 and x%23 < 18:
                    value *= .78
                if (x+int(3*math.sin(y*.13)))%31 == 0 and y%21 > 12:
                    value *= .88
            else:
                if rng.random() < .08:
                    value += rng.choice((-.10, .09))
            pixels.extend((base[0]*value, base[1]*value, base[2]*value, 1))
    image = bpy.data.images.new("hd2d_"+name, width=64, height=64, alpha=False)
    image.pixels[:] = pixels
    image.filepath_raw = str(TEX/("hd2d_"+name+"_64.png"))
    image.file_format = "PNG"
    image.save()

bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.fbx(filepath=str(OUT/"hd2d_cave_modules.fbx"), use_selection=True,
                         object_types={"MESH"}, axis_forward="-Z", axis_up="Y",
                         apply_unit_scale=True, bake_anim=False, add_leaf_bones=False)
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/"tools/art/cave_hd2d_modules.blend"))
(OUT/"reference_manifest.json").write_text(json.dumps(records, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
print("HD2D_MODULES_EXPORTED:", len(bpy.context.scene.objects), "meshes, five distinct dual-grid shapes, native 64 px textures")
