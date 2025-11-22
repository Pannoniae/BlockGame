using BlockGame.GL;
using BlockGame.util;
using BlockGame.world.item.inventory;
using Molten;
using Silk.NET.Input;

namespace BlockGame.ui.menu;

public class ChestMenu : InventoryMenu {

    public const int textOffsetX = 5;
    public const int textOffsetY = 6;

    private readonly ChestMenuContext chestCtx;

    public ChestMenu(Vector2I guiPos, ChestMenuContext ctx) {
        this.guiPos = guiPos;
        this.chestCtx = ctx;

        invTex?.Dispose();
        invTex = new BTexture2D("textures/chest_inventory.png");
        invTex.reload();

        resize(guiPos);
    }

    public void setup() {
        slots = chestCtx.getSlots();
    }

    protected override string getTitle() => "Chest";
    protected override BTexture2D getTexture() => invTex;

    protected override int getWidth() => invTex.width;

    protected override int getHeight() => invTex.height;

    protected override int getTextOffsetX() => textOffsetX;
    protected override int getTextOffsetY() => textOffsetY;

    protected override void handleSlotClick(ItemSlot slot, MouseButton button) {
        var clickType = button == MouseButton.Left ? ClickType.LEFT : ClickType.RIGHT;
        chestCtx.handleSlotClick(slot, clickType);
    }

    public override void deactivate() {
        base.deactivate();

        // nothing to drop, chest persists
    }
}