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
            // remove old regen effect (overrid)
            player.removeEffect(EffectRegistry.REGEN);
            player.addEffect(new RegenEffect(600, heal, 0));

            newStack = stack.consume(player, 1);
        }

        return newStack;
    }

    public override int getMaxStackSize() {
        return 4;
    }
}