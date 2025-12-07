using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.entity;

namespace BlockGame.world.item;

public class DoorItem : Item {
    private readonly Block doorBlock;

    public DoorItem(string name, Door doorBlock) : base(name) {
        this.doorBlock = doorBlock;
        // also set the item field of the door block
        doorBlock.theDoor = this;

    }

    public override ItemStack? useBlock(ItemStack stack, World world, Player player, int x, int y, int z, Placement info) {
        // check if we can place the door
        if (!doorBlock.canPlace(world, x, y, z, info)) {
            return null;
        }

        doorBlock.place(world, x, y, z, 0, info);
        return stack.consume(player, 1);
    }
}