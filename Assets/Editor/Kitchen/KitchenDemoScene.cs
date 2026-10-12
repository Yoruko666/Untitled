using UnityEditor;
using UnityEngine;

namespace Untitled.Editor
{
    public static partial class KitchenDemoBuilder
    {
        private static void BuildShell(Transform room)
        {
            Transform shell = Group("01_SquareCaveShell_WestAndSouthEntrances", room);
            var foundation = new Geometry();
            B(foundation, V(0, -.25f, 0), V(17.0f, .5f, 17.0f), Finish.StoneDark, .15f);
            Transform floor = MeshObject("kitchen_floor_foundation", foundation, shell).transform;
            BoxCollision(floor, V(0, -.20f, 0), V(16.8f, .40f, 16.8f));
            for (int chunk = 0; chunk < 4; chunk++)
            {
                var tiles = new Geometry();
                for (int z = 0; z < 8; z++) for (int x = 0; x < 8; x++)
                {
                    float px = -7.5f + x + (chunk % 2) * 8, pz = -7.5f + z + (chunk / 2) * 8;
                    Finish finish = (x * 3 + z + chunk) % 9 == 0 ? Finish.StoneLight : Finish.Stone;
                    B(tiles, V(px, -.009f + Mathf.Sin(x * 7 + z * 11) * .008f, pz), V(.975f, .067f, .975f), finish, .038f);
                }
                MeshObject("kitchen_worn_floor_tiles_" + chunk, tiles, shell);
            }
            Wall(shell, "kitchen_north_cave_wall", V(0, 0, 8), 16, 3.30f, 0);
            Wall(shell, "kitchen_east_cutaway_wall", V(8, 0, 0), 16, 2.05f, 90);
            Wall(shell, "kitchen_west_north_wall", V(-8, 0, 2.35f), 11.3f, 3.05f, 90);
            Wall(shell, "kitchen_west_south_jamb", V(-8, 0, -7), 2, .45f, 90);
            Wall(shell, "kitchen_south_west_cutaway", V(-4.8f, 0, -8), 6.4f, .36f, 0);
            Wall(shell, "kitchen_south_east_cutaway", V(4.8f, 0, -8), 6.4f, .36f, 0);
            var entrance = new Geometry();
            B(entrance, V(-8.15f, .012f, -4.65f), V(1.6f, .10f, 2.65f), Finish.StoneLight, .04f);
            B(entrance, V(0, .012f, -8.15f), V(3.2f, .10f, 1.6f), Finish.StoneLight, .04f);
            foreach (float z in new[] { -3.30f, -6.0f })
            {
                B(entrance, V(-8, 1.45f, z), V(.27f, 2.9f, .27f), Finish.OakDark);
                B(entrance, V(-8, .28f, z), V(.30f, .16f, .30f), Finish.Iron);
            }
            B(entrance, V(-8, 2.95f, -4.65f), V(.32f, .20f, 3.0f), Finish.Oak);
            MeshObject("kitchen_open_entrance_frames_and_thresholds", entrance, shell);
            foreach (Vector3 p in new[] { V(-8, 0, 8), V(-8, 0, 1.8f), V(8, 0, 8), V(8, 0, -.7f) })
            {
                var brace = new Geometry();
                float height = p.x > 0 ? 2.15f : 3.22f;
                B(brace, p + V(0, height / 2, 0), V(.24f, height, .24f), Finish.OakDark);
                foreach (float y in new[] { .22f, height - .20f }) B(brace, p + V(0, y, 0), V(.28f, .14f, .28f), Finish.Iron);
                MeshObject("kitchen_timber_brace_" + p.x + "_" + p.z, brace, shell);
            }
        }

        private static void Wall(Transform parent, string name, Vector3 origin, float length, float height, float yaw)
        {
            var g = new Geometry(); Quaternion q = Quaternion.Euler(0, yaw, 0);
            int rows = Mathf.CeilToInt(height / .56f), columns = Mathf.CeilToInt(length / 1.15f);
            for (int row = 0; row < rows; row++) for (int col = 0; col < columns; col++)
            {
                float h = Mathf.Min(.53f, height - row * .56f);
                float x = -length / 2 + (col + .5f) * length / columns;
                g.Box(origin + q * V(x, row * .56f + h / 2, .035f * Mathf.Sin(row + col)), V(length / columns - .025f, h, .63f), (row + col) % 5 == 0 ? Finish.StoneLight : Finish.StoneDark, q, .075f);
            }
            for (int i = 0; i < columns; i++)
                g.Rock(origin + q * V(-length / 2 + (i + .5f) * length / columns, height + .06f, 0), V(length / columns + .07f, .31f, .76f), Finish.StoneDark, i);
            Transform wall = MeshObject(name, g, parent).transform;
            Transform blocker = Group("BoundaryCollider", wall); blocker.localPosition = origin; blocker.localRotation = q;
            BoxCollision(blocker, V(0, 1.7f, 0), V(length, 3.4f, .66f));
        }

        private static void BuildPreparation(Transform room)
        {
            Transform prep = Group("02_LongChoppingTable_ExistingHD2D", room);
            Transform table = Reuse(prep, "Kitchen/chopping_table_hd2d", V(-1.75f, .035f, 2.05f));
            table.name = "ExistingChoppingTable_LongChefWorkstation";
            table.localScale = V(5.35f, 1.70f, 1.55f);
            // Original eight-direction sprite and its reference remain intact.
            SetDirection(table, 0);
            Reuse(prep, "Tavern/wooden_stool_3d", V(-3.70f, .03f, .83f), .70f, 8);
            Place(prep, "roots_and_mushrooms_tub_3d", V(-4.95f, .03f, 1.75f), -8, .82f);
            Place(prep, "open_grain_sack_3d", V(-4.28f, .03f, 3.24f), 9, .76f);
            var details = new Geometry();
            // A coarse woven work mat and worn stepping stones anchor the pixel table.
            B(details, V(-1.75f, .043f, 1.23f), V(5.15f, .022f, 1.22f), Finish.Straw, .04f);
            for (int i = 0; i < 20; i++) B(details, V(-4.15f + i * .25f, .058f, .58f), V(.025f, .010f, .14f), Finish.OakDark, .002f);
            MeshObject("kitchen_preparation_woven_mat", details, prep);
            SetLamp(Reuse(prep, "Tavern/oil_lantern_3d", V(-4.15f, 2.35f, 2.68f), .48f), 2.25f, 4.9f);
            var hanger = new Geometry();
            BuildLanternHook(hanger, V(-4.15f, 0, 2.87f), 2.91f);
            MeshObject("kitchen_prep_lantern_timber_hook", hanger, prep);
            BuildRightPrepLamp(prep);
        }

        private static void BuildLanternHook(Geometry g, Vector3 p, float height)
        {
            B(g, p + V(0, height / 2, 0), V(.11f, height, .11f), Finish.OakDark);
            g.Beam(p + V(0, height, 0), p + V(0, height, -.34f), .07f, Finish.Iron);
            g.Beam(p + V(0, height, -.34f), p + V(0, height - .33f, -.34f), .028f, Finish.Iron);
        }

        private static void BuildHotLine(Transform room)
        {
            Transform hot = Group("03_CauldronSpitRoastAndMeatRack", room);
            Place(hot, "cauldron_stone_fire_ring_3d", V(3.35f, .035f, 2.32f));
            Transform pot = Reuse(hot, "Kitchen/cauldron_b_hd2d", V(3.35f, .48f, 2.40f), 2.65f);
            pot.name = "ExistingLargeCauldron_HD2D";
            AddFire(hot, "CauldronCookingFire", V(3.35f, .72f, 1.96f), 3.3f, 5.0f, true);
            Place(hot, "spit_roast_charcoal_grill_3d", V(3.65f, .035f, 6.35f));
            AddFire(hot, "SpitRoastCharcoalGlow", V(3.65f, 1.02f, 6.18f), 4.0f, 5.6f, true);
            Reuse(hot, "Kitchen/meat_rack_hd2d", V(-.86f, .035f, 6.53f), 1.38f);
            Reuse(hot, "Kitchen/cooking_stove_hd2d", V(6.63f, .035f, 6.53f), 1.24f);
            Reuse(hot, "Kitchen/firewood_stack_hd2d", V(6.90f, .035f, 3.24f), 1.68f);
            Reuse(hot, "Kitchen/firewood_bundle_hd2d", V(6.28f, .035f, 4.36f), 1.10f);
            Reuse(hot, "Kitchen/firewood_scattered_hd2d", V(5.65f, .035f, 2.99f), .89f);
            Place(hot, "cleavers_pan_and_ladle_rail_3d", V(.25f, 3.18f, 7.61f));
            Place(hot, "lidded_spice_crock_3d", V(6.29f, .035f, 5.53f), 2, .66f);
            var marks = new Geometry();
            for (int i = 0; i < 14; i++)
                marks.Rock(V(2.2f + (i % 5) * .55f, .038f, 5.28f + (i / 5) * .46f), V(.39f, .022f, .27f), Finish.Charcoal, i);
            MeshObject("kitchen_hearth_ash_and_soot", marks, hot);
        }

        private static void BuildPantry(Transform room)
        {
            Transform pantry = Group("04_GrainJarsHerbsAndReceivingCrates", room);
            Place(pantry, "provision_jars_and_grain_shelf_3d", V(-5.66f, .035f, 6.90f));
            Place(pantry, "hanging_garlic_and_cave_herbs_3d", V(-3.0f, 3.19f, 7.59f));
            Place(pantry, "open_grain_sack_3d", V(-6.62f, .035f, 4.57f), 7, 1.17f);
            Place(pantry, "open_grain_sack_3d", V(-5.45f, .035f, 4.53f), -5, .92f);
            Reuse(pantry, "Tavern/supply_crate_3d", V(-6.55f, .035f, -6.45f), .92f, 3);
            Reuse(pantry, "Tavern/supply_crate_3d", V(-5.29f, .035f, -6.65f), .90f, -7);
            Reuse(pantry, "Tavern/supply_crate_3d", V(-6.55f, .93f, -6.45f), .79f, -5);
            Reuse(pantry, "Kitchen/produce_roots_hd2d", V(-5.29f, .93f, -6.65f), .91f);
            Reuse(pantry, "Kitchen/produce_onions_hd2d", V(-6.55f, 1.70f, -6.45f), .67f);
            Place(pantry, "roots_and_mushrooms_tub_3d", V(-4.61f, .035f, -5.26f), 10, 1.02f);
            Place(pantry, "open_grain_sack_3d", V(-4.25f, .035f, -6.65f), -8);
            Reuse(pantry, "Kitchen/produce_leaves_hd2d", V(-6.82f, .035f, -1.92f), .96f);
            // A bone-and-cleaver insignia gives the doorway a goblin kitchen identity.
            var sign = new Geometry();
            B(sign, V(-6.58f, 2.18f, -2.35f), V(1.18f, .51f, .09f), Finish.OakDark);
            sign.Beam(V(-6.90f, 1.99f, -2.415f), V(-6.40f, 2.38f, -2.415f), .065f, Finish.Bone);
            B(sign, V(-6.38f, 2.22f, -2.422f), V(.33f, .25f, .035f), Finish.Iron);
            MeshObject("kitchen_bone_and_cleaver_sign", sign, pantry);
        }

        private static void BuildWashing(Transform room)
        {
            Transform wash = Group("05_WashingWaterAndUsedCrockery", room);
            Reuse(wash, "Kitchen/stone_cistern_wide_3d", V(-6.46f, .035f, .04f), 1.34f);
            Reuse(wash, "Tavern/ale_barrel_3d", V(-6.51f, .035f, 2.36f), .81f, 6);
            Place(wash, "lidded_spice_crock_3d", V(-5.38f, .035f, -.66f), 3, .72f);
            Reuse(wash, "Tavern/wooden_stool_3d", V(-4.89f, .035f, -.42f), .65f);
            Place(wash, "stacked_bowls_and_stew_3d", V(-4.89f, .718f, -.42f), -9, .67f);
            var g = new Geometry();
            // Water inside the existing open cistern, bucket and a wet stone apron.
            B(g, V(-6.46f, 1.00f, .04f), V(1.65f, .018f, .63f), Finish.Teal, .045f);
            g.Lathe(V(-5.47f, .035f, 1.50f), new[] { V(.24f, 0, 0), V(.32f, .10f, 0), V(.36f, .49f, 0), V(.30f, .49f, 0), V(.25f, .10f, 0) }, Finish.OakDark, 20);
            g.Ring(V(-5.47f, .19f, 1.50f), .322f, .302f, .045f, Finish.Iron, 20);
            g.Ring(V(-5.47f, .42f, 1.50f), .355f, .335f, .045f, Finish.Iron, 20);
            g.Ring(V(-5.47f, .63f, 1.50f), .30f, .274f, .035f, Finish.Iron, 20, Quaternion.Euler(90, 0, 0));
            for (int i = 0; i < 6; i++) g.Rock(V(-6.8f + (i % 3) * .63f, .045f, -.76f - (i / 3) * .42f), V(.61f, .025f, .36f), Finish.StoneDark, i);
            MeshObject("kitchen_water_bucket_and_wet_stones", g, wash);
        }

        private static void BuildServingAndClutter(Transform room)
        {
            Transform serve = Group("06_ServingBenchAndSupplyCorners", room);
            Place(serve, "rough_timber_serving_bench_3d", V(3.56f, .035f, -5.98f));
            Place(serve, "stacked_bowls_and_stew_3d", V(3.04f, 1.36f, -5.98f));
            Place(serve, "lidded_spice_crock_3d", V(4.65f, 1.36f, -5.88f), 9, .67f);
            Place(serve, "lidded_spice_crock_3d", V(2.55f, .40f, -5.98f), -5, .68f);
            Reuse(serve, "Tavern/wooden_stool_3d", V(5.50f, .035f, -5.15f), .83f, -8);
            Reuse(serve, "Tavern/ale_barrel_3d", V(6.49f, .035f, -3.50f), .84f);
            Reuse(serve, "Tavern/ale_barrel_3d", V(7.05f, .035f, -1.76f), .67f);
            Reuse(serve, "Tavern/supply_crate_3d", V(6.40f, .035f, -6.60f), .84f, 7);
            Place(serve, "open_grain_sack_3d", V(6.76f, .035f, -5.26f), -12, .81f);
            Reuse(serve, "Tavern/wall_bench_3d", V(7.00f, .035f, .23f), .74f, 90);
            Place(serve, "lidded_spice_crock_3d", V(7.00f, .69f, .20f), 0, .64f);
            BuildCentralCartLayout(serve);
            Transform clutter = Group("07_EdgeDebrisStrawAndKitchenWear", room);
            var debris = new Geometry();
            for (int i = 0; i < 32; i++)
            {
                float x = i % 2 == 0 ? 7.48f : -7.48f, z = -7.15f + (i / 2) * .92f;
                if (x < 0 && z > -6.3f && z < -2.9f) continue;
                debris.Rock(V(x, .075f, z), V(.22f + (i % 3) * .05f, .13f, .31f), i % 5 == 0 ? Finish.Moss : Finish.StoneDark, i);
                if (i % 3 == 0) debris.Beam(V(x - .20f, .043f, z + .1f), V(x + .16f, .043f, z + .17f), .018f, Finish.Straw);
            }
            foreach (Vector3 p in new[] { V(-4.8f, .05f, -5.8f), V(-5.2f, .05f, 3.9f), V(5.0f, .05f, 4.8f) })
                for (int i = 0; i < 7; i++) debris.Beam(p + V(-.28f + i * .08f, 0, -.16f), p + V(.03f + i * .06f, .004f, .13f), .016f, Finish.Straw);
            MeshObject("kitchen_rubble_straw_and_peelings", debris, clutter);
        }

        private static void BuildLights(Transform room)
        {
            Transform lights = Group("08_WarmOilAndWallFire", room);
            SetLamp(Reuse(lights, "Tavern/wall_brazier_3d", V(1.56f, 2.10f, 7.61f), .63f), 2.30f, 4.5f);
            BuildRoastLamp(lights);
            SetLamp(Reuse(lights, "Tavern/wall_brazier_3d", V(-7.61f, 1.95f, -2.70f), .61f, -90), 2.0f, 4.5f);
            SetLamp(Reuse(lights, "Tavern/oil_lantern_3d", V(-5.65f, 2.96f, 6.90f), .48f), 2.1f, 4.3f);
            SetLamp(Reuse(lights, "Tavern/oil_lantern_3d", V(3.96f, 1.37f, -5.99f), .45f), 2.3f, 5.0f);
            SetLamp(Reuse(lights, "Tavern/oil_lantern_3d", V(6.49f, 1.30f, -3.50f), .46f), 1.80f, 4.2f);
        }

        private static void BuildCentralCartLayout(Transform serve)
        {
            Transform cart = Place(serve, "ingredient_prep_cart_3d", V(-1.25f, .035f, -2.67f), -5);
            cart.name = "CentralIngredientPrepCart";
            Reuse(cart, "Tavern/supply_crate_3d", V(-.43f, .47f, 0), .39f);
            SetLamp(Reuse(cart, "Tavern/oil_lantern_3d", V(.73f, 1.15f, .10f), .38f), 2.40f, 4.8f);
            var mats = new Geometry();
            B(mats, V(-1.25f, .044f, -2.67f), V(2.74f, .025f, 1.85f), Finish.Cloth, .035f);
            B(mats, V(3.56f, .044f, -5.73f), V(4.05f, .025f, 2.2f), Finish.Cloth, .035f);
            foreach (float x in new[] { -2.55f, .05f }) B(mats, V(x, .060f, -2.67f), V(.035f, .012f, 1.65f), Finish.Straw, .004f);
            for (int i = 0; i < 11; i++) mats.Rock(V(-2.25f + i * .18f, .041f, -3.82f + Mathf.Sin(i * 2.4f) * .15f), V(.045f, .03f, .052f), Finish.Straw, i);
            MeshObject("kitchen_cart_and_serving_work_mats", mats, serve);
        }

        private static void BuildRightPrepLamp(Transform prep)
        {
            SetLamp(Reuse(prep, "Tavern/oil_lantern_3d", V(.77f, 2.37f, 2.82f), .43f), 2.35f, 4.6f);
            var hanger = new Geometry(); BuildLanternHook(hanger, V(.77f, 0, 3.03f), 2.93f);
            MeshObject("kitchen_prep_right_lantern_hook", hanger, prep);
        }

        private static void BuildRoastLamp(Transform lights)
        {
            Transform lamp = Reuse(lights, "Tavern/wall_brazier_3d", V(5.68f, 2.30f, 7.61f), .64f);
            lamp.name = "RoastWorkLight"; SetLamp(lamp, 3.2f, 5.2f);
        }

        private static void AddFire(Transform parent, string name, Vector3 position, float intensity, float range, bool shadows)
        {
            Transform fire = Group(name, parent); fire.localPosition = position;
            Light light = fire.gameObject.AddComponent<Light>(); light.type = LightType.Point;
            light.color = new Color(1, .44f, .12f); light.intensity = intensity; light.range = range;
            fire.gameObject.AddComponent<HybridFireLight>(); SetLamp(fire, intensity, range, shadows);
        }

        private static void SetDirection(Transform item, int direction)
        {
            HybridProp prop = item.GetComponent<HybridProp>();
            var settings = new SerializedObject(prop); settings.FindProperty("directionFrame").intValue = direction;
            settings.ApplyModifiedPropertiesWithoutUndo(); prop.ApplyVisual();
        }
    }
}
