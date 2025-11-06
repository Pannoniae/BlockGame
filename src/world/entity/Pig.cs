using BlockGame.main;
using BlockGame.util;
using BlockGame.world;
using BlockGame.world.item;
using Molten.DoublePrecision;

namespace Core.world.entity;

public class Pig : Mob {
    public Pig(World world) : base(world, "pig") {
        tex = Game.textures.pig;
    }

    public override AABB calcAABB(Vector3D pos) {
        return new AABB(
            pos.X - 0.5, pos.Y, pos.Z - 0.5,
            pos.X + 0.5, pos.Y + 1.2f, pos.Z + 0.5
        );
    }

    public override (Item item, byte metadata, int count) getDrop() {
        return (Item.PORKCHOP, 0, 1);
    }
    
}