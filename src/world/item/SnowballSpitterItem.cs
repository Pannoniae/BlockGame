using System.Numerics;
using BlockGame.util;
using BlockGame.util.stuff;
using BlockGame.world.entity;
using Molten.DoublePrecision;

namespace BlockGame.world.item;

public class SnowballSpitterItem : Item {
    public const double SHOOT_VELOCITY = 16.0;
    // todo this should be an item attrib instead of an instance variable, like Item.useDelay[id]
    public const int FIRE_DELAY = 30;
    public const double RECOIL_STRENGTH = 1.5;

    public SnowballSpitterItem(string name) : base(name) {

    }

    protected override void onRegister(int id) {
        Registry.ITEMS.autoUse[id] = true;
        Registry.ITEMS.useDelay[id] = FIRE_DELAY;
    }

    /**
     * Shoot snowball when used (auto-fire)
     */
    public override ItemStack? use(ItemStack stack, World world, Player player) {
        // check for snowball ammo (in survival mode)
        if (player.gameMode.gameplay && !player.inventory.hasItem(SNOWBALL.id)) {
            return stack;
        }

        shootSnowball(player);

        // consume ammo in survival
        if (player.gameMode.gameplay) {
            player.inventory.removeItem(SNOWBALL.id, 1);
        }

        player.applyRecoil(RECOIL_STRENGTH);

        return stack;
    }

    /**
     * Same behavior when clicking on blocks
     */
    public override ItemStack? useBlock(ItemStack stack, World world, Player player, int x, int y, int z, Placement info) {
        return use(stack, world, player);
    }

    private static void shootSnowball(Player player) {
        var snowball = new SnowballEntity(player.world);
        snowball.owner = player;

        var eyeHeight = player.sneaking ? Player.sneakingEyeHeight : Player.eyeHeight;
        snowball.position = player.position + new Vector3D(0, eyeHeight, 0);

        var direction = player.camFacing();
        snowball.velocity = new Vector3D(direction.X, direction.Y, direction.Z) * SHOOT_VELOCITY;

        snowball.rotation = new Vector3(
            player.rotation.X,
            player.rotation.Y,
            0
        );

        player.world.addEntity(snowball);
    }

    public override int getMaxStackSize() => 1; // weapons don't stack
}
