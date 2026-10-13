"""Run with Blender: blender --background --python tools/build_cave_terrain.py.

Explicitly exports the authored landform and reusable rock modules. Unity scene
placement remains saved in the scene and is never regenerated at runtime.
"""
import math
import random
from pathlib import Path

import bpy
from mathutils import Vector
from mathutils.geometry import tessellate_polygon

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/Art/Models/CaveTerraced"
SOURCE = ROOT / "tools/art/cave_terraced_terrain.blend"
OUT.mkdir(parents=True, exist_ok=True)
SOURCE.parent.mkdir(parents=True, exist_ok=True)
rng = random.Random(731)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
materials = {}
for name, color in {
    "rock": (0.30, 0.28, 0.25, 1),
    "earth": (0.52, 0.43, 0.31, 1),
    "wet": (0.25, 0.33, 0.29, 1),
    "water": (0.08, 0.32, 0.34, 1),
    "stone": (0.46, 0.43, 0.36, 1),
    "moss": (0.22, 0.31, 0.24, 1),
    "path": (0.56, 0.47, 0.34, 1),
}.items():
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    materials[name] = mat


def group(name):
    obj = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(obj)
    return obj


def mesh(name, vertices, faces, slots, indices=None, parent=None):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.parent = parent
    for slot in slots:
        data.materials.append(materials[slot])
    if indices:
        for poly, index in zip(data.polygons, indices):
            poly.material_index = index
    # Blender XY is Unity XZ; Blender Z is Unity Y.
    uv = data.uv_layers.new(name="UVMap")
    for poly in data.polygons:
        for loop in poly.loop_indices:
            p = data.vertices[data.loops[loop].vertex_index].co
            uv.data[loop].uv = (p.x * 0.22, p.y * 0.22)
    return obj


def platform(name, outline, height, bottom, surface, parent):
    area = sum(outline[i][0]*outline[(i+1)%len(outline)][1]
               - outline[(i+1)%len(outline)][0]*outline[i][1] for i in range(len(outline)))
    if area < 0:
        outline = list(reversed(outline))
    n = len(outline)
    verts = [(x, y, height) for x, y in outline]
    # A broken intermediate stratum produces a real, shaded cliff silhouette.
    verts += [(x * 1.003, y * 1.003, bottom + (height-bottom)*0.43)
              for x, y in outline]
    verts += [(x, y, bottom) for x, y in outline]
    vectors = [Vector(v) for v in verts[:n]]
    lookup = {tuple(v): i for i, v in enumerate(vectors)}
    faces = [tuple(v if isinstance(v, int) else lookup[tuple(v)] for v in tri)
             for tri in tessellate_polygon([vectors])]
    indices = [0] * len(faces)
    for ring in range(2):
        for i in range(n):
            j = (i + 1) % n
            faces.append((ring*n+i, (ring+1)*n+i, (ring+1)*n+j, ring*n+j))
            indices.append(1)
    return mesh(name, verts, faces, [surface, "rock"], indices, parent)


def rock_shape(name, seed, pointed=False):
    randomizer = random.Random(seed)
    sides = 7 if pointed else 9
    verts = []
    radii = (0.62, 0.90, 0.68, 0.035) if pointed else (0.73, 1.0, 0.83, 0.35)
    levels = (0, 0.24, 0.65, 1.45) if pointed else (0, 0.24, 0.66, 1)
    for ring, (radius, height) in enumerate(zip(radii, levels)):
        for i in range(sides):
            angle = 2 * math.pi * i / sides + 0.05 * ring
            r = radius * randomizer.uniform(0.87, 1.13)
            verts.append((math.cos(angle)*r + 0.08*ring,
                          math.sin(angle)*r - 0.035*ring,
                          height + (0 if ring == 0 else randomizer.uniform(-0.07, 0.07))))
    faces = [tuple(reversed(range(sides)))]
    for ring in range(3):
        for i in range(sides):
            j = (i+1) % sides
            faces.append((ring*sides+i, ring*sides+j, (ring+1)*sides+j, (ring+1)*sides+i))
    faces.append(tuple(range(3*sides, 4*sides)))
    return mesh(name, verts, faces, ["rock"])


library = group("RockModules")
modules = []
for i in range(3):
    obj = rock_shape("cave_boulder_%02d" % (i+1), 111+i)
    obj.parent = library
    modules.append(obj)
stalagmite = rock_shape("cave_stalagmite_01", 172, True)
stalagmite.parent = library
modules.append(stalagmite)


def instance(template, name, xyz, scale, angle, parent):
    obj = template.copy()
    obj.name = name
    bpy.context.collection.objects.link(obj)
    obj.parent = parent
    obj.location = xyz
    obj.scale = scale
    obj.rotation_euler.z = math.radians(angle)
    return obj


terrain = group("CaveTerracedTerrain")
land = group("01_Landform_LowMinus1p2_Main0_HighPlus2p4")
land.parent = terrain
main_outline = [(-6,-21),(0,-22),(7,-20),(12,-22),(20,-19),(23,-14),
                (25,-8),(24,-1),(26,6),(23,14),(21,22),(14,25),
                (6,26),(-1,24),(-8,22),(-12,17),(-11,11),
                (-4.6,11.2),(-4.6,9),(-7.4,6),(-7,2),(-8,-3),(-7,-7),(-7,-11),(-8,-15)]
platform("MainSettlementGround_0m", main_outline, 0, -4.0, "earth", land)
west_outline = [(-24,-17),(-20,-20),(-17,-19),(-15,-14),(-16,-8),
                (-14,-3),(-12,3),(-14,9),(-18,14),(-19,21),
                (-23,22),(-27,17),(-28,9),(-26,2),(-28,-6)]
platform("RiverbankLowland_Minus1p2m", west_outline, -1.2, -4.0, "wet", land)
upper_outline = [(-5,12),(-2,10),(-0.7,10),(-0.7,11.8),(2.7,11.8),(2.7,11),
                 (3,11),(7,10),(11,12),(14,12),
                 (14,16),(17,18),(17,22),(12,25),(6,26),
                 (0,24),(-6,23),(-8,20),(-7,16)]
platform("NorthernHallTerrace_Plus2p4m", upper_outline, 2.4, -0.2, "earth", land)
home_outline = [(15,3),(20,3),(24,3),(24,7),(22,11),(18,12),
                (15,9),(14,5)]
platform("EasternResidentialShelf_Plus1p2m", home_outline, 1.2, -0.15, "earth", land)

river = group("02_CurvedRiverAndBanks")
river.parent = terrain
course = [(-12,-24),(-13,-19),(-13,-14),(-14,-9),(-12,-3),
          (-10,3),(-12,9),(-16,15),(-17,20),(-15,26)]


def river_center(y):
    for (a, ya), (b, yb) in zip(course, course[1:]):
        if y <= yb:
            return a + (b-a) * max(0, (y-ya)/(yb-ya))
    return course[-1][0]


# A continuous east-bank ledge joins both bridges to the main-layer ascents.
east_bank = [(river_center(y)+2.55,y) for y in range(-20,20,2)]
east_bank += [(river_center(y)+4.6,y) for y in reversed(range(-20,20,2))]
platform("EastBankSupplyLedge_Minus1p2m", east_bank, -1.2,-3.1,"wet",river)

verts, faces = [], []
sections = 86
for i in range(sections):
    y = -24 + 50 * i/(sections-1)
    x = river_center(y)
    width = 2.6 + 0.2*math.sin(y*0.65)
    verts.extend([(x-width,y,-1.75),(x+width,y,-1.75)])
    if i:
        faces.append((2*i-2,2*i-1,2*i+1,2*i))
water = mesh("RiverSurface", verts, faces, ["water"], parent=river)
colors = water.data.color_attributes.new(name="Banks", type="BYTE_COLOR", domain="CORNER")
uv = water.data.uv_layers.active
for poly in water.data.polygons:
    for loop in poly.loop_indices:
        vi = water.data.loops[loop].vertex_index
        uv.data[loop].uv = (vi % 2, vi//2/(sections-1))
        colors.data[loop].color = (1, 1, 0, 0)

connections = group("03_StairsRampsAndStoneCrossings")
connections.parent = terrain


def path(name, points, width, surface="earth"):
    vertices = []
    for i, (x,y,z) in enumerate(points):
        before = Vector(points[max(0,i-1)][:2])
        after = Vector(points[min(len(points)-1,i+1)][:2])
        direction = (after-before).normalized()
        normal = Vector((-direction.y, direction.x)) * width*0.5
        vertices.extend([(x+normal.x,y+normal.y,z), (x-normal.x,y-normal.y,z)])
    faces = [(2*i,2*i+1,2*i+3,2*i+2) for i in range(len(points)-1)]
    # Solid sidewalls meet the base; the walking deck is the authoritative height.
    count = len(vertices)
    bottom = min(-0.2, min(p[2] for p in points)-0.35)
    vertices += [(x,y,bottom) for x,y,z in vertices]
    indices = [0]*len(faces)
    for i in range(len(points)-1):
        faces += [(2*i,2*i+2,count+2*i+2,count+2*i),
                  (2*i+1,count+2*i+1,count+2*i+3,2*i+3)]
        indices += [1,1]
    return mesh(name, vertices, faces, [surface,"rock"], indices, connections)


path("EastWindingAscent", [(22,9,0.03),(22.5,12,0.03),(21.5,15,0.65),
                            (20,18,1.35),(17.5,20.5,2.42),(14,21,2.42)], 3.1)
path("ResidentialRamp", [(18,-3,0.02),(19,0,0.02),(19,3,1.22)], 2.8)
path("WesternSupplyAscent", [(-7.5,6,-1.18),(-6.3,9,-0.65),(-6,11,0.03)], 2.7)
path("LowlandNorthRamp", [(-19,12,-1.18),(-18,16,-1.18)], 2.7, "wet")

# Low-contrast sediment ribbons make circulation legible without painted lines.
path("EntranceSedimentPath", [(4,-20,0.012),(4,-14,0.014),(3,-8,0.015),
                              (3,-2,0.012),(1,2,0.013),(1,4.8,0.012)], 3.1, "path")
path("EasternLoopSediment", [(4,-13,0.012),(10,-12,0.015),(16,-9,0.013),
                             (19,-4,0.012),(20,1,0.013)], 2.6, "path")
path("SupplySedimentPath", [(-6,-9,0.012),(-3,-7,0.013),(2,-4,0.012)], 2.4, "path")
path("UpperTerraceSediment", [(1,12,2.413),(3,16,2.413),(6,18,2.413),
                              (11,20,2.413),(14,21,2.413)], 2.7, "path")

# Treads rise by 0.15m and remain independent editable meshes.
for i in range(16):
    y = 4.8 + i * 0.43
    h = (i+1)*0.15
    platform("CentralStair_%02d" % (i+1),
             [(-0.65,y),(2.65,y),(2.65,y+0.55),(-0.65,y+0.55)],
             h, -0.02, "stone", connections)
for i in range(8):
    x = -9.8 + i*0.36
    h = -1.2 + (i+1)*0.15
    platform("RiverToMainStair_%02d" % (i+1),
             [(x,-10.4),(x+0.38,-10.4),(x+0.38,-8.1),(x,-8.1)],
             h,-1.6,"stone", connections)

for name, y in [("SouthStoneCrossing", -9.2),("NorthSupplyCrossing", 7.4)]:
    x = river_center(y)
    path(name, [(x-3.6,y,-1.16),(x-1.8,y,-0.99),(x,y,-0.92),
                (x+1.8,y,-0.99),(x+3.6,y,-1.16)], 2.2, "stone")
    for edge in (-1,1):
        for i in range(8):
            instance(modules[i%3], name+"_EdgeStone_%02d_%s" % (i,edge),
                     (x-3.3+i*0.95,y+edge*1.12,-1.12),
                     (0.48,0.30,0.32), rng.uniform(0,360), connections)

walls = group("04_BackAndSideCaveWalls")
walls.parent = terrain
# Open southern cutaway retains the main entrance and route visibility.
perimeter = [(-27,-13),(-29,-7),(-28,0),(-29,7),(-28,14),(-25,21),
             (-21,25),(-15,29),(-9,28),(-3,28),(4,30),(11,29),
             (18,27),(24,24),(27,18),(28,11),(29,4),(28,-3),
             (27,-10),(23,-17)]
for i, (x,y) in enumerate(perimeter):
    if i in (6,7):
        continue  # Preserve the real aperture behind the river-source arch.
    height = rng.uniform(11.5,14.0) if y > 16 else rng.uniform(8.5,11.0)
    if x < -20:
        height *= 0.82
    instance(modules[i%3], "CaveWallMass_%02d" % i, (x,y,-3.3),
             (rng.uniform(4.2,5.6),rng.uniform(3.4,4.7),height),
             rng.uniform(-24,24), walls)
    instance(modules[(i+1)%3], "WallLowerStratum_%02d" % i,
             (x*0.955,y*0.955,-2.0),
             (rng.uniform(3.4,4.5),rng.uniform(2.4,3.3),rng.uniform(3.8,5.0)),
             rng.uniform(-35,35), walls)

# Natural arch at the northwestern source: two piers plus faceted vault stones.
arch = group("05_RiverSourceNaturalArch")
arch.parent = terrain
for side in (-1,1):
    instance(modules[1], "SourceArchPier_%s" % side, (-15+side*4.5,24,-1.4),
             (2.0,2.5,7.0), side*12, arch)
for i in range(7):
    angle = math.pi * i/6
    instance(modules[i%3], "SourceArchVault_%02d" % i,
             (-15+4.5*math.cos(angle),24,3.0+2.0*math.sin(angle)),
             (1.8,2.7,1.5), rng.uniform(-15,15), arch)

dressing = group("06_StrataBouldersAndMineralFormations")
dressing.parent = terrain
# Repeated rock clusters follow cliff edges; routes and gathering areas stay clear.
for edge, h in [(upper_outline,0.05),(main_outline,-1.7),(west_outline,-1.8)]:
    for i in range(len(edge)):
        a, b = edge[i], edge[(i+1)%len(edge)]
        distance = math.dist(a,b)
        for j in range(max(1,int(distance/1.7))):
            t = (j+0.4)/max(1,int(distance/1.7))
            x, y = a[0]+(b[0]-a[0])*t, a[1]+(b[1]-a[1])*t
            # Clear the authored connections through the cliff edge.
            if (-2<x<4 and 4<y<13) or (x>12 and 17<y<23) or (-11<x<-5 and -12<y<12):
                continue
            size = rng.uniform(0.65,1.3)
            instance(modules[i%3], "CliffFoot_%03d" % len(dressing.children),
                     (x,y,h),(size,size*0.8,size*rng.uniform(0.7,1.3)),
                     rng.uniform(0,360),dressing)
for x,y,h in [(-22,-12,-1.2),(-24,3,-1.2),(-23,15,-1.2),
              (22,-13,0),(24,-5,0),(23,15,0),(-5,20,2.4),
              (12,23,2.4),(-6,-18,0)]:
    for j in range(3):
        instance(stalagmite,"MineralCluster_%02d" % len(dressing.children),
                 (x+rng.uniform(-0.6,0.6),y+rng.uniform(-0.6,0.6),h),
                 (0.35+j*0.12,0.4+j*0.1,0.7+j*0.7),rng.uniform(0,360),dressing)

for x,y,h in [(-4,-3,0),(12,-1,0),(12,7,0),(-6,15,2.4),(23,-8,0)]:
    for j in range(4):
        size = rng.uniform(0.45,1.25)
        instance(modules[j%3],"InteriorStrata_%03d" % len(dressing.children),
                 (x+rng.uniform(-1.0,1.0),y+rng.uniform(-0.8,0.8),h-0.1),
                 (size,size*0.65,size*rng.uniform(0.55,1.4)),rng.uniform(0,360),dressing)

for i in range(60):
    x,y = rng.uniform(-4,21),rng.uniform(-16,8)
    if math.hypot(x-3,y+2)<3 or (abs(x-1)<2 and y>3):
        continue
    size = rng.uniform(0.08,0.22)
    instance(modules[i%3],"LooseFloorStone_%03d" % i,(x,y,0.01),
             (size,size*rng.uniform(0.6,1.3),size*0.35),rng.uniform(0,360),dressing)

# Sparse flattened stones and damp moss create material transitions.
for i in range(48):
    y = rng.uniform(-18,22)
    x = river_center(y)+rng.choice((-1,1))*rng.uniform(2.75,3.4)
    h = -1.3 if x<river_center(y) else -1.0
    obj = instance(modules[i%3],"WaterlineStone_%02d" % i,(x,y,h),
                   (rng.uniform(0.25,0.55),rng.uniform(0.25,0.6),rng.uniform(0.18,0.5)),
                   rng.uniform(0,360),river)
for i, (x,y,h) in enumerate([(-22,-3,-1.17),(-21,8,-1.17),(-21,18,-1.17),
                             (-17,-15,-1.17),(-24,13,-1.17)]):
    patch = [ (x+math.cos(j*math.pi/4)*rng.uniform(0.9,1.6),
               y+math.sin(j*math.pi/4)*rng.uniform(0.6,1.1)) for j in range(8)]
    platform("DampMossPatch_%02d" % i,patch,h,h-0.025,"moss",river)

# Entry rocks frame the foreground without hiding the central circulation.
foreground = group("07_LowForegroundCutaway")
foreground.parent = terrain
for i,(x,y) in enumerate([(-25,-18),(-20,-21),(-9,-22),(13,-23),(21,-20)]):
    instance(modules[i%3],"ForegroundRock_%02d" % i,(x,y,-3.8),
             (3.2,2.5,4.8),rng.uniform(-25,25),foreground)


def select_tree(root):
    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for obj in root.children_recursive:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root


def export(root, path):
    select_tree(root)
    bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True,
                             object_types={"MESH","EMPTY"},
                             axis_forward="-Z", axis_up="Y",
                             apply_unit_scale=True, bake_anim=False,
                             use_mesh_modifiers=True, add_leaf_bones=False,
                             mesh_smooth_type="FACE", use_triangles=True)


export(terrain, OUT / "cave_terraced_landform.fbx")
export(library, OUT / "cave_rock_modules.fbx")
library.hide_viewport = True
library.hide_render = True
select_tree(terrain)
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE))
print("CAVE TERRAIN EXPORTED:", len(terrain.children_recursive), "editable objects")
