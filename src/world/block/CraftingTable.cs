using BlockGame.main;
using BlockGame.net.srv;
using BlockGame.ui;
using BlockGame.ui.menu;
using BlockGame.world.entity;
using BlockGame.world.item.inventory;
using Molten;

namespace BlockGame.world.block;

public class CraftingTable : Block {
    public CraftingTable(string name) : base(name) {
    }

    public override bool onUse(World world, int x, int y, int z, Player player) {
        // MP client: server will handle opening (client sent PlaceBlockPacket, will receive InventoryOpenPacket)
        if (Net.mode.isMPC()) {
            return true; // return true to prevent block placement
        }

        // server-side: open crafting table inventory
        if (Net.mode.isDed()) {
            var ctx = new CraftingTableContext(player);
            var craftingGrid = ctx.getCraftingGrid();

            return GameServer.openInventory(
                (ServerPlayer)player,
                ctx,
                invType: 1, // crafting table
                title: "Crafting",
                position: new Vector3I(x, y, z),
                slots: craftingGrid.grid // 3x3 crafting grid (starts empty)
            );
        }
        else {
            // singleplayer - open directly
            var ctx = new CraftingTableContext(player);
            player.currentCtx = ctx;

            Screen.GAME_SCREEN.switchToMenu(new CraftingTableMenu(new Vector2I(0, 32), ctx));
            ((CraftingTableMenu)Screen.GAME_SCREEN.currentMenu!).setup();

            world.inMenu = true;
            Game.instance.unlockMouse();

            return true;
        }
    }
}