using System.Numerics;
using BlockGame.GL;
using BlockGame.main;
using BlockGame.ui.element;
using BlockGame.util;
using BlockGame.world.item.inventory;
using BlockGame.world.entity;
using Molten;
using Silk.NET.Input;
using Molten.DoublePrecision;

namespace BlockGame.ui.menu;

public class CraftingTableMenu : Menu {

    public const int textOffsetX = 4;
    public const int textOffsetY = 3;

    public List<ItemSlot> slots = [];

    private readonly CraftingTableContext craftingCtx;

    public Vector2I guiPos;
    public Rectangle guiBounds;

    public readonly BTexture2D invTex;

    public override bool isModal() {
        return false;
    }

    public CraftingTableMenu(Vector2I guiPos, CraftingTableContext ctx) {
        this.guiPos = guiPos;
        this.craftingCtx = ctx;

        invTex?.Dispose();
        invTex = new BTexture2D("textures/crafting_table.png");
        invTex.reload();

        resize(guiPos);
    }

    public void setup() {
        slots = craftingCtx.getSlots();
    }

    public override void draw() {
        base.draw();
        Game.gui.drawUIImmediate(invTex, new Vector2(guiBounds.X, guiBounds.Y));
        // draw title
        Game.gui.drawStringUI("Crafting Table", new Vector2(guiBounds.X + textOffsetX, guiBounds.Y + textOffsetY), Color.White);

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
                        Game.gui.drawSlotTint(new Vector2(guiBounds.X, guiBounds.Y), slot.rect, tint);
                    }
                }
            }

            Game.gui.drawItem(slot, new Vector2(guiBounds.X, guiBounds.Y));
        }

        // draw cursor item
        var player = Game.world.player;
        if (player?.inventory?.cursor != null) {
            var mousePos = Game.mousePos;
            Game.gui.drawCursorItem(player.inventory.cursor, mousePos);
        }
    }

    protected override string? getTooltipText() {
        var player = Game.world.player;

        // if holding an item in cursor, show its tooltip
        if (player?.inventory?.cursor != null && player.inventory.cursor != ItemStack.EMPTY) {
            return player.inventory.cursor.getItem().getName(player.inventory.cursor);
        }

        var guiPos = GUI.s2u(Game.mousePos);

        // check slots first
        foreach (var slot in slots) {
            var absoluteRect = new Rectangle(guiBounds.X + slot.rect.X, guiBounds.Y + slot.rect.Y, slot.rect.Width, slot.rect.Height);
            if (absoluteRect.Contains((int)guiPos.X, (int)guiPos.Y)) {
                var stack = slot.getStack();
                if (stack != ItemStack.EMPTY && stack.id != 0) {
                    return stack.getItem().getName(stack);
                }
                break;
            }
        }

        // fallback to base (GUIElement tooltips)
        return base.getTooltipText();
    }

    public override void onMouseUp(Vector2 pos, MouseButton button) {
        base.onMouseUp(pos, button);
        var guiPos = GUI.s2u(pos);
        var player = Game.world.player;

        foreach (var slot in slots) {
            var absoluteRect = new Rectangle(guiBounds.X + slot.rect.X, guiBounds.Y + slot.rect.Y, slot.rect.Width, slot.rect.Height);
            if (absoluteRect.Contains((int)guiPos.X, (int)guiPos.Y)) {
                handleSlotClick(slot, button, player);
                return;
            }
        }
    }

    private void handleSlotClick(ItemSlot slot, MouseButton button, world.Player player) {
        // convert MouseButton to ClickType and delegate to the crafting context
        var clickType = button == MouseButton.Left ? ClickType.LEFT : ClickType.RIGHT;
        craftingCtx.handleSlotClick(slot, clickType);
    }

    public sealed override void resize(Vector2I newSize) {
        base.resize(newSize);
        guiBounds = GUIElement.resolveAnchors(new Rectangle(guiPos.X, guiPos.Y, (int)invTex.width, (int)invTex.height),
            HorizontalAnchor.CENTREDCONTENTS, VerticalAnchor.TOP, this);
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