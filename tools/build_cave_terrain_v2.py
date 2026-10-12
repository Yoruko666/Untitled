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


import json

river_course=[(-12.4,-15),(-13.3,-10),(-14.3,-3),(-12.5,4),(-12.2,10),
              (-13.8,16),(-16.3,23),(-15.6,28),(-13,33.4)]
west_fork=[(-25,-34),(-24,-29),(-20.8,-25),(-16.7,-20),(-12.4,-15)]
east_fork=[(-9.6,-34),(-8.5,-29),(-9,-25),(-10.7,-20),(-12.4,-15)]
river_half=3.9  # Previous version's 5.2m channel, widened by 50 percent.


def center(course,z):
    for (xa,za),(xb,zb) in zip(course,course[1:]):
        if z<=zb:
            t=max(0,(z-za)/(zb-za))
            return xa+(xb-xa)*t*t*(3-2*t)
    return course[-1][0]


def river_center(z):
    return center(river_course,z)


def fork_half(z):
    return 2.6+(river_half-2.6)*max(0,min(1,(z+20)/5))


def shore(side,z):
    if z>=-15:
        return river_center(z)+side*river_half
    return center(west_fork if side<0 else east_fork,z)+side*fork_half(z)


terrain = group("CaveTerracedTerrain")
land = group("01_Landform_NorthGoal_SouthEntrance")
land.parent = terrain
main_outline = [(shore(1,z)+0.45,z) for z in (-34,-31,-27,-23,-19,-15,-11,-6,0,6,12,18,24,29)]
main_outline += [(-6,31),(2,32),(8,31),(15,33),(23,32),(28,29),
                 (31,24),(32,17),(30.2,10),(33,4),(30.5,-3),
                 (33,-10),(30,-17),(31,-25),(29,-30),(20,-32),
                 (10,-32),(2,-34)]
platform("MainSettlementGround_0m",main_outline,0,-4,"earth",land)
west_outline = [(-36,-38),(-31,-40),(shore(-1,-34)-0.7,-34)]
west_outline += [(shore(-1,z)-0.7,z) for z in (-30,-26,-22,-18,-15,-11,-7,-1,7,14,22,29)]
west_outline += [(-21,33),(-25,31),
                (-30,27),(-30.5,20),(-30,12),(-31,5),(-31,-4),(-31,-14),(-34,-24),(-36,-31)]
platform("RiverbankLowland_Minus1p2m",west_outline,-1.2,-4,"wet",land)
upper_outline = [(-7.8,21),(-3.25,21),(-3.25,21.15),(0.25,21.15),(0.25,21),
                 (1.65,21),(1.65,21.15),(4.35,21.15),(4.35,21),(5.0,21.4),
                 (5.1,26),(3,28.5),(-3,29),(-7.5,27.5),(-8,24)]
platform("09_AltarHallTerrace_Plus2p4m",upper_outline,2.4,-0.2,"earth",land)
elder_outline = [(9.5,22.1),(12,21.6),(18,21.8),(22.0,23.4),
                 (23,28),(20.5,31.0),(13,31.5),(9.8,28.5)]
platform("10_ElderAndPrivateStore_Plus2p4m",elder_outline,2.4,-0.2,"earth",land)

# Relative area relationships come from the plan; contours retain the prior cave's natural river and terraces.
# The 15 area names are terrain reservations, not implemented businesses.
zones = [
    (1,"Evacuation",0,-30,0,4.4,2.0,"earth"),
    (2,"EntranceForecourt",0,-22.6,0,4.2,3.0,"stone"),
    (3,"MilitaryTraining",19.2,10.8,0.45,4.4,3.8,"stone"),
    (4,"Residence",5.9,-15.5,0.30,3.8,2.6,"earth"),
    (5,"KitchenPreparation",17.7,-12.6,0.30,4.2,3.2,"stone"),
    (6,"Tavern",-6.3,-3.3,0.30,3.3,2.5,"earth"),
    (7,"Casino",11.8,-0.7,0.30,3.6,2.5,"stone"),
    (8,"Feast",2.3,10.4,0.30,4.5,3.1,"earth"),
    (9,"AltarHall",-1.5,23.4,2.4,5.0,3.4,"earth"),
    (10,"Elder",15.5,25.6,2.4,5.4,3.8,"earth"),
    (11,"Planting",-22.3,7.8,-1.2,3.5,4.0,"moss"),
    (12,"Warehouse",-5.1,10.4,0.30,2.4,3.2,"stone"),
    (13,"UnloadingCourt",19.2,-22.6,0,4.4,3.0,"stone"),
    (14,"PublicHearth",1.5,-2.9,0,3.0,2.5,"stone"),
    (15,"RiverWorkshop",-22.3,-10,-1.2,3.8,3.5,"wet"),
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
east_bank += [(shore(1,z)+1.1,z) for z in reversed(range(-34,34))]
platform("EastBankLedge_Minus1p2m",east_bank,-1.2,-3.8,"wet",river)
west_bank=[(shore(-1,z)-1.1,z) for z in range(-34,34)]
west_bank += [(shore(-1,z)+0.02,z) for z in reversed(range(-34,34))]
platform("WestBankLedge_Minus1p2m",west_bank,-1.2,-3.8,"wet",river)
def contour(course,start,end,side,width):
    steps=math.ceil(abs(end-start))
    return [(center(course,z)+side*width(z),z,0 if side<0 else 1)
            for z in [start+(end-start)*i/steps for i in range(steps+1)]]


# One concave water mesh gives a continuous Y confluence without overlapping water surfaces.
outline=contour(river_course,33.4,-15,-1,lambda z:river_half)
outline+=contour(west_fork,-15,-34,-1,fork_half)[1:]
outline += [(west_fork[0][0]+2.6*math.cos(a),-34+2.6*math.sin(a),(1+math.cos(a))/2)
            for a in [-math.pi+math.pi*i/12 for i in range(1,12)]]
outline+=contour(west_fork,-34,-20.8,1,fork_half)
outline+=contour(east_fork,-20.8,-34,-1,fork_half)
outline += [(east_fork[0][0]+2.6*math.cos(a),-34+2.6*math.sin(a),(1+math.cos(a))/2)
            for a in [-math.pi+math.pi*i/12 for i in range(1,12)]]
outline+=contour(east_fork,-34,-15,1,fork_half)
outline+=contour(river_course,-15,33.4,1,lambda z:river_half)[1:]
area=sum(outline[i][0]*outline[(i+1)%len(outline)][1]-outline[(i+1)%len(outline)][0]*outline[i][1]
         for i in range(len(outline)))
if area<0:
    outline.reverse()
verts=[(x,z,-1.75) for x,z,side in outline]
vectors=[Vector(v) for v in verts]
lookup={tuple(v):i for i,v in enumerate(vectors)}
faces=[tuple(v if isinstance(v,int) else lookup[tuple(v)] for v in tri) for tri in tessellate_polygon([vectors])]
water=mesh("RiverSurface",verts,faces,["water"],parent=river)
colors=water.data.color_attributes.new(name="Banks",type="BYTE_COLOR",domain="CORNER")
for poly in water.data.polygons:
    for loop in poly.loop_indices:
        vi=water.data.loops[loop].vertex_index
        water.data.uv_layers.active.data[loop].uv=(outline[vi][2],0.5)
        colors.data[loop].color=(1,1,0,0)

island_outline=[(center(west_fork,z)+fork_half(z)+0.15,z) for z in (-21,-24,-27,-30,-34)]
island_outline += [(-20.8,-35.4),(-18,-37.0),(-15.2,-37.0),(-12.8,-35.5)]
island_outline += [(center(east_fork,z)-fork_half(z)-0.15,z) for z in (-34,-30,-27,-24,-21)]
platform("SouthForkErodedIsland",island_outline,-1.2,-3.8,"moss",river)

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
    faces=[(2*i,2*i+1,2*i+3,2*i+2) for i in range(len(points)-1)]
    count=len(vertices)
    bottom=min(-0.2,min(p[2] for p in points)-0.35)
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


path("SouthernDryRim",[(0,-31,0.03),(0,-36,0.03),(-6,-38,0.03),(-14,-39,-1.17),
                       (-26,-39,-1.17),(-33,-37,-1.17),(-33,-32,-1.17)],7.0,"wet",check=True)
path("B_SouthSupplyEntrance",[(-33,-32,-1.17),(-31,-27,-1.17),(-27,-21,-1.17),
                             (-23.5,-17,-1.17),(-22.3,-10,-1.17)],3.2,"wet",check=True)
# The west-side route passes through the workshop and planting reservations.
path("B_WestBankSupplyRoute",[(-22.3,-10,-1.17),
                             (-22.3,-1,-1.17),(-22.3,7.8,-1.17),(-22.3,14.47,-1.17)],3.0,"wet",check=True)

bridge_zs=[-10,14.47]
for name,z in [("EntranceBridge",-10),("SupplyBridge",14.47)]:
    x=river_center(z)
    half=river_half+0.75
    path(name,[(x-half,z,0.03),(x,z,0.03),(x+half,z,0.03)],2.8,"stone",check=True)
    path(name+"WestRamp",[(-22.3,z,-1.17),(x-half,z,0.03)],2.8,"wet",check=True)
    path(name+"EastLanding",[(x+half,z,0.03),(-7.7,z,0.03)],2.8,"earth",check=True)
    for edge in (-1,1):
        for j in range(11):
            instance(modules[j%3],name+"_EdgeStone_%d_%d"%(j,edge),
                     (x-half+j*half*2/10,z+edge*1.47,-0.08),(0.39,0.3,0.30),j*37,connections)
path("B_WestHallAscent",[(-7.7,14.47,0.03),(-9.4,18.0,0.85),
                         (-9.4,23.4,2.43),(-6.7,23.4,2.43)],2.4,check=True)

# A stays in the center; C follows the far eastern boundary before turning to the hall.
path("A_CentralPublicApproach",[(0,-30,0.035),(0,-22.6,0.035),(0,-18.47,0.035),
                               (1.5,-2.9,0.035),(2.3,10.4,0.035),(0.0,15.3,0.035),
                               (-1.5,17.1,0.035)],3.4,check=False)
# Keep baseline paths as optional review overlays; their corridors remain clear on the base ground.
path("C_EasternPerimeterRoute",[(0,-30,0.035),(26.1,-30,0.035),(26.1,-22.6,0.035),
                               (26.1,-12.6,0.035),(24.0,-8,0.035),(24.0,2.6,0.035),
                               (26.0,10.8,0.035),(26.0,19.0,0.035),(14.0,19.0,0.035),
                               (7.5,17.5,0.035),(3.0,16.4,0.035),(3.0,17.1,0.035)],3.0)

for name,x,width in [("A_CentralHallStair",-1.5,3.4),("C_EastHallStair",3.0,2.6)]:
    for i in range(16):
        z=17.1+i*0.25
        h=(i+1)*0.15
        platform(name+"_%02d"%(i+1),[(x-width/2,z),(x+width/2,z),
                                     (x+width/2,z+0.31),(x-width/2,z+0.31)],
                 h,-0.03,"stone",connections)
        route_samples.append(dict(X=x,Z=z+0.18,Height=h,Name=name))
path("D_SharedHallToElderNeck",[(3.8,24.0,2.43),(7.0,24.0,2.43),
                               (10.8,24.0,2.43),(13.5,25.6,2.43)],2.2,check=True)

# Each low shelf has an explicit shallow ramp, keeping the gray cross-connections usable.
for number in (3,4,5,6,7,8,12):
    _,name,x,z,h,w,d,_=zones[number-1]
    if number==8:
        # A public road traverses the feast's western edge, with a rise and fall.
        path("A_FeastSouthRamp",[(0,5.5,0.03),(0,7.35,0.33),(0,8.2,0.33)],2.8,parent=zone_objects[number],check=True)
        path("A_FeastNorthRamp",[(0,12.0,0.33),(0,13.7,0.33),(0,15.5,0.03)],2.8,parent=zone_objects[number],check=True)
    elif number==12:
        # Adjacent warehouse and feast reservations share the same shallow shelf height.
        path("12_WarehouseFeastSaddle",[(-0.8,z,0.33),(-3.5,z,0.33)],2.4,
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
    ("A_Central",[(0,-30,0),(0,-22.6,0),(0,-18.47,0),(1.5,-2.9,0),
                   (2.3,4.5,0),(0,5.8,0),(0,8.2,0.3),(0,12,0.3),(0,15,0),(-1.5,17.0,0)],3.5),
    ("C_East",[(0,-30,0),(26.1,-30,0),(26.1,-22.6,0),(26.1,-12.6,0),
                (24,-8,0),(24,2.6,0),(26,10.8,0),(26,19,0),(14,19,0),
                (7.5,17.5,0),(3,16.4,0),(3,17.0,0)],3.1),
    ("HearthCasinoLink",[(-6.3,-3.3,0.3),(1.5,-2.9,0),(11.8,-0.7,0.3),(24,2.6,0)],2.5),
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
    if -20<x<-7 and any(abs(z-b)<2.3 for b in bridge_zs):
        return True
    if -11<x<-6 and 13<z<25:
        return True
    if -4<x<5 and 16<z<23:
        return True
    return False


walls=group("04_OuterCaveWallsAndInteriorSpurs")
walls.parent=terrain
perimeter=[(-41,-34),(-39,-26),(-37,-19),(-35,-12),(-35,-4),(-34,5),(-33,14),(-32,23),
           (-29,30),(-23,35),(-8,35),(-1,36),(7,36),(15,37),(23,36),(29,33),
           (34,27),(35,20),(34,13),(36,5),(34,-3),(37,-11),(34,-19),(35,-26)]
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
            if on_corridor(x,z) or (5<x<11 and 22.5<z<25.5):
                continue
            size=rng.uniform(0.6,1.0)
            instance(modules[i%3],"HighCliffFoot_%03d"%len(dressing.children),
                     (x,z,h),(size,size*0.8,size*1.1),rng.uniform(0,360),dressing)
for i in range(118):
    z=rng.uniform(-33,32)
    if any(abs(z-b)<2.0 for b in bridge_zs):
        continue
    side=rng.choice((-1,1))
    x=shore(side,z)+side*rng.uniform(0.05,0.35)
    instance(modules[i%3],"WaterlineStone_%03d"%i,(x,z,-1.4),
             (rng.uniform(0.25,0.5),rng.uniform(0.3,0.6),rng.uniform(0.2,0.5)),rng.uniform(0,360),river)
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
for i,(x,z,h) in enumerate([(-26,25,-1.2),(-26,-19,-1.2),(-26,1,-1.2),
                          (28,-5,0),(28,16,0),(-6,27,2.4),(20,29,2.4)]):
    for j in range(3):
        instance(stalagmite,"MineralCluster_%d_%d"%(i,j),
                 (x+j*0.45,z+j*0.3,h),(0.35+j*0.12,0.4,0.8+j*0.55),j*55,dressing)

foreground=group("07_SouthEntranceCutaway")
foreground.parent=terrain
for i,(x,z) in enumerate([(-39,-36),(-21,-36),(-15,-35),(-5,-34.5),(10,-35),(31,-32)]):
    instance(modules[i%3],"ForegroundRock_%02d"%i,(x,z,-3.7),
             (2.4,2.0,4.1),rng.uniform(-20,20),foreground)

# Metadata is exported with the mesh, avoiding a second layout in the Unity importer.
metadata=dict(Zones=[dict(Id=n,Name=name,X=x,Z=z,Height=h) for n,name,x,z,h,*_ in zones],
              RouteSamples=route_samples,
              RiverCourse=[dict(X=river_center(z),Z=z,HalfWidth=river_half) for z in list(range(-15,34))+[33.4]],
              WestFork=[dict(X=center(west_fork,z),Z=z,HalfWidth=fork_half(z)) for z in range(-34,-14)],
              EastFork=[dict(X=center(east_fork,z),Z=z,HalfWidth=fork_half(z)) for z in range(-34,-14)],
              RiverStart=river_start,RiverEnd=river_end,RiverHalfWidth=river_half,
              BridgeZs=bridge_zs,
              RouteAnchors=[dict(Name=name,X=x,Z=z,Height=h) for name,x,z,h in [
                  ("O1_EntranceObservation",0,-18.47,0),("O2_SupplyBridgeObservation",-22.3,14.47,-1.2),
                  ("O3_PerimeterObservation",24,2.6,0),("SharedEnd_HallToElder",7,24,2.4),
                  ("PrivateStoreInsideElder",18,27.7,2.4)]])
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
