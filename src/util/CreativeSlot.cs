using BlockGame.world.item.inventory;

namespace BlockGame.util;

public class CreativeSlot : ItemSlot {
    public CreativeSlot(Inventory inv, int index, int x, int y) : base(inv, index, x, y) {
    }

    public override bool accept() {
        return false;
    }
}