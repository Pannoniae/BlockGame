using System.Numerics;
using Plane = System.Numerics.Plane;

namespace BlockGame.util;

public static class PlaneExtensions {
    /// <summary>
    /// -1 if on the back side, 0 if on the plane, 1 if on the front side
    /// </summary>
    /// <param name="plane"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    public static int planeSide(this Plane plane, Vector3 point) {
        float dist = Vector3.Dot(plane.Normal, point) + plane.D;
        if (dist == 0.0F) {
            return 0;
        }
        return dist < 0.0F ? -1 : 1;
    }
}