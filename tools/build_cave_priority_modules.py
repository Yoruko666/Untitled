"""Author this cave kit: python tools/build_cave_priority_modules.py.

Optional editable Blender source and preview:
blender --background --python tools/build_cave_priority_modules.py -- --preview
Only this kit's assets are written. Scene placement is never rebuilt at runtime.
"""
import collections
import math
import re
import struct
import sys
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MODEL = ROOT / "Assets/Art/Models/CaveModules"
PREFAB = ROOT / "Assets/Prefabs/Terrain/CaveModules"
ROCK = uuid.uuid5(uuid.NAMESPACE_URL, "untitled/cave-kit/Assets/Art/Materials/CaveHD2D/cave_module_rock.mat").hex
ROOT_TRANSFORM = 1908250060230632613


def write(path, content):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")


def guid(path):
    return uuid.uuid5(uuid.NAMESPACE_URL, "untitled/cave-kit/" + str(path.relative_to(ROOT)).replace("\\", "/")).hex


def meta(path, importer, file_id=None):
    target = Path(str(path) + ".meta")
    if target.exists():
        return re.search(r"guid: (\w+)", target.read_text()).group(1)
    extra = f"  mainObjectFileID: {file_id}\n" if file_id else ""
    write(target, f"fileFormatVersion: 2\nguid: {guid(path)}\n{importer}:\n  externalObjects: {{}}\n{extra}  userData:\n  assetBundleName:\n  assetBundleVariant:\n")
    return guid(path)


def folder(path):
    path.mkdir(parents=True, exist_ok=True)
    target = Path(str(path) + ".meta")
    if not target.exists():
        write(target, f"fileFormatVersion: 2\nguid: {guid(path)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n")


def sub(a, b):
    return tuple(x - y for x, y in zip(a, b))


def cross(a, b):
    return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])


def dot(a, b):
    return sum(x*y for x, y in zip(a, b))


def unit(v):
    length = math.sqrt(dot(v, v))
    if length < 1e-8:
        raise ValueError("Degenerate face")
    return tuple(x / length for x in v)


class Shape:
    def __init__(self, name):
        self.name = name
        self.triangles = []

    def face(self, vertices, outward, material=0):
        for i in range(1, len(vertices)-1):
            triangle = [vertices[0], vertices[i], vertices[i+1]]
            normal = cross(sub(triangle[1], triangle[0]), sub(triangle[2], triangle[0]))
            if dot(normal, outward) < 0:
                triangle.reverse()
            self.triangles.append((triangle, material))

    def check(self):
        # Weld only for the topology check; render vertices retain flat normals.
        edges = collections.Counter()
        directed = collections.Counter()
        volume = 0
        for points, _ in self.triangles:
            normal = cross(sub(points[1], points[0]), sub(points[2], points[0]))
            assert dot(normal, normal) > 1e-16
            volume += dot(points[0], cross(points[1], points[2])) / 6
            keys = [tuple(round(v, 6) for v in p) for p in points]
            for a, b in zip(keys, keys[1:] + keys[:1]):
                edges[tuple(sorted((a, b)))] += 1
                directed[(a, b)] += 1
        assert all(count == 2 for count in edges.values()), (self.name, "Non-manifold edge")
        assert all(directed[(b, a)] == count for (a, b), count in directed.items()), (self.name, "Inconsistent winding")
        assert volume > 0, (self.name, "Inverted volume")


def wall(name, path, height=4, soil=False, variation=0):
    shape = Shape(name)
    levels = [0, 1.25/4*height, 2.6/4*height, height]
    rings = []
    normals = []
    for i, (x, z) in enumerate(path):
        before = unit((path[i][0]-path[max(0, i-1)][0], 0, path[i][1]-path[max(0, i-1)][1])) if i else unit((path[1][0]-x, 0, path[1][1]-z))
        after = unit((path[min(i+1, len(path)-1)][0]-x, 0, path[min(i+1, len(path)-1)][1]-z)) if i < len(path)-1 else before
        tangent = unit(tuple(a+b for a, b in zip(before, after)))
        normal = (-tangent[2], 0, tangent[0])
        miter = 0.75 / dot(normal, (-before[2], 0, before[0]))
        normals.append(normal)
        sides = []
        for sign in (-1, 1):
            side = []
            for j, y in enumerate(levels):
                interior = 0 < i < len(path)-1
                bump = variation * math.sin(i*2.3+j*1.7+sign) if interior and 0 < j < 3 else 0
                top = y + (variation*0.45*math.sin(i*2.2) if interior and j == 3 and not soil else 0)
                side.append((x + normal[0]*sign*(miter+bump), top, z + normal[2]*sign*(miter+bump)))
            sides.append(side)
        rings.append(sides)
    for i in range(len(rings)-1):
        a, b = rings[i], rings[i+1]
        for side, sign in enumerate((-1, 1)):
            outward = tuple(sign*(normals[i][j]+normals[i+1][j]) for j in range(3))
            for level in range(3):
                shape.face([a[side][level], b[side][level], b[side][level+1], a[side][level+1]], outward)
        shape.face([a[0][3], b[0][3], b[1][3], a[1][3]], (0, 1, 0), int(soil))
        shape.face([a[0][0], b[0][0], b[1][0], a[1][0]], (0, -1, 0))
    for index, tangent in [(0, sub((path[0][0], 0, path[0][1]), (path[1][0], 0, path[1][1]))), (-1, sub((path[-1][0], 0, path[-1][1]), (path[-2][0], 0, path[-2][1])))]:
        ring = rings[index]
        for j in range(3):
            shape.face([ring[0][j], ring[1][j], ring[1][j+1], ring[0][j+1]], tangent)
    return shape


def endcap():
    shape = Shape("cave_wall_end_01")
    # A straight-wall interface at X=0, followed by a chamfered terminal.
    outline = [(0, -.75), (1.1, -.75), (1.7, -.4), (1.9, 0), (1.7, .4), (1.1, .75), (0, .75)]
    n = len(outline)
    rings = [[(x, y, z) for x, z in outline] for y in [0, 1.25, 2.6, 4]]
    for band in range(3):
        for i in range(n):
            j = (i+1) % n
            dx, dz = outline[j][0]-outline[i][0], outline[j][1]-outline[i][1]
            shape.face([rings[band][i], rings[band][j], rings[band+1][j], rings[band+1][i]], (dz, 0, -dx))
    shape.face(rings[0], (0, -1, 0))
    shape.face(rings[-1], (0, 1, 0))
    return shape


def arch(name, opening):
    shape = Shape(name)
    inner_half = opening / 2
    outer_half = inner_half + 2
    # Corresponding inner/outer bands keep the opening hollow, including Collider.
    outer = [(-outer_half, 0), (-outer_half, 1.25), (-outer_half, 2.6), (-outer_half, 4), (0, 4.18), (outer_half, 4), (outer_half, 2.6), (outer_half, 1.25), (outer_half, 0)]
    inner = [(-inner_half, 0), (-inner_half, 2.1), (-inner_half+.15, 2.65), (-inner_half+.65, 3.05), (0, 3.2), (inner_half-.65, 3.05), (inner_half-.15, 2.65), (inner_half, 2.1), (inner_half, 0)]
    for i in range(len(outer)-1):
        j = i+1
        for z, sign in [(-.75, -1), (.75, 1)]:
            shape.face([(outer[i][0], outer[i][1], z), (outer[j][0], outer[j][1], z), (inner[j][0], inner[j][1], z), (inner[i][0], inner[i][1], z)], (0, 0, sign))
        for curve, direction in [(outer, 1), (inner, -1)]:
            dx, dy = curve[j][0]-curve[i][0], curve[j][1]-curve[i][1]
            shape.face([(curve[i][0], curve[i][1], -.75), (curve[j][0], curve[j][1], -.75), (curve[j][0], curve[j][1], .75), (curve[i][0], curve[i][1], .75)], (-dy*direction, dx*direction, 0))
    for i in [0, len(outer)-1]:
        shape.face([(outer[i][0], 0, -.75), (inner[i][0], 0, -.75), (inner[i][0], 0, .75), (outer[i][0], 0, .75)], (0, -1, 0))
    return shape


def ramp(name, rise):
    shape = Shape(name)
    # Flat entry/exit landing; rise occurs only in the middle four units.
    strips = [(-3, 0), (-2, 0), (0, rise/2), (2, rise), (3, rise)]
    for i in range(len(strips)-1):
        z0, y0 = strips[i]
        z1, y1 = strips[i+1]
        shape.face([(-2, y0, z0), (2, y0, z0), (2, y1, z1), (-2, y1, z1)], (0, 1, -(y1-y0)/(z1-z0)), 1)
        shape.face([(-2, -.6, z0), (2, -.6, z0), (2, -.6, z1), (-2, -.6, z1)], (0, -1, 0))
        for x in [-2, 2]:
            shape.face([(x, -.6, z0), (x, y0, z0), (x, y1, z1), (x, -.6, z1)], (x, 0, 0))
    for z, y, outward in [(-3, 0, (0, 0, -1)), (3, rise, (0, 0, 1))]:
        shape.face([(-2, -.6, z), (2, -.6, z), (2, y, z), (-2, y, z)], outward)
    return shape


def landing():
    shape = Shape("cave_landing_01")
    lo, hi = -.6, 0
    shape.face([(-2, hi, -1), (2, hi, -1), (2, hi, 1), (-2, hi, 1)], (0, 1, 0), 1)
    shape.face([(-2, lo, -1), (2, lo, -1), (2, lo, 1), (-2, lo, 1)], (0, -1, 0))
    corners = [(-2, -1), (2, -1), (2, 1), (-2, 1)]
    for i, (x, z) in enumerate(corners):
        nx, nz = corners[(i+1) % 4]
        shape.face([(x, lo, z), (nx, lo, nz), (nx, hi, nz), (x, hi, z)], (nz-z, 0, x-nx))
    return shape


def shapes():
    return [
        wall("cave_wall_straight_02", [(-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0)], variation=.24),
        wall("cave_wall_corner_inner_01", [(-2, 0), (0, 0), (0, 2)], variation=.12),
        wall("cave_wall_corner_outer_01", [(-2, 0), (0, 0), (0, -2)], variation=.12),
        endcap(), arch("cave_entrance_wide_01", 4), arch("cave_passage_narrow_01", 2.4),
        wall("cave_ledge_straight_01", [(-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0)], height=1.2, soil=True, variation=.08),
        wall("cave_ledge_corner_01", [(-2, 0), (0, 0), (0, 2)], height=1.2, soil=True, variation=0),
        ramp("cave_ramp_low_01", 1.2), ramp("cave_ramp_high_01", 2.4), landing(),
    ]


def vector(v):
    return "{x: %.8g, y: %.8g, z: %.8g}" % tuple(v)


def mesh_asset(shape):
    template = (ROOT / "Assets/Art/Models/CaveHD2D/cliff_straight_01.asset").read_text(encoding="utf-8")
    ordered = sorted(shape.triangles, key=lambda item: item[1])
    points = [p for tri, _ in ordered for p in tri]
    center = [(min(p[i] for p in points)+max(p[i] for p in points))/2 for i in range(3)]
    extent = [(max(p[i] for p in points)-min(p[i] for p in points))/2 for i in range(3)]
    aabb = f"      m_Center: {vector(center)}\n      m_Extent: {vector(extent)}"
    groups = collections.Counter(material for _, material in ordered)
    first = 0
    entries = []
    for material in sorted(groups):
        count = groups[material]*3
        entries.append(f"  - serializedVersion: 2\n    firstByte: {first*2}\n    indexCount: {count}\n    topology: 0\n    baseVertex: 0\n    firstVertex: {first}\n    vertexCount: {count}\n    localAABB:\n{aabb}\n")
        first += count
    template = re.sub(r"  m_SubMeshes:.*?(?=  m_Shapes:)", "  m_SubMeshes:\n"+"".join(entries), template, flags=re.S)
    data = bytearray()
    for tri, material in ordered:
        normal = unit(cross(sub(tri[1], tri[0]), sub(tri[2], tri[0])))
        for p in tri:
            # Existing shader uses world projection; UV retained for other materials.
            uv = (p[0]/4, p[2]/4) if abs(normal[1]) > .5 else (p[0]/4, p[1]/4)
            data.extend(struct.pack("<12f", *p, *normal, float(material == 0), 1, 1, 1, *uv))
    count = len(points)
    template = re.sub(r"  m_Name: .*", "  m_Name: "+shape.name, template, count=1)
    template = re.sub(r"  m_IndexBuffer: .*", "  m_IndexBuffer: "+struct.pack("<"+"H"*count, *range(count)).hex(), template)
    template = re.sub(r"    m_VertexCount: \d+", f"    m_VertexCount: {count}", template)
    template = re.sub(r"    m_DataSize: \d+", f"    m_DataSize: {len(data)}", template)
    template = re.sub(r"    _typelessdata: .*", "    _typelessdata: "+data.hex(), template)
    template = re.sub(r"  m_LocalAABB:.*?(?=  m_MeshUsageFlags:)", f"  m_LocalAABB:\n    m_Center: {vector(center)}\n    m_Extent: {vector(extent)}\n", template, flags=re.S)
    path = MODEL / (shape.name+".asset")
    write(path, template)
    return meta(path, "NativeFormatImporter", 4300000)


def prefab_asset(shape, mesh_guid, soil_guid):
    template = (ROOT / "Assets/Prefabs/Terrain/cliff_straight_01.prefab").read_text(encoding="utf-8")
    template = template.replace("cliff_straight_01", shape.name).replace("7d1123a41b334199bfdd3eab91fa526e", mesh_guid)
    template = template.replace("32f431d560ef41139ab437a3106cb123", ROCK)
    if any(material == 1 for _, material in shape.triangles):
        template = template.replace(f"  - {{fileID: 2100000, guid: {ROCK}, type: 2}}", f"  - {{fileID: 2100000, guid: {ROCK}, type: 2}}\n  - {{fileID: 2100000, guid: {soil_guid}, type: 2}}")
    path = PREFAB / (shape.name+".prefab")
    write(path, template)
    return meta(path, "PrefabImporter")


def gallery(assets):
    source = (ROOT / "Assets/Scenes/Props/HybridPropGallery.unity").read_text(encoding="utf-8")
    blocks = re.findall(r"--- !u!\d+ &\d+[^\n]*\n.*?(?=--- !u!|\Z)", source, re.S)
    by_id = {re.search(r"&([0-9]+)", block).group(1): block for block in blocks}
    settings = [block for block in blocks if re.match(r"--- !u!(29|104|157|196) ", block)]
    output = "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n"+"".join(settings)
    # Copy only standalone Camera / Light and their owned components.
    for block in blocks:
        if not block.startswith("--- !u!1 "):
            continue
        ids = re.findall(r"component: \{fileID: (\d+)\}", block)
        camera = any(by_id[key].startswith("--- !u!20 ") for key in ids)
        light = any(by_id[key].startswith("--- !u!108 ") for key in ids)
        if not camera and not light:
            continue
        output += block
        for key in ids:
            component = by_id[key]
            if component.startswith("--- !u!4 "):
                if camera:
                    component = re.sub(r"  m_LocalPosition: .*", "  m_LocalPosition: {x: 14, y: 29, z: -30}", component)
                    angle = math.radians(38)/2
                    component = re.sub(r"  m_LocalRotation: .*", f"  m_LocalRotation: {{x: {math.sin(angle)}, y: 0, z: 0, w: {math.cos(angle)}}}", component)
                component = re.sub(r"  m_Children:.*?(?=  m_Father:)", "  m_Children: []\n", component, flags=re.S)
            if component.startswith("--- !u!20 "):
                component = re.sub(r"  orthographic: .*", "  orthographic: 1", component)
                component = re.sub(r"  orthographic size: .*", "  orthographic size: 24", component)
            output += component
    placements = [(asset_guid, ((i % 4)*10, 0, (i//4)*10), 0) for i, (_, asset_guid) in enumerate(assets)]
    kit = dict(assets)
    placements.extend([
        (kit["cave_wall_straight_02"], (0, 0, 36), 0),
        (kit["cave_wall_straight_02"], (4, 0, 36), 0),
        (kit["cave_wall_corner_inner_01"], (8, 0, 36), 0),
        (kit["cave_wall_straight_02"], (8, 0, 40), -90),
        (kit["cave_wall_end_01"], (-2, 0, 36), 180),
        (kit["cave_ramp_low_01"], (18, 0, 36), 0),
        (kit["cave_landing_01"], (18, 0, 32), 0),
        (kit["cave_landing_01"], (18, 1.2, 40), 0),
        (kit["cave_ledge_straight_01"], (18, 0, 41.75), 0),
    ])
    for i, (asset_guid, position, yaw) in enumerate(placements):
        instance = 800000000+i
        modifications = ""
        angle = math.radians(yaw)/2
        for property_name, values in [("m_LocalPosition", position), ("m_LocalRotation", (0, math.sin(angle), 0, math.cos(angle)))]:
            for field, value in zip("xyzw", values):
                modifications += f"    - target: {{fileID: {ROOT_TRANSFORM}, guid: {asset_guid}, type: 3}}\n      propertyPath: {property_name}.{field}\n      value: {value}\n      objectReference: {{fileID: 0}}\n"
        output += f"--- !u!1001 &{instance}\nPrefabInstance:\n  m_ObjectHideFlags: 0\n  serializedVersion: 2\n  m_Modification:\n    serializedVersion: 3\n    m_TransformParent: {{fileID: 0}}\n{modifications}    m_RemovedComponents: []\n    m_RemovedGameObjects: []\n    m_AddedGameObjects: []\n    m_AddedComponents: []\n  m_SourcePrefab: {{fileID: 100100000, guid: {asset_guid}, type: 3}}\n"
    path = ROOT / "Assets/Scenes/Props/CaveModuleGallery.unity"
    write(path, output)
    meta(path, "DefaultImporter")


def build():
    folder(MODEL)
    folder(PREFAB)
    for name, texture in [("rock", "cave_rock_face"), ("soil", "cave_earth_wet")]:
        path = ROOT / f"Assets/Art/Materials/CaveHD2D/cave_module_{name}.mat"
        material = (ROOT / "Assets/Art/Materials/CaveHD2D/cliff_module_rock.mat").read_text(encoding="utf-8")
        texture_guid = re.search(r"guid: (\w+)", (ROOT / f"Assets/Art/Textures/TerrainBasic/{texture}.png.meta").read_text()).group(1)
        material = material.replace("m_Name: cliff_module_rock", f"m_Name: cave_module_{name}").replace("87e7a19ed11a4c9e8ae49a7f5a971611", texture_guid)
        material = re.sub(r"_BaseColor: .*", "_BaseColor: {r: 0.65, g: 0.61, b: 0.53, a: 1}", material)
        write(path, material)
        material_guid = meta(path, "NativeFormatImporter", 2100000)
        if name == "rock":
            assert material_guid == ROCK
        else:
            soil_guid = material_guid
    assets = []
    for shape in shapes():
        shape.check()
        assets.append((shape.name, prefab_asset(shape, mesh_asset(shape), soil_guid)))
        print(f"{shape.name}: {len(shape.triangles)} triangles, closed / outward")
    gallery(assets)
    return shapes()


def preview(items):
    import bpy
    from mathutils import Vector
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    materials = []
    for name, texture, tint in [("cave_rock", "TerrainBasic/cave_rock_face.png", (.65, .61, .53)), ("cave_soil", "TerrainBasic/cave_earth_wet.png", (.65, .61, .53))]:
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
        nodes, links = mat.node_tree.nodes, mat.node_tree.links
        principled = next((node for node in nodes if node.type == "BSDF_PRINCIPLED"), None)
        if principled is None:
            principled = nodes.new("ShaderNodeBsdfPrincipled")
            output = nodes.new("ShaderNodeOutputMaterial")
            links.new(principled.outputs["BSDF"], output.inputs["Surface"])
        principled.inputs["Roughness"].default_value = 1
        coord = nodes.new("ShaderNodeTexCoord")
        mapping = nodes.new("ShaderNodeVectorMath")
        mapping.operation = "SCALE"
        mapping.inputs[3].default_value = .25
        links.new(coord.outputs["Object"], mapping.inputs[0])
        tex = nodes.new("ShaderNodeTexImage")
        tex.image = bpy.data.images.load(str(ROOT / "Assets/Art/Textures" / texture))
        tex.interpolation = "Closest"
        tex.projection = "BOX"
        tex.projection_blend = 0
        links.new(mapping.outputs[0], tex.inputs["Vector"])
        mix = nodes.new("ShaderNodeMixRGB")
        mix.blend_type = "MIX"
        mix.inputs[0].default_value = .8
        mix.inputs[1].default_value = (*tint, 1)
        links.new(tex.outputs["Color"], mix.inputs[2])
        links.new(mix.outputs[0], principled.inputs["Base Color"])
        materials.append(mat)
    label_material = bpy.data.materials.new("preview_label")
    label_material.use_nodes = True
    label_material.node_tree.nodes.clear()
    label_shader = label_material.node_tree.nodes.new("ShaderNodeBsdfPrincipled")
    label_output = label_material.node_tree.nodes.new("ShaderNodeOutputMaterial")
    label_shader.inputs["Base Color"].default_value = (.025, .03, .025, 1)
    label_material.node_tree.links.new(label_shader.outputs["BSDF"], label_output.inputs["Surface"])
    for i, shape in enumerate(items):
        vertices = [(p[0], -p[2], p[1]) for tri, _ in shape.triangles for p in tri]
        faces = [(j, j+1, j+2) for j in range(0, len(vertices), 3)]
        data = bpy.data.meshes.new(shape.name)
        data.from_pydata(vertices, [], faces)
        data.materials.append(materials[0])
        data.materials.append(materials[1])
        for polygon, (_, material) in zip(data.polygons, shape.triangles):
            polygon.material_index = material
        obj = bpy.data.objects.new(shape.name, data)
        bpy.context.collection.objects.link(obj)
        obj.location = ((i % 4)*10, -(i//4)*10, 0)
        # Number labels correspond to the inventory table, kept out of Unity assets.
        bpy.ops.object.text_add(location=(obj.location.x-3, obj.location.y-4.5, .02))
        text = bpy.context.object
        text.name = "label_"+shape.name
        text.data.body = f"{i+1:02d}"
        text.data.size = .9
        text.data.materials.append(label_material)
    bpy.ops.mesh.primitive_plane_add(size=200, location=(12, -10, -.62))
    floor = bpy.context.object
    mat = bpy.data.materials.new("preview_floor")
    mat.use_nodes = True
    mat.node_tree.nodes.clear()
    floor_shader = mat.node_tree.nodes.new("ShaderNodeBsdfPrincipled")
    floor_output = mat.node_tree.nodes.new("ShaderNodeOutputMaterial")
    floor_shader.inputs["Base Color"].default_value = (.18, .20, .19, 1)
    mat.node_tree.links.new(floor_shader.outputs["BSDF"], floor_output.inputs["Surface"])
    floor.data.materials.append(mat)
    bpy.ops.object.camera_add(location=(40, -51, 40))
    camera = bpy.context.object
    camera.rotation_euler = (Vector((14, -10, 1))-camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 52
    bpy.context.scene.camera = camera
    bpy.ops.object.light_add(type="AREA", location=(0, -15, 30))
    bpy.context.object.data.energy = 6500
    bpy.context.object.data.shape = "DISK"
    bpy.context.object.data.size = 25
    scene = bpy.context.scene
    scene.world.color = (.3, .3, .3)
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 24
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1200
    scene.render.resolution_percentage = 100
    scene.view_settings.view_transform = "Standard"
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(ROOT / "docs/images/cave-module-gallery.png")
    # Save the editable source with packed existing textures; do not modify them.
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / "tools/art/cave_priority_modules.blend"))
    bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    result = build()
    if "--preview" in sys.argv:
        preview(result)
