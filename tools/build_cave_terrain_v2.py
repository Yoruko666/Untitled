"""Run with Blender: blender --background --python tools/build_cave_terrain_v2.py.

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
OUT = ROOT / "Assets/Art/Models/CaveTerracedV2"
SOURCE = ROOT / "tools/art/cave_terraced_terrain_v2.blend"
OUT.mkdir(parents=True, exist_ok=True)
SOURCE.parent.mkdir(parents=True, exist_ok=True)
PLAN_SCALE = 2.0  # Double XZ extents; heights and final river width remain independent.
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
    "wood": (0.32, 0.21, 0.12, 1),
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


import json

river_course=[(-12.4,-15),(-13.3,-10),(-14.3,-3),(-12.5,4),(-12.2,10),
              (-13.8,16),(-16.3,23),(-15.6,28),(-13,33.4)]
west_fork=[(-27,-34),(-25,-29),(-21.8,-25),(-16.7,-20),(-12.4,-15)]
east_fork=[(-7.6,-34),(-7.4,-29),(-8.4,-25),(-10.7,-20),(-12.4,-15)]
river_half=3.9*0.9/PLAN_SCALE
fork_width=2.34/PLAN_SCALE
low=-2.4
high=4.8
water_height=low-0.65


def center(course,z):
    for (xa,za),(xb,zb) in zip(course,course[1:]):
        if z<=zb:
            t=max(0,(z-za)/(zb-za))
            return xa+(xb-xa)*t*t*(3-2*t)
    return course[-1][0]


def river_center(z):
    return center(river_course,z)


def fork_half(z):
    return fork_width+(river_half-fork_width)*max(0,min(1,(z+20)/5))


def shore(side,z):
    if z>=-15:
        return river_center(z)+side*river_half
    return center(west_fork if side<0 else east_fork,z)+side*fork_half(z)


terrain = group("CaveTerracedTerrain")
land = group("01_Landform_NorthGoal_SouthEntrance")
land.parent = terrain
bridge_zs=[-10,14.47]
def main_margin(z):
    # Carve pockets so the main floor never hides the low river stairs.
    return 3.2+1.2*max(0,max(1-abs(z-b)/2.2 for b in bridge_zs))
main_outline = [(shore(1,z)+main_margin(z),z) for z in range(-34,30)]
main_outline += [(-6,31),(2,32),(8,31),(15,33),(23,32),(28,29),
                 (31,24),(32,17),(30.2,10),(33,4),(30.5,-3),
                 (33,-10),(30,-17),(31,-25),(29,-30),(20,-32),
                 (10,-32),(2,-34)]
platform("MainSettlementGround_0m",main_outline,0,-6,"earth",land)
west_outline = [(-36,-38),(-31,-40),(shore(-1,-34)-2.6,-34)]
west_outline += [(shore(-1,z)-2.6,z) for z in range(-33,30)]
west_outline += [(-21,33),(-25,31),
                (-30,27),(-30.5,20),(-30,12),(-31,5),(-31,-4),(-31,-14),(-34,-24),(-36,-31)]
platform("RiverbankLowland_Minus2p4m",west_outline,low,-6,"wet",land)
upper_outline = [(-9.2,25.0),(-5.8,24.8),(-1,25.1),(3.8,24.9),(7.8,25.5),
                 (9,28.0),(5.5,32),(-1,33),(-7.8,30.5),(-9.6,27)]
platform("09_AltarHallTerrace_Plus4p8m",upper_outline,high,-0.2,"earth",land)
elder_outline = [(14,24),(17,23.5),(21,24),(23,26),
                 (22,30),(18.5,31.3),(14.5,29.5)]
platform("10_ElderAndPrivateStore_Plus4p8m",elder_outline,high,-0.2,"earth",land)

# Relative area relationships come from the plan; contours retain the prior cave's natural river and terraces.
# The 15 area names are terrain reservations, not implemented businesses.
zones = [
    (1,"Evacuation",0,-30,0,4.4,2.0,"earth"),
    (2,"EntranceForecourt",0,-22.6,0,4.2,3.0,"stone"),
    (3,"MilitaryTraining",19.2,10.8,0.45,4.4,3.8,"stone"),
    (4,"Residence",5.9,-15.5,0.30,3.8,2.6,"earth"),
    (5,"KitchenPreparation",17.7,-12.6,0.30,4.2,3.2,"stone"),
    (6,"Tavern",-4.0,-3.3,0.30,2.8,2.5,"earth"),
    (7,"Casino",11.8,-0.7,0.30,3.6,2.5,"stone"),
    (8,"Feast",2.3,10.4,0.30,4.5,3.1,"earth"),
    (9,"AltarHall",-1.5,28.5,high,7.2,3.2,"earth"),
    (10,"Elder",18.3,27.2,high,3.6,3.0,"earth"),
    (11,"Planting",-24,7.8,low,3.3,4.0,"moss"),
    (12,"Warehouse",-2.7,10.4,0.30,2.1,3.2,"stone"),
    (13,"UnloadingCourt",19.2,-22.6,0,4.4,3.0,"stone"),
    (14,"PublicHearth",1.5,-2.9,0,3.0,2.5,"stone"),
    (15,"RiverWorkshop",-24,-10,low,3.5,3.5,"wet"),
]
zone_root = group("08_AreaTerrainReservations_01_to_15")
zone_root.parent = terrain
zone_objects = {}
for number,name,x,z,h,w,d,material in zones:
    area = group("%02d_%s" % (number,name))
    area.parent = zone_root
    zone_objects[number] = area
    if number in (9,10):
        continue
    # Chamfered, uneven excavation edges read as terrain rather than diagram boxes.
    outline = [(x-w+0.7,z-d),(x+w-0.8,z-d+0.15),(x+w,z-d+0.8),
               (x+w-0.1,z+d-0.6),(x+w-0.8,z+d),(x-w+0.8,z+d-0.1),
               (x-w,z+d-0.8),(x-w+0.1,z-d+0.8)]
    if h > 0:
        platform("%02d_%sExcavatedShelf" % (number,name),outline,h,-0.08,material,area)
    elif number in (1,2,13,14):
        # Disconnected paving stones, not coincident floor overlay meshes.
        for j in range(9 if number==14 else 5):
            angle=2*math.pi*j/(9 if number==14 else 5)
            instance(modules[j%3],"%02d_CourtPaving_%02d"%(number,j),
                     (x+math.cos(angle)*w*0.63,z+math.sin(angle)*d*0.65,-0.08),
                     (rng.uniform(0.5,0.8),rng.uniform(0.4,0.65),0.10),j*33,area)

river = group("02_RiverBoundary_TwoBridgeConnections")
river.parent = terrain
river_start,river_end = -15,33.4


east_bank=[(shore(1,z)-0.02,z) for z in range(-34,34)]
east_bank += [(shore(1,z)+3.3,z) for z in reversed(range(-34,34))]
platform("EastBankGravelBench_Minus2p4m",east_bank,low,-5.8,"wet",river)
west_bank=[(shore(-1,z)-2.7,z) for z in range(-34,34)]
west_bank += [(shore(-1,z)+0.02,z) for z in reversed(range(-34,34))]
platform("WestBankGravelBench_Minus2p4m",west_bank,low,-5.8,"wet",river)
def contour(course,start,end,side,width):
    steps=math.ceil(abs(end-start))
    return [(center(course,z)+side*width(z),z,0 if side<0 else 1)
            for z in [start+(end-start)*i/steps for i in range(steps+1)]]


# One concave water mesh gives a continuous Y confluence without overlapping water surfaces.
outline=contour(river_course,33.4,-15,-1,lambda z:river_half)
outline+=contour(west_fork,-15,-34,-1,fork_half)[1:]
outline += [(west_fork[0][0]+fork_width*math.cos(a),-34+fork_width*math.sin(a),(1+math.cos(a))/2)
            for a in [-math.pi+math.pi*i/12 for i in range(1,12)]]
outline+=contour(west_fork,-34,-20.8,1,fork_half)
outline+=contour(east_fork,-20.8,-34,-1,fork_half)
outline += [(east_fork[0][0]+fork_width*math.cos(a),-34+fork_width*math.sin(a),(1+math.cos(a))/2)
            for a in [-math.pi+math.pi*i/12 for i in range(1,12)]]
outline+=contour(east_fork,-34,-15,1,fork_half)
outline+=contour(river_course,-15,33.4,1,lambda z:river_half)[1:]
area=sum(outline[i][0]*outline[(i+1)%len(outline)][1]-outline[(i+1)%len(outline)][0]*outline[i][1]
         for i in range(len(outline)))
if area<0:
    outline.reverse()
verts=[(x,z,water_height) for x,z,side in outline]
vectors=[Vector(v) for v in verts]
lookup={tuple(v):i for i,v in enumerate(vectors)}
faces=[tuple(v if isinstance(v,int) else lookup[tuple(v)] for v in tri) for tri in tessellate_polygon([vectors])]
water=mesh("RiverSurface",verts,faces,["water"],parent=river)
mesh("Riverbed_VisibleThroughWater",[(x,z,water_height-1.7) for x,z,_ in verts],faces,["wet"],parent=river)
colors=water.data.color_attributes.new(name="Banks",type="BYTE_COLOR",domain="CORNER")
for poly in water.data.polygons:
    for loop in poly.loop_indices:
        vi=water.data.loops[loop].vertex_index
        water.data.uv_layers.active.data[loop].uv=(outline[vi][2],0.5)
        colors.data[loop].color=(1,1,0,0)

island_outline=[(center(west_fork,z)+fork_half(z)+0.15,z) for z in (-21,-24,-27,-30,-34)]
island_outline += [(-20.8,-35.4),(-18,-37.0),(-15.2,-37.0),(-12.8,-35.5)]
island_outline += [(center(east_fork,z)-fork_half(z)-0.15,z) for z in (-34,-30,-27,-24,-21)]
platform("SouthForkErodedIsland",island_outline,low,-5.8,"moss",river)

# Dress the inner banks too: continuous gravel coves follow both fork channels.
for course,label,side in [(west_fork,"IslandWest",1),(east_fork,"IslandEast",-1)]:
    inner=[(center(course,z)+side*(fork_half(z)+0.03),z) for z in range(-34,-20)]
    inner += [(center(course,z)+side*(fork_half(z)+0.85),z) for z in reversed(range(-34,-20))]
    platform(label+"GravelBench",inner,low,-5.8,"wet",river)

connections=group("03_ThreeApproaches_SharedHallToElderEnd")
connections.parent=terrain
route_samples=[]


def path(name,points,width,surface="earth",parent=None,check=False):
    vertices=[]
    for i,(x,z,h) in enumerate(points):
        before=Vector(points[max(0,i-1)][:2])
        after=Vector(points[min(len(points)-1,i+1)][:2])
        normal=Vector((-(after-before).y,(after-before).x)).normalized()*width*0.5
        vertices.extend([(x+normal.x,z+normal.y,h),(x-normal.x,z-normal.y,h)])
    # Preserve the authored centerline on steep bends. A single non-planar quad
    # can otherwise triangulate above the intended walking height.
    centers=len(vertices)
    vertices += [(x,z,h) for x,z,h in points]
    faces=[]
    for i in range(len(points)-1):
        a,b=centers+i,centers+i+1
        faces += [(2*i,a,b),(2*i,b,2*i+2),
                  (a,2*i+1,2*i+3),(a,2*i+3,b)]
    count=len(vertices)
    base=-6 if name.startswith("ContinuousCaveRock") else -3.2 if name=="B_WestHallAscent" else -0.2
    bottom=min(base,min(p[2] for p in points)-0.35)
    vertices += [(x,z,bottom) for x,z,h in vertices]
    indices=[0]*len(faces)
    for i in range(len(points)-1):
        faces += [(2*i,2*i+2,count+2*i+2,count+2*i),
                  (2*i+1,count+2*i+1,count+2*i+3,2*i+3)]
        indices += [1,1]
    obj=mesh(name,vertices,faces,[surface,"rock"],indices,parent or connections)
    if check:
        for a,b in zip(points,points[1:]):
            for j in range(5):
                t=j/4
                route_samples.append(dict(X=a[0]+(b[0]-a[0])*t,
                                          Z=a[1]+(b[1]-a[1])*t,
                                          Height=a[2]+(b[2]-a[2])*t,Name=name))
    return obj


path("SouthernDryRim",[(0,-31,0.03),(0,-36,0.03),(-6,-38,0.03),(-14,-39,low+0.03),
                       (-26,-39,low+0.03),(-33,-37,low+0.03),(-33,-32,low+0.03)],7.0,"wet",check=True)
path("B_SouthSupplyEntrance",[(-33,-32,low+0.03),(-31,-27,low+0.03),(-27,-21,low+0.03),
                             (-24,-17,low+0.03),(-24,-10,low+0.03)],3.2,"wet",check=True)
# The west-side route passes through the workshop and planting reservations.
path("B_WestBankSupplyRoute",[(-24,-10,low+0.03),
                             (-24,-1,low+0.03),(-24,7.8,low+0.03),(-24,14.47,low+0.03)],3.0,"wet",check=True)

def timber(name,x,z,h,sx,sz,sh):
    outline=[(x-sx/2,z-sz/2),(x+sx/2,z-sz/2),(x+sx/2,z+sz/2),(x-sx/2,z+sz/2)]
    obj=platform(name,outline,h+sh/2,h-sh/2,"wood",connections)
    for polygon in obj.data.polygons:
        polygon.material_index=0
    return obj

for name,z in [("EntranceBridge",-10),("SupplyBridge",14.47)]:
    x=river_center(z)
    half=river_half+0.5
    # Continuous hidden deck collider lies flush under separate wooden planks.
    deck=path(name+"TimberDeck",[(x-half,z,low+0.03),(x,z,low+0.03),(x+half,z,low+0.03)],1.85,"wood",check=True)
    for polygon in deck.data.polygons: polygon.material_index=0
    for vertex in deck.data.vertices: vertex.co.z -= 0.10
    count=math.ceil(half*2/0.19)
    for j in range(count):
        px=x-half+(j+.5)*half*2/count
        timber(name+"_Plank_%02d"%j,px,z,low-.03,half*2/count-.025,1.85,0.12)
    for side in (-1,1):
        timber(name+"_LongBeam_%d"%side,x,z+side*.68,low-.2,half*2+.3,.14,.28)
        for j,offset in enumerate((-half,-half*.42,half*.42,half)):
            bottom=water_height-2.0
            top=low+1.05
            timber(name+"_SubmergedPile_%d_%d"%(side,j),x+offset,z+side*.89,
                   (top+bottom)/2,.14,.14,top-bottom)
        timber(name+"_Handrail_%d"%side,x,z+side*.89,low+.85,half*2+.15,.09,.12)
    path(name+"WestLanding",[(-24,z,low+0.03),(x-half,z,low+0.03)],1.85,"wet",check=True)
    for i in range(12):
        sx=x+half+0.12+i*0.30
        h=low+(i+1)*0.2
        platform(name+"EastStair_%02d"%(i+1),[(sx,z-.925),(sx+.36,z-.925),
                 (sx+.36,z+.925),(sx,z+.925)],h,low-.1,"stone",connections)
        route_samples.append(dict(X=sx+.18,Z=z,Height=h,Name=name+"EastStair"))
    path(name+"EastLanding",[(x+half+3.72,z,.03),(-5.2,z,.03)],1.85,"earth",check=True)
path("B_WestHallAscent",[(-5.2,14.47,0.03),(-6.8,16.5,0.03),(-10.2,18.8,1.4),(-11.4,22,2.5),
                         (-11.1,24.8,3.5),(-10.2,26,high+0.03),(-9.2,27,high+0.03),(-6.7,27,high+0.03)],2.0,check=True)

# A stays in the center; C follows the far eastern boundary before turning to the hall.
path("A_CentralPublicApproach",[(0,-30,0.035),(0,-22.6,0.035),(0,-18.47,0.035),
                               (1.5,-2.9,0.035),(2.3,10.4,0.035),(0.0,15.3,0.035),
                               (-1.5,14.7,0.035)],3.4,check=False)
# Keep baseline paths as optional review overlays; their corridors remain clear on the base ground.
path("C_EasternPerimeterRoute",[(0,-30,0.035),(26.1,-30,0.035),(26.1,-22.6,0.035),
                               (26.1,-12.6,0.035),(24.0,-8,0.035),(24.0,2.6,0.035),
                               (26.0,10.8,0.035),(26.0,19.0,0.035),(14.0,19.0,0.035),
                               (10.5,17.5,0.035),(8.5,14.7,0.035),(5.5,14.7,0.035)],3.0)

# The ascent winds across two inhabited foothill benches before the ceremonial plateau.
platform("HallFoothill_LowerLanding_1p2",[(-5.5,17.8),(-3.6,17.7),(-1.8,17.4),(.2,17.65),(1.9,17.45),(3.7,17.8),(5.7,17.8),(6.8,18.7),
         (4.5,19.7),(2,19.5),(-.5,19.8),(-3.5,19.5),(-5.8,18.8)],1.2,-0.15,"earth",land)
platform("HallFoothill_MiddleLanding_2p4",[(-5.0,21.5),(-3.7,21.2),(-1,21.4),(1.5,21.1),(3.8,21.35),(6.2,21.5),(8.8,22.2),
         (8.5,24.4),(6.1,24.6),(3.2,24.5),(.8,24.7),(-2.4,24.4),(-5.7,24.0)],2.4,-0.15,"earth",land)
ascent=[(-1.5,14.6,0.03),(-2.8,15.2,0.03),(-4.2,16.5,0.55),
        (-4.8,18,1.23),(-3,18.5,1.23),(3,18.5,1.23),(6,18.5,1.23),(8,19.4,1.65),
        (9.5,21.1,2.43),(8.0,22.5,2.43),(3,22.5,2.43),(-1,22.5,2.43),
        (-3.0,23.5,3.1),(-6.5,24.7,4.83),(-6.5,27,4.83)]
path("A_WindingHallProcession",ascent,2.1,"earth",check=True)
# Broken, earth-covered ramp shoulders soften the ascent's retaining edges.
for side in (-1,1):
    shoulder_verts=[]
    for i,(x,z,h) in enumerate(ascent):
        before=Vector(ascent[max(0,i-1)][:2]); after=Vector(ascent[min(len(ascent)-1,i+1)][:2])
        normal=Vector((-(after-before).y,(after-before).x)).normalized()*side
        inner=1.07; outer=inner+1.0+0.2*math.sin(i*1.7)
        shoulder_verts += [(x+normal.x*inner,z+normal.y*inner,h-.025),
                           (x+normal.x*outer,z+normal.y*outer,max(-.06,h-1.0))]
    shoulder_faces=[(2*i,2*i+1,2*i+3,2*i+2) for i in range(len(ascent)-1)]
    if side<0: shoulder_faces=[tuple(reversed(f)) for f in shoulder_faces]
    mesh("HallAscentWeatheredShoulder_%d"%side,shoulder_verts,shoulder_faces,["earth"],parent=land)
east_ascent=[(17,19,0.03),(14,16.2,0.03),(11.8,16.5,0.3),
             (9,16.7,0.85),(7,17,1.23),(5,18.5,1.23)]
path("C_LivingQuarterToHallFoothill",east_ascent,2.2,"earth",check=True)
# A dedicated second path from the east food-hall door to the hearth plaza.
feast_hearth=[(5.4,9.4,0.33),(6.5,8.1,0.33),(7.4,6.7,0.03),
             (6.8,4.6,0.03),(5.3,2.3,0.03),(3.8,0.1,0.03),(1.5,-2.9,0.03)]
path("FeastToHearthGardenWalk",feast_hearth,1.6,"path",check=True)
path("D_SharedHallToElderNeck",[(6.8,26,high+0.03),(10.5,26,high+0.03),
                               (14.7,26,high+0.03),(17,27.2,high+0.03)],2.6,check=True)

# Each low shelf has an explicit shallow ramp, keeping the gray cross-connections usable.
for number in (3,4,5,6,7,8,12):
    _,name,x,z,h,w,d,_=zones[number-1]
    if number==8:
        # A public road traverses the feast's western edge, with a rise and fall.
        path("A_FeastSouthRamp",[(0,5.5,0.03),(0,7.35,0.33),(0,8.2,0.33)],2.8,parent=zone_objects[number],check=True)
        path("A_FeastNorthRamp",[(0,12.0,0.33),(0,13.7,0.33),(0,14.6,0.03)],2.8,parent=zone_objects[number],check=True)
    elif number==12:
        # Adjacent warehouse and feast reservations share the same shallow shelf height.
        path("12_WarehouseFeastSaddle",[(-0.8,z,0.33),(-2.7,z,0.33)],2.4,
             parent=zone_objects[number],check=True)
    else:
        # From the right/left access alley to the raised excavation floor.
        direction=-1 if number in (4,5,7) else 1
        edge=x+direction*w
        path("%02d_%sAccessRamp"%(number,name),[(edge+direction*1.9,z,0.03),
                                               (edge+direction*0.1,z,h+0.03),
                                               (edge-direction*0.8,z,h+0.03)],2.4,
             parent=zone_objects[number],check=True)

# Gray links in the plan are preserved as open corridors, not additional river crossings.
clear_routes=[
    ("HallProcession",ascent,2.1),
    ("LivingQuarterAscent",east_ascent,2.2),
    ("FoodHallHearthWalk",feast_hearth,1.6),
    ("A_Central",[(0,-30,0),(0,-22.6,0),(0,-18.47,0),(1.5,-2.9,0),
                   (2.3,4.5,0),(0,5.8,0),(0,8.2,0.3),(0,12,0.3),(-1.5,14.7,0)],3.5),
    ("C_East",[(0,-30,0),(26.1,-30,0),(26.1,-22.6,0),(26.1,-12.6,0),
                (24,-8,0),(24,2.6,0),(26,10.8,0),(26,19,0),(14,19,0),
                (10.5,17.5,0),(8.5,14.7,0),(5.5,14.7,0)],3.1),
    ("HearthCasinoLink",[(-4,-3.3,0.3),(1.5,-2.9,0),(11.8,-0.7,0.3),(24,2.6,0)],2.5),
    ("FeastMilitaryLink",[(6.8,10.4,0),(14.8,10.4,0),(19.2,10.8,0.45),(26,10.8,0)],2.5),
    ("ResidenceKitchenLink",[(5.9,-15.5,0.3),(12,-15,0),(17.7,-12.6,0.3),(19.2,-22.6,0)],2.4),
]

def distance_to_segment(x,z,a,b):
    dx,dz=b[0]-a[0],b[1]-a[1]
    t=max(0,min(1,((x-a[0])*dx+(z-a[1])*dz)/(dx*dx+dz*dz))) if dx*dx+dz*dz else 0
    return math.hypot(x-a[0]-t*dx,z-a[1]-t*dz)


def on_corridor(x,z,margin=0.7):
    for _,points,width in clear_routes:
        if any(distance_to_segment(x,z,a,b)<width/2+margin for a,b in zip(points,points[1:])):
            return True
    if -26<x<-4 and any(abs(z-b)<2.3 for b in bridge_zs):
        return True
    if -13<x<-6 and 13<z<27:
        return True
    if -4<x<8 and 14<z<23:
        return True
    return False


walls=group("04_OuterCaveWallsAndInteriorSpurs")
walls.parent=terrain
perimeter=[(-41,-34),(-39,-26),(-37,-19),(-35,-12),(-35,-4),(-34,5),(-33,14),(-32,23),
           (-29,30),(-23,35),(-8,35),(-1,36),(7,36),(15,37),(23,36),(29,33),
           (34,27),(35,20),(34,13),(36,5),(34,-3),(37,-11),(34,-19),(35,-26)]
# Continuous rock beneath the irregular masses closes daylight gaps. The split
# deliberately leaves the northern river aperture and southern cutaway open.
for label,points in [("West",perimeter[:10]),("NorthEast",perimeter[10:])]:
    path("ContinuousCaveRock"+label,[(x,z,7.2 if z>28 else 4.1) for x,z in points],
         6.8,"rock",parent=walls)
for i,(x,z) in enumerate(perimeter):
    height=rng.uniform(9.0,12.2) if z>28 else rng.uniform(7.5,10.0)
    instance(modules[i%3],"CaveWallMass_%02d"%i,(x,z,-2.8),
             (rng.uniform(3.8,4.6),rng.uniform(4.2,5.1),height),rng.uniform(-20,20),walls)
    instance(modules[(i+1)%3],"WallLowerStratum_%02d"%i,(x*0.982,z*0.982,-1.0),
             (3.3,3.7,rng.uniform(3,4.5)),rng.uniform(-25,25),walls)

# Low ridges reserve doorways and break sight lines, without populating the plan's guards.
cover_positions=[(-9.2,-1.1,0,1.8,2.6),(-7.8,12.4,0.3,1.7,2.8),(14.4,1.2,0.3,1.9,2.5),
                 (5.2,-24,0,1.8,1.6),(-5.0,-24,0,1.8,1.6),
                 (11.0,-19,0,2.1,2.0),(22.5,-17,0,1.9,2.2),
                 (9.3,6.5,0,1.8,3.0),(14.0,15.3,0,2.0,2.8),
                 (-7.7,6.4,0,1.0,2.0)]
for i,(x,z,h,w,d) in enumerate(cover_positions):
    if on_corridor(x,z,0.2):
        continue
    instance(modules[i%3],"InteriorRockSpur_%02d"%i,(x,z,h-0.1),
             (w,d,rng.uniform(1.7,2.4)),rng.uniform(-20,20),walls)

arch=group("05_NorthRiverAperture")
arch.parent=terrain
for side in (-1,1):
    instance(modules[1],"SourceArchPier_%s"%side,(-13.0+side*(river_half+1.3),33,-1.5),
             (2.0,2.3,7.5),side*12,arch)
for i in range(7):
    angle=math.pi*i/6
    instance(modules[i%3],"SourceArchVault_%02d"%i,
             (-13.0+(river_half+1.3)*math.cos(angle),33,3.0+2.4*math.sin(angle)),
             (1.6,2.5,1.3),rng.uniform(-15,15),arch)

dressing=group("06_CliffStrataAndRiverDetails")
dressing.parent=terrain
for outline,h in [(upper_outline,0.1),(elder_outline,0.1)]:
    for i,a in enumerate(outline):
        b=outline[(i+1)%len(outline)]
        count=max(1,int(math.dist(a,b)/1.7))
        for j in range(count):
            t=(j+0.4)/count
            x,z=a[0]+(b[0]-a[0])*t,a[1]+(b[1]-a[1])*t
            if on_corridor(x,z) or (6<x<16 and 24.3<z<27.5):
                continue
            size=rng.uniform(0.8,1.3)
            instance(modules[i%3],"HighCliffFoot_%03d"%len(dressing.children),
                     (x,z,h),(size,size*0.8,size*1.1),rng.uniform(0,360),dressing)
# Talus and exposed strata turn cut platforms into a weathered hillside.
for i,(x,z,h,w,d,height) in enumerate([
    (-7.0,24.5,.1,1.0,.65,2.7),(-5.8,24.6,.2,.7,.55,3.4),
    (.2,24.7,.1,.8,.55,3.1),(2.1,24.6,.1,1.1,.65,3.4),(5.2,25.0,.1,1.0,.7,3.3),
    (-.6,16.9,-.1,.7,.45,1.0),(1.9,17.1,-.1,.85,.45,1.1),
    (-4.8,20.9,.0,.7,.6,1.8),(.2,20.8,.1,.75,.4,1.8),(3.7,21.0,.0,.8,.5,1.9)]):
    instance(modules[i%3],"FoothillWeatheredCrag_%02d"%i,(x,z,h),(w,d,height),i*29,dressing)
# Smaller wall buttresses break up the silhouette of the large structural masses.
for i,(a,b) in enumerate(zip(perimeter,perimeter[1:])):
    if i==9: continue
    x,z=(a[0]+b[0])*.5,(a[1]+b[1])*.5
    inset=1.8
    length=math.hypot(x,z)
    instance(modules[(i+2)%3],"WallInnerButtress_%02d"%i,
             (x-x/length*inset,z-z/length*inset,-1.6),
             (rng.uniform(1.1,1.7),rng.uniform(1.3,2.0),rng.uniform(3.2,5.8)),i*19,walls)
for i in range(155):
    z=rng.uniform(-33,32)
    if any(abs(z-b)<2.0 for b in bridge_zs):
        continue
    side=rng.choice((-1,1))
    x=shore(side,z)+side*rng.uniform(0.05,0.35)
    instance(modules[i%3],"WaterlineStone_%03d"%i,(x,z,low-0.2),
             (rng.uniform(0.25,0.5),rng.uniform(0.3,0.6),rng.uniform(0.2,0.5)),rng.uniform(0,360),river)
    # Broken rock at the foot of the main cliff leaves a clear gravel shelf.
    if side==1 and z>-14:
        instance(modules[(i+1)%3],"RiverCliffTalus_%03d"%i,
                 (shore(1,z)+2.8,z,low-0.1),
                 (rng.uniform(0.35,0.65),rng.uniform(0.45,0.8),rng.uniform(0.5,1.2)),rng.uniform(0,360),dressing)
for number,name,x,z,h,w,d,material in zones:
    if number in (1,2,14):
        continue
    for j in range(3):
        px=x+rng.choice((-1,1))*w*0.85
        pz=z+rng.choice((-1,1))*d*0.85
        if on_corridor(px,pz,0.4):
            continue
        size=rng.uniform(0.3,0.6)
        instance(modules[j%3],"%02d_ReservationEdgeRock_%02d"%(number,j),
                 (px,pz,h),(size,size*0.75,size*0.7),rng.uniform(0,360),zone_objects[number])
for i,(x,z,h) in enumerate([(-28,25,low),(-28,-19,low),(-28,1,low),
                          (28,-5,0),(28,16,0),(-6,30,high),(21,29,high)]):
    for j in range(3):
        instance(stalagmite,"MineralCluster_%d_%d"%(i,j),
                 (x+j*0.45,z+j*0.3,h),(0.35+j*0.12,0.4,0.8+j*0.55),j*55,dressing)

# Select the existing point-filtered cave kit; scene placement is authored here,
# not generated by a runtime scatter component. Wet vegetation follows water,
# dry rubble stays at walls, and district centers are left available for buildings.
decorations=[]
def detail(prefab,x,z,h,scale=1.0,yaw=-5,group_name="Riverbank"):
    decorations.append(dict(Prefab="cave_"+prefab+"_01",X=x,Z=z,Height=h,
                            Scale=scale,Yaw=yaw,Name=group_name))

for side in (-1,1):
    for i,z in enumerate(range(-31,32,3)):
        if any(abs(z-b)<2.4 for b in bridge_zs):
            continue
        x=shore(side,z)+side*rng.uniform(0.8,1.5)
        detail("river_stones",x,z,low+0.03,rng.uniform(1.0,1.6),group_name="BothBankPebbles")
        detail("moss_patch",x+side*.5,z+.6,low+.04,rng.uniform(.7,1.1),rng.uniform(0,360),"WetGround")
        plants=("river_reeds","fern_clump","mushroom_cream","moss_weeds","mushroom_teal")
        detail(plants[i%len(plants)],x,z+.9,low+.03,rng.uniform(1.1,1.6),group_name="BothBankGrowth")
        if side<0:
            detail("mushroom_brown",x-1.1,z-.4,low+.03,1.2,group_name="WestFungalColonies")
        elif z>-14:
            # Roots hang from the exposed earth lip, over the actual 2.4m cliff.
            detail("roots_hanging",shore(1,z)+3.18,z,0,0.55,group_name="CentralCliffRoots")

for course,label,side in [(west_fork,"ForkWestInner",1),(east_fork,"ForkEastInner",-1)]:
    for i,z in enumerate(range(-32,-23,3)):
        x=center(course,z)+side*(fork_half(z)+.6)
        detail("river_stones",x,z,low+.03,1.1,group_name=label)
        detail("mushroom_ochre",x+side*.4,z+.6,low+.03,1.2,group_name=label)

for x,z in [(-27,-14),(-28,-4),(-27,3),(-28,15),(-26,22),(-25,27)]:
    for j,prefab in enumerate(("moss_patch","fern_clump","mushroom_ochre","mushroom_cream")):
        detail(prefab,x+j*.65,z+math.sin(j)*.8,low+.03,1.35,group_name="WestValleyClusters")

for x,z,h in [(28,-25,0),(27,-18,0),(28,-10,0),(27,-2,0),(28,6,0),(28,14,0),(26,25,0),
              (7,-27,0),(10,-20,0),(10,-8,0),(18,4,0),(13,15,0),
              (-7,28,high),(4,30,high),(20,29,high),(20,24.7,high)]:
    if on_corridor(x,z,1.0):
        continue
    detail("fractured_rock",x,z,h+.03,1.25,group_name="DryRubbleCorners")
    detail("dry_grass",x+1.0,z-.3,h+.03,1.15,group_name="DryRubbleCorners")
    detail("slate_rubble",x-.5,z+.7,h+.03,1.35,group_name="DryRubbleCorners")
    detail("dry_dirt_patch",x+.3,z+.3,h+.04,1.1,rng.uniform(0,360),"GroundVariation")
for x,z,h in [(-29,26,low),(-28,-20,low),(29,18,0),(28,-7,0),(-6,30,high),(21,29,high)]:
    detail("iron_ore",x,z,h+.03,1.2,group_name="MineralSeams")
    detail("crystal_rock",x+.8,z+.5,h+.03,.9,group_name="MineralSeams")

for i in range(48):
    x,z=rng.uniform(-4,27),rng.uniform(-28,13)
    if on_corridor(x,z,.2) or any(abs(x-zx)<w+.7 and abs(z-zz)<d+.7 for _,_,zx,zz,_,w,d,_ in zones):
        continue
    detail("small_rocks",x,z,.03,rng.uniform(.55,.9),group_name="OpenTerracePebbles")

foreground=group("07_SouthEntranceCutaway")
foreground.parent=terrain
for i,(x,z) in enumerate([(-39,-36),(-21,-36),(-15,-35),(-5,-34.5),(10,-35),(31,-32)]):
    instance(modules[i%3],"ForegroundRock_%02d"%i,(x,z,-3.7),
             (2.4,2.0,4.1),rng.uniform(-20,20),foreground)

# Metadata is exported with the mesh, avoiding a second layout in the Unity importer.
metadata=dict(Zones=[dict(Id=n,Name=name,X=x,Z=z,Height=h) for n,name,x,z,h,*_ in zones],
              Decorations=decorations,LowlandHeight=low,HighlandHeight=high,WaterHeight=water_height,
              RouteSamples=route_samples,
              RiverCourse=[dict(X=river_center(z),Z=z,HalfWidth=river_half) for z in list(range(-15,34))+[33.4]],
              WestFork=[dict(X=center(west_fork,z),Z=z,HalfWidth=fork_half(z)) for z in range(-34,-14)],
              EastFork=[dict(X=center(east_fork,z),Z=z,HalfWidth=fork_half(z)) for z in range(-34,-14)],
              RiverStart=river_start,RiverEnd=river_end,RiverHalfWidth=river_half,
              BridgeZs=bridge_zs,
              RouteAnchors=[dict(Name=name,X=x,Z=z,Height=h) for name,x,z,h in [
                  ("O1_EntranceObservation",0,-18.47,0),("O2_SupplyBridgeObservation",-24,14.47,low),
                  ("O3_PerimeterObservation",24,2.6,0),("SharedEnd_HallToElder",10.5,26,high),
                  ("PrivateStoreInsideElder",20,28.3,high)]])
for field in ("Zones", "Decorations", "RouteSamples", "RouteAnchors", "RiverCourse", "WestFork", "EastFork"):
    for point in metadata[field]:
        point["X"] *= PLAN_SCALE
        point["Z"] *= PLAN_SCALE
        if "HalfWidth" in point:
            point["HalfWidth"] *= PLAN_SCALE
for field in ("RiverStart", "RiverEnd", "RiverHalfWidth"):
    metadata[field] *= PLAN_SCALE
metadata["BridgeZs"] = [z*PLAN_SCALE for z in bridge_zs]
metadata["PlanScale"] = PLAN_SCALE
metadata["Zones"] = [dict(item, Width=zones[i][5]*2*PLAN_SCALE, Depth=zones[i][6]*2*PLAN_SCALE)
                     for i,item in enumerate(metadata["Zones"])]
# Bake plan expansion into individual meshes and transforms, retaining editable unit-scale roots.
for obj in terrain.children_recursive:
    if obj.type == "MESH":
        obj.data=obj.data.copy()
        for v in obj.data.vertices:
            v.co.x *= PLAN_SCALE
            v.co.y *= PLAN_SCALE
    obj.location.x *= PLAN_SCALE
    obj.location.y *= PLAN_SCALE
(OUT/"cave_terraced_layout.json").write_text(json.dumps(metadata,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")


def select_tree(root):
    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for obj in root.children_recursive:
        obj.select_set(True)
    bpy.context.view_layer.objects.active=root


def export(root,file):
    select_tree(root)
    bpy.ops.export_scene.fbx(filepath=str(file),use_selection=True,object_types={"MESH","EMPTY"},
                             axis_forward="-Z",axis_up="Y",apply_unit_scale=True,bake_anim=False,
                             use_mesh_modifiers=True,add_leaf_bones=False,mesh_smooth_type="FACE",use_triangles=True)


export(terrain,OUT/"cave_terraced_landform.fbx")
export(library,OUT/"cave_rock_modules.fbx")
library.hide_viewport=True
library.hide_render=True
select_tree(terrain)
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE))
print("CAVE PLAN V2 EXPORTED:",len(terrain.children_recursive),"objects,",len(zones),"areas,",len(route_samples),"route samples")
