using BlockGame.util;
using BlockGame.world.entity;
using Molten.DoublePrecision;

namespace BlockGame.world.item;


public class BowItem : Item {
    public const double ARROW_VELOCITY = 48.0;

    public BowItem(string name) : base(name) {
    }

    /** fire arrow instantly on use */
    public override ItemStack? use(ItemStack stack, World world, Player player) {
        fireArrow(player);
        return stack;
    }

    /** bows work regardless of what you're looking at */
    public override ItemStack? useBlock(ItemStack stack, World world, Player player, int x, int y, int z, Placement info) {
        fireArrow(player);
        return stack;
    }

    private static void fireArrow(Player player) {
        // consume arrow in survival
        if (player.gameMode.gameplay) {
            if (!player.inventory.removeItem(ARROW_WOOD.id, 1)) {
                return; // no arrows
            }
        }

        // spawn arrow
        var arrow = new ArrowEntity(player.world);
        arrow.owner = player;

        // position at eye level
        var eyeHeight = player.sneaking ? Player.sneakingEyeHeight : Player.eyeHeight;
        arrow.position = player.position + new Vector3D(0, eyeHeight, 0);

        // fire at full velocity for now....
        var direction = player.camFacing();
        arrow.velocity = new Vector3D(direction.X, direction.Y, direction.Z) * ARROW_VELOCITY;

        player.world.addEntity(arrow);
    }

    public override int getMaxStackSize() => 1;
}
