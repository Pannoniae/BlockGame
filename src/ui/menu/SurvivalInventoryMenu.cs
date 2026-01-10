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

    public const int invOffsetY = 13;
    public const int textOffsetX = 6;
    public const int textOffsetY = 3;
    public const int invOffsetX = 5;

    // player model rendering constants
    private const int playerModelX = 47;
    private const int playerModelY = 15;
    private const int playerModelW = 60;
    private const int playerModelH = 64;

    private readonly SurvivalInventoryContext survivalCtx;

    // player model rotation state
    public float proty = 5f;
    public float protx = -20f;
    private bool draggingPlayer = false;
    private Vector2 lastDragPos;

    // inertia
    private Vector2 rotVel = Vector2.Zero;
    private const float ROT_DAMPING = 0.95f;
    private const float ROT_EPSILON = 0.1f;

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

    public override void update(double dt) {
        base.update(dt);

        // apply inertia when not dragging
        if (!draggingPlayer) {
            protx += rotVel.Y * (float)dt;
            proty += rotVel.X * (float)dt;

            protx = float.Clamp(protx, -80f, 80f);

            // decay velocity
            rotVel *= ROT_DAMPING;

            // stop if too small
            if (rotVel.LengthSquared() < ROT_EPSILON * ROT_EPSILON) {
                rotVel = Vector2.Zero;
            }
        }
    }

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


        // draw player model on left side of inventory
        var player = Game.world?.player;
        if (player != null) {
            Game.gui.drawPlayerUI(player, guiBounds.X + playerModelX, guiBounds.Y + playerModelY,
                playerModelW, playerModelH, protx, proty);
        }
    }

    public override void onMouseDown(IMouse mouse, MouseButton button) {
        // check player model area BEFORE base (which handles elements)
        var guiPos = GUI.s2u(Game.mousePos);
        var areax = guiBounds.X + playerModelX;
        var areay = guiBounds.Y + playerModelY;

        if (button == MouseButton.Left &&
            guiPos.X >= areax && guiPos.X <= areax + playerModelW &&
            guiPos.Y >= areay && guiPos.Y <= areay + playerModelH) {
            draggingPlayer = true;
            lastDragPos = Game.mousePos;
            return;
        }

        base.onMouseDown(mouse, button);
    }

    public override void onMouseMove(IMouse mouse, Vector2 pos) {
        if (draggingPlayer) {
            var delta = pos - lastDragPos;
            var rotDelta = new Vector2(delta.X * 0.4f, delta.Y * 0.15f);

            protx += rotDelta.Y;
            proty += rotDelta.X;
            protx = float.Clamp(protx, -80f, 80f);

            // update velocity for inertia
            rotVel = rotDelta * 60f;

            lastDragPos = pos;
            return;
        }

        base.onMouseMove(mouse, pos);
    }

    public override void onMouseUp(Vector2 pos, MouseButton button) {
        if (button == MouseButton.Left && draggingPlayer) {
            draggingPlayer = false;
            return;
        }

        base.onMouseUp(pos, button);
    }

    protected override void handleSlotClick(ItemSlot slot, MouseButton button) {
        var clickType = button == MouseButton.Left ? ClickType.LEFT : ClickType.RIGHT;
        survivalCtx.handleSlotClick(slot, clickType);
    }

    public override void deactivate() {
        base.deactivate();

        // return all items from 2x2 crafting grid to player inventory
        var player = Game.player;
        if (player == null) {
            return;
        }

        var craftingGrid = survivalCtx.getCraftingGrid();

        for (int i = 0; i < craftingGrid.grid.Length; i++) {
            var stack = craftingGrid.grid[i];
            if (stack != ItemStack.EMPTY && stack.quantity > 0) {
                player.dropItemStack(stack, withVelocity: true);
                craftingGrid.grid[i] = ItemStack.EMPTY;
            }
        }
    }
}