using System.Numerics;
using BlockGame.GL;
using BlockGame.main;
using BlockGame.util;
using BlockGame.world.item.inventory;
using Molten;
using Silk.NET.Input;

namespace BlockGame.ui.menu;

public class SurvivalInventoryMenu : InventoryMenu {
    public const int rows = 5;
    public const int cols = 10;

    public const int invOffsetY = 20;
    public const int textOffsetX = 4;
    public const int textOffsetY = 3;
    public const int invOffsetX = 5;

    private readonly SurvivalInventoryContext survivalCtx;

    public SurvivalInventoryMenu(Vector2I guiPos) {
        this.guiPos = guiPos;

        invTex?.Dispose();
        invTex = new BTexture2D("textures/inventory.png");
        invTex.reload();

        survivalCtx = (SurvivalInventoryContext)Game.player.inventoryCtx;

        resize(guiPos);
    }

    public void setup() {
        slots = survivalCtx.getSlots();
    }

    protected override string getTitle() => "Inventory";
    protected override BTexture2D getTexture() => invTex;
    protected override int getTextOffsetX() => textOffsetX;
    protected override int getTextOffsetY() => textOffsetY;

    protected override void drawSlots(Vector2 guiBoundsPos) {
        foreach (var slot in slots) {
            // draw tint for crafting result slot
            if (slot is CraftingResultSlot resultSlot) {
                if (resultSlot.inventory is CraftingGridInventory craftingGrid) {
                    Color tint = craftingGrid.matchStatus switch {
                        CraftingMatchStatus.EMPTY => Color.Transparent,
                        CraftingMatchStatus.NO_MATCH => new Color(255, 0, 0, 80),
                        CraftingMatchStatus.PARTIAL_MATCH => new Color(255, 255, 0, 80),
                        CraftingMatchStatus.FULL_MATCH => new Color(0, 255, 0, 80),
                        _ => Color.Transparent
                    };

                    if (tint.A > 0) {
                        Game.gui.drawSlotTint(guiBoundsPos, slot.rect, tint);
                    }
                }
            }

            Game.gui.drawItem(slot, guiBoundsPos);
        }
    }

    protected override void handleSlotClick(ItemSlot slot, MouseButton button) {
        var clickType = button == MouseButton.Left ? ClickType.LEFT : ClickType.RIGHT;
        survivalCtx.handleSlotClick(slot, clickType);
    }

    public override void deactivate() {
        base.deactivate();

        // return all items from 2x2 crafting grid to player inventory
        var player = Game.world.player;
        if (player == null) return;

        var craftingGrid = survivalCtx.getCraftingGrid();

        for (int i = 0; i < craftingGrid.grid.Length; i++) {
            var stack = craftingGrid.grid[i];
            if (stack != ItemStack.EMPTY && stack.quantity > 0) {
                player.dropItemStack(stack, withVelocity: false);
                craftingGrid.grid[i] = ItemStack.EMPTY;
            }
        }
    }
}