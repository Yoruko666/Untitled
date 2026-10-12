using UnityEngine;

namespace Untitled.Editor
{
    public static partial class KitchenDemoBuilder
    {
        private static void PrepareFurniture()
        {
            BuildGrill();
            BuildPantryShelf();
            BuildSack();
            BuildCrockAndBowls();
            BuildUtensils();
            BuildServingBench();
            BuildVegetableTub();
            BuildHerbs();
            BuildCauldronHearth();
            BuildPrepCart();
        }

        private static void B(Geometry g, Vector3 p, Vector3 s, Finish f, float bevel = .025f)
            => g.Box(p, s, f, Quaternion.identity, bevel);

        private static void BuildGrill()
        {
            var g = new Geometry();
            // Open stone trough, charcoal bed, grate, spit and a hand crank.
            B(g, V(0, .11f, 0), V(3.1f, .22f, 1.55f), Finish.StoneDark, .08f);
            foreach (float z in new[] { -.68f, .68f })
                for (int i = 0; i < 6; i++)
                    B(g, V(-1.25f + i * .5f, .34f, z), V(.48f, .42f, .28f), i % 3 == 0 ? Finish.StoneDark : Finish.Stone, .065f);
            foreach (float x in new[] { -1.43f, 1.43f })
                B(g, V(x, .34f, 0), V(.30f, .42f, 1.18f), Finish.Stone, .06f);
            for (int i = 0; i < 48; i++)
            {
                float x = -1.16f + (i % 8) * .33f, z = -.42f + (i / 8) * .16f;
                g.Rock(V(x, .29f, z), V(.24f, .17f, .17f), i % 4 == 0 ? Finish.Ember : Finish.Charcoal, i * .77f);
            }
            foreach (float z in new[] { -.49f, .49f }) g.Beam(V(-1.4f, .72f, z), V(1.4f, .72f, z), .07f, Finish.Iron);
            for (int i = 0; i < 16; i++) g.Beam(V(-1.23f + i * .164f, .73f, -.5f), V(-1.23f + i * .164f, .73f, .5f), .034f, Finish.Iron);
            foreach (float x in new[] { -1.64f, 1.64f })
            {
                B(g, V(x, .91f, 0), V(.12f, 1.82f, .14f), Finish.Iron);
                B(g, V(x, .06f, 0), V(.50f, .12f, .57f), Finish.StoneDark, .05f);
                foreach (float y in new[] { .31f, 1.37f }) B(g, V(x, y, 0), V(.16f, .12f, .18f), Finish.Brass);
                g.Ring(V(x, 1.67f, 0), .16f, .11f, .09f, Finish.Iron, 16, Quaternion.Euler(0, 0, 90));
            }
            g.Beam(V(-1.88f, 1.68f, 0), V(1.97f, 1.68f, 0), .065f, Finish.Iron);
            g.Lathe(V(-.84f, 1.68f, 0), new[] { V(.10f, 0, 0), V(.30f, .14f, 0), V(.45f, .48f, 0), V(.40f, 1.08f, 0), V(.26f, 1.53f, 0), V(.08f, 1.67f, 0) }, Finish.Roast, 24, Quaternion.Euler(0, 0, -90));
            foreach (float x in new[] { -.50f, -.08f, .34f })
            {
                // Thin charred bands and visible fat strips around the roast.
                g.Ring(V(x, 1.68f, 0), .419f, .397f, .032f, Finish.Charcoal, 24, Quaternion.Euler(0, 0, 90));
                g.Beam(V(x - .15f, 1.97f, -.22f), V(x + .06f, 2.01f, -.17f), .035f, Finish.Fat);
            }
            g.Beam(V(.96f, 1.68f, 0), V(1.36f, 1.68f, 0), .12f, Finish.Bone);
            g.Rock(V(1.37f, 1.68f, 0), V(.19f, .21f, .22f), Finish.Bone, 2);
            g.Beam(V(1.95f, 1.68f, 0), V(1.95f, 1.25f, 0), .07f, Finish.Iron);
            g.Beam(V(1.95f, 1.25f, 0), V(2.16f, 1.25f, 0), .10f, Finish.OakDark);
            for (int i = 0; i < 7; i++)
                g.Rock(V(-.99f + i * .33f, .56f, .08f), V(.15f, .48f + (i % 3) * .15f, .18f), Finish.Flame, i);
            SaveFurniture("spit_roast_charcoal_grill_3d", g, V(0, .78f, 0), V(3.65f, 1.55f, 1.6f));
        }

        private static void Jar(Geometry g, Vector3 p, float scale, Finish finish = Finish.Ceramic)
        {
            Vector3[] profile = { V(.18f, 0, 0), V(.30f, .09f, 0), V(.36f, .35f, 0), V(.31f, .60f, 0), V(.21f, .69f, 0), V(.22f, .78f, 0), V(.16f, .78f, 0), V(.16f, .68f, 0) };
            for (int i = 0; i < profile.Length; i++) profile[i] *= scale;
            g.Lathe(p, profile, finish, 20);
            g.Ring(p + V(0, .74f * scale, 0), .226f * scale, .18f * scale, .045f * scale, Finish.OakDark, 20);
            B(g, p + V(0, .47f * scale, -.34f * scale), V(.20f * scale, .16f * scale, .014f), Finish.Parchment, .008f);
            B(g, p + V(0, .48f * scale, -.355f * scale), V(.045f * scale, .075f * scale, .008f), Finish.Moss, .002f);
        }

        private static void Bowl(Geometry g, Vector3 p, float scale, bool filled = false)
        {
            Vector3[] profile = { V(.13f, 0, 0), V(.19f, .025f, 0), V(.30f, .20f, 0), V(.33f, .24f, 0), V(.285f, .24f, 0), V(.235f, .15f, 0), V(.10f, .055f, 0) };
            for (int i = 0; i < profile.Length; i++) profile[i] *= scale;
            g.Lathe(p, profile, Finish.Ceramic, 24);
            if (filled)
            {
                g.Lathe(p + V(0, .17f * scale, 0), new[] { V(.24f * scale, 0, 0), V(.24f * scale, .01f, 0) }, Finish.Soup, 24);
                for (int i = 0; i < 4; i++) g.Rock(p + V((i % 2 == 0 ? -.09f : .08f) * scale, .189f * scale, (i < 2 ? -.09f : .09f) * scale), V(.07f, .026f, .055f) * scale, i % 2 == 0 ? Finish.Fat : Finish.Moss, i);
            }
        }

        private static void BuildPantryShelf()
        {
            var g = new Geometry();
            foreach (float x in new[] { -1.55f, 1.55f }) foreach (float z in new[] { -.47f, .47f })
            {
                B(g, V(x, 1.42f, z), V(.15f, 2.84f, .15f), Finish.OakDark);
                B(g, V(x, .23f, z), V(.18f, .10f, .18f), Finish.Iron);
            }
            foreach (float y in new[] { .22f, 1.12f, 2.04f, 2.86f })
            {
                for (int i = 0; i < 5; i++) B(g, V(-1.26f + i * .63f, y, 0), V(.61f, .12f, 1.04f), i % 2 == 0 ? Finish.Oak : Finish.OakLight);
                B(g, V(0, y - .13f, -.46f), V(3.25f, .15f, .10f), Finish.OakDark);
            }
            g.Beam(V(-1.53f, .25f, .5f), V(1.53f, 2.78f, .5f), .09f, Finish.OakDark);
            g.Beam(V(1.53f, .25f, .5f), V(-1.53f, 2.78f, .5f), .09f, Finish.OakDark);
            foreach (float x in new[] { -1.11f, -.38f, .39f, 1.1f }) Jar(g, V(x, 1.19f, 0), .88f, x < 0 ? Finish.Ceramic : Finish.Bottle);
            foreach (float x in new[] { -.98f, .0f, 1.0f }) Jar(g, V(x, 2.11f, .03f), .82f);
            for (int i = 0; i < 3; i++)
            {
                Vector3 p = V(-1.02f + i * 1.01f, .30f, 0);
                g.Lathe(p, new[] { V(.30f, 0, 0), V(.39f, .18f, 0), V(.34f, .58f, 0), V(.25f, .63f, 0) }, Finish.Straw, 16);
                g.Ring(p + V(0, .62f, 0), .27f, .19f, .045f, Finish.OakDark, 16);
                for (int n = 0; n < 14; n++) g.Rock(p + V(Mathf.Cos(n * 2.4f) * .17f, .63f, Mathf.Sin(n * 2.4f) * .17f), V(.065f, .038f, .05f), i == 0 ? Finish.Garlic : Finish.Moss, n);
            }
            foreach (float x in new[] { -1.13f, 0, 1.13f })
            {
                B(g, V(x, 2.78f, -.535f), V(.40f, .18f, .027f), Finish.Parchment);
                g.Beam(V(x - .1f, 2.81f, -.556f), V(x + .1f, 2.75f, -.556f), .018f, Finish.OakDark);
            }
            SaveFurniture("provision_jars_and_grain_shelf_3d", g, V(0, 1.46f, 0), V(3.32f, 2.92f, 1.10f));
        }

        private static void BuildSack()
        {
            var g = new Geometry();
            g.Lathe(Vector3.zero, new[] { V(.25f, 0, 0), V(.42f, .17f, 0), V(.45f, .44f, 0), V(.37f, .79f, 0), V(.28f, .88f, 0), V(.20f, .88f, 0) }, Finish.Straw, 20);
            g.Ring(V(0, .86f, 0), .294f, .215f, .045f, Finish.OakDark, 20);
            for (int i = 0; i < 35; i++)
            {
                float r = .22f * Mathf.Sqrt((i + .5f) / 35), a = i * 2.4f;
                g.Rock(V(Mathf.Cos(a) * r, .868f - r * .08f, Mathf.Sin(a) * r), V(.065f, .031f, .053f), Finish.Garlic, i);
            }
            foreach (float y in new[] { .20f, .29f, .38f, .47f, .56f, .65f })
                g.Beam(V(-.032f, y, -.421f), V(.032f, y + .022f, -.421f), .015f, Finish.OakDark);
            B(g, V(0, .54f, -.44f), V(.21f, .19f, .016f), Finish.Parchment, .012f);
            B(g, V(0, .54f, -.452f), V(.055f, .09f, .008f), Finish.Moss);
            SaveFurniture("open_grain_sack_3d", g, V(0, .43f, 0), V(.82f, .86f, .82f));
        }

        private static void BuildCrockAndBowls()
        {
            var g = new Geometry(); Jar(g, Vector3.zero, 1.05f);
            g.Lathe(V(0, .82f, 0), new[] { V(.235f, 0, 0), V(.25f, .04f, 0), V(.22f, .075f, 0) }, Finish.Oak, 20);
            g.Rock(V(0, .93f, 0), V(.11f, .07f, .10f), Finish.OakDark, 3);
            SaveFurniture("lidded_spice_crock_3d", g, Vector3.zero, Vector3.zero);
            g = new Geometry();
            for (int i = 0; i < 5; i++) Bowl(g, V(-.36f, i * .075f, 0), 1);
            Bowl(g, V(.36f, .0f, .04f), 1, true);
            g.Beam(V(.26f, .23f, -.10f), V(.62f, .26f, -.25f), .035f, Finish.OakLight);
            SaveFurniture("stacked_bowls_and_stew_3d", g, Vector3.zero, Vector3.zero);
        }

        private static void BuildUtensils()
        {
            var g = new Geometry();
            B(g, V(0, 0, .045f), V(2.8f, .18f, .12f), Finish.OakDark);
            foreach (float x in new[] { -1.20f, -.6f, 0, .6f, 1.2f })
            {
                g.Beam(V(x, .05f, 0), V(x, -.14f, -.10f), .035f, Finish.Iron);
                g.Ring(V(x, -.17f, -.10f), .08f, .052f, .025f, Finish.Iron, 16, Quaternion.Euler(90, 0, 0));
            }
            // Cleaver, ladle, pan, fork and a chopping knife.
            B(g, V(-1.20f, -.35f, -.10f), V(.10f, .29f, .085f), Finish.Oak);
            B(g, V(-1.09f, -.65f, -.10f), V(.37f, .34f, .034f), Finish.Iron);
            B(g, V(-1.07f, -.81f, -.125f), V(.35f, .028f, .012f), Finish.Bone);
            g.Beam(V(-.60f, -.22f, -.10f), V(-.60f, -.68f, -.10f), .045f, Finish.OakLight);
            g.Lathe(V(-.6f, -.81f, -.12f), new[] { V(.04f, 0, 0), V(.14f, .07f, 0), V(.18f, .15f, 0), V(.14f, .15f, 0), V(.09f, .08f, 0) }, Finish.Iron, 20);
            g.Beam(V(0, -.22f, -.10f), V(0, -.50f, -.10f), .055f, Finish.Iron);
            g.Lathe(V(0, -.79f, -.09f), new[] { V(.23f, 0, 0), V(.27f, .06f, 0), V(.27f, .10f, 0) }, Finish.Charcoal, 24, Quaternion.Euler(90, 0, 0));
            g.Beam(V(.6f, -.22f, -.1f), V(.6f, -.66f, -.1f), .05f, Finish.Oak);
            foreach (float x in new[] { .51f, .60f, .69f }) g.Beam(V(x, -.63f, -.1f), V(x, -.85f, -.1f), .025f, Finish.Iron);
            B(g, V(1.2f, -.35f, -.10f), V(.09f, .29f, .08f), Finish.OakDark);
            g.Box(V(1.20f, -.63f, -.1f), V(.13f, .31f, .026f), Finish.Iron, Quaternion.Euler(0, 0, -7), .015f);
            SaveFurniture("cleavers_pan_and_ladle_rail_3d", g, Vector3.zero, Vector3.zero);
        }

        private static void BuildServingBench()
        {
            var g = new Geometry();
            foreach (float x in new[] { -1.32f, 1.32f }) foreach (float z in new[] { -.38f, .38f })
            {
                B(g, V(x, .61f, z), V(.16f, 1.22f, .16f), Finish.OakDark);
                B(g, V(x, .17f, z), V(.18f, .10f, .18f), Finish.Iron);
            }
            for (int i = 0; i < 5; i++) B(g, V(0, 1.25f, -.48f + i * .24f), V(3.10f, .15f, .22f), i % 2 == 0 ? Finish.Oak : Finish.OakLight);
            B(g, V(0, .31f, 0), V(2.75f, .10f, .89f), Finish.Oak);
            g.Beam(V(-1.32f, .35f, .38f), V(1.32f, 1.13f, .38f), .085f, Finish.OakDark);
            foreach (float x in new[] { -1.2f, 1.2f }) foreach (float z in new[] { -.45f, .45f })
                g.Rock(V(x, 1.337f, z), V(.045f, .023f, .045f), Finish.Iron, 1);
            SaveFurniture("rough_timber_serving_bench_3d", g, V(0, .65f, 0), V(3.10f, 1.30f, 1.15f));
        }

        private static void BuildVegetableTub()
        {
            var g = new Geometry();
            g.Lathe(Vector3.zero, new[] { V(.45f, 0, 0), V(.59f, .07f, 0), V(.67f, .46f, 0), V(.60f, .46f, 0), V(.53f, .09f, 0) }, Finish.Oak, 20);
            foreach (float y in new[] { .11f, .36f }) g.Ring(V(0, y, 0), .585f + y * .20f, .57f + y * .20f, .048f, Finish.Iron, 20);
            for (int i = 0; i < 14; i++)
            {
                float a = i * 2.4f, r = .44f * Mathf.Sqrt((i + 1f) / 15);
                Vector3 p = V(Mathf.Cos(a) * r, .32f + (i % 3) * .07f, Mathf.Sin(a) * r);
                g.Rock(p, V(.18f, .22f, .18f), i % 3 == 0 ? Finish.Garlic : i % 3 == 1 ? Finish.RedChip : Finish.Moss, i);
                g.Beam(p + V(0, .08f, 0), p + V(.10f, .24f, .035f), .045f, Finish.Moss);
            }
            SaveFurniture("roots_and_mushrooms_tub_3d", g, V(0, .27f, 0), V(1.22f, .54f, 1.22f));
        }

        private static void BuildHerbs()
        {
            var g = new Geometry();
            g.Beam(V(-1.45f, 0, 0), V(1.45f, 0, 0), .04f, Finish.Straw);
            for (int i = 0; i < 6; i++)
            {
                float x = -1.18f + i * .47f, length = .34f + (i % 3) * .13f;
                g.Beam(V(x, 0, 0), V(x, -length, -.05f), .022f, Finish.Straw);
                for (int n = 0; n < 5; n++)
                {
                    Vector3 p = V(x + Mathf.Cos(n * 2.4f) * .075f, -length - n * .085f, -.06f + Mathf.Sin(n * 2.4f) * .04f);
                    g.Rock(p, i % 2 == 0 ? V(.16f, .14f, .13f) : V(.18f, .30f, .07f), i % 2 == 0 ? Finish.Garlic : Finish.Moss, n);
                }
            }
            SaveFurniture("hanging_garlic_and_cave_herbs_3d", g, Vector3.zero, Vector3.zero);
        }

        private static void BuildPrepCart()
        {
            var g = new Geometry();
            foreach (float x in new[] { -.73f, .73f }) foreach (float z in new[] { -.42f, .42f })
                B(g, V(x, .63f, z), V(.10f, .85f, .10f), Finish.OakDark);
            for (int i = 0; i < 4; i++)
            {
                B(g, V(0, 1.09f, -.42f + i * .28f), V(1.93f, .105f, .265f), i % 2 == 0 ? Finish.OakLight : Finish.Oak);
                B(g, V(0, .42f, -.42f + i * .28f), V(1.55f, .08f, .265f), Finish.OakDark);
            }
            foreach (float z in new[] { -.44f, .44f })
            {
                g.Beam(V(-.88f, .23f, z), V(.88f, .23f, z), .073f, Finish.Iron);
                foreach (float x in new[] { -.88f, .88f })
                {
                    Quaternion q = Quaternion.Euler(0, 0, 90);
                    g.Ring(V(x, .23f, z), .227f, .171f, .065f, Finish.Iron, 24, q);
                    g.Lathe(V(x, .23f, z), new[] { V(.06f, 0, 0), V(.06f, .07f, 0) }, Finish.Brass, 16, q);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * Mathf.PI / 3;
                        g.Beam(V(x - .032f, .23f, z), V(x - .032f, .23f + Mathf.Cos(a) * .17f, z + Mathf.Sin(a) * .17f), .036f, Finish.Oak);
                    }
                }
            }
            foreach (float z in new[] { -.40f, .40f }) g.Beam(V(1.0f, .82f, z), V(1.2f, 1.09f, z), .055f, Finish.Iron);
            g.Beam(V(1.2f, 1.09f, -.40f), V(1.2f, 1.09f, .40f), .075f, Finish.OakDark);
            B(g, V(-.39f, 1.166f, 0), V(.93f, .05f, .73f), Finish.OakLight);
            g.Lathe(V(-.44f, 1.194f, .015f), new[] { V(.14f, 0, 0), V(.27f, .015f, 0), V(.24f, .095f, 0), V(.10f, .11f, 0) }, Finish.Meat, 20);
            foreach (float x in new[] { -.56f, -.43f, -.30f }) g.Beam(V(x, 1.297f, -.10f), V(x + .06f, 1.29f, .12f), .025f, Finish.Fat);
            B(g, V(-.71f, 1.21f, .23f), V(.28f, .035f, .13f), Finish.Iron);
            B(g, V(-.89f, 1.21f, .23f), V(.15f, .06f, .07f), Finish.OakDark);
            Bowl(g, V(.27f, 1.15f, -.15f), .66f, true);
            Jar(g, V(.22f, .48f, .04f), .62f);
            SaveFurniture("ingredient_prep_cart_3d", g, V(.1f, .66f, 0), V(2.18f, 1.32f, 1.2f));
        }

        private static void BuildCauldronHearth()
        {
            var g = new Geometry();
            g.Lathe(V(0, .01f, 0), new[] { V(1.27f, 0, 0), V(1.28f, .10f, 0) }, Finish.Charcoal, 32);
            for (int i = 0; i < 13; i++)
            {
                float a = i * Mathf.PI * 2 / 13;
                g.Box(V(Mathf.Cos(a) * 1.10f, .26f, Mathf.Sin(a) * 1.10f), V(.53f, .44f, .44f), i % 3 == 0 ? Finish.StoneDark : Finish.Stone, Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0), .075f);
            }
            for (int i = 0; i < 24; i++)
            {
                float a = i * 2.4f, r = .84f * Mathf.Sqrt((i + 1f) / 25);
                g.Rock(V(Mathf.Cos(a) * r, .19f, Mathf.Sin(a) * r), V(.23f, .15f, .19f), i % 3 == 0 ? Finish.Ember : Finish.Charcoal, i);
            }
            foreach (float x in new[] { -.60f, .60f }) g.Beam(V(x, .42f, -.66f), V(x, .42f, .66f), .08f, Finish.Iron);
            for (int i = 0; i < 4; i++) g.Rock(V(-.6f + i * .37f, .43f, -.52f), V(.13f, .39f, .15f), Finish.Flame, i);
            SaveFurniture("cauldron_stone_fire_ring_3d", g, V(0, .22f, 0), V(2.55f, .44f, 2.55f));
        }
    }
}
