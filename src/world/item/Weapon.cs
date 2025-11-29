using BlockGame.util;
using BlockGame.util.stuff;
using BlockGame.world.block;

namespace BlockGame.world.item;

public class Weapon : Item {
    public double damage;
    public MaterialTier tier;

    public Weapon(string name, MaterialTier tier, double damage) : base(name) {
        this.tier = tier;
        this.damage = damage;
    }

    protected override void onRegister(int id) {
        durability[id] = tier.durability;
        Registry.ITEMS.rot[id] = 90;
    }

    public override int getMaxStackSize() => 1;

    public override double getDamage(ItemStack stack) {
        return damage;
    }
}