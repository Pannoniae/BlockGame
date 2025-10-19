using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.item.inventory;
using JetBrains.Annotations;

namespace BlockGame.world.item;

public class Recipe {
    public static Recipe WOOD_PICKAXE;
    public static Recipe PLANKS;
    public static Recipe CRAFTING_TABLE;
    public static Recipe TORCH;
    public static Recipe STICK;


    public static readonly List<Recipe> recipes = [];

    private ItemStack result;
    private int shapePattern; // encoded pattern (e.g., 111_020_020)
    private Item[] ingredientList = [];
    private int[] quantitiesList = []; // required quantity for each ingredient (default 1)
    private bool isShapeless;
    private int gridSize; // 2 for 2x2, 3 for 3x3

    public static void preLoad() {
        // dye mixing (any 2 dyes -> 2 of average colour)
        recipes.Add(new DyeRecipe());

        // shapeless: 1 log -> 4 planks
        PLANKS = register(new ItemStack(Item.block(Blocks.PLANKS), 4));
        PLANKS.noShape();
        PLANKS.ingredients(Item.block(Blocks.LOG));

        // crafting table (4 planks in square)
        CRAFTING_TABLE = register(new ItemStack(Item.block(Blocks.CRAFTING_TABLE), 1));
        CRAFTING_TABLE.shape(11_11, 2);
        CRAFTING_TABLE.ingredients(Item.block(Blocks.PLANKS));

        // torch (1 coal on top of 1 stick)
        TORCH = register(new ItemStack(Item.block(Blocks.TORCH), 4));
        TORCH.shape(01_02, 2);
        TORCH.ingredients(Item.COAL, Item.STICK);

        // stick (2 planks vertically)
        STICK = register(new ItemStack(Item.STICK, 4));
        STICK.shape(01_01, 2);
        STICK.ingredients(Item.block(Blocks.PLANKS));

        // tools
        tool(Item.WOOD_PICKAXE, Item.block(Blocks.PLANKS), 111_020_020);
        tool(Item.WOOD_AXE, Item.block(Blocks.PLANKS), 011_021_020);
        tool(Item.WOOD_AXE, Item.block(Blocks.PLANKS), 110_120_020);
        tool(Item.WOOD_SHOVEL, Item.block(Blocks.PLANKS), 010_020_020);
        tool(Item.WOOD_SWORD, Item.block(Blocks.PLANKS), 010_010_020);

        tool(Item.STONE_PICKAXE, Item.block(Blocks.STONE), 111_020_020);
        tool(Item.STONE_AXE, Item.block(Blocks.STONE), 011_021_020);
        tool(Item.STONE_AXE, Item.block(Blocks.STONE), 110_120_020);
        tool(Item.STONE_SHOVEL, Item.block(Blocks.STONE), 010_020_020);
        tool(Item.STONE_SWORD, Item.block(Blocks.STONE), 010_010_020);
        tool(Item.STONE_HOE, Item.block(Blocks.STONE), 110_020_020);
        tool(Item.STONE_SCYTHE, Item.block(Blocks.STONE), 111_002_002);

        tool(Item.COPPER_PICKAXE, Item.COPPER_INGOT, 111_020_020);
        tool(Item.COPPER_AXE, Item.COPPER_INGOT, 011_021_020);
        tool(Item.COPPER_AXE, Item.COPPER_INGOT, 110_120_020);
        tool(Item.COPPER_SHOVEL, Item.COPPER_INGOT, 010_020_020);
        tool(Item.COPPER_SWORD, Item.COPPER_INGOT, 010_010_020);

        tool(Item.IRON_PICKAXE, Item.IRON_INGOT, 111_020_020);
        tool(Item.GOLD_PICKAXE, Item.GOLD_INGOT, 111_020_020);
    }


    private static Recipe tool(Item result, Item material, int shape) {
        int gridSize = shape > 9999 ? 3 : 2;
        var r = register(result);
        r.shape(shape, gridSize);
        r.ingredients(material, Item.STICK);
        return r;
    }

    // todo add ItemStack result too for metadata items, maybe NBT items in the future too...

    public static Recipe register(Item result) {
        return register(new ItemStack(result, 1));
    }

    public static Recipe register(ItemStack result) {
        var recipe = new Recipe { result = result };
        recipes.Add(recipe);
        return recipe;
    }

    public Recipe shape(int shape, int gridSize) {
        shapePattern = shape;
        this.gridSize = gridSize;
        isShapeless = false;
        return this;
    }

    public Recipe noShape() {
        isShapeless = true;
        gridSize = 2; // shapeless recipes work on 2x2+ grids
        return this;
    }

    public Recipe ingredients(params ReadOnlySpan<Item> ingredients) {
        ingredientList = ingredients.ToArray();
        // default all quantities to 1
        quantitiesList = new int[ingredientList.Length];
        Array.Fill(quantitiesList, 1);
        return this;
    }

    public Recipe quantities(params ReadOnlySpan<int> quantities) {
        this.quantitiesList = quantities.ToArray();
        return this;
    }


    public virtual bool matches(CraftingGridInventory grid) {
        if (isShapeless) {
            return matchesShapeless(grid);
        }

        return matchesShaped(grid, checkQty:true);
    }

    /** Matches shape only, ignoring quantities */
    public virtual bool matchesShape(CraftingGridInventory grid) {
        if (isShapeless) {
            return matchesShapelessShape(grid);
        }

        return matchesShaped(grid, checkQty:false);
    }

    /** Find matching recipe for given grid, or null */
    public static Recipe? findMatch(CraftingGridInventory grid) {
        foreach (var recipe in recipes) {
            if (recipe.matches(grid)) {
                return recipe;
            }
        }

        return null;
    }

    /** Find recipe that matches shape (ignoring quantities), or null */
    public static Recipe? findShapeMatch(CraftingGridInventory grid) {
        foreach (var recipe in recipes) {
            if (recipe.matchesShape(grid)) {
                return recipe;
            }
        }

        return null;
    }

    private bool matchesShaped(CraftingGridInventory grid, bool checkQty) {
        var digits = extractDigits(shapePattern, gridSize * gridSize);

        // trim to minimal bounding box
        (int minRow, int maxRow, int minCol, int maxCol) = findBounds(digits, gridSize);
        int patternHeight = maxRow - minRow + 1;
        int patternWidth = maxCol - minCol + 1;

        // pattern must fit in grid
        if (grid.rows < patternHeight || grid.cols < patternWidth) {
            return false;
        }

        // try all offsets where trimmed pattern could fit
        for (int offsetRow = 0; offsetRow <= grid.rows - patternHeight; offsetRow++) {
            for (int offsetCol = 0; offsetCol <= grid.cols - patternWidth; offsetCol++) {
                if (matchesAt(grid, digits, gridSize, minRow, minCol, patternHeight, patternWidth, offsetRow,
                        offsetCol, checkQty)) {
                    return true;
                }
            }
        }

        return false;
    }

    private bool matchesAt(CraftingGridInventory grid, int[] pattern, int patternGridSize,
        int minRow, int minCol, int height, int width,
        int offsetRow, int offsetCol, bool checkQty) {
        for (int r = 0; r < grid.rows; r++) {
            for (int c = 0; c < grid.cols; c++) {
                int gridIdx = r * grid.cols + c;
                var slot = grid.grid[gridIdx];

                bool inPattern = r >= offsetRow && r < offsetRow + height &&
                                 c >= offsetCol && c < offsetCol + width;

                if (inPattern) {
                    int pr = r - offsetRow;
                    int pc = c - offsetCol;
                    int origPatternIdx = (minRow + pr) * patternGridSize + (minCol + pc);
                    int patternIdx = pattern[origPatternIdx];

                    if (patternIdx == 0) {
                        if (!isEmpty(slot)) {
                            return false;
                        }
                    }
                    else {
                        if (patternIdx - 1 >= ingredientList.Length) {
                            return false;
                        }

                        var requiredItem = ingredientList[patternIdx - 1];

                        if (isEmpty(slot) || slot.id != requiredItem.id) {
                            return false;
                        }

                        if (checkQty) {
                            var requiredQty = (patternIdx - 1 < quantitiesList.Length) ? quantitiesList[patternIdx - 1] : 1;
                            if (slot.quantity < requiredQty) {
                                return false;
                            }
                        }
                    }
                }
                else {
                    // outside pattern must be empty
                    if (!isEmpty(slot)) return false;
                }
            }
        }

        return true;
    }

    private static (int minRow, int maxRow, int minCol, int maxCol) findBounds(int[] pattern, int size) {
        int minRow = size, maxRow = -1, minCol = size, maxCol = -1;

        for (int r = 0; r < size; r++) {
            for (int c = 0; c < size; c++) {
                if (pattern[r * size + c] != 0) {
                    if (r < minRow) minRow = r;
                    if (r > maxRow) maxRow = r;
                    if (c < minCol) minCol = c;
                    if (c > maxCol) maxCol = c;
                }
            }
        }

        return (minRow, maxRow, minCol, maxCol);
    }

    private bool matchesShapeless(CraftingGridInventory grid) {
        var gridItems = new Dictionary<int, int>();
        var requiredItems = new Dictionary<int, int>();

        // sum quantities in grid
        foreach (var slot in grid.grid) {
            if (!isEmpty(slot)) {
                gridItems[slot.id] = gridItems.GetValueOrDefault(slot.id) + slot.quantity;
            }
        }

        // sum required quantities
        for (int i = 0; i < ingredientList.Length; i++) {
            var item = ingredientList[i];
            var qty = quantitiesList.Length > 0 ? quantitiesList[i] : 1;
            requiredItems[item.id] = requiredItems.GetValueOrDefault(item.id) + qty;
        }

        if (gridItems.Count != requiredItems.Count) return false;
        foreach (var (id, count) in requiredItems) {
            if (gridItems.GetValueOrDefault(id) < count) return false;
        }

        return true;
    }


    private bool matchesShapelessShape(CraftingGridInventory grid) {
        var gridItems = new HashSet<int>();
        var requiredItems = new HashSet<int>();

        // collect item types in grid
        foreach (var slot in grid.grid) {
            if (!isEmpty(slot)) {
                gridItems.Add(slot.id);
            }
        }

        // collect required item types
        foreach (var item in ingredientList) {
            requiredItems.Add(item.id);
        }

        // check if sets match
        return gridItems.SetEquals(requiredItems);
    }

    private static int[] extractDigits(int pattern, int count) {
        var digits = new int[count];
        for (int i = count - 1; i >= 0; i--) {
            digits[i] = pattern % 10;
            pattern /= 10;
        }

        return digits;
    }

    private static bool isEmpty(ItemStack slot) => slot == ItemStack.EMPTY || slot.quantity <= 0;

    public ItemStack getResult() => result.copy();

    /** Get result based on grid contents (for dynamic recipes) */
    public virtual ItemStack getResult(CraftingGridInventory grid) => getResult();

    /** Consumes ingredients from the grid for this recipe */
    public virtual void consumeIngredients(CraftingGridInventory grid) {
        if (isShapeless) {
            consumeShapeless(grid);
        }
        else {
            consumeShaped(grid);
        }
    }

    private void consumeShaped(CraftingGridInventory grid) {
        var digits = extractDigits(shapePattern, gridSize * gridSize);

        // find bounds and try all offsets to find the match
        (int minRow, int maxRow, int minCol, int maxCol) = findBounds(digits, gridSize);
        int patternHeight = maxRow - minRow + 1;
        int patternWidth = maxCol - minCol + 1;

        // find the offset where pattern matches
        for (int offsetRow = 0; offsetRow <= grid.rows - patternHeight; offsetRow++) {
            for (int offsetCol = 0; offsetCol <= grid.cols - patternWidth; offsetCol++) {
                if (matchesAt(grid, digits, gridSize, minRow, minCol, patternHeight, patternWidth, offsetRow,
                        offsetCol, checkQty:true)) {
                    // found the match, consume ingredients at this offset
                    consumeAtOffset(grid, digits, gridSize, minRow, minCol, patternHeight, patternWidth, offsetRow,
                        offsetCol);
                    return;
                }
            }
        }
    }

    private void consumeAtOffset(CraftingGridInventory grid, int[] pattern, int patternGridSize,
        int minRow, int minCol, int height, int width,
        int offsetRow, int offsetCol) {
        for (int r = 0; r < grid.rows; r++) {
            for (int c = 0; c < grid.cols; c++) {
                int gridIdx = r * grid.cols + c;

                bool inPattern = r >= offsetRow && r < offsetRow + height &&
                                 c >= offsetCol && c < offsetCol + width;

                if (inPattern) {
                    int pr = r - offsetRow;
                    int pc = c - offsetCol;
                    int origPatternIdx = (minRow + pr) * patternGridSize + (minCol + pc);
                    int patternIdx = pattern[origPatternIdx];

                    if (patternIdx != 0) {
                        var requiredQty = (patternIdx - 1 < quantitiesList.Length) ? quantitiesList[patternIdx - 1] : 1;
                        grid.grid[gridIdx].quantity -= requiredQty;
                        if (grid.grid[gridIdx].quantity <= 0) {
                            grid.grid[gridIdx] = ItemStack.EMPTY;
                        }
                    }
                }
            }
        }
    }

    private void consumeShapeless(CraftingGridInventory grid) {
        var requiredItems = new Dictionary<int, int>();

        // sum required quantities
        for (int i = 0; i < ingredientList.Length; i++) {
            var item = ingredientList[i];
            var qty = quantitiesList.Length > 0 ? quantitiesList[i] : 1;
            requiredItems[item.id] = requiredItems.GetValueOrDefault(item.id) + qty;
        }

        // consume from grid
        foreach (var (itemId, neededQty) in requiredItems) {
            int remaining = neededQty;

            for (int i = 0; i < grid.grid.Length && remaining > 0; i++) {
                var slot = grid.grid[i];
                if (!isEmpty(slot) && slot.id == itemId) {
                    int consume = Math.Min(remaining, slot.quantity);
                    slot.quantity -= consume;
                    remaining -= consume;

                    if (slot.quantity <= 0) {
                        grid.grid[i] = ItemStack.EMPTY;
                    }
                }
            }
        }
    }
}