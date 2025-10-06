using System.Numerics;
using BlockGame.GL;
using BlockGame.main;
using BlockGame.ui.element;
using BlockGame.util;
using BlockGame.world.item.inventory;
using Molten;
using Silk.NET.Input;

namespace BlockGame.ui.menu;

public class SurvivalInventoryMenu : Menu {
    public const int rows = 5;
    public const int cols = 10;

    public const int invOffsetY = 20;
    public const int textOffsetX = 4;
    public const int textOffsetY = 3;
    public const int invOffsetX = 5;

    public List<ItemSlot> slots = [];

    private readonly SurvivalInventoryContext survivalCtx;

    public Vector2I guiPos;
    public Rectangle guiBounds;

    public readonly BTexture2D invTex;

    public override bool isModal() {
        return false;
    }

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

    public override void draw() {
        base.draw();
        Game.gui.drawUIImmediate(invTex, new Vector2(guiBounds.X, guiBounds.Y));
        // draw inventory text
        Game.gui.drawStringUI("Inventory", new Vector2(guiBounds.X + textOffsetX, guiBounds.Y + textOffsetY), Color.White);

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
        // convert MouseButton to ClickType and delegate to the survival context
        var clickType = button == MouseButton.Left ? ClickType.LEFT : ClickType.RIGHT;
        survivalCtx.handleSlotClick(slot, clickType);
    }

    public sealed override void resize(Vector2I newSize) {
        base.resize(newSize);
        guiBounds = GUIElement.resolveAnchors(new Rectangle(guiPos.X, guiPos.Y, (int)invTex.width, (int)invTex.height),
            HorizontalAnchor.CENTREDCONTENTS, VerticalAnchor.TOP, this);
    }
}