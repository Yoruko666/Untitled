using UnityEngine;

namespace Untitled
{
    // Scene configuration for derived terrain; the saved Tilemaps remain the layout source.
    [DisallowMultipleComponent]
    public sealed class CaveTerrainStyle : MonoBehaviour
    {
        public Material Soil;
        public Material Settlement;
        public Material WetGround;
        public Material Hall;
        public Material RockCap;
        [Min(1f)] public float RockHeight = 2.6f;
        [Min(0f)] public float RockVariation = 1.2f;
        [Range(2, 12)] public int RockMargin = 6;
        public int Seed = 731;
        public bool NaturalOutline;
    }
}
