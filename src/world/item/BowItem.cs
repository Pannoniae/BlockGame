using BlockGame.util;
using BlockGame.world.entity;
using Molten.DoublePrecision;

namespace BlockGame.world.item;


public class BowItem : Item {
    public const int MAX_CHARGE_TIME = 20;
    public const double MIN_VELOCITY = 3.0;
    public const double MAX_VELOCITY = 48.0;

    public BowItem(string name) : base(name) {
    }

    /**
     * Called when player uses item (right-click in air).
     * Client-side: starts charging on initial press
     * Server-side: fires arrow with given charge ratio
     */
    public override ItemStack? use(ItemStack stack, World world, Player player) {
        return fire(stack, player);
    }

    /**
     * Called when player uses item on a block (right-click on block).
     * Bows should work regardless of what you're looking at.
     */
    public override ItemStack? useBlock(ItemStack stack, World world, Player player, int x, int y, int z, Placement info) {
        return fire(stack, player);
    }

    private static ItemStack? fire(ItemStack stack, Player player) {
        // if we're currently charging, ignore this call (happens during hold)
        if (player.isChargingBow) {
            return null;
        }

        // if bowCharge is set, we just released and should fire
        if (player.bowCharge > 0) {
            fireArrow(player, (float)player.bowCharge);
            return stack;
        }

        // otherwise, start charging (initial press)
        if (player.inventory.hasItem(ARROW_WOOD.id)) {
            player.startBowCharge();
        }

        // return null to prevent hand swing when starting to charge
        return null;
    }

    private static void fireArrow(Player player, float chargeRatio) {
        // only fire if bow is charged enough (at least 20% charged)
        if (chargeRatio < 0.2f) {
            return;
        }

        // check if player has arrows
        if (player.gameMode.gameplay) {
            if (!player.inventory.removeItem(ARROW_WOOD.id, 1)) {
                return;
            }
        }

        var velocity = MIN_VELOCITY + (MAX_VELOCITY - MIN_VELOCITY) * chargeRatio;

        // spawn arrow
        var arrow = new ArrowEntity(player.world);
        arrow.owner = player;

        // position arrow at player's eye position
        var eyeHeight = player.sneaking ? Player.sneakingEyeHeight : Player.eyeHeight;
        arrow.position = player.position + new Vector3D(0, eyeHeight, 0);

        // set arrow velocity based on player's facing direction
        var direction = player.camFacing();
        arrow.velocity = new Vector3D(direction.X, direction.Y, direction.Z) * velocity;

        player.world.addEntity(arrow);
    }

    public override int getMaxStackSize() => 1; // bows don't stack
}
