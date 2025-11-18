using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.entity;

namespace BlockGame.world.item;

/**
 * My cat wrote this one
 */
public class SignItem : Item {
    private readonly Block signBlock;

    public SignItem(string name, Block block) : base(name) {
        signBlock = block;
    }

    public override ItemStack? useBlock(ItemStack stack, World world, Player player, int x, int y, int z, Placement info) {
        var face = info.face;

        // determine if wall sign or standing sign based on placement face
        bool isWall = face is RawDirection.NORTH or RawDirection.SOUTH or RawDirection.EAST or RawDirection.WEST;

        byte metadata;
        if (isWall) {
            // wall sign: use face direction
            byte rotation = face switch {
                RawDirection.SOUTH => 2,
                RawDirection.WEST => 3,
                RawDirection.NORTH => 4,
                RawDirection.EAST => 5,
                _ => 0
            };
            metadata = (byte)(rotation | 0x10); // set wall bit
        } else {
            float yaw = player.rotation.Y % 360f;
            if (yaw < 0) {
                yaw += 360f;
            }

            byte rotation = (byte)((yaw / 22.5f + 0.5f) % 16);
            metadata = rotation;
        }

        if (!signBlock.canPlace(world, x, y, z, info)) {
            return null;
        }

        signBlock.place(world, x, y, z, metadata, info);
        return stack.consume(player, 1);
    }
}