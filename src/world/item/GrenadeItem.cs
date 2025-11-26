using BlockGame.util;
using BlockGame.world.entity;
using Molten.DoublePrecision;

namespace BlockGame.world.item;

public class GrenadeItem : Item {
    public const double THROW_VELOCITY = 18.0; // heavier than snowball

    public GrenadeItem(string name) : base(name) {
    }

    /**
     * Throw grenade when used
     */
    public override ItemStack? use(ItemStack stack, World world, Player player) {
        throwGrenade(player);

        if (player.gameMode.gameplay) {
            return stack.consume(player, 1);
        }

        return stack;
    }

    public override ItemStack? useBlock(ItemStack stack, World world, Player player, int x, int y, int z, Placement info) {
        return use(stack, world, player);
    }

    private static void throwGrenade(Player player) {
        var grenade = new GrenadeEntity(player.world);
        grenade.owner = player;

        var eyeHeight = player.sneaking ? Player.sneakingEyeHeight : Player.eyeHeight;
        grenade.position = player.position + new Vector3D(0, eyeHeight, 0);

        // throw in facing direction
        var direction = player.camFacing();
        grenade.velocity = new Vector3D(direction.X, direction.Y, direction.Z) * THROW_VELOCITY;

        player.world.addEntity(grenade);
    }

    public override int getMaxStackSize() => 8; // grenades stack to 8
}
