using UnityEngine;

namespace Untitled.Editor
{
    public static partial class ElderRoomDemoBuilder
    {
        private static void PrepareFurniture()
        {
            BuildBookcase();
            BuildDesk();
            BuildAltar();
            BuildCabinet();
            BuildBedsideChest();
            BuildRelics();
            BuildSoftFurnishings();
        }

        private static void Book(Geometry g, Vector3 p, float width, float height, float depth, Finish cover, float lean = 0)
        {
            Quaternion q = Quaternion.Euler(0, 0, lean);
            Vector3 P(float x, float y, float z) => p + q * V(x, y, z);
            g.Box(P(0, height * .5f, 0), V(width - .025f, height - .04f, depth - .03f), Finish.Parchment, q, .008f);
            foreach (float x in new[] { -width * .5f, width * .5f })
                g.Box(P(x, height * .5f, 0), V(.028f, height, depth), cover, q, .008f);
            g.Box(P(0, height * .5f, -depth * .5f), V(width + .018f, height, .04f), cover, q, .012f);
            foreach (float y in new[] { height * .18f, height * .82f })
                g.Box(P(0, y, -depth * .5f - .027f), V(width + .025f, .027f, .016f), Finish.Brass, q, .003f);
            g.Box(P(0, height * .55f, -depth * .5f - .027f), V(width * .46f, height * .20f, .014f), Finish.Parchment, q, .004f);
        }

        private static void ClosedBook(Geometry g, Vector3 p, Vector3 size, Finish cover, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            g.Box(p + V(0, size.y * .5f, 0), size - V(.025f, .025f, .025f), Finish.Parchment, q, .012f);
            foreach (float y in new[] { .012f, size.y - .012f })
                g.Box(p + V(0, y, 0), V(size.x, .026f, size.z), cover, q, .015f);
            g.Box(p + q * V(-size.x * .5f, size.y * .5f, 0), V(.03f, size.y, size.z), cover, q, .008f);
            g.Box(p + V(0, size.y + .008f, 0), V(size.x * .4f, .01f, size.z * .45f), Finish.Brass, q, .03f);
        }

        private static void Scroll(Geometry g, Vector3 p, float yaw, float length = .66f)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 90);
            g.Lathe(p, new[] { V(.076f, -length * .5f, 0), V(.076f, length * .5f, 0) }, Finish.Parchment, 16, q);
            g.Ring(p, .083f, .074f, .044f, Finish.Purple, 16, q);
            foreach (float end in new[] { -length * .5f - .025f, length * .5f + .025f })
                g.Lathe(p + q * V(0, end, 0), new[] { V(.053f, -.035f, 0), V(.053f, .035f, 0) }, Finish.OakDark, 12, q);
        }

        private static void Bottle(Geometry g, Vector3 p, float scale, Finish finish)
        {
            g.Lathe(p, new[] { V(.12f * scale, 0, 0), V(.19f * scale, .09f * scale, 0), V(.19f * scale, .31f * scale, 0), V(.075f * scale, .41f * scale, 0), V(.075f * scale, .52f * scale, 0) }, finish, 16);
            g.Lathe(p + V(0, .50f * scale, 0), new[] { V(.075f * scale, 0, 0), V(.07f * scale, .09f * scale, 0) }, Finish.Cork, 12);
            g.Box(p + V(0, .23f * scale, -.194f * scale), V(.15f * scale, .15f * scale, .016f), Finish.Parchment, Quaternion.identity, .018f);
        }

        private static void Candle(Geometry g, Vector3 p, float height)
        {
            g.Lathe(p, new[] { V(.13f, 0, 0), V(.14f, .04f, 0), V(.075f, .08f, 0) }, Finish.Brass, 20);
            g.Lathe(p + V(0, .07f, 0), new[] { V(.065f, 0, 0), V(.065f, height, 0) }, Finish.Wax, 16);
            for (int i = 0; i < 3; i++)
                g.Rock(p + V(Mathf.Sin(i * 2.1f) * .062f, height * (.7f + i * .07f), Mathf.Cos(i * 2.1f) * .062f), V(.035f, .13f, .035f), Finish.Wax, i);
            g.Lathe(p + V(0, height + .07f, 0), new[] { V(.025f, 0, 0), V(.050f, .065f, 0), V(0, .20f, 0) }, Finish.Flame, 10);
        }

        private static void Skull(Geometry g, Vector3 p, float scale)
        {
            g.Rock(p + V(0, .26f, 0) * scale, V(.65f, .53f, .48f) * scale, Finish.Bone, 2);
            foreach (float x in new[] { -.17f, .17f })
                g.Rock(p + V(x, .29f, -.216f) * scale, V(.17f, .17f, .06f) * scale, Finish.Charcoal, x);
            g.Rock(p + V(0, .15f, -.25f) * scale, V(.085f, .12f, .035f) * scale, Finish.Charcoal, 3);
            for (int i = 0; i < 5; i++)
                g.Box(p + V(-.15f + i * .075f, .05f, -.20f) * scale, V(.052f, .10f, .085f) * scale, Finish.Bone, Quaternion.identity, .01f);
            foreach (float x in new[] { -.27f, .27f })
                g.Lathe(p + V(x, .42f, .025f) * scale, new[] { V(.08f * scale, 0, 0), V(.055f * scale, .25f * scale, 0), V(0, .43f * scale, 0) }, Finish.Bone, 10, Quaternion.Euler(0, 0, x < 0 ? 30 : -30));
        }

        private static void BuildBookcase()
        {
            var g = new Geometry();
            g.Box(V(0, 1.9f, .40f), V(4.08f, 3.55f, .10f), Finish.OakDark, Quaternion.identity, .035f);
            foreach (float x in new[] { -1.98f, 1.98f })
            {
                g.Box(V(x, 1.85f, 0), V(.22f, 3.7f, .95f), Finish.Oak, Quaternion.identity, .045f);
                foreach (float y in new[] { .40f, 3.28f })
                    g.Box(V(x, y, -.51f), V(.23f, .16f, .08f), Finish.Brass, Quaternion.identity, .015f);
                g.Box(V(x, .12f, 0), V(.35f, .24f, 1.03f), Finish.OakDark, Quaternion.identity, .05f);
            }
            for (int row = 0; row < 4; row++)
            {
                float y = .36f + row * .85f;
                g.Box(V(0, y, 0), V(4.12f, .12f, .96f), Finish.OakLight, Quaternion.identity, .028f);
                g.Box(V(0, y, -.51f), V(4.05f, .10f, .055f), Finish.Brass, Quaternion.identity, .008f);
                for (int i = 0; i < 10; i++)
                {
                    if (row == 2 && i > 6) continue;
                    if (row == 3 && i < 3) continue;
                    Finish cover = new[] { Finish.Purple, Finish.Teal, Finish.Burgundy, Finish.OakDark, Finish.Cloth }[(i + row) % 5];
                    Book(g, V(-1.68f + i * .36f, y + .075f, -.035f), .22f + (i % 3) * .024f, .53f + (i % 4) * .055f, .62f, cover, i % 5 == 0 ? -7 : i % 5 == 4 ? 6 : 0);
                }
            }
            for (int i = 0; i < 3; i++) Scroll(g, V(-1.22f + i * .16f, 3.15f, -.025f), 90);
            Skull(g, V(1.35f, 2.16f, -.08f), .7f);
            g.Box(V(0, 3.78f, 0), V(4.38f, .23f, 1.03f), Finish.OakDark, Quaternion.identity, .065f);
            for (int i = 0; i < 7; i++)
            {
                float x = -.90f + i * .3f;
                g.Beam(V(x - .08f, 3.75f, -.532f), V(x, 3.83f, -.532f), .024f, Finish.Brass);
                g.Beam(V(x, 3.83f, -.532f), V(x + .08f, 3.75f, -.532f), .024f, Finish.Brass);
            }
            SaveFurniture("rune_bound_bookcase_3d", g, V(0, 1.94f, 0), V(4.38f, 3.88f, 1.03f));
        }

        private static void BuildDesk()
        {
            var g = new Geometry();
            foreach (float x in new[] { -1.5f, 1.5f }) foreach (float z in new[] { -.67f, .67f })
            {
                g.Box(V(x, .73f, z), V(.20f, 1.46f, .20f), Finish.OakDark, Quaternion.identity, .025f);
                g.Box(V(x, .22f, z), V(.23f, .18f, .23f), Finish.Brass, Quaternion.identity, .017f);
            }
            g.Beam(V(-1.50f, .31f, .67f), V(1.5f, .31f, .67f), .13f, Finish.Oak);
            g.Box(V(-1.30f, 1.02f, 0), V(.80f, .71f, 1.44f), Finish.OakDark, Quaternion.identity, .04f);
            for (int i = 0; i < 2; i++)
            {
                g.Box(V(-1.30f, .86f + i * .32f, -.76f), V(.68f, .27f, .065f), Finish.Oak, Quaternion.identity, .025f);
                g.Ring(V(-1.30f, .86f + i * .32f, -.813f), .065f, .043f, .025f, Finish.Brass, 16, Quaternion.Euler(90, 0, 0));
            }
            for (int i = 0; i < 5; i++)
                g.Box(V(0, 1.47f, -.80f + i * .40f), V(3.8f, .20f, .385f), i % 2 == 0 ? Finish.Oak : Finish.OakLight, Quaternion.identity, .035f);
            foreach (float x in new[] { -1.81f, 1.81f })
            {
                g.Box(V(x, 1.59f, 0), V(.11f, .06f, 1.90f), Finish.Iron, Quaternion.identity, .012f);
                foreach (float z in new[] { -.70f, .70f }) g.Bolt(V(x, 1.633f, z), .038f);
            }
            g.Box(V(0, 1.586f, 0), V(1.86f, .025f, 1.17f), Finish.Purple, Quaternion.identity, .026f);
            foreach (float x in new[] { -.39f, .39f })
            {
                Quaternion q = Quaternion.Euler(0, 0, x < 0 ? 5 : -5);
                g.Box(V(x, 1.64f, -.08f), V(.78f, .05f, .92f), Finish.OakDark, q, .025f);
                g.Box(V(x, 1.68f, -.08f), V(.74f, .028f, .86f), Finish.Parchment, q, .012f);
                for (int i = 0; i < 7; i++) g.Box(V(x, 1.71f, -.36f + i * .095f), V(.52f - (i % 3) * .07f, .009f, .018f), Finish.Cork, Quaternion.identity, .002f);
            }
            g.Box(V(.09f, 1.719f, .10f), V(.045f, .018f, .69f), Finish.Burgundy, Quaternion.identity, .002f);
            ClosedBook(g, V(-1.05f, 1.58f, .48f), V(.65f, .14f, .75f), Finish.Teal, -12);
            ClosedBook(g, V(-1.05f, 1.72f, .49f), V(.54f, .13f, .63f), Finish.Burgundy, 8);
            Scroll(g, V(.97f, 1.67f, .52f), 10);
            g.Lathe(V(.90f, 1.58f, -.39f), new[] { V(.11f, 0, 0), V(.13f, .16f, 0), V(.065f, .22f, 0) }, Finish.Iron, 16);
            g.Beam(V(.94f, 1.75f, -.39f), V(1.15f, 2.12f, -.28f), .022f, Finish.Bone);
            g.Box(V(1.14f, 2.02f, -.28f), V(.11f, .30f, .018f), Finish.Bone, Quaternion.Euler(0, 0, -28), .015f);
            SaveFurniture("elder_manuscript_desk_3d", g, V(0, .79f, 0), V(3.8f, 1.58f, 1.96f));
        }

        private static void BuildAltar()
        {
            var g = new Geometry();
            foreach (float x in new[] { -1.13f, 1.13f })
            {
                g.Box(V(x, .52f, 0), V(.42f, 1.04f, 1.14f), Finish.StoneDark, Quaternion.identity, .085f);
                g.Box(V(x, .13f, 0), V(.59f, .26f, 1.26f), Finish.StoneLight, Quaternion.identity, .06f);
            }
            g.Box(V(0, 1.10f, 0), V(3.12f, .23f, 1.48f), Finish.Stone, Quaternion.identity, .12f);
            g.Box(V(0, 1.24f, 0), V(1.63f, .035f, 1.49f), Finish.Purple, Quaternion.identity, .03f);
            g.Box(V(0, .80f, -.76f), V(1.62f, .87f, .042f), Finish.Purple, Quaternion.identity, .027f);
            foreach (float x in new[] { -.72f, .72f }) g.Box(V(x, .80f, -.788f), V(.035f, .85f, .015f), Finish.Brass, Quaternion.identity, .003f);
            g.Beam(V(-.26f, .69f, -.802f), V(0, .95f, -.802f), .045f, Finish.Brass);
            g.Beam(V(0, .95f, -.802f), V(.26f, .69f, -.802f), .045f, Finish.Brass);
            g.Beam(V(-.26f, .69f, -.802f), V(.26f, .69f, -.802f), .045f, Finish.Brass);
            g.Box(V(0, 1.39f, .22f), V(.90f, .27f, .82f), Finish.OakDark, Quaternion.identity, .055f);
            Skull(g, V(0, 1.55f, .16f), 1.25f);
            Candle(g, V(-1.20f, 1.23f, -.32f), .36f);
            Candle(g, V(-.88f, 1.23f, -.23f), .21f);
            Candle(g, V(1.13f, 1.23f, -.34f), .46f);
            g.Lathe(V(.69f, 1.23f, -.32f), new[] { V(.23f, 0, 0), V(.30f, .12f, 0), V(.24f, .20f, 0) }, Finish.Brass, 24);
            g.Lathe(V(.69f, 1.428f, -.32f), new[] { V(.20f, 0, 0), V(.20f, .016f, 0) }, Finish.Ember, 20);
            SaveFurniture("bone_relic_ritual_altar_3d", g, V(0, .61f, 0), V(3.12f, 1.22f, 1.48f));
        }

        private static void BuildCabinet()
        {
            var g = new Geometry();
            g.Box(V(0, 1.56f, 0), V(2.36f, 3.12f, 1.1f), Finish.OakDark, Quaternion.identity, .09f);
            for (int i = 0; i < 8; i++)
                g.Box(V(-1.02f + i * .292f, 1.57f, -.58f), V(.277f, 2.83f, .06f), i % 3 == 0 ? Finish.OakLight : Finish.Oak, Quaternion.identity, .016f);
            foreach (float y in new[] { .30f, 2.88f }) g.Box(V(0, y, -.63f), V(2.18f, .11f, .08f), Finish.Iron, Quaternion.identity, .018f);
            foreach (float x in new[] { -.10f, .10f }) g.Ring(V(x, 1.44f, -.725f), .066f, .044f, .03f, Finish.Brass, 16, Quaternion.Euler(90, 0, 0));
            g.Box(V(0, 3.19f, 0), V(2.56f, .17f, 1.26f), Finish.OakLight, Quaternion.identity, .05f);
            g.Box(V(0, .14f, 0), V(2.53f, .28f, 1.25f), Finish.Oak, Quaternion.identity, .055f);
            foreach (float x in new[] { -.97f, .97f }) foreach (float y in new[] { .33f, 2.85f }) g.Bolt(V(x, y, -.69f), .04f);
            SaveFurniture("elder_robe_wardrobe_3d", g, V(0, 1.63f, 0), V(2.56f, 3.28f, 1.26f));
        }

        private static void BuildBedsideChest()
        {
            var g = new Geometry();
            foreach (float x in new[] { -.57f, .57f }) foreach (float z in new[] { -.40f, .40f })
                g.Box(V(x, .40f, z), V(.14f, .80f, .14f), Finish.OakDark, Quaternion.identity, .018f);
            g.Box(V(0, .60f, 0), V(1.28f, .47f, .92f), Finish.Oak, Quaternion.identity, .05f);
            for (int i = 0; i < 3; i++) g.Box(V(0, .87f, -.36f + i * .36f), V(1.43f, .12f, .34f), Finish.OakLight, Quaternion.identity, .023f);
            g.Box(V(0, .63f, -.49f), V(1.06f, .31f, .075f), Finish.OakDark, Quaternion.identity, .025f);
            g.Ring(V(0, .66f, -.56f), .065f, .044f, .028f, Finish.Brass, 16, Quaternion.Euler(90, 0, 0));
            SaveFurniture("carved_bedside_chest_3d", g, V(0, .46f, 0), V(1.43f, .93f, 1.04f));
        }

        private static void BuildRelics()
        {
            var g = new Geometry();
            ClosedBook(g, V(-.27f, 0, .10f), V(.70f, .13f, .62f), Finish.Purple, -10);
            ClosedBook(g, V(-.28f, .13f, .1f), V(.57f, .12f, .51f), Finish.Teal, 7);
            Scroll(g, V(.37f, .095f, -.10f), -18);
            Scroll(g, V(.43f, .23f, -.06f), 18);
            Bottle(g, V(.26f, 0, .40f), .65f, Finish.Bottle);
            SaveFurniture("books_scrolls_and_ink_3d", g, Vector3.zero, Vector3.zero);
            var bottles = new Geometry();
            Bottle(bottles, V(-.29f, 0, 0), 1.0f, Finish.Bottle);
            Bottle(bottles, V(.12f, 0, -.10f), .68f, Finish.Purple);
            Bottle(bottles, V(.36f, 0, .22f), .82f, Finish.Teal);
            g = bottles;
            g.Box(V(-.40f, .015f, .32f), V(.23f, .025f, .18f), Finish.Parchment, Quaternion.Euler(0, 25, 0), .012f);
            SaveFurniture("herbal_tincture_bottles_3d", g, Vector3.zero, Vector3.zero);
            var staff = new Geometry();
            staff.Beam(V(0, .03f, 0), V(.1f, 2.9f, .04f), .11f, Finish.OakDark);
            foreach (float y in new[] { .42f, 1.52f, 2.73f }) staff.Ring(V(y * .032f, y, .02f), .083f, .053f, .11f, Finish.Brass, 16);
            staff.Ring(V(.11f, 3.13f, .04f), .36f, .28f, .065f, Finish.Bone, 28, Quaternion.Euler(90, 0, 0));
            staff.Rock(V(.11f, 3.13f, .06f), V(.25f, .46f, .21f), Finish.Crystal, 4);
            for (int i = 0; i < 3; i++)
            {
                staff.Beam(V(-.16f + i * .18f, 2.88f, .04f), V(-.18f + i * .18f, 2.50f - i * .06f, .04f), .014f, Finish.Cork);
                staff.Rock(V(-.18f + i * .18f, 2.49f - i * .06f, .04f), V(.09f, .15f, .07f), Finish.Bone, i);
            }
            SaveFurniture("elder_bone_sceptre_3d", staff, Vector3.zero, Vector3.zero);
        }

        private static void BuildSoftFurnishings()
        {
            var g = new Geometry();
            g.Lathe(Vector3.zero, new[] { V(2.07f, .01f, 0), V(2.13f, .027f, 0), V(2.05f, .043f, 0) }, Finish.Purple, 80);
            g.Ring(V(0, .045f, 0), 1.92f, 1.85f, .008f, Finish.Brass, 80);
            g.Ring(V(0, .045f, 0), 1.70f, 1.67f, .008f, Finish.Teal, 72);
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI / 8;
                g.Box(V(Mathf.Sin(a) * 1.78f, .051f, Mathf.Cos(a) * 1.78f), V(.09f, .009f, .09f), Finish.Bone, Quaternion.Euler(0, 45, 0), .015f);
                g.Beam(V(Mathf.Sin(a) * 2.03f, .022f, Mathf.Cos(a) * 2.03f), V(Mathf.Sin(a) * 2.19f, .022f, Mathf.Cos(a) * 2.19f), .015f, Finish.Straw);
            }
            foreach (float yaw in new[] { 0f, 120f, 240f })
            {
                Quaternion q = Quaternion.Euler(0, yaw, 0);
                g.Beam(q * V(-.57f, .052f, -.38f), q * V(0, .052f, .62f), .052f, Finish.Brass);
            }
            SaveFurniture("ritual_woven_round_rug_3d", g, Vector3.zero, Vector3.zero);
            var cushion = new Geometry();
            cushion.Lathe(Vector3.zero, new[] { V(.53f, .03f, 0), V(.60f, .13f, 0), V(.52f, .28f, 0), V(.19f, .31f, 0) }, Finish.Teal, 32);
            cushion.Ring(V(0, .13f, 0), .607f, .59f, .021f, Finish.Brass, 32);
            cushion.Rock(V(0, .32f, 0), V(.06f, .025f, .06f), Finish.Brass, 0);
            SaveFurniture("elder_meditation_cushion_3d", cushion, Vector3.zero, Vector3.zero);
        }
    }
}
