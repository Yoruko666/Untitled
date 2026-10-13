using System.Collections.Generic;
using UnityEngine;

namespace Untitled.Editor
{
    public static partial class CasinoDemoBuilder
    {
        // Keep this scene's Finish enum and palette policy out of the shared mesh builder.
        private sealed class Geometry
        {
            private readonly ProceduralMeshGeometry geometry = new ProceduralMeshGeometry();

            public IEnumerable<int> UsedMaterials => geometry.UsedMaterials;

            public void Box(Vector3 center, Vector3 size, Finish finish, Quaternion rotation, float bevel = 0.025f)
                => geometry.Box(center, size, (int)finish, rotation, bevel);

            public void Beam(Vector3 from, Vector3 to, float width, Finish finish)
                => geometry.Beam(from, to, width, (int)finish);

            public void Lathe(Vector3 origin, Vector3[] profile, Finish finish, int sides, Quaternion? rotation = null)
                => geometry.Lathe(origin, profile, (int)finish, sides, rotation);

            public void Ring(Vector3 origin, float outer, float inner, float height, Finish finish, int sides, Quaternion? rotation = null)
                => geometry.Ring(origin, outer, inner, height, (int)finish, sides, rotation);

            public void Rock(Vector3 center, Vector3 size, Finish finish, float seed)
                => geometry.Rock(center, size, (int)finish, seed);

            public void SectorSide(float from, float to, float radius0, float radius1, float height0, float height1, Finish finish)
                => geometry.SectorSide(from, to, radius0, radius1, height0, height1, (int)finish);

            public void Bolt(Vector3 center, float radius)
                => Rock(center, V(radius * 2, radius * 2, radius * 2), Finish.Iron, 0);

            public void RoundPlanks(Vector3 center, float radius, float thickness, int count)
                => geometry.RoundPlanks(center, radius, thickness, count, (int)Finish.Oak, (int)Finish.OakLight, (int)Finish.OakDark);

            public Mesh ToMesh(string name) => geometry.ToMesh(name);
        }
    }
}
