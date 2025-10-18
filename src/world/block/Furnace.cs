using BlockGame.main;
using BlockGame.ui;
using BlockGame.ui.menu;
using BlockGame.world.item.inventory;
using Molten;

namespace BlockGame.world.block;

public class Furnace : Block {
    public Furnace(ushort id, string name) : base(id, name) {
    }

    public override bool onUse(World world, int x, int y, int z, Player player) {
        // open the crafting table UI
        var ctx = new FurnaceMenuContext(player.inventory);
        player.currentCtx = ctx;

        Screen.GAME_SCREEN.switchToMenu(new FurnaceMenu(new Vector2I(0, 32), ctx));
        ((FurnaceMenu)Screen.GAME_SCREEN.currentMenu!).setup();

        world.inMenu = true;
        Game.instance.unlockMouse();

        return true;
    }
}