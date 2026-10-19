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
    public float protx = PITCH_REST;
    private bool draggingPlayer = false;
    private Vector2 lastDragPos;

    // deg per GUI pixel, todo add this to the prefs, like mouse accel * constant?
    private static readonly Vector2 SENS = new(1.6f, 0.6f);

    // inertia, X = yaw, Y = pitch
    private Vector2 rotVel = Vector2.Zero;
    private Vector2 frameRot = Vector2.Zero;
    private const float VEL_TAU = 0.15f;
    // coast friction
    private const float ROT_FRICTION = 3.5f;
    private const float ROT_EPSILON = 2f;
    private const float PITCH_MAX = 80f;
    private const float PITCH_REST = -20f;
    // spring stiffness, rad/s. ~0.5s to settle
    private const float PITCH_OMEGA = 10f;

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

    public override void render(double dt, double interp) {
        base.render(dt, interp);

        var fdt = (float)dt;
        if (fdt <= 0f) {
            return;
        }

        if (draggingPlayer) {
            var a = 1f - float.Exp(-fdt / VEL_TAU);
            rotVel = Vector2.Lerp(rotVel, frameRot / fdt, a);
            frameRot = Vector2.Zero;
            return;
        }

        // yaw coasting
        if (rotVel.X != 0f) {
            proty += rotVel.X * fdt;
            rotVel.X *= float.Exp(-ROT_FRICTION * fdt);
            if (float.Abs(rotVel.X) < ROT_EPSILON) {
                rotVel.X = 0f;
            }
        }

        var x = protx - PITCH_REST;
        if (x != 0f || rotVel.Y != 0f) {
            var e = float.Exp(-PITCH_OMEGA * fdt);
            var tmp = (rotVel.Y + PITCH_OMEGA * x) * fdt;
            x = (x + tmp) * e;
            rotVel.Y = (rotVel.Y - PITCH_OMEGA * tmp) * e;

            if (float.Abs(x) < 0.01f && float.Abs(rotVel.Y) < ROT_EPSILON) {
                x = 0f;
                rotVel.Y = 0f;
            }

            protx = PITCH_REST + x;
        }

        clampPitch();
    }

    private void clampPitch() {
        if (protx is > PITCH_MAX or < -PITCH_MAX) {
            protx = float.Clamp(protx, -PITCH_MAX, PITCH_MAX);
            // kill the pitch speed
            rotVel.Y = 0f;
        }
        proty %= 360f;
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
            lastDragPos = mouse.Position;
            rotVel = Vector2.Zero;
            frameRot = Vector2.Zero;
            return;
        }

        base.onMouseDown(mouse, button);
    }

    public override void onMouseMove(IMouse mouse, Vector2 pos) {
        if (draggingPlayer) {
            var rd = (pos - lastDragPos) / GUI.guiScale * SENS;

            // only count pitch that actually got applied, otherwise shoving it into the clamp increases it too...
            var ox = protx;
            proty += rd.X;
            protx += rd.Y;
            clampPitch();
            frameRot += new Vector2(rd.X, protx - ox);

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