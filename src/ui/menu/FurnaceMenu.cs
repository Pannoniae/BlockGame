using System.Numerics;
using BlockGame.GL;
using BlockGame.main;
using BlockGame.util;
using BlockGame.world.block.entity;
using BlockGame.world.item.inventory;
using Molten;
using Silk.NET.Input;

namespace BlockGame.ui.menu;

public class FurnaceMenu : InventoryMenu {

    public const int textOffsetX = 4;
    public const int textOffsetY = 3;

    public const int arrowEmptyX1 = 210;
    public const int arrowEmptyY1 = 40;
    public const int arrowFilledX = 210;
    public const int arrowFilledY = 47;
    public const int arrowW = 13;
    public const int arrowH = 7;

    public const int flameEmptyX = 210;
    public const int flameEmptyY = 0;
    public const int flameFilledX = 210;
    public const int flameFilledY = 20;  // filled flame is below empty in texture
    public const int flameW = 20;
    public const int flameH = 20;

    public const int arrowOffsetX = 100;
    public const int arrowOffsetY = 38;

    public const int flameOffsetX = 71;
    public const int flameOffsetY = 33;

    private readonly FurnaceMenuContext furnaceCtx;

    public FurnaceMenu(Vector2I guiPos, FurnaceMenuContext ctx) {
        this.guiPos = guiPos;
        this.furnaceCtx = ctx;

        invTex?.Dispose();
        invTex = new BTexture2D("textures/furnace_inventory.png");
        invTex.reload();

        resize(guiPos);
    }

    public void setup() {
        slots = furnaceCtx.getSlots();
    }

    protected override string getTitle() => "Furnace";
    protected override BTexture2D getTexture() => invTex;

    protected override int getWidth() => (int)invTex.width - 20;

    protected override int getHeight() => (int)invTex.height;

    protected override int getTextOffsetX() => textOffsetX;
    protected override int getTextOffsetY() => textOffsetY;

    protected override void handleSlotClick(ItemSlot slot, MouseButton button) {
        var clickType = button == MouseButton.Left ? ClickType.LEFT : ClickType.RIGHT;
        furnaceCtx.handleSlotClick(slot, clickType);
    }

    public override void draw() {
        base.draw(); // inventory bg + slots

        // draw progress
        if (furnaceCtx.getFurnaceInventory() is FurnaceBlockEntity be) {
            drawSmeltArrow(be);
            drawFuelFlame(be);
        }
    }

    private void drawSmeltArrow(FurnaceBlockEntity be) {
        float progress = be.getSmeltProgress(); // 0.0 to 1.0



        int screenX = guiBounds.X + arrowOffsetX;
        int screenY = guiBounds.Y + arrowOffsetY;

        int fillWidth = (int)(arrowW * progress);

        // draw empty arrow bg
        Game.gui.drawUI(invTex, new Vector2(screenX, screenY),
            new Rectangle(arrowEmptyX1, arrowEmptyY1, arrowW, arrowH));

        // draw filled
        if (fillWidth > 0) {
            Game.gui.drawUI(invTex,
                new RectangleF(screenX, screenY, fillWidth, arrowH),
                new Rectangle(arrowFilledX, arrowFilledY, fillWidth, arrowH));
        }
    }

    private void drawFuelFlame(FurnaceBlockEntity be) {
        float fuelPercent = be.getFuelProgress(); // 0.0 to 1.0

        int screenX = guiBounds.X + flameOffsetX;
        int screenY = guiBounds.Y + flameOffsetY;

        // draw empty flame bg
        Game.gui.drawUI(invTex, new Vector2(screenX, screenY),
            new Rectangle(flameEmptyX, flameEmptyY, flameW, flameH));

        // draw filled from bottom up
        int fillHeight = (int)(flameH * fuelPercent);
        if (fillHeight > 0) {
            int yOffset = flameH - fillHeight;
            Game.gui.drawUI(invTex,
                new RectangleF(screenX, screenY + yOffset, flameW, fillHeight),
                new Rectangle(flameFilledX, flameFilledY + yOffset, flameW, fillHeight));
        }
    }

    public override void deactivate() {
        base.deactivate();

        // todo drop slots
    }
}