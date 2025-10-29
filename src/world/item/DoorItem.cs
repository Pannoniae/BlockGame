using BlockGame.util;

namespace BlockGame.world.item;

public class DoorItem : Item {
    private readonly block.Block doorBlock;

    public DoorItem(string name, block.Block doorBlock) : base(name) {
        this.doorBlock = doorBlock;
    }

    public override void useBlock(ItemStack stack, World world, Player player, int x, int y, int z, RawDirection dir) {
        doorBlock.place(world, x, y, z, 0, dir);
    }
}