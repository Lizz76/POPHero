using UnityEngine;

namespace POPHero
{
    public class ArenaSurfaceMarker : MonoBehaviour
    {
        public ArenaSurfaceType surfaceType;
    }

    public enum ArenaSurfaceType
    {
        Top,
        Bottom,
        Left,
        Right,
        Block
    }
}
