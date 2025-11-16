using BlockGame.main;
using BlockGame.util;
using BlockGame.util.xNBT;
using BlockGame.world.block;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

/** Animated falling block entity (sand, gravel, etc.)
 *  TODO this kinda desyncs in multiplayer, also sync velocity in the EntityTracker maybe? or not sure
 */
public class FallingBlockEntity : Entity {
    const double size = 1 - EPSILON_GROUND_CHECK;

    public FallingBlockEntity(World world) : base(world, "fallingBlock") {
    }

    public ushort blockID;
    public byte blockMeta;
    public int fallTime;

    protected override bool needsGravity => true;
    protected override bool needsBodyRotation => false;
    protected override bool needsFootsteps => false;
    protected override bool needsFallDamage => false;
    protected override bool needsAnimation => false;
    protected override bool needsBlockInteraction => false;

    public override bool blocksPlacement => false;

    protected override bool shouldContinueUpdate(double dt) {
        fallTime++;

        // if falling for too long (300 ticks = ~5 sec), remove
        if (fallTime > 300) {
            remove();
            return false;
        }

        return true;
    }

    protected override void updatePhysics(double dt) {
        // apply gravity
        if (!onGround) {
            velocity.Y -= GRAVITY * dt;
        }

        // clamp terminal velocity
        if (velocity.Y < -50) {
            velocity.Y = -50;
        }

        // collision + movement
        collide(dt);

        // if landed, place block
        if (onGround && !Net.mode.isMPC()) {
            landAndPlace();
        }
    }

    private void landAndPlace() {
        var landPos = position.toBlockPos();

        // check if can place block at landing position
        var blockBelow = world.getBlock(landPos);

        // if there's a solid block below, place on top of it
        if (Block.collision[blockBelow]) {
            landPos.Y += 1;
        }

        // try to place the block
        var existing = world.getBlock(landPos);
        if (existing == 0 || !Block.collision[existing]) {
            world.setBlock(landPos.X, landPos.Y, landPos.Z, blockID);
            if (blockMeta != 0) {
                world.setBlockMetadata(landPos.X, landPos.Y, landPos.Z, ((uint)blockID).setMetadata(blockMeta));
            }
        }
        else {
            // can't place - drop as item
            var block = Block.get(blockID);
            if (block != null) {
                world.spawnBlockDrop(landPos.X, landPos.Y, landPos.Z, block.getItem(), 1, blockMeta);
            }
        }

        remove();
    }

    private void remove() {
        active = false;
    }

    public override AABB calcAABB(Vector3D pos) {
        return AABB.fromSize(
            new Vector3D(pos.X - size / 2, pos.Y, pos.Z - size / 2),
            new Vector3D(size, size, size)
        );
    }

    protected override void readx(NBTCompound data) {
        if (data.has("blockID")) {
            blockID = (ushort)data.getInt("blockID");
        }
        if (data.has("blockMeta")) {
            blockMeta = (byte)data.getInt("blockMeta");
        }
        if (data.has("fallTime")) {
            fallTime = data.getInt("fallTime");
        }
    }

    public override void writex(NBTCompound data) {
        data.addInt("blockID", blockID);
        data.addInt("blockMeta", blockMeta);
        data.addInt("fallTime", fallTime);
    }
}