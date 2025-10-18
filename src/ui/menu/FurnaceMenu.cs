using System.Numerics;
using BlockGame.GL;
using BlockGame.main;
using BlockGame.util;
using BlockGame.world.item.inventory;
using Molten;
using Silk.NET.Input;

namespace BlockGame.ui.menu;

public class FurnaceMenu : InventoryMenu {

    public const int textOffsetX = 4;
    public const int textOffsetY = 3;

    private readonly FurnaceMenuContext craftingCtx;

    public FurnaceMenu(Vector2I guiPos, FurnaceMenuContext ctx) {
        this.guiPos = guiPos;
        this.craftingCtx = ctx;

        invTex?.Dispose();
        invTex = new BTexture2D("textures/furnace_inventory.png");
        invTex.reload();

        resize(guiPos);
    }

    public void setup() {
        slots = craftingCtx.getSlots();
    }

    protected override string getTitle() => "Furnace";
    protected override BTexture2D getTexture() => invTex;
    protected override int getTextOffsetX() => textOffsetX;
    protected override int getTextOffsetY() => textOffsetY;

    protected override void handleSlotClick(ItemSlot slot, MouseButton button) {
        var clickType = button == MouseButton.Left ? ClickType.LEFT : ClickType.RIGHT;
        craftingCtx.handleSlotClick(slot, clickType);
    }

    public override void deactivate() {
        base.deactivate();

        // return all items from crafting grid to player inventory
        var player = Game.world.player;
        if (player == null) return;

        var craftingGrid = craftingCtx.getCraftingGrid();

        for (int i = 0; i < craftingGrid.grid.Length; i++) {
            var stack = craftingGrid.grid[i];
            if (stack != ItemStack.EMPTY && stack.quantity > 0) {
                // try to add to player inventory, drop if full
                if (!player.inventory.addItem(stack)) {
                    player.dropItemStack(stack, true);
                }
                craftingGrid.grid[i] = ItemStack.EMPTY;
            }
        }
    }
}