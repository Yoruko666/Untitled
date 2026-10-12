"""Make this cave detail kit with Blender's existing PNG reader (no image edits).

blender --background --python tools/build_cave_medium_assets.py
Add -- --preview for the editable .blend and two material previews.
The previous kit and map scenes are not regenerated.
"""
import math
import re
import struct
import sys
from pathlib import Path

import bpy
from mathutils import Vector
sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_cave_priority_modules as kit

ROOT = kit.ROOT
kit.MODEL = ROOT / "Assets/Art/Models/CaveDetails"
kit.PREFAB = ROOT / "Assets/Prefabs/Terrain/CaveDetails"
MATERIAL = ROOT / "Assets/Art/Materials/CaveDetails"
SPRITES = ROOT / "Assets/Art/Sprites/Terrain/CaveBasics"
SOIL = kit.guid(ROOT / "Assets/Art/Materials/CaveHD2D/cave_module_soil.mat")


def stone(name, radius, height, seed=1, center=(0, 0), slender=False):
    result = kit.Shape(name)
    n = 8
    rings = []
    levels = [(0, 1), (.18, 1.08), (.5, .86), (.82, .66), (1, .22 if slender else .55)]
    for row, (fraction, scale) in enumerate(levels):
        ring = []
        for i in range(n):
            angle = 2*math.pi*i/n
            jag = 1 + .12*math.sin(i*2.4+seed+row*.6)
            x = center[0]+radius[0]*scale*jag*math.cos(angle)+fraction*.12*math.sin(seed)
            z = center[1]+radius[1]*scale*jag*math.sin(angle)+fraction*.15*math.cos(seed)
            y = height*fraction + (height*.035*math.sin(i*1.7+seed) if row == 4 else 0)
            ring.append((x, y, z))
        rings.append(ring)
    for row in range(len(rings)-1):
        for i in range(n):
            j = (i+1) % n
            outward = (rings[row][i][0]+rings[row][j][0]-2*center[0], 0, rings[row][i][2]+rings[row][j][2]-2*center[1])
            result.face([rings[row][i], rings[row][j], rings[row+1][j], rings[row+1][i]], outward)
    result.face(rings[0], (0, -1, 0))
    # Top is not planar: use a common central point, keeping each edge paired.
    middle = tuple(sum(p[i] for p in rings[-1])/n for i in range(3))
    for i in range(n):
        result.face([middle, rings[-1][i], rings[-1][(i+1)%n]], (0, 1, 0))
    return result


def cluster(name, parts):
    result = kit.Shape(name)
    for i, (x, z, radius, height) in enumerate(parts):
        result.triangles.extend(stone(name, (radius, radius*.8), height, i+8, (x, z), slender=True).triangles)
    return result


def solids():
    lip = kit.wall("cave_cliff_top_lip_01", [(-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0)], height=.6, soil=True, variation=.1)
    lip.triangles = [([(x, y-.6, z) for x, y, z in tri], mat) for tri, mat in lip.triangles]
    gravel = kit.wall("cave_river_gravel_edge_01", [(-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0)], height=.25, soil=True, variation=.06)
    return [
        stone("cave_rock_pillar_01", (1.4, 1.2), 6, 2),
        stone("cave_rock_pillar_02", (1.8, 1.4), 8, 5),
        stone("cave_large_boulder_01", (2.1, 1.7), 2.6, 3),
        stone("cave_large_boulder_02", (2.7, 1.6), 3.7, 4),
        stone("cave_large_boulder_03", (1.6, 2.2), 3.1, 7),
        stone("cave_rock_slab_01", (3, 1.8), 1.2, 6),
        cluster("cave_stalagmite_cluster_01", [(-1.1, 0, .5, 1.8), (0, .1, .65, 3.5), (1.2, -.2, .45, 2.3)]),
        cluster("cave_stalagmite_cluster_02", [(-1.4, 0, .55, 2.5), (0, .3, .85, 5.2), (1.5, -.1, .5, 3.4), (.5, -1.3, .4, 1.7)]),
        cluster("cave_cliff_foot_rubble_01", [(-1.6, 0, .35, .4), (-.7, .1, .5, .65), (.45, -.15, .55, .55), (1.6, .1, .35, .35)]),
        lip, gravel,
    ]


SHEETS = [
    ("cave_grass_ferns_sheet", ["grass_tuft", "fern_clump", "river_reeds", "dry_grass", "leaf_clump", "moss_weeds"], [1, 1.4, 2.2, 1.1, 1, .9], False),
    ("cave_mushrooms_sheet", ["mushroom_ochre", "mushroom_brown", "fungus_slate", "fungus_bracket", "mushroom_teal", "mushroom_cream"], [.9, 1.1, 1.1, .9, 1, .8], False),
    ("cave_roots_ore_sheet", ["roots_tangled", "roots_hanging", "rooted_stone", "iron_ore", "crystal_rock", "slate_rubble"], [2, 2.8, 1.3, 1.1, 1.2, .7], False),
    ("cave_ground_patches_sheet", ["moss_patch", "moss_strip", "wet_mud_patch", "gravel_patch", "dry_dirt_patch", "stepping_stones"], [2.4, 3.4, 2.5, 2.2, 2.4, 2.5], True),
    ("cave_rocks_stalagmites_sheet", ["small_rocks", "fractured_rock", "river_stones", "short_stalagmite", "stalagmite_sprites", "mossy_boulder"], [.8, 1.6, .8, 1.5, 2.5, 1.8], False),
]


def material(name, sheet, frame):
    source = (ROOT / "Assets/Art/Materials/Props/TotemsBones/bone_pile_hd2d.mat").read_text(encoding="utf-8")
    texture = re.search(r"guid: (\w+)", Path(str(sheet)+".meta").read_text()).group(1)
    source = re.sub(r"  m_Name: .*", f"  m_Name: {name}", source)
    source = re.sub(r"m_Texture: \{fileID: 2800000, guid: \w+, type: 3\}", f"m_Texture: {{fileID: 2800000, guid: {texture}, type: 3}}", source)
    source = re.sub(r"    - _FrameRect: .*", "    - _FrameRect: {r: %.9g, g: %.9g, b: %.9g, a: %.9g}" % frame, source)
    source = re.sub(r"    - _Emission: .*", "    - _Emission: 0", source)
    path = MATERIAL / (name+".mat")
    kit.write(path, source)
    return kit.meta(path, "NativeFormatImporter", 2100000)


def decoration(sheet, name, index, size, flat):
    image = bpy.data.images.load(str(sheet), check_existing=True)
    width, height = image.size
    # Blender image pixels are bottom-up, matching Unity's UV coordinates.
    pixels = list(image.pixels)
    col, top_row = index % 3, index // 3
    boundaries = [0]
    for fraction in (1/3, 2/3):
        empty_columns = [x for x in range(round(width*(fraction-.08)), round(width*(fraction+.08)))
                         if not any(pixels[(y*width+x)*4+3] >= .45 for y in range(height))]
        assert empty_columns, (name, "No transparent gap between columns")
        boundaries.append(min(empty_columns, key=lambda x: abs(x-width*fraction)))
    boundaries.append(width)
    x0, x1 = boundaries[col:col+2]
    # Some generated objects touch the nominal half-height line. Split at the
    # closest transparent row in each column, rather than clipping the baseline.
    empty_rows = [y for y in range(round(height*.35), round(height*.65))
                  if not any(pixels[(y*width+x)*4+3] >= .45 for x in range(x0, x1))]
    assert empty_rows, (name, "No transparent gap between rows")
    split = min(empty_rows, key=lambda y: abs(y-height/2))
    y0, y1 = (split, height) if top_row == 0 else (0, split)
    samples = [(x, y) for y in range(y0, y1) for x in range(x0, x1) if pixels[(y*width+x)*4+3] >= .45]
    assert samples, (sheet, index, "Empty alpha cell")
    left, right = max(x0, min(x for x,y in samples)-2), min(x1, max(x for x,y in samples)+3)
    bottom, top = max(y0, min(y for x,y in samples)-2), min(y1, max(y for x,y in samples)+3)
    # The chosen separators are transparent. A visible pixel immediately next
    # to a separator is retained; it does not require an extra empty pixel.
    frame = (left/width, bottom/height, (right-left)/width, (top-bottom)/height)
    assert frame[0] >= 0 and frame[1] >= 0 and frame[0]+frame[2] <= 1 and frame[1]+frame[3] <= 1
    hanging = name in ("roots_tangled", "roots_hanging")
    w = size if flat else size*(right-left)/(top-bottom)
    h = size*(top-bottom)/(right-left) if flat else size
    mesh = kit.Shape("cave_"+name+"_01")
    if flat:
        points = [(-w/2, .025, -h/2), (-w/2, .025, h/2), (w/2, .025, h/2), (w/2, .025, -h/2)]
        normal = (0, 1, 0)
    else:
        low, high = (-h, 0) if hanging else (0, h)
        points = [(-w/2, low, 0), (-w/2, high, 0), (w/2, high, 0), (w/2, low, 0)]
        normal = (0, 0, -1)
    mesh.face(points, normal)
    # Serialization is shared with the previous kit, but these are open visual planes.
    mesh_guid = kit.mesh_asset(mesh)
    path = kit.MODEL / (mesh.name+".asset")
    text = path.read_text()
    data = bytearray.fromhex(re.search(r"_typelessdata: (\w+)", text).group(1))
    for i, p in enumerate(p for tri, _ in mesh.triangles for p in tri):
        u = p[0]/w+.5
        v = (p[2]/h+.5) if flat else (p[1]/h+int(hanging))
        struct.pack_into("<2f", data, i*48+40, u, v)
    text = re.sub(r"_typelessdata: \w+", "_typelessdata: "+data.hex(), text)
    kit.write(path, text)
    mat_guid = material(mesh.name, sheet, frame)
    prefab_guid = kit.prefab_asset(mesh, mesh_guid, SOIL)
    path = kit.PREFAB / (mesh.name+".prefab")
    text = path.read_text().replace(kit.ROCK, mat_guid)
    text = re.sub(r"  - component: \{fileID: 6500001\}\n", "", text)
    text = re.sub(r"--- !u!64 &6500001\n.*", "", text, flags=re.S)
    if flat:
        text = text.replace("m_CastShadows: 1", "m_CastShadows: 0")
    kit.write(path, text)
    return dict(name=mesh.name, mesh=mesh, prefab=prefab_guid, sheet=sheet, frame=frame, flat=flat, hanging=hanging)


def gallery(items):
    # Reuse only the existing gallery camera/light/settings, never its instances.
    source = (ROOT / "Assets/Scenes/Props/CaveModuleGallery.unity").read_text(encoding="utf-8")
    source = source.split("--- !u!1001")[0]
    source = re.sub(r"  m_LocalPosition: \{x: 14, y: 29, z: -30\}", "  m_LocalPosition: {x: 15, y: 35, z: -18}", source)
    source = source.replace("orthographic size: 24", "orthographic size: 29")
    for i, item in enumerate(items):
        x, y, z = (i % 6)*6, 3 if item.get("hanging") else 0, (i//6)*6
        fields = ""
        for axis, value in zip("xyz", (x,y,z)):
            fields += f"    - target: {{fileID: {kit.ROOT_TRANSFORM}, guid: {item['prefab']}, type: 3}}\n      propertyPath: m_LocalPosition.{axis}\n      value: {value}\n      objectReference: {{fileID: 0}}\n"
        source += f"--- !u!1001 &{900000000+i}\nPrefabInstance:\n  m_ObjectHideFlags: 0\n  serializedVersion: 2\n  m_Modification:\n    serializedVersion: 3\n    m_TransformParent: {{fileID: 0}}\n{fields}    m_RemovedComponents: []\n    m_RemovedGameObjects: []\n    m_AddedGameObjects: []\n    m_AddedComponents: []\n  m_SourcePrefab: {{fileID: 100100000, guid: {item['prefab']}, type: 3}}\n"
    path = ROOT / "Assets/Scenes/Props/CaveDetailGallery.unity"
    kit.write(path, source)
    kit.meta(path, "DefaultImporter")


def build():
    for folder in (kit.MODEL, kit.PREFAB, MATERIAL): kit.folder(folder)
    gravel_path = MATERIAL / "cave_detail_gravel.mat"
    source = (ROOT / "Assets/Art/Materials/CaveHD2D/cave_module_soil.mat").read_text()
    texture = re.search(r"guid: (\w+)", (ROOT / "Assets/Art/Textures/TerrainBasic/cave_river_gravel.png.meta").read_text()).group(1)
    source = source.replace("m_Name: cave_module_soil", "m_Name: cave_detail_gravel")
    source = re.sub(r"m_Texture: \{fileID: 2800000, guid: \w+, type: 3\}", f"m_Texture: {{fileID: 2800000, guid: {texture}, type: 3}}", source)
    kit.write(gravel_path, source)
    gravel = kit.meta(gravel_path, "NativeFormatImporter", 2100000)
    items = []
    for shape in solids():
        shape.check()
        mat = gravel if shape.name == "cave_river_gravel_edge_01" else SOIL
        items.append(dict(name=shape.name, mesh=shape, prefab=kit.prefab_asset(shape, kit.mesh_asset(shape), mat)))
    for sheet, names, sizes, flat in SHEETS:
        for i, (name, size) in enumerate(zip(names, sizes)):
            items.append(decoration(SPRITES / (sheet+".png"), name, i, size, flat))
    gallery(items)
    print(f"Exported {len(items)} prefabs: 11 closed collision meshes, 30 nonblocking atlas details")
    return items


def shader_material(name, color):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.node_tree.nodes.clear()
    shader = mat.node_tree.nodes.new("ShaderNodeBsdfPrincipled")
    shader.inputs["Base Color"].default_value = (*color, 1)
    shader.inputs["Roughness"].default_value = 1
    output = mat.node_tree.nodes.new("ShaderNodeOutputMaterial")
    mat.node_tree.links.new(shader.outputs[0], output.inputs["Surface"])
    return mat, shader


def preview(items):
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    world_materials = []
    for name in ("cave_rock_face", "cave_earth_wet", "cave_river_gravel"):
        mat, shader = shader_material(name, (.65, .61, .53))
        nodes, links = mat.node_tree.nodes, mat.node_tree.links
        coord = nodes.new("ShaderNodeTexCoord")
        scale = nodes.new("ShaderNodeVectorMath"); scale.operation = "SCALE"; scale.inputs[3].default_value = .25
        links.new(coord.outputs["Object"], scale.inputs[0])
        tex = nodes.new("ShaderNodeTexImage")
        tex.image = bpy.data.images.load(str(ROOT / f"Assets/Art/Textures/TerrainBasic/{name}.png"), check_existing=True)
        tex.interpolation = "Closest"; tex.projection = "BOX"
        links.new(scale.outputs[0], tex.inputs[0]); links.new(tex.outputs[0], shader.inputs["Base Color"])
        world_materials.append(mat)
    groups = []
    for i, item in enumerate(items):
        mesh = item["mesh"]
        vertices = [(x, -z, y) for tri, _ in mesh.triangles for x,y,z in tri]
        data = bpy.data.meshes.new(item["name"])
        data.from_pydata(vertices, [], [(j,j+1,j+2) for j in range(0,len(vertices),3)])
        obj = bpy.data.objects.new(item["name"],data); bpy.context.collection.objects.link(obj)
        if "sheet" in item:
            index = i-11
            obj.location = ((index%6)*4, -(index//6)*5, 3 if item["hanging"] else 0)
            mat, shader = shader_material(item["name"], (.4,.4,.4))
            nodes, links = mat.node_tree.nodes, mat.node_tree.links
            tex = nodes.new("ShaderNodeTexImage"); tex.image = bpy.data.images.load(str(item["sheet"]), check_existing=True); tex.interpolation = "Closest"
            links.new(tex.outputs[0],shader.inputs["Base Color"])
            output = next(n for n in nodes if n.type == "OUTPUT_MATERIAL")
            transparent = nodes.new("ShaderNodeBsdfTransparent")
            mix = nodes.new("ShaderNodeMixShader")
            greater = nodes.new("ShaderNodeMath"); greater.operation="GREATER_THAN"; greater.inputs[1].default_value=.45
            links.new(tex.outputs["Alpha"],greater.inputs[0]); links.new(greater.outputs[0],mix.inputs[0]); links.new(transparent.outputs[0],mix.inputs[1]); links.new(shader.outputs[0],mix.inputs[2]); links.new(mix.outputs[0],output.inputs["Surface"])
            data.materials.append(mat)
            raw = (kit.MODEL/(item["name"]+".asset")).read_text()
            buf = bytes.fromhex(re.search(r"_typelessdata: (\w+)",raw)[1])
            uv = data.uv_layers.new(name="atlas")
            fx,fy,fw,fh = item["frame"]
            for loop in data.loops:
                u,v = struct.unpack_from("<2f",buf,loop.vertex_index*48+40)
                uv.data[loop.index].uv = (fx+u*fw,fy+v*fh)
            groups.append((obj,1))
        else:
            obj.location = ((i%4)*9,-(i//4)*9,.6 if item["name"] == "cave_cliff_top_lip_01" else 0)
            data.materials.append(world_materials[0])
            data.materials.append(world_materials[2 if item["name"]=="cave_river_gravel_edge_01" else 1])
            for polygon,(_,mat) in zip(data.polygons,mesh.triangles): polygon.material_index=mat
            groups.append((obj,0))
    floor_mat,_=shader_material("preview_floor",(.18,.2,.19))
    bpy.ops.mesh.primitive_plane_add(size=150, location=(12,-10,-.01)); bpy.context.object.data.materials.append(floor_mat)
    bpy.ops.object.light_add(type="AREA",location=(0,-15,25)); bpy.context.object.data.energy=6000; bpy.context.object.data.size=25
    bpy.ops.object.camera_add(location=(38,-45,32)); camera=bpy.context.object; camera.data.type="ORTHO"
    scene=bpy.context.scene; scene.camera=camera; scene.world.color=(.3,.3,.3); scene.render.engine="CYCLES"; scene.cycles.samples=24
    scene.view_settings.view_transform="Standard"; scene.render.resolution_x=1600; scene.render.resolution_y=1200; scene.render.resolution_percentage=100
    for group, name, target, scale in [(0,"cave-detail-solids",(13,-8,2),45),(1,"cave-detail-atlas",(10,-10,1),32)]:
        for obj, kind in groups: obj.hide_render=kind!=group
        camera.rotation_euler=(Vector(target)-camera.location).to_track_quat("-Z","Y").to_euler(); camera.data.ortho_scale=scale
        scene.render.filepath=str(ROOT / f"docs/images/{name}.png")
        bpy.ops.render.render(write_still=True)
    for obj,_ in groups: obj.hide_render=False
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / "tools/art/cave_medium_assets.blend"))


if __name__ == "__main__":
    result=build()
    if "--preview" in sys.argv: preview(result)
