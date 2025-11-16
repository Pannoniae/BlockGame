using BlockGame.util;
using BlockGame.world.item;

namespace BlockGame.world.item.inventory;

public enum CraftingMatchStatus {
    EMPTY,          // grid is empty
    NO_MATCH,       // grid has items but no recipe matches
    PARTIAL_MATCH,  // recipe shape matches but insufficient quantities
    FULL_MATCH      // recipe fully matches, can craft
}

public class CraftingGridInventory : Inventory {

    public ItemStack[] grid;
    public ItemStack result = ItemStack.EMPTY;
    public CraftingMatchStatus matchStatus = CraftingMatchStatus.EMPTY;

    private readonly InventoryContext parentCtx;

    public readonly int rows;
    public readonly int cols;

    public CraftingGridInventory(InventoryContext parentCtx, int rows, int cols) {
        this.parentCtx = parentCtx;
        this.rows = rows;
        this.cols = cols;
        grid = new ItemStack[rows * cols];
        for (int i = 0; i < grid.Length; i++) {
            grid[i] = ItemStack.EMPTY;
        }
    }

    public int size() {
        return grid.Length;
    }

    public ItemStack getStack(int index) {
        if (index < 0 || index >= grid.Length) return ItemStack.EMPTY;
        return grid[index];
    }

    public void setStack(int index, ItemStack stack) {
        if (index < 0 || index >= grid.Length) return;
        grid[index] = stack;
        updateResult();
    }

    public ItemStack removeStack(int index, int count) {
        if (index < 0 || index >= grid.Length) return ItemStack.EMPTY;
        var stack = grid[index];
        if (stack == ItemStack.EMPTY || count <= 0) return ItemStack.EMPTY;

        var removeAmount = Math.Min(count, stack.quantity);
        var removed = new ItemStack(stack.getItem(), removeAmount, stack.metadata);

        stack.quantity -= removeAmount;
        if (stack.quantity <= 0) {
            grid[index] = ItemStack.EMPTY;
        }

        updateResult();
        return removed;
    }

    public ItemStack clear(int index) {
        if (index < 0 || index >= grid.Length) return ItemStack.EMPTY;
        var stack = grid[index];
        grid[index] = ItemStack.EMPTY;
        updateResult();
        return stack;
    }

    public void clearAll() {
        for (int i = 0; i < grid.Length; i++) {
            grid[i] = ItemStack.EMPTY;
        }
        updateResult();
    }

    public bool add(int index, int count) {
        if (index < 0 || index >= grid.Length || count <= 0) return false;
        var stack = grid[index];
        if (stack == ItemStack.EMPTY) return false;

        stack.quantity += count;
        updateResult();
        return true;
    }

    public bool isEmpty() {
        foreach (var slot in grid) {
            if (slot != ItemStack.EMPTY && slot.quantity > 0) {
                return false;
            }
        }
        return true;
    }

    public string name() {
        return "Crafting";
    }

    public void setDirty(bool dirty) {
        // not needed for crafting grid
    }

    /** Recalculate result based on current grid contents */
    public void updateResult() {
        // check if grid is empty
        if (isEmpty()) {
            matchStatus = CraftingMatchStatus.EMPTY;
            result = ItemStack.EMPTY;
            return;
        }

        // try full match (shape + quantities)
        var fullMatch = Recipe.findMatch(this);
        if (fullMatch != null) {
            matchStatus = CraftingMatchStatus.FULL_MATCH;
            result = fullMatch.getResult(this);
            return;
        }

        // try shape-only match
        var shapeMatch = Recipe.findShapeMatch(this);
        if (shapeMatch != null) {
            matchStatus = CraftingMatchStatus.PARTIAL_MATCH;
            result = ItemStack.EMPTY;
            return;
        }

        // no match at all
        matchStatus = CraftingMatchStatus.NO_MATCH;
        result = ItemStack.EMPTY;
    }

    /** notify parent context that grid slots changed (for multiplayer sync) */
    public void notifyChanged() {
        parentCtx.notifyInventorySlotsChanged(this);
    }
}