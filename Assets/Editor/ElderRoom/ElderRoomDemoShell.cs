using UnityEngine;

namespace Untitled.Editor
{
    public static partial class ElderRoomDemoBuilder
    {
        private static void BuildShell(Transform room)
        {
            Transform shell = Group("01_StoneChamberAndMainEntrance", room);
            var foundation = new Geometry();
            foundation.Box(V(0, -.30f, 0), V(19.1f, .60f, 19.1f), Finish.StoneDark, Quaternion.identity, .15f);
            Transform floor = MeshObject("elder_floor_foundation", foundation, shell).transform;
            BoxCollision(floor, V(0, -.18f, 0), V(18.8f, .36f, 18.8f));
            for (int chunk = 0; chunk < 4; chunk++)
            {
                var tiles = new Geometry();
                for (int z = 0; z < 8; z++) for (int x = 0; x < 8; x++)
                {
                    float px = -8.4375f + x * 1.125f + (chunk % 2) * 9;
                    float pz = -8.4375f + z * 1.125f + (chunk / 2) * 9;
                    Finish f = (x + z * 3 + chunk) % 7 == 0 ? Finish.StoneLight : Finish.Stone;
                    tiles.Box(V(px, -.004f, pz), V(1.111f, .065f, 1.111f), f, Quaternion.identity, .028f);
                }
                MeshObject("elder_floor_tiles_" + chunk, tiles, shell);
            }
            Wall(shell, "elder_north_wall_before_secret_door", V(-1.5f, 0, 9), 15, 3.32f, 0);
            Wall(shell, "elder_north_secret_east_jamb", V(8.90f, 0, 9), .20f, 3.32f, 0);
            Wall(shell, "elder_west_upper_wall", V(-9, 0, 1.85f), 14.3f, 3.20f, 90);
            Wall(shell, "elder_west_entrance_lower_jamb", V(-9, 0, -8.65f), .70f, .65f, 90);
            Wall(shell, "elder_east_wall", V(9, 0, 0), 18, 2.65f, 90);
            Wall(shell, "elder_south_cutaway", V(0, 0, -9), 18, .42f, 0);
            var gate = new Geometry();
            foreach (float z in new[] { -5.35f, -8.15f })
            {
                gate.Box(V(-9, 1.54f, z), V(.30f, 3.08f, .30f), Finish.OakDark, Quaternion.identity, .04f);
                gate.Box(V(-9, .35f, z), V(.34f, .19f, .34f), Finish.Iron, Quaternion.identity, .02f);
            }
            gate.Box(V(-9, 3.13f, -6.75f), V(.38f, .30f, 3.24f), Finish.Oak, Quaternion.identity, .055f);
            gate.Box(V(-9.18f, .015f, -6.75f), V(1.85f, .11f, 2.78f), Finish.StoneLight, Quaternion.identity, .035f);
            Transform threshold = MeshObject("elder_main_entrance_open_frame", gate, shell).transform;
            BoxCollision(threshold, V(-9.18f, -.05f, -6.75f), V(1.85f, .1f, 2.78f));
            foreach (Vector3 p in new[] { V(-9, 0, 9), V(-9, 0, 0), V(9, 0, 3), V(9, 0, -5) })
            {
                var brace = new Geometry();
                brace.Box(p + V(0, 1.55f, 0), V(.26f, 3.10f, .26f), Finish.OakDark, Quaternion.identity, .035f);
                foreach (float y in new[] { .24f, 2.78f }) brace.Box(p + V(0, y, 0), V(.29f, .15f, .29f), Finish.Iron, Quaternion.identity, .015f);
                MeshObject("elder_timber_brace_" + p.x + "_" + p.z, brace, shell);
            }
            BuildSecretPassage(room);
        }

        private static void Wall(Transform parent, string name, Vector3 origin, float length, float height, float yaw)
        {
            var g = new Geometry();
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            int rows = Mathf.CeilToInt(height / .56f), columns = Mathf.CeilToInt(length / 1.32f);
            for (int row = 0; row < rows; row++) for (int col = 0; col < columns; col++)
            {
                float h = Mathf.Min(.52f, height - row * .56f);
                float x = -length * .5f + (col + .5f) * length / columns;
                g.Box(origin + q * V(x, row * .56f + h * .5f, 0), V(length / columns - .033f, h, .64f), (row + col) % 5 == 0 ? Finish.StoneLight : Finish.Stone, q, .064f);
            }
            for (int i = 0; i < columns; i++)
            {
                float x = -length * .5f + (i + .5f) * length / columns;
                g.Rock(origin + q * V(x, height + .13f, .08f), V(length / columns + .10f, .37f, .78f), Finish.StoneDark, i * .37f);
            }
            Transform wall = MeshObject(name, g, parent).transform;
            Transform blocker = Group("BoundaryCollider", wall);
            blocker.localPosition = origin;
            blocker.localRotation = q;
            BoxCollision(blocker, V(0, 1.80f, 0), V(length, 3.6f, .67f));
        }

        private static void BuildSecretPassage(Transform room)
        {
            Transform passage = Group("02_NorthEastSecretPassage", room);
            var g = new Geometry();
            g.Box(V(7.46f, -.10f, 10.87f), V(2.90f, .20f, 4.33f), Finish.StoneDark, Quaternion.identity, .035f);
            for (int i = 0; i < 6; i++)
                g.Box(V(7.46f, .012f, 9.12f + i * .68f), V(2.62f, .04f, .65f), i % 3 == 0 ? Finish.StoneDark : Finish.Stone, Quaternion.identity, .02f);
            g.Box(V(7.47f, 3.40f, 9), V(3.30f, .35f, .78f), Finish.StoneDark, Quaternion.identity, .08f);
            g.Box(V(7.47f, .035f, 8.78f), V(2.79f, .07f, .88f), Finish.StoneDark, Quaternion.identity, .028f);
            Transform floor = MeshObject("elder_secret_door_lintel_and_corridor_floor", g, passage).transform;
            BoxCollision(floor, V(7.46f, -.09f, 10.87f), V(2.9f, .18f, 4.33f));
            Wall(passage, "secret_corridor_west_wall", V(6.02f, 0, 11.16f), 4.3f, 3.16f, 90);
            Wall(passage, "secret_corridor_east_wall", V(8.90f, 0, 11.16f), 4.3f, 3.16f, 90);
            Wall(passage, "secret_corridor_demo_end", V(7.46f, 0, 13.25f), 2.9f, 3.16f, 0);
            Transform bookcase = Place(passage, "rune_bound_bookcase_3d", V(6.08f, .034f, 8.32f), -7);
            bookcase.name = "ConcealingBookcase_PartiallySlidAside";
            bookcase.localScale = V(.60f, .84f, .72f);
            var tracks = new Geometry();
            foreach (float z in new[] { 8.15f, 8.49f })
                tracks.Box(V(6.37f, .04f, z), V(3.60f, .012f, .027f), Finish.Charcoal, Quaternion.identity, .002f);
            tracks.Box(V(7.42f, .044f, 8.72f), V(.50f, .009f, .075f), Finish.StoneLight, Quaternion.Euler(0, -14, 0), .002f);
            MeshObject("elder_subtle_shelf_slide_marks", tracks, passage);
            Transform bounce = Group("DimColdLightInsideSecretPassage", passage);
            bounce.localPosition = V(7.78f, 1.38f, 11.57f);
            Light light = bounce.gameObject.AddComponent<Light>();
            light.type = LightType.Point; light.color = new Color(.20f, .43f, .39f); light.intensity = 1.05f; light.range = 3.6f;
        }

        private static void Rug(Transform parent, string name, Vector3 center, Vector2 size, Finish finish)
        {
            var g = new Geometry();
            g.Box(center, V(size.x, .035f, size.y), finish, Quaternion.identity, .05f);
            foreach (float x in new[] { -size.x * .5f + .14f, size.x * .5f - .14f })
                g.Box(center + V(x, .024f, 0), V(.043f, .012f, size.y - .20f), Finish.Brass, Quaternion.identity, .004f);
            foreach (float z in new[] { -size.y * .5f + .14f, size.y * .5f - .14f })
            {
                g.Box(center + V(0, .024f, z), V(size.x - .20f, .012f, .043f), Finish.Brass, Quaternion.identity, .004f);
                int count = Mathf.FloorToInt(size.x / .17f);
                for (int i = 0; i < count; i++)
                    g.Box(center + V(-size.x * .5f + .13f + i * .17f, -.005f, Mathf.Sign(z) * (size.y * .5f + .055f)), V(.028f, .012f, .15f), Finish.Straw, Quaternion.identity, .004f);
            }
            MeshObject(name, g, parent);
        }
    }
}
