using BlockGame.util;
using BlockGame.world.entity;

namespace BlockGame.world.item;

public class Food : Item {
    public readonly int heal;

    public Food(string id, int heal) : base(id) {
        this.heal = heal;
    }

    public override ItemStack use(ItemStack stack, World world, Player player) {
        var newStack = base.use(stack, world, player);

        // don't do anything in creative
        if (player.gameMode.gameplay) {
            player.heal(heal);
            newStack = stack.consume(player, 1);
        }

        return newStack;
    }

    public override int getMaxStackSize() {
        return 4;
    }
}