using BlockGame.util;
using BlockGame.util.stuff;
using BlockGame.world.block;
using BlockGame.world.item.inventory;

namespace BlockGame.world.item;

public class Recipe {
    public static Recipe WOOD_PICKAXE;
    public static Recipe OAK_PLANKS;
    public static Recipe MAHOGANY_PLANKS;
    public static Recipe MAPLE_PLANKS;
    public static Recipe OAK_SLAB;
    public static Recipe MAHOGANY_SLAB;
    public static Recipe MAPLE_SLAB;
    public static Recipe STONE_SLAB;
    public static Recipe COBBLESTONE_SLAB;
    public static Recipe CANDY_SLAB;
    public static Recipe BASALT_SLAB;
    public static Recipe BRICKBLOCK_SLAB;

    public static Recipe OAK_STAIRS;
    public static Recipe MAHOGANY_STAIRS;
    public static Recipe MAPLE_STAIRS;
    public static Recipe STONE_STAIRS;
    public static Recipe COBBLESTONE_STAIRS;
    public static Recipe CANDY_STAIRS;
    public static Recipe BASALT_STAIRS;
    public static Recipe BRICKBLOCK_STAIRS;

    public static Recipe STICK;
    public static Recipe CRAFTING_TABLE;
    public static Recipe TORCH;
    public static Recipe OAK_CHEST;
    public static Recipe MAHOGANY_CHEST;
    public static Recipe GOLD_CANDY;
    public static Recipe BRICK_FURNACE;
    public static Recipe FURNACE;
    public static Recipe BRICKBLOCK;
    public static Recipe STONE_BRICK;
    public static Recipe STONE_BRICK_SLAB;
    public static Recipe STONE_BRICK_STAIRS;
    public static Recipe SAND_BRICK;
    public static Recipe SAND_BRICK_SLAB;
    public static Recipe SAND_BRICK_STAIRS;
    public static Recipe CINNABAR_CANDY;
    public static Recipe DIAMOND_CANDY;
    public static Recipe LIGHTER;
    public static Recipe BUCKET;
    public static Recipe LADDER;
    public static Recipe SIGN;
    public static Recipe OAK_DOOR;
    public static Recipe MAHOGANY_DOOR;
    public static Recipe BOTTLE;
    //public static Recipe BOW_WOOD;
    //public static Recipe ARROW_WOOD;
    //public static Recipe STRING;
    public static Recipe yellow_dye;
    public static Recipe orange_dye;
    public static Recipe sky_blue_dye;
    public static Recipe violet_dye;
    public static Recipe beige_dye;
    public static Recipe darkgreen_dye;
    public static Recipe LANTERN;

    public static XUList<Recipe> recipes => Registry.RECIPES.values;

    private ItemStack result;
    private int shapePattern; // encoded pattern (e.g., 111_020_020)
    private Item[] ingredientList = [];
    private int[] quantitiesList = []; // required quantity for each ingredient (default 1)
    private bool isShapeless;
    private int gridSize; // 2 for 2x2, 3 for 3x3

    public static void preLoad() {
        // dye mixing (any 2 dyes -> 2 of average colour)
        Registry.RECIPES.register("dye", new DyeRecipe());

        // candy block mixing (any 2 candy blocks -> 2 of average colour)
        Registry.RECIPES.register("candyblock", new CandyBlockRecipe());

        //dyes crafted from flowers
        yellow_dye =  register(new ItemStack(Item.DYE, 6, 6));
        yellow_dye.noShape();
        yellow_dye.ingredients(Block.YELLOW_FLOWER.item);

        orange_dye = register(new ItemStack(Item.DYE, 6, 5));
        orange_dye.noShape();
        orange_dye.ingredients(Block.MARIGOLD.item);

        sky_blue_dye = register(new ItemStack(Item.DYE, 6, 10));
        sky_blue_dye.noShape();
        sky_blue_dye.ingredients(Block.BLUE_TULIP.item);

        violet_dye = register(new ItemStack(Item.DYE, 6, 13));
        violet_dye.noShape();
        violet_dye.ingredients(Block.THISTLE.item);

        //white_dye = register(new ItemStack(Item.DYE, 6, 0));
        //white_dye.noShape();
        //white_dye.ingredients(Block.CALCITE.item);

        beige_dye = register(new ItemStack(Item.DYE, 6, 16));
        beige_dye.noShape();
        beige_dye.ingredients(Item.CLAY);

        darkgreen_dye = register(new ItemStack(Item.DYE, 6, 8));
        darkgreen_dye.noShape();
        darkgreen_dye.ingredients(Block.LEAVES.item);

       // shapeless: 1 log -> 4 planks
       OAK_PLANKS = register(new ItemStack(Block.OAK_PLANKS.item, 4));
       OAK_PLANKS.noShape();
       OAK_PLANKS.ingredients(Block.OAK_LOG.item);
        register(new ItemStack(Block.MAHOGANY_PLANKS.item, 4))
            .noShape()
            .ingredients(Block.MAHOGANY_LOG.item);
        register(new ItemStack(Block.MAPLE_PLANKS.item, 4))
            .noShape()
            .ingredients(Block.MAPLE_LOG.item);

       // stick (2 planks vertically) - any plank type works
        STICK = register(new ItemStack(Item.STICK, 4));
        STICK.shape(01_01, 2);
        STICK.ingredients(Block.OAK_PLANKS.item);
        register(new ItemStack(Item.STICK, 4))
            .shape(01_01, 2)
            .ingredients(Block.MAHOGANY_PLANKS.item);
        register(new ItemStack(Item.STICK, 4))
            .shape(01_01, 2)
            .ingredients(Block.MAPLE_PLANKS.item);

        // ladder (3 sticks in H shape)
        LADDER = register(new ItemStack(Block.LADDER.item, 1));
        LADDER.shape(101_111__101, 3);
        LADDER.ingredients(Item.STICK);

        // sign (6 planks and 1 stick) - any plank type works
        SIGN = register(new ItemStack(Item.SIGN_ITEM, 1));
        SIGN.shape(111_111_020, 3);
        SIGN.ingredients(Block.OAK_PLANKS.item, Item.STICK);
        register(new ItemStack(Block.SIGN.item, 1))
         .shape(111_111_020, 3)
         .ingredients(Block.MAHOGANY_PLANKS.item, Item.STICK);
        register(new ItemStack(Block.SIGN.item, 1))
            .shape(111_111_020, 3)
            .ingredients(Block.MAPLE_PLANKS.item, Item.STICK);

        // slab (3 planks horizontally) - any plank type works
        OAK_SLAB = register(new ItemStack(Block.OAK_SLAB.item, 1));
        OAK_SLAB.shape(000_000_111, 3);
        OAK_SLAB.ingredients(Block.OAK_PLANKS.item);
        register(new ItemStack(Block.MAHOGANY_SLAB.item, 1))
            .shape(000_000_111, 3)
            .ingredients(Block.MAHOGANY_PLANKS.item);
        register(new ItemStack(Block.MAPLE_SLAB.item, 1))
            .shape(000_000_111, 3)
            .ingredients(Block.MAPLE_PLANKS.item);
        register(new ItemStack(Block.STONE_SLAB.item, 1))
            .shape(000_000_111, 3)
            .ingredients(Block.STONE.item);
        register(new ItemStack(Block.COBBLESTONE_SLAB.item, 1))
            .shape(000_000_111, 3)
            .ingredients(Block.COBBLESTONE.item);
        register(new ItemStack(Block.CANDY_SLAB.item, 1))
            .shape(000_000_111, 3)
            .ingredients(Block.CANDY.item);
        register(new ItemStack(Block.BASALT_SLAB.item, 1))
            .shape(000_000_111, 3)
            .ingredients(Block.BASALT.item);
        register(new ItemStack(Block.BRICKBLOCK_SLAB.item, 1))
            .shape(000_000_111, 3)
            .ingredients(Block.BRICK_BLOCK.item);
        register(new ItemStack(Block.STONE_BRICK_SLAB.item, 1))
            .shape(000_000_111, 3)
            .ingredients(Block.STONE_BRICK.item);
        register(new ItemStack(Block.SAND_BRICK_SLAB.item, 1))
            .shape(000_000_111, 3)
            .ingredients(Block.SAND_BRICK.item);

        // stairs (1/2/3 planks horizontally) - any plank type works
        OAK_STAIRS = register(new ItemStack(Block.OAK_STAIRS.item, 1));
        OAK_STAIRS.shape(100_110_111, 3);
        OAK_STAIRS.ingredients(Block.OAK_PLANKS.item);
        register(new ItemStack(Block.MAHOGANY_STAIRS.item, 1))
            .shape(100_110_111, 3)
            .ingredients(Block.MAHOGANY_PLANKS.item);
        register(new ItemStack(Block.MAPLE_STAIRS.item, 1))
            .shape(100_110_111, 3)
            .ingredients(Block.MAPLE_PLANKS.item);
        register(new ItemStack(Block.STONE_STAIRS.item, 1))
            .shape(100_110_111, 3)
            .ingredients(Block.STONE.item);
        register(new ItemStack(Block.COBBLESTONE_STAIRS.item, 1))
            .shape(100_110_111, 3)
            .ingredients(Block.COBBLESTONE.item);
        register(new ItemStack(Block.CANDY_STAIRS.item, 1))
            .shape(100_110_111, 3)
            .ingredients(Block.CANDY.item);
        register(new ItemStack(Block.BASALT_STAIRS.item, 1))
            .shape(100_110_111, 3)
            .ingredients(Block.BASALT.item);
        register(new ItemStack(Block.BRICKBLOCK_STAIRS.item, 1))
            .shape(100_110_111, 3)
            .ingredients(Block.BRICK_BLOCK.item);
        register(new ItemStack(Block.STONE_BRICK_STAIRS.item, 1))
            .shape(100_110_111, 3)
            .ingredients(Block.STONE_BRICK.item);
        register(new ItemStack(Block.SAND_BRICK_STAIRS.item, 1))
            .shape(100_110_111, 3)
            .ingredients(Block.SAND_BRICK.item);

        // door (6 planks in 2x3)
        OAK_DOOR = register(new ItemStack(Item.OAK_DOOR, 1));
        OAK_DOOR.shape(110_110_110, 3);
        OAK_DOOR.ingredients(Block.OAK_PLANKS.item);
            register(new ItemStack(Item.MAHOGANY_DOOR,1))
            .shape(110_110_110, 3)
            .ingredients(Block.MAHOGANY_PLANKS.item);


        // crafting table (4 planks in square)
        CRAFTING_TABLE = register(new ItemStack(Block.CRAFTING_TABLE.item, 1));
        CRAFTING_TABLE.shape(11_11, 2);
        CRAFTING_TABLE.ingredients(Block.OAK_PLANKS.item);

        // torch (1 coal on top of 1 stick)
        TORCH = register(new ItemStack(Block.TORCH.item, 4));
        TORCH.shape(01_02, 2);
        TORCH.ingredients(Item.COAL, Item.STICK);

        OAK_CHEST = register(new ItemStack(Block.OAK_CHEST.item, 1));
        OAK_CHEST.shape(111_101_111, 3);
        OAK_CHEST.ingredients(Block.OAK_PLANKS.item);

        MAHOGANY_CHEST = register(new ItemStack(Block.MAHOGANY_CHEST.item, 1));
        MAHOGANY_CHEST.shape(111_101_111, 3);
        MAHOGANY_CHEST.ingredients(Block.MAHOGANY_PLANKS.item);

        BRICK_FURNACE = register(new ItemStack(Block.BRICK_FURNACE.item, 1));
        BRICK_FURNACE.shape(111_121_232, 3);
        BRICK_FURNACE.ingredients(Item.BRICK, Item.IRON_INGOT, Block.TORCH.item);

        FURNACE = register(new ItemStack(Block.FURNACE.item, 1));
        FURNACE.shape(111_101_111, 3);
        FURNACE.ingredients(Block.COBBLESTONE.item);

        BRICKBLOCK = register(new ItemStack(Block.BRICK_BLOCK.item, 1));
        BRICKBLOCK.shape(111_111_111, 3);
        BRICKBLOCK.ingredients(Item.BRICK);

        STONE_BRICK = register(new ItemStack(Block.STONE_BRICK.item, 1));
        STONE_BRICK.shape(111_111_111, 3);
        STONE_BRICK.ingredients(Block.STONE.item);

        SAND_BRICK = register(new ItemStack(Block.SAND_BRICK.item, 1));
        SAND_BRICK.shape(111_111_111, 3);
        SAND_BRICK.ingredients(Block.SAND.item);

        GOLD_CANDY = register(new ItemStack(Block.GOLD_CANDY.item, 1));
        GOLD_CANDY.noShape();
        GOLD_CANDY.ingredients(Block.CANDY.item, Item.GOLD_INGOT);

        CINNABAR_CANDY = register(new ItemStack(Block.CINNABAR_CANDY.item, 1));
        CINNABAR_CANDY.noShape();
        CINNABAR_CANDY.ingredients(Block.CANDY.item, Item.CINNABAR);

        DIAMOND_CANDY = register(new ItemStack(Block.DIAMOND_CANDY.item, 1));
        DIAMOND_CANDY.noShape();
        DIAMOND_CANDY.ingredients(Block.CANDY.item, Item.DIAMOND);

        BUCKET = register(new ItemStack(Item.BUCKET, 1));
        BUCKET.shape(000_101_010, 3);
        BUCKET.ingredients(Item.IRON_INGOT);

        BOTTLE = register(new ItemStack(Item.BOTTLE, 3));
        BOTTLE.shape(000_101_010, 3);
        BOTTLE.ingredients(Block.GLASS.item);

        LIGHTER = register(new ItemStack(Item.LIGHTER, 1));
        LIGHTER.shape(000_010_202, 3);
        LIGHTER.ingredients(Item.FLINT, Block.STONE.item);

        LANTERN = register(new ItemStack(Block.LANTERN.item, 1));
        LANTERN.shape(141_222_131, 3);
        LANTERN.ingredients(Item.IRON_INGOT, Block.GLASS.item, Block.TORCH.item, Item.DYE); // any dye

        //BOW_WOOD = register(new ItemStack(Item.BOW_WOOD, 1));
        //BOW_WOOD.shape(012_102_012, 3);
        //BOW_WOOD.ingredients(Item.STICK, Item.STRING);

        //ARROW_WOOD = register(new ItemStack(Item.ARROW_WOOD, 1));
        //ARROW_WOOD.shape(010_020_030, 3);
        //ARROW_WOOD.ingredients(Item.FLINT, Item.STICK, Item.FEATHER);

        //STRING = register(new ItemStack(Item.STRING, 9));
        //STRING.noShape();
        //STRING.ingredients(Item.IRON_INGOT);


        // tools
        tool(Item.WOOD_PICKAXE, Block.OAK_PLANKS.item, 111_020_020);
        tool(Item.WOOD_AXE, Block.OAK_PLANKS.item, 110_120_020);
        tool(Item.WOOD_SHOVEL, Block.OAK_PLANKS.item, 010_020_020);
        tool(Item.WOOD_SWORD, Block.OAK_PLANKS.item, 010_010_020);

        tool(Item.STONE_PICKAXE, Block.COBBLESTONE.item, 111_020_020);
        tool(Item.STONE_AXE, Block.COBBLESTONE.item, 110_120_020);
        tool(Item.STONE_SHOVEL, Block.COBBLESTONE.item, 010_020_020);
        tool(Item.STONE_SWORD, Block.COBBLESTONE.item, 010_010_020);
        tool(Item.STONE_HOE, Block.COBBLESTONE.item, 110_020_020);
        tool(Item.STONE_SCYTHE, Block.COBBLESTONE.item, 111_002_002);

        tool(Item.COPPER_PICKAXE, Item.COPPER_INGOT, 111_020_020, 1);
        tool(Item.COPPER_AXE, Item.COPPER_INGOT, 110_120_020, 1);
        tool(Item.COPPER_SHOVEL, Item.COPPER_INGOT, 010_020_020, 1);
        tool(Item.COPPER_SWORD, Item.COPPER_INGOT, 010_010_020, 1);
        tool(Item.COPPER_HOE, Item.COPPER_INGOT, 110_020_020, 1);
        tool(Item.COPPER_SCYTHE, Item.COPPER_INGOT, 111_002_002, 1);

        tool(Item.IRON_PICKAXE, Item.IRON_INGOT, 111_020_020, 2);
        tool(Item.IRON_AXE, Item.IRON_INGOT, 110_120_020, 2);
        tool(Item.IRON_SHOVEL, Item.IRON_INGOT, 010_020_020, 2);
        tool(Item.IRON_SWORD, Item.IRON_INGOT, 010_010_020, 2);
        tool(Item.IRON_HOE, Item.IRON_INGOT, 110_020_020, 2);
        tool(Item.IRON_SCYTHE, Item.IRON_INGOT, 111_002_002, 2);

        tool(Item.GOLD_PICKAXE, Item.GOLD_INGOT, 111_020_020, 3);
        tool(Item.GOLD_AXE, Item.GOLD_INGOT, 110_120_020, 3);
        tool(Item.GOLD_SHOVEL, Item.GOLD_INGOT, 010_020_020, 3);
        tool(Item.GOLD_SWORD, Item.GOLD_INGOT, 010_010_020, 3);
        tool(Item.GOLD_HOE, Item.GOLD_INGOT, 110_020_020, 3);
        tool(Item.GOLD_SCYTHE, Item.GOLD_INGOT, 111_002_002, 3);

        // ore blocks
        register(new ItemStack(Block.COPPER_BLOCK.item, 1))
            .shape(111_111_111, 3)
            .ingredients(Item.COPPER_INGOT);

        register(new ItemStack(Block.IRON_BLOCK.item, 1))
            .shape(111_111_111, 3)
            .ingredients(Item.IRON_INGOT);

        register(new ItemStack(Block.GOLD_BLOCK.item, 1))
            .shape(111_111_111, 3)
            .ingredients(Item.GOLD_INGOT);

        register(new ItemStack(Block.DIAMOND_BLOCK.item, 1))
            .shape(111_111_111, 3)
            .ingredients(Item.DIAMOND);

        register(new ItemStack(Block.COAL_BLOCK.item, 1))
            .shape(111_111_111, 3)
            .ingredients(Item.COAL);

    }




    private static Recipe tool(Item result, Item material, int shape, int q = 1) {
        int gridSize = shape > 9999 ? 3 : 2;
        var r = register(result);
        r.shape(shape, gridSize);
        r.ingredients(material, Item.STICK);
        r.quantities(q, 1);
        return r;
    }

    // todo add ItemStack result too for metadata items, maybe NBT items in the future too...

    private static int recipeCounter = 0;

    public static Recipe register(Item result) {
        return register(new ItemStack(result, 1));
    }

    public static Recipe register(ItemStack result) {
        var recipe = new Recipe { result = result };
        // generate id from result item string id + counter
        var stringId = Registry.ITEMS.getName(result.id);
        var id = $"{stringId}_{recipeCounter++}";
        Registry.RECIPES.register(id, recipe);
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
        quantitiesList = quantities.ToArray();
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