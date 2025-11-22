namespace BlockGame.util;

public class CreativeSlot : ItemSlot {
    private readonly ItemStack template;

    public CreativeSlot(ItemStack template, int x, int y) : base(null, -1, x, y) {
        this.template = template;
    }

    public override ItemStack getStack() {
        return template;
    }

    public override bool accept(ItemStack stack) {
        return true; // creative slots swallow all items
    }

    public override ItemStack take(int count) {
        return template.copy(); // infinite supply
    }

    public override ItemStack place(ItemStack stack) {
        return ItemStack.EMPTY; // creative slots swallow all items
    }

    public override ItemStack swap(ItemStack stack) {
        return stack; // can't swap with creative slots
    }
}