using UnityEditor;
using UnityEngine;

namespace Untitled.Editor
{
    public static partial class ElderRoomDemoBuilder
    {
        private static void BuildStudy(Transform room)
        {
            Transform study = Group("03_ManuscriptStudyAndLibrary", room);
            Rug(study, "elder_study_woven_rug", V(-4.60f, .054f, 2.95f), new Vector2(6.25f, 5.75f), Finish.Teal);
            Place(study, "rune_bound_bookcase_3d", V(-5.56f, .028f, 7.98f));
            Place(study, "rune_bound_bookcase_3d", V(-8.02f, .028f, 3.02f), -90, .80f);
            Place(study, "elder_manuscript_desk_3d", V(-4.55f, .083f, 2.54f));
            Reuse(study, "Casino/boss_bone_crest_chair_3d", V(-4.55f, .086f, 4.40f), .93f);
            Reuse(study, "Tavern/wooden_stool_3d", V(-6.18f, .086f, .65f), .75f);
            Place(study, "books_scrolls_and_ink_3d", V(-6.97f, .028f, 6.63f), 17, 1.12f);
            Place(study, "books_scrolls_and_ink_3d", V(-6.90f, .32f, 6.63f), -9, .92f);
            Place(study, "elder_bone_sceptre_3d", V(-2.82f, .08f, 4.68f), 7);
            SetLamp(Reuse(study, "Tavern/oil_lantern_3d", V(-6.06f, 1.655f, 2.07f), .52f), 2.6f, 5.9f);
            Reuse(study, "TotemsBones/rune_banner_hd2d", V(-4.82f, 1.12f, 8.62f), 1.25f);
        }

        private static void BuildSleeping(Transform room)
        {
            Transform sleeping = Group("04_NorthEastSleepingAlcove", room);
            Rug(sleeping, "elder_sleeping_woven_rug", V(3.85f, .054f, 5.33f), new Vector2(4.9f, 5.65f), Finish.Purple);
            Transform socket = Group("LargeBedReplacementSocket", sleeping);
            socket.localPosition = V(3.65f, .082f, 5.90f);
            Transform bed = Reuse(socket, "Camp/wooden_bed_hd2d", Vector3.zero, 2.15f);
            bed.name = "ExistingWoodenBed_Enlarged_2_15x";
            var prop = bed.GetComponent<HybridProp>();
            var settings = new SerializedObject(prop);
            settings.FindProperty("directionFrame").intValue = 1;
            settings.ApplyModifiedPropertiesWithoutUndo();
            prop.ApplyVisual();
            Place(sleeping, "carved_bedside_chest_3d", V(1.83f, .08f, 6.73f));
            SetLamp(Reuse(sleeping, "Tavern/oil_lantern_3d", V(1.82f, 1.012f, 6.72f), .48f), 2.1f, 4.7f);
            Place(sleeping, "elder_robe_wardrobe_3d", V(7.81f, .028f, 4.94f), 90);
            Place(sleeping, "books_scrolls_and_ink_3d", V(1.48f, .083f, 4.22f), 14, .72f);
            Reuse(sleeping, "Tavern/supply_crate_3d", V(5.97f, .065f, 3.10f), .65f, -8);
            Place(sleeping, "elder_meditation_cushion_3d", V(4.93f, .084f, 3.17f), 0, .70f);
            Reuse(sleeping, "TotemsBones/rune_shield_hd2d", V(3.6f, 1.91f, 8.62f), 1.10f);
        }

        private static void BuildRitual(Transform room)
        {
            Transform ritual = Group("05_AncestorShrineAndMeditation", room);
            Place(ritual, "bone_relic_ritual_altar_3d", V(-.49f, .035f, 7.28f));
            Reuse(ritual, "TotemsBones/beast_totem_hd2d", V(-.45f, 2.01f, 8.58f), 1.33f);
            Place(ritual, "ritual_woven_round_rug_3d", V(.73f, .040f, -.28f));
            Place(ritual, "elder_meditation_cushion_3d", V(.73f, .10f, -.28f));
            Place(ritual, "elder_meditation_cushion_3d", V(-.39f, .085f, -1.61f), 0, .70f);
            Place(ritual, "elder_meditation_cushion_3d", V(1.87f, .085f, -1.61f), 0, .70f);
            SetLamp(Reuse(ritual, "Lighting/bone_fire_stand_hd2d", V(-1.59f, .028f, 5.76f), 1.05f), 2.3f, 5.9f);
            var g = new Geometry();
            Candle(g, V(-.95f, .10f, .56f), .28f);
            Candle(g, V(2.25f, .10f, .58f), .38f);
            g.Lathe(V(.74f, .086f, 1.32f), new[] { V(.28f, 0, 0), V(.41f, .16f, 0), V(.31f, .22f, 0) }, Finish.Brass, 28);
            g.Rock(V(.74f, .24f, 1.32f), V(.39f, .035f, .34f), Finish.Ember, 1);
            MeshObject("elder_meditation_candles_and_incense_bowl", g, ritual);
        }

        private static void BuildTreasury(Transform room)
        {
            Transform treasury = Group("06_PrivateVaultAndOfferings", room);
            Rug(treasury, "elder_vault_rug", V(6.25f, .054f, -4.21f), new Vector2(4.35f, 4.65f), Finish.Teal);
            Reuse(treasury, "Casino/riveted_vault_safe_3d", V(6.40f, .083f, -2.85f), .82f);
            Reuse(treasury, "Casino/stacked_gold_reserve_3d", V(6.34f, .083f, -5.37f), .70f, 8);
            foreach (Vector3 p in new[] { V(4.88f, .08f, -2.75f), V(7.96f, .08f, -4.83f), V(4.74f, .08f, -5.33f) })
                Reuse(treasury, "Casino/tied_coin_sack_3d", p, .95f, 14);
            Reuse(treasury, "Tavern/supply_crate_3d", V(7.65f, .03f, -7.18f), .72f, -10);
            Place(treasury, "books_scrolls_and_ink_3d", V(7.65f, .68f, -7.18f), 6, .82f);
            SetLamp(Reuse(treasury, "Tavern/oil_lantern_3d", V(4.56f, .08f, -4.37f), .65f), 2.1f, 4.8f);
        }

        private static void BuildClutter(Transform room)
        {
            Transform visitors = Group("07_VisitorsTeaAndHerbalSupplies", room);
            Rug(visitors, "elder_visitor_mat", V(-4.29f, .051f, -4.07f), new Vector2(4.10f, 4.12f), Finish.Burgundy);
            Reuse(visitors, "Tavern/round_table_3d", V(-4.25f, .079f, -3.82f), .65f);
            Reuse(visitors, "Tavern/wooden_stool_3d", V(-5.87f, .081f, -4.36f), .85f);
            Reuse(visitors, "Tavern/wooden_stool_3d", V(-2.55f, .081f, -3.31f), .85f);
            Place(visitors, "herbal_tincture_bottles_3d", V(-4.14f, 1.096f, -3.86f), 15);
            Place(visitors, "books_scrolls_and_ink_3d", V(-4.75f, 1.096f, -3.65f), -20, .58f);
            SetLamp(Reuse(visitors, "Tavern/oil_lantern_3d", V(-3.54f, 1.096f, -4.10f), .46f), 2.0f, 4.8f);
            Transform stores = Group("08_WallStorageAndPersonalObjects", room);
            Reuse(stores, "Tavern/wine_rack_3d", V(-8.01f, .026f, -1.72f), .77f, -90);
            Reuse(stores, "Tavern/wall_bench_3d", V(.53f, .028f, -7.80f), .93f);
            Place(stores, "books_scrolls_and_ink_3d", V(.32f, .858f, -7.80f), -5, .85f);
            foreach (Vector3 p in new[] { V(-7.68f, .028f, -3.21f), V(-7.78f, .028f, -4.61f), V(-7.73f, .74f, -3.23f) })
                Reuse(stores, "Tavern/supply_crate_3d", p, .71f, p.z * 3);
            Reuse(stores, "Tavern/ale_barrel_3d", V(7.67f, .028f, .42f), .64f, 8);
            Reuse(stores, "Kitchen/stone_cistern_narrow_3d", V(7.79f, .028f, 1.91f), .68f, 90);
            Place(stores, "herbal_tincture_bottles_3d", V(-7.75f, 1.44f, -3.21f), -8, .8f);
            Rug(stores, "elder_main_entrance_runner", V(-6.03f, .057f, -6.77f), new Vector2(5.84f, 1.55f), Finish.Purple);
            var debris = new Geometry();
            for (int i = 0; i < 18; i++)
            {
                float x = i % 2 == 0 ? 8.41f : -8.45f, z = -8.1f + (i / 2) * 1.77f;
                if (x < 0 && z < -5) continue;
                debris.Rock(V(x, .074f, z), V(.18f, .16f, .32f), Finish.StoneDark, i);
                if (i % 3 == 0) debris.Rock(V(x, .034f, z + .15f), V(.25f, .05f, .43f), Finish.Moss, i);
            }
            MeshObject("elder_wall_edge_rubble_and_moss", debris, stores);
        }

        private static void BuildLights(Transform room)
        {
            Transform lights = Group("09_OilLampsAndWallFire", room);
            foreach (float x in new[] { -7.75f, -2.40f, 2.16f })
                SetLamp(Reuse(lights, "Tavern/wall_brazier_3d", V(x, 2.05f, 8.64f), .70f), 2.35f, 5.8f);
            foreach (float z in new[] { -2.34f, 3.0f })
                SetLamp(Reuse(lights, "Tavern/wall_brazier_3d", V(8.66f, 1.97f, z), .70f, 90), 2.2f, 5.6f);
            SetLamp(Reuse(lights, "Tavern/wall_brazier_3d", V(-8.66f, 1.94f, -4.85f), .70f, -90), 2.2f, 5.5f);
            SetLamp(Reuse(lights, "Tavern/oil_lantern_3d", V(1.81f, .887f, -7.80f), .53f), 1.9f, 4.7f);
            SetLamp(Reuse(lights, "Tavern/iron_chandelier_3d", V(-.13f, 4.17f, 2.20f), .48f), 3.8f, 9.0f, true);
        }
    }
}
