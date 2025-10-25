using BlockGame.util;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

public class Cow : Entity {
    public Cow(World world) : base(world, "cow") {

    }

    public override AABB calcAABB(Vector3D pos) {
        // cow model: body 10w x 6h x 16d, head 8w x 8h x 6d sticks out front
        // total extent: x ±5px, y 0-20px, z -13px to +8px (in model space)
        // max horizontal extent: 13px = 0.8125 blocks
        // must be symmetric since entity rotates
        return new AABB(
            pos.X - 0.9, pos.Y, pos.Z - 0.9,
            pos.X + 0.9, pos.Y + 1.5, pos.Z + 0.9
        );
    }

    public override void update(double dt) {
        base.update(dt);

        // update AABB for frustum culling
        aabb = calcAABB(position);

        // basic gravity and collision
        if (!flyMode && !noClip) {
            accel = new Vector3D(0, Constants.gravity, 0);
            collide(dt);
        }

        // update animation position based on movement
        var dist = Vector3D.Distance(position, prevPosition);
        apos += (float)(dist * 2.0); // slower animation than player
    }
}