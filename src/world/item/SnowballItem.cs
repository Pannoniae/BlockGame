using System.Numerics;
using BlockGame.util;
using BlockGame.world.entity;
using Molten.DoublePrecision;

namespace BlockGame.world.item;

public class SnowballItem : Item {
    public const double THROW_VELOCITY = 24.0;

    public SnowballItem(string name) : base(name) {
    }

    public override ItemStack? use(ItemStack stack, World world, Player player) {
        throwSnowball(player);

        if (player.gameMode.gameplay) {
            return stack.consume(player, 1);
        }

        return stack;
    }

    public override ItemStack? useBlock(ItemStack stack, World world, Player player, int x, int y, int z, Placement info) {
        return use(stack, world, player);
    }

    private static void throwSnowball(Player player) {
        var snowball = new SnowballEntity(player.world);
        snowball.owner = player;

        var eyeHeight = player.sneaking ? Player.sneakingEyeHeight : Player.eyeHeight;
        snowball.position = player.position + new Vector3D(0, eyeHeight, 0);

        var direction = player.camFacing();
        snowball.velocity = new Vector3D(direction.X, direction.Y, direction.Z) * THROW_VELOCITY;

        snowball.rotation = new Vector3(
            player.rotation.X,
            player.rotation.Y,
            0
        );

        player.world.addEntity(snowball);
    }

    public override int getMaxStackSize() => 16; // snowballs stack to 16
}
