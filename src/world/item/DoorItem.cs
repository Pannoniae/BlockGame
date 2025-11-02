using BlockGame.util;

namespace BlockGame.world.item;

public class DoorItem : Item {
    private readonly block.Block doorBlock;

    public DoorItem(string name, block.Block doorBlock) : base(name) {
        this.doorBlock = doorBlock;
    }

    public override ItemStack? useBlock(ItemStack stack, World world, Player player, int x, int y, int z, RawDirection dir) {
        // check if we can place the door
        if (!doorBlock.canPlace(world, x, y, z, dir)) {
            return null;
        }

        doorBlock.place(world, x, y, z, 0, dir);
        return stack.consume(1);
    }
}