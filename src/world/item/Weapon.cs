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
    }

    public override int getMaxStackSize() => 1;
}