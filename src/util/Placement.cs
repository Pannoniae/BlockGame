using Molten.DoublePrecision;

namespace BlockGame.util;

public struct Placement(RawDirection face, RawDirection facing, RawDirectionH hfacing, Vector3D hitPoint) {

    // NOTE the fields are laid out in a better-aligned order for perf, constructor doesn't match on purpose

    /** The hit point of the raycast */
    public Vector3D hitPoint = hitPoint;
    /** The clicked block face */
    public RawDirection face = face;
    /** The facing of the player */
    public RawDirection facing = facing;
    public RawDirectionH hfacing = hfacing;

}