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
        if (!world.isServer) {
            return true; // return true to prevent block placement
        }

        var ctx = new CraftingTableContext(player);
        return GameServer.openInventory(
            (ServerPlayer)player,
            ctx,
            invType: 1, // crafting table
            title: "Crafting",
            position: new Vector3I(x, y, z),
            slots: ctx.getCraftingGrid().grid // 3x3 crafting grid (starts empty)
        );
    }
}