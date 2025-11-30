using BlockGame.util;
using BlockGame.world.item;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

public class Pig : Mob {
    public Pig(World world) : base(world, "pig") {
        tex = "textures/entity/pig.png";
        hp = 15;
    }

    public override AABB calcAABB(Vector3D pos) {
        return new AABB(
            pos.X - 0.5, pos.Y, pos.Z - 0.5,
            pos.X + 0.5, pos.Y + 1.2f, pos.Z + 0.5
        );
    }

    public override void getDrop(List<ItemStack> drops) {
        drops.Add(new ItemStack(Item.PORKCHOP, 1, 0));
    }
    
}