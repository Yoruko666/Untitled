"""Author the reference shop's editable Blender assets and export meter-scale FBX.

blender --background --factory-startup --python tools/build_milk_tea_reference.py
"""
import json
import math
import random
import re
from pathlib import Path

import bpy
from mathutils import Matrix, Vector


ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path(r"D:/download/MilkTeaShop_Package_v2/MilkTeaShop_V2/MilkTea_Assets_49.blend")
OUT = ROOT / "Assets/Art/Models/MilkTeaReference"
BLEND = ROOT / "tools/art/milk_tea_reference.blend"
OUT.mkdir(parents=True, exist_ok=True)
random.seed(49)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.name = "Milk Tea Reference Asset Library"
scene.unit_settings.system = "METRIC"
scene.unit_settings.scale_length = 1
asset_collections = {}
current = None


def material(name, color, metallic=0, roughness=0.5):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    mat.metallic = metallic
    mat.roughness = roughness
    return mat


def asset(name):
    global current
    current = bpy.data.collections.new(name)
    scene.collection.children.link(current)
    asset_collections[name] = current
    return current


def move_to_asset(obj, name, mat):
    obj.name = name
    for collection in list(obj.users_collection):
        collection.objects.unlink(obj)
    current.objects.link(obj)
    if mat:
        obj.data.materials.append(materials[mat])
    return obj


def box(name, location, size, mat, bevel=0.01):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = move_to_asset(bpy.context.object, name, mat)
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        modifier = obj.modifiers.new("Soft manufactured edges", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
    return obj


def cylinder(name, location, radius, depth, mat, vertices=24, rotation=None, top_radius=None):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius,
                                   radius2=radius if top_radius is None else top_radius,
                                   depth=depth, location=location,
                                   rotation=rotation or (0, 0, 0))
    return move_to_asset(bpy.context.object, name, mat)


def sphere(name, location, radius, mat, subdivisions=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=radius, location=location)
    return move_to_asset(bpy.context.object, name, mat)


def rod(name, start, end, radius, mat):
    direction = Vector(end) - Vector(start)
    obj = cylinder(name, (Vector(start) + Vector(end)) / 2, radius, direction.length, mat, 10)
    obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    return obj


def panel_shape(name, points, depth, mat):
    vertices = [(x, -depth / 2, z) for x, z in points] + [(x, depth / 2, z) for x, z in points]
    count = len(points)
    faces = [tuple(reversed(range(count))), tuple(range(count, count * 2))]
    faces += [(i, (i + 1) % count, (i + 1) % count + count, i + count) for i in range(count)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    current.objects.link(obj)
    obj.data.materials.append(materials[mat])
    return obj


selected = {
    "02_pendant_lamp_A": "pendant_lamp",
    "03_pendant_lamp_B": "pendant_lamp_small",
    "06_wood_window": "wood_window",
    "15_cup_sealing_machine": "cup_sealing_machine",
    "17_straw_holder": "straw_holder",
    "18_cup_shelf": "cup_shelf",
    "19_tea_dispenser_01": "tea_dispenser",
    "20_tea_dispenser_02": "tea_dispenser_green",
    "22_syrup_pumps": "syrup_pumps",
    "24_blender": "blender",
    "25_finished_boba": "finished_boba",
    "27_ingredient_jars": "ingredient_jars",
    "31_potted_plant": "potted_plant",
    "32_wall_vines": "wall_vines",
    "33_tissue_dispenser": "tissue_dispenser",
    "34_cardboard_box": "cardboard_box",
    "35_wooden_crate": "wooden_crate",
    "38_takeout_bags": "takeout_bags",
    "42_sink_counter": "sink_counter",
    "44_wall_module": "wall_module",
    "45_floor_tile": "floor_tile",
    "46_ceiling_module": "ceiling_module",
    "47_wooden_beam": "wooden_beam",
}
with bpy.data.libraries.load(str(SOURCE), link=False) as (src, dst):
    dst.collections = list(selected) + ["41_work_counter", "07_basement_portal"]
source_collections = {c.name: c for c in dst.collections}
# Source collections are stored outside the source scene's preview instances.
# Link and evaluate them before reading matrix_world; unlinked datablocks may
# retain identity matrices even when location, rotation and scale are authored.
for collection in dst.collections:
    scene.collection.children.link(collection)
bpy.context.view_layer.update()
materials = {}
for mat in list(bpy.data.materials):
    canonical = re.sub(r"\.\d{3}$", "", mat.name)
    if canonical not in materials:
        materials[canonical] = mat

for name, color, metal, rough in (
    ("CabinetGreen", (.027, .046, .036), .18, .48),
    ("CabinetInset", (.040, .061, .047), .15, .5),
    ("FridgeSteel", (.115, .145, .142), .55, .36),
    ("FridgeDoor", (.19, .21, .185), .5, .32),
    ("PanSteel", (.38, .42, .4), .75, .28),
    ("Pearls", (.025, .017, .009), .05, .23),
    ("Coconut", (.88, .79, .59), .0, .34),
    ("Mango", (.98, .49, .045), .0, .27),
    ("RedBean", (.48, .035, .018), .0, .3),
    ("Aloe", (.51, .59, .36), .0, .25),
    ("TeaJelly", (.18, .069, .013), .0, .23),
    ("CupClear", (.65, .74, .70), .0, .18),
    ("LampGlow", (1, .70, .27), .0, .25),
    ("PendantBrass", (.38, .23, .055), .55, .35),
    ("MenuPaper", (.90, .82, .63), .0, .76),
    ("MenuTea", (.58, .30, .09), .0, .6),
    ("MenuMatcha", (.36, .46, .12), .0, .6),
    ("RockDark", (.095, .059, .031), .0, .87),
    ("RockWarm", (.16, .10, .047), .0, .84),
):
    materials[name] = material(name, color, metal, rough)


def copy_collection(source, destination, transform=Matrix.Identity(4), exclude=None):
    for obj in source.objects:
        if exclude and exclude(obj):
            continue
        world = transform @ obj.matrix_world
        if obj.instance_type == "COLLECTION" and obj.instance_collection:
            copy_collection(obj.instance_collection, destination,
                            world @ Matrix.Translation(-obj.instance_collection.instance_offset), exclude)
        elif obj.type in {"MESH", "CURVE", "FONT"}:
            copied = obj.copy()
            copied.data = obj.data.copy()
            copied.parent = None
            copied.matrix_world = world
            destination.objects.link(copied)
            if copied.type == "FONT" and not copied.data.materials:
                copied.data.materials.append(materials["Ink"])
            for slot in copied.material_slots:
                if slot.material:
                    slot.material = materials.get(re.sub(r"\.\d{3}$", "", slot.material.name), slot.material)
    for child in source.children:
        copy_collection(child, destination, transform, exclude)


for original, name in selected.items():
    copy_collection(source_collections[original], asset(name))
    if name.startswith("pendant_lamp"):
        for obj in current.objects:
            if obj.name.startswith("Amber tapered shade"):
                for slot in obj.material_slots:
                    slot.material = materials["PendantBrass"]
            if obj.name.startswith("Bulb"):
                obj.location.y = 0
                obj.location.z = -1.015 * (.82 if name.endswith("small") else 1)
    if name == "sink_counter":
        for obj in current.objects:
            if obj.name.startswith(("Sink cabinet", "Cabinet inset")):
                for slot in obj.material_slots:
                    slot.material = materials["CabinetGreen"]

# The supplied counter keeps its editable wooden top. Replace open shelving with
# dark refrigerated cabinet fronts, matching the reference's work surfaces.
asset("galley_counter")
copy_collection(source_collections["41_work_counter"], current,
                exclude=lambda obj: obj.type == "FONT" or obj.name.startswith(("Paper label", "Door moulding", "Cupboard door", "Cabinet knob")))
for obj in current.objects:
    if obj.type == "MESH" and obj.location.z < .82:
        for slot in obj.material_slots:
            slot.material = materials["CabinetGreen"]
for x in (-.58, 0, .58):
    box("Refrigerated cabinet door", (x, -.304, .432), (.545, .035, .744), "CabinetGreen", .012)
    box("Inset door panel", (x, -.326, .43), (.464, .012, .61), "CabinetInset", .008)
    box("Steel pull", (x + .16, -.35, .72), (.13, .028, .024), "Steel", .006)
    for z in (.14, .18, .22):
        box("Vent slot", (x, -.336, z), (.29, .006, .008), "Black", .002)

asset("inset_topping_counter")
box("Cabinet body", (0, .015, .435), (1.8, .62, .84), "CabinetGreen", .012)
for x in (-.59, 0, .59):
    box("Inset cabinet door", (x, -.307, .428), (.558, .035, .74), "CabinetInset", .01)
    box("Metal pull", (x + .17, -.339, .73), (.12, .025, .022), "Steel", .005)
box("Oak worktop front rail", (0, -.302, .87), (1.84, .075, .075), "Oak", .014)
box("Oak worktop back rail", (0, .302, .87), (1.84, .075, .075), "Oak", .014)
for x in (-.884, .884):
    box("Oak worktop end", (x, 0, .87), (.07, .545, .075), "Oak", .012)
box("Inset steel tray", (0, 0, .873), (1.69, .535, .038), "PanSteel", .009)
toppings = ["Pearls", "Coconut", "RedBean", "TeaJelly", "Aloe", "Mango",
            "Pearls", "Coconut", "Mango", "TeaJelly", "Coconut", "RedBean"]
for row in range(4):
    for column in range(3):
        x, y = -.624 + row * .416, -.18 + column * .18
        index = row * 3 + column
        box("Ingredient pan floor", (x, y, .895), (.39, .158, .018), toppings[index], .005)
        for dx in (-.197, .197):
            box("Pan steel rim", (x + dx, y, .915), (.012, .17, .025), "PanSteel", .004)
        for dy in (-.082, .082):
            box("Pan steel rim", (x, y + dy, .915), (.40, .012, .025), "PanSteel", .004)
        for item in range(46):
            position = (x + random.uniform(-.175, .175), y + random.uniform(-.061, .061), .913 + random.uniform(0, .009))
            if toppings[index] in {"Pearls", "RedBean", "Mango"}:
                sphere("Fresh topping", position, random.uniform(.009, .015), toppings[index])
            else:
                obj = box("Fresh jelly cube", position, (.022, .014, .018), toppings[index], .002)
                obj.rotation_euler.z = random.uniform(-.7, .7)

asset("tall_refrigerator")
box("Fridge housing", (0, .03, 1.025), (.69, .70, 2.05), "FridgeSteel", .024)
for z, height in ((1.76, .51), (.81, 1.32)):
    box("Refrigerator gasket", (0, -.33, z), (.657, .025, height), "Black", .009)
    box("Brushed steel door", (0, -.352, z), (.631, .043, height - .024), "FridgeDoor", .018)
    box("Long pull handle", (-.255, -.401, z + .06), (.028, .04, min(height - .12, .47)), "Steel", .009)
box("Bottom kickplate", (0, -.34, .072), (.62, .04, .10), "Iron", .004)
for x in (-.22, -.16, -.1, -.04, .02, .08, .14, .20):
    box("Compressor grille", (x, -.365, .075), (.012, .008, .063), "Black", .002)
box("Fridge badge", (.15, -.38, 1.81), (.15, .008, .04), "Iron", .003)

asset("menu_triptych")
box("Menu frame", (0, .015, .412), (1.27, .055, .79), "Walnut", .016)
for i in range(3):
    x = -.405 + i * .405
    box("Menu parchment", (x, -.019, .413), (.366, .012, .714), "MenuPaper", .003)
    box("Iron clipboard clip", (x, -.035, .761), (.072, .025, .071), "Iron", .007)
    box("Clip glint", (x, -.05, .779), (.037, .006, .015), "Steel", .002)
    shape = [(-.078, .23), (.078, .23), (.104, .516), (-.104, .516)]
    obj = panel_shape("Drink silhouette", [(px + x, z) for px, z in shape], .008, "MenuTea" if i != 2 else "MenuMatcha")
    obj.location.y = -.03
    for side in (-1, 1):
        rod("Printed cup outline", (x + side * .078, -.039, .232), (x + side * .104, -.039, .516), .004, "Ink")
    box("Cup lid illustration", (x, -.041, .519), (.229, .008, .015), "Ink", .005)
    rod("Printed straw", (x + .02, -.043, .49), (x + .002, -.043, .646), .005, "Honey")
    for dot in range(11):
        cylinder("Printed tapioca pearl", (x + random.uniform(-.06, .06), -.042, random.uniform(.25, .319)), .009, .004, "Pearls", 12, (math.pi / 2, 0, 0))
    for z in (.123, .079):
        cylinder("Menu bullet", (x - .105, -.041, z), .009, .004, "Ink", 12, (math.pi / 2, 0, 0))
        box("Menu rule", (x + .02, -.042, z), (.16, .004, .006), "Honey", .001)
box("Menu oak ledge", (0, -.035, .024), (1.35, .16, .048), "Oak", .012)
for x in (-.52, .52):
    rod("Rear menu support", (x, .025, .63), (x, .23, .015), .013, "Walnut")

asset("wall_storage_shelf")
for x in (-.67, .67):
    box("Black metal upright", (x, .105, .44), (.043, .052, 1.01), "Iron", .006)
    for z in (.10, .78):
        cylinder("Shelf wall fixing", (x, .073, z), .016, .009, "Steel", 12, (math.pi / 2, 0, 0))
for z in (.055, .64):
    box("Oak shelf", (0, -.055, z), (1.5, .34, .055), "Oak", .012)
    box("Shelf front lip", (0, -.229, z + .027), (1.52, .033, .078), "Honey", .007)
    for x in (-.67, .67):
        rod("Angled shelf bracket", (x, .10, z - .14), (x, -.17, z - .015), .013, "Iron")

asset("clear_cup_stack")
# Open double-sided tapered shell and separate rims give an actual hollow cup.
vertices, faces = [], []
segments = 32
for radius, z in ((.035, 0), (.050, .28), (.0475, .28), (.033, .004)):
    for i in range(segments):
        angle = i * math.tau / segments
        vertices.append((radius * math.cos(angle), radius * math.sin(angle), z))
for ring in range(3):
    for i in range(segments):
        j = (i + 1) % segments
        faces.append((ring * segments + i, ring * segments + j, (ring + 1) * segments + j, (ring + 1) * segments + i))
mesh = bpy.data.meshes.new("Hollow tapered cup")
mesh.from_pydata(vertices, [], faces)
mesh.materials.append(materials["CupClear"])
obj = bpy.data.objects.new("Hollow tapered cup", mesh)
current.objects.link(obj)
for i in range(12):
    bpy.ops.mesh.primitive_torus_add(major_segments=32, minor_segments=6, location=(0, 0, .28 + i * .009), major_radius=.05, minor_radius=.0018)
    move_to_asset(bpy.context.object, "Nested cup rim", "CupClear")

asset("cave_entrance")
copy_collection(source_collections["07_basement_portal"], current,
                exclude=lambda obj: obj.type == "FONT" or obj.name.startswith(("Arched_recessed", "Portal_sign", "Goblin", "Glow", "Eye", "Smile", "Side_switch", "Switch_plate")) or any(slot.material and "PortalGlow" in slot.material.name for slot in obj.material_slots))
# The corrected arch opens onto modeled rock and descending treads rather than
# the source's closed black recess. Front door frame remains source geometry.
for i in range(6):
    box("Descending stone tread", (0, .30 + i * .32, -.05 - i * .11), (1.26, .34, .11), "Stone", .012)
for side in (-1, 1):
    for i in range(6):
        for level in range(3):
            rock = sphere("Cave wall outcrop", (side * (.86 + random.uniform(-.07, .06)), .49 + i * .36, .25 + level * .65 - i * .04), .47, "RockDark" if (i + level) % 3 else "RockWarm", 1)
            rock.scale = (.63, .81, 1.03)
    for i in range(4):
        rod("Stair handrail post", (side * .58, .53 + i * .43, -.07 - i * .145), (side * .58, .53 + i * .43, .72 - i * .145), .018, "DarkWood")
    rod("Descending timber handrail", (side * .58, .37, .78), (side * .58, 1.97, .24), .029, "Honey")
for i in range(5):
    rock = sphere("Cave vault", (0, .43 + i * .42, 2.0 - i * .04), .64, "RockDark", 1)
    rock.scale = (1.4, .7, .52)
box("Tunnel far darkness", (0, 2.68, .61), (2.0, .12, 3.1), "RockDark", .03)
box("Tunnel deep floor", (0, 1.5, -.78), (1.6, 2.7, .12), "RockDark", .02)
for obj in current.objects:
    obj.location.x *= .72
    obj.scale.x *= .72

asset("arch_wall_infill")
for index in range(12):
    a, b = index * math.pi / 12, (index + 1) * math.pi / 12
    points = [(.468 * math.cos(a), 1.3 + .65 * math.sin(a)),
              (.468 * math.cos(b), 1.3 + .65 * math.sin(b)),
              (.468 * math.cos(b), 2.6), (.468 * math.cos(a), 2.6)]
    panel_shape("Plaster above arch", points, .06, "BeigePlaster").location.y = .19
for side in (-1, 1):
    box("Plaster beside arch", (side * .6215, .19, 1.3), (.307, .06, 2.6), "BeigePlaster", 0)

asset("cave_lantern")
box("Lantern wall plate", (0, .025, .22), (.105, .04, .23), "Iron", .009)
rod("Lantern hanging arm", (0, 0, .32), (0, -.18, .32), .013, "Iron")
box("Lantern amber glass", (0, -.19, .14), (.105, .09, .17), "LampGlow", .005)
for x in (-.067, .067):
    for y in (-.246, -.134):
        rod("Lantern cage corner", (x, y, .035), (x, y, .244), .008, "Iron")
for z in (.033, .247):
    box("Lantern frame cap", (0, -.19, z), (.16, .14, .023), "Iron", .005)
cylinder("Lantern roof", (0, -.19, .279), .107, .05, "Iron", 4, top_radius=.035)
rod("Lantern loop", (0, -.19, .30), (0, -.19, .34), .012, "Iron")

asset("serving_counter")
box("Service counter body", (0, 0, .48), (2.59, .49, .96), "CabinetGreen", .014)
for i in range(10):
    x = -1.222 + i * .2715
    box("Individual honey oak slab", (x, 0, 1.006), (.265, .58, .09), "Oak" if i % 3 else "Honey", .014)
for x in (-.87, 0, .87):
    box("Counter face inset", (x, -.253, .50), (.78, .022, .73), "CabinetInset", .008)
    box("Counter face top trim", (x, -.27, .88), (.8, .02, .036), "Walnut", .005)

# Material metadata is shared by every exported FBX; Unity explicitly remaps it
# to editable URP materials instead of relying on Blender's procedural nodes.
manifest = {"source": str(SOURCE), "units": "meters", "materials": [], "assets": []}
used_materials = set()
export_scene = bpy.data.scenes.new("FBX Export")
for name, collection in asset_collections.items():
    bpy.context.window.scene = scene
    bpy.context.view_layer.update()
    depsgraph = bpy.context.evaluated_depsgraph_get()
    exported = []
    for obj in collection.objects:
        if obj.type not in {"MESH", "CURVE", "FONT"}:
            continue
        evaluated = obj.evaluated_get(depsgraph)
        mesh = bpy.data.meshes.new_from_object(evaluated, preserve_all_data_layers=True, depsgraph=depsgraph)
        copy = bpy.data.objects.new(obj.name, mesh)
        copy.matrix_world = obj.matrix_world
        export_scene.collection.objects.link(copy)
        exported.append(copy)
    bpy.context.window.scene = export_scene
    for obj in exported:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = exported[0]
    bpy.ops.object.join()
    joined = bpy.context.object
    joined.name = name
    # Bake object transforms into vertices and preserve origin-based anchors.
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.context.view_layer.update()
    bounds = [joined.matrix_world @ Vector(corner) for corner in joined.bound_box]
    original = next((key for key, value in selected.items() if value == name), None)
    if original:
        source_manifest = json.loads((SOURCE.parent / "manifest.json").read_text(encoding="utf-8"))
        specification = next(info for info in source_manifest["assets"] if info["collection"] == original)
        actual = [max(p[i] for p in bounds) - min(p[i] for p in bounds) for i in range(3)]
        if max(abs(a - b) for a, b in zip(actual, specification["dimensions_m"])) > .035:
            raise ValueError(f"Source meter dimensions differ for {name}: {actual} / {specification['dimensions_m']}")
    for mat in joined.data.materials:
        if mat:
            used_materials.add(mat.name)
    bpy.ops.export_scene.fbx(filepath=str(OUT / (name + ".fbx")), use_selection=True,
                             object_types={"MESH"}, global_scale=1, apply_unit_scale=True,
                             apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y",
                             bake_space_transform=False, use_mesh_modifiers=True,
                             mesh_smooth_type="FACE", add_leaf_bones=False, bake_anim=False)
    manifest["assets"].append({"name": name, "dimensions": [max(p[i] for p in bounds) - min(p[i] for p in bounds) for i in range(3)], "triangles": sum(len(p.vertices) - 2 for p in joined.data.polygons)})
    bpy.data.objects.remove(joined, do_unlink=True)
    print("EXPORTED " + name, flush=True)

for name in sorted(used_materials):
    mat = bpy.data.materials[name]
    manifest["materials"].append({"name": name, "color": list(mat.diffuse_color), "metallic": mat.metallic, "roughness": mat.roughness})
(OUT / "source_manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")
bpy.context.window.scene = scene
bpy.data.scenes.remove(export_scene)
for collection in dst.collections:
    scene.collection.children.unlink(collection)
preview = bpy.data.collections.new("00_Asset_Preview")
scene.collection.children.link(preview)
for index, (name, collection) in enumerate(asset_collections.items()):
    instance = bpy.data.objects.new(name + "_preview", None)
    instance.instance_type = "COLLECTION"
    instance.instance_collection = collection
    instance.location = ((index % 6) * 3, -(index // 6) * 3.5, 0)
    preview.objects.link(instance)
    scene.collection.children.unlink(collection)
scene.world.color = (.15, .15, .15)
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND))
print("MILK_TEA_REFERENCE_ASSETS_OK " + str(len(manifest["assets"])), flush=True)
