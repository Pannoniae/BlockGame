using System.Diagnostics.CodeAnalysis;
using BlockGame.util;
using BlockGame.world.block;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace BlockGame.world.item;

/**
 * Items have IDs from 1 and up,
 * blocks have IDs from -1 and down.
 *
 * itemID = -blockID
 */
[SuppressMessage("Compiler",
    "CS8618:Non-nullable field must contain a non-null value when exiting constructor. Consider adding the \'required\' modifier or declaring as nullable.")]
public class Item {
    public int id;
    public string name;

    public UVPair tex = new UVPair(0, 0);

    /**
     * Problem is, we can't get the actual name without an itemstack, because the metadata might change the name (e.g. different colours of candy).
     */
    public virtual string getName(ItemStack stack) => name;

    private const int INITIAL_ITEM_CAPACITY = 128;
    private const int GROW_SIZE = 128;
    private static int MAXITEMS = INITIAL_ITEM_CAPACITY;
    private static int RMAXITEMS = MAXITEMS * 2 + 1;

    /**
     * Gets the index into the items arrays.
     */
    public int idx => id + MAXITEMS;

    /**
     * We "cheat", half of this goes for blocks (the negatives), half for items.
     * We shift it automatically in register, shh.
     */
    public static Item[] items = new Item[INITIAL_ITEM_CAPACITY * 2 + 1];

    /** is this item armour? */
    public static bool[] armour = new bool[INITIAL_ITEM_CAPACITY * 2 + 1];

    /** is this item an accessory? */
    public static bool[] accessory = new bool[INITIAL_ITEM_CAPACITY * 2 + 1];

    /**
     * is this item a material (used for crafting or a placeable building block)
     * if true, player drops it on death.
     */
    public static bool[] material = new bool[INITIAL_ITEM_CAPACITY * 2 + 1];

    public static int currentID = 0;

    public static Item AIR;
    public static Item GOLD_INGOT;
    public static Item IRON_INGOT;
    public static Item TIN_INGOT;
    public static Item SILVER_INGOT;
    public static Item COPPER_INGOT;
    public static Tool GOLD_PICKAXE;
    public static Tool IRON_PICKAXE;
    public static Tool STONE_PICKAXE;
    public static Tool STONE_AXE;
    public static Tool STONE_SHOVEL;
    public static Item STONE_SWORD;
    public static Tool STONE_HOE;
    public static Tool STONE_SCYTHE;
    public static Tool WOOD_PICKAXE;
    public static Tool WOOD_AXE;
    public static Tool WOOD_SHOVEL;
    public static Item WOOD_SWORD;
    public static Item STICK;
    public static Item COPPER_PICKAXE;
    public static Item COPPER_AXE;
    public static Item COPPER_SHOVEL;
    public static Item COPPER_SWORD;
    public static Item COPPER_HOE;
    public static Item COPPER_SCYTHE;
    public static Tool IRON_AXE;
    public static Tool IRON_SHOVEL;
    public static Item IRON_SWORD;
    public static Tool IRON_HOE;
    public static Tool IRON_SCYTHE;
    public static Tool GOLD_AXE;
    public static Tool GOLD_SHOVEL;
    public static Item GOLD_SWORD;
    public static Tool GOLD_HOE;
    public static Tool GOLD_SCYTHE;
    public static DyeItem DYE;
    public static Item APPLE;
    public static Item MAPLE_SYRUP;
    public static Item COAL;
    public static Item BRICK;
    public static Item CLAY;
    public static Item DIAMOND;
    public static Item CINNABAR;

    public Item(int id, string name) {
        this.id = id;
        this.name = name;
    }

    private static void ensureCapacity(int id) {
        int idx = id + MAXITEMS;
        if (idx >= 0 && idx < items.Length) return;

        // need more space - grow all arrays
        int newMaxItems = MAXITEMS + GROW_SIZE;
        int newRMaxItems = newMaxItems * 2 + 1;

        var newItems = new Item[newRMaxItems];
        var newArmour = new bool[newRMaxItems];
        var newAccessory = new bool[newRMaxItems];
        var newMaterial = new bool[newRMaxItems];

        // copy old data to centre of new arrays
        int offset = newMaxItems - MAXITEMS;
        for (int i = 0; i < items.Length; i++) {
            newItems[i + offset] = items[i];
            newArmour[i + offset] = armour[i];
            newAccessory[i + offset] = accessory[i];
            newMaterial[i + offset] = material[i];
        }

        items = newItems;
        armour = newArmour;
        accessory = newAccessory;
        material = newMaterial;
        MAXITEMS = newMaxItems;
        RMAXITEMS = newRMaxItems;
    }

    public static Item register(Item item) {
        ensureCapacity(item.id);

        // handles negatives correctly!
        if (item.id >= currentID) {
            currentID = item.id + 1;
        }

        return items[getIdx(item.id)] = item;
    }

    private static int getIdx(int id) {
        return id + MAXITEMS;
    }

    public static Item get(int i) {
        if (i <= -MAXITEMS || i >= MAXITEMS) {
            return null!;
        }

        return items[getIdx(i)];
    }

    public static Item block(int blockID) {
        return get(-blockID);
    }

    public static int blockID(int blockID) {
        return -blockID;
    }

    public bool isBlock() => id < 0 && -id < Block.currentID && Block.blocks[-id] != null;

    public bool isItem() => id > 0 && id < currentID && items[getIdx(id)] != null;

    public int getBlockID() => isBlock() ? -id : 0;

    public Block getBlock() => isBlock() ? Block.blocks[-id] : null!;

    public static void preLoad() {
        AIR = register(new Item(Items.AIR, "Air"));

        GOLD_INGOT = new Item(Items.GOLD_INGOT, "Gold Ingot");
        GOLD_INGOT.tex = new UVPair(0, 0);
        register(GOLD_INGOT);
        material[GOLD_INGOT.idx] = true;

        IRON_INGOT = new Item(Items.IRON_INGOT, "Iron Ingot");
        IRON_INGOT.tex = new UVPair(1, 0);
        register(IRON_INGOT);
        material[IRON_INGOT.idx] = true;

        TIN_INGOT = new Item(Items.TIN_INGOT, "Tin Ingot");
        TIN_INGOT.tex = new UVPair(2, 0);
        register(TIN_INGOT);
        material[TIN_INGOT.idx] = true;


        SILVER_INGOT = new Item(Items.SILVER_INGOT, "Silver Ingot");
        SILVER_INGOT.tex = new UVPair(3, 0);
        register(SILVER_INGOT);
        material[SILVER_INGOT.idx] = true;


        COPPER_INGOT = new Item(Items.COPPER_INGOT, "Copper Ingot");
        COPPER_INGOT.tex = new UVPair(4, 0);
        register(COPPER_INGOT);
        material[COPPER_INGOT.idx] = true;


        GOLD_PICKAXE = new Tool(Items.GOLD_PICKAXE, "Gold Pickaxe", ToolType.PICKAXE, MaterialTier.GOLD, 2f);
        GOLD_PICKAXE.tex = new UVPair(2, 7);
        register(GOLD_PICKAXE);

        IRON_PICKAXE = new Tool(Items.IRON_PICKAXE, "Iron Pickaxe", ToolType.PICKAXE, MaterialTier.IRON, 1.7f);
        IRON_PICKAXE.tex = new UVPair(2, 6);
        register(IRON_PICKAXE);

        STONE_PICKAXE = new Tool(Items.STONE_PICKAXE, "Stone Pickaxe", ToolType.PICKAXE, MaterialTier.STONE, 1.25);
        STONE_PICKAXE.tex = new UVPair(2, 3);
        register(STONE_PICKAXE);

        STONE_AXE = new Tool(Items.STONE_AXE, "Stone Axe", ToolType.AXE, MaterialTier.STONE, 1.25);
        STONE_AXE.tex = new UVPair(3, 3);
        register(STONE_AXE);

        STONE_SHOVEL = new Tool(Items.STONE_SHOVEL, "Stone Shovel", ToolType.SHOVEL, MaterialTier.STONE, 1.25);
        STONE_SHOVEL.tex = new UVPair(4, 3);
        register(STONE_SHOVEL);

        STONE_SWORD = new Item(Items.STONE_SWORD, "Stone Sword");
        STONE_SWORD.tex = new UVPair(5, 3);
        register(STONE_SWORD);

        STONE_HOE = new Tool(Items.STONE_HOE, "Stone Hoe", ToolType.HOE, MaterialTier.STONE, 1.25);
        STONE_HOE.tex = new UVPair(6, 3);
        register(STONE_HOE);

        STONE_SCYTHE = new Tool(Items.STONE_SCYTHE, "Stone Scythe", ToolType.HOE, MaterialTier.STONE, 1.25);
        STONE_SCYTHE.tex = new UVPair(7, 3);
        register(STONE_SCYTHE);

        WOOD_PICKAXE = new Tool(Items.WOOD_PICKAXE, "Wood Pickaxe", ToolType.PICKAXE, MaterialTier.WOOD, 1.0);
        WOOD_PICKAXE.tex = new UVPair(2, 4);
        register(WOOD_PICKAXE);

        WOOD_AXE = new Tool(Items.WOOD_AXE, "Wood Axe", ToolType.AXE, MaterialTier.WOOD, 1.0);
        WOOD_AXE.tex = new UVPair(3, 4);
        register(WOOD_AXE);

        WOOD_SHOVEL = new Tool(Items.WOOD_SHOVEL, "Wood Shovel", ToolType.SHOVEL, MaterialTier.WOOD, 1.0);
        WOOD_SHOVEL.tex = new UVPair(4, 4);
        register(WOOD_SHOVEL);

        WOOD_SWORD = new Item(Items.WOOD_SWORD, "Wood Sword");
        WOOD_SWORD.tex = new UVPair(5, 4);
        register(WOOD_SWORD);

        STICK = new Item(Items.STICK, "Stick");
        STICK.tex = new UVPair(0, 8);
        register(STICK);
        material[STICK.idx] = true;

        COPPER_PICKAXE = new Tool(Items.COPPER_PICKAXE, "Copper Pickaxe", ToolType.PICKAXE, MaterialTier.WOOD, 1.5);
        COPPER_PICKAXE.tex = new UVPair(2, 5);
        register(COPPER_PICKAXE);

        COPPER_AXE = new Tool(Items.COPPER_AXE, "Copper Axe", ToolType.AXE, MaterialTier.WOOD, 1.5);
        COPPER_AXE.tex = new UVPair(3, 5);
        register(COPPER_AXE);

        COPPER_SHOVEL = new Tool(Items.COPPER_SHOVEL, "Copper Shovel", ToolType.SHOVEL, MaterialTier.WOOD, 1.5);
        COPPER_SHOVEL.tex = new UVPair(4, 5);
        register(COPPER_SHOVEL);

        COPPER_SWORD = new Item(Items.COPPER_SWORD, "Copper Sword");
        COPPER_SWORD.tex = new UVPair(5, 5);
        register(COPPER_SWORD);

        COPPER_HOE = new Item(Items.COPPER_HOE, "Copper Hoe");
        COPPER_HOE.tex = new UVPair(6, 5);
        register(COPPER_HOE);

        COPPER_SCYTHE = new Item(Items.COPPER_SCYTHE, "Copper Scythe");
        COPPER_SCYTHE.tex = new UVPair(7, 5);
        register(COPPER_SCYTHE);

        IRON_AXE = new Tool(Items.IRON_AXE, "Iron Axe", ToolType.AXE, MaterialTier.IRON, 2.5);
        IRON_AXE.tex = new UVPair(3, 6);
        register(IRON_AXE);

        IRON_SHOVEL = new Tool(Items.IRON_SHOVEL, "Iron Shovel", ToolType.SHOVEL, MaterialTier.IRON, 2.5);
        IRON_SHOVEL.tex = new UVPair(4, 6);
        register(IRON_SHOVEL);

        IRON_SWORD = new Item(Items.IRON_SWORD, "Iron Sword");
        IRON_SWORD.tex = new UVPair(5, 6);
        register(IRON_SWORD);

        IRON_HOE = new Tool(Items.IRON_HOE, "Iron Hoe", ToolType.HOE, MaterialTier.IRON, 2.5);
        IRON_HOE.tex = new UVPair(6, 6);
        register(IRON_HOE);

        IRON_SCYTHE = new Tool(Items.IRON_SCYTHE, "Iron Scythe", ToolType.HOE, MaterialTier.IRON, 2.5);
        IRON_SCYTHE.tex = new UVPair(7, 6);
        register(IRON_SCYTHE);

        GOLD_AXE = new Tool(Items.GOLD_AXE, "Gold Axe", ToolType.AXE, MaterialTier.GOLD, 3.0);
        GOLD_AXE.tex = new UVPair(3, 7);
        register(GOLD_AXE);

        GOLD_SHOVEL = new Tool(Items.GOLD_SHOVEL, "Gold Shovel", ToolType.SHOVEL, MaterialTier.GOLD, 3.0);
        GOLD_SHOVEL.tex = new UVPair(4, 7);
        register(GOLD_SHOVEL);

        GOLD_SWORD = new Item(Items.GOLD_SWORD, "Gold Sword");
        GOLD_SWORD.tex = new UVPair(5, 7);
        register(GOLD_SWORD);

        GOLD_HOE = new Tool(Items.GOLD_HOE, "Gold Hoe", ToolType.HOE, MaterialTier.GOLD, 3.0);
        GOLD_HOE.tex = new UVPair(6, 7);
        register(GOLD_HOE);

        GOLD_SCYTHE = new Tool(Items.GOLD_SCYTHE, "Gold Scythe", ToolType.HOE, MaterialTier.GOLD, 3.0);
        GOLD_SCYTHE.tex = new UVPair(7, 7);
        register(GOLD_SCYTHE);

        DYE = new DyeItem(Items.DYE, "Dye");
        register(DYE);

        APPLE = new Item(Items.APPLE, "Apple");
        APPLE.tex = new UVPair(2, 9);
        register(APPLE);

        MAPLE_SYRUP = new Item(Items.MAPLE_SYRUP, "Maple Syrup");
        MAPLE_SYRUP.tex = new UVPair(3, 9);
        register(MAPLE_SYRUP);

        COAL = new Item(Items.COAL, "Coal");
        COAL.tex = new UVPair(1, 9);
        register(COAL);
        material[COAL.idx] = true;

        BRICK = new Item(Items.BRICK, "Brick");
        BRICK.tex = new UVPair(5, 0);
        register(BRICK);
        material[BRICK.idx] = true;

        CLAY = new Item(Items.CLAY, "Clay");
        CLAY.tex = new UVPair(4, 9);
        register(CLAY);
        material[CLAY.idx] = true;

        DIAMOND = new Item(Items.DIAMOND, "Diamond");
        DIAMOND.tex = new UVPair(5, 9);
        register(DIAMOND);
        material[DIAMOND.idx] = true;

        CINNABAR = new Item(Items.CINNABAR, "Cinnabar");
        CINNABAR.tex = new UVPair(6, 9);
        register(CINNABAR);
        material[CINNABAR.idx] = true;


        // mark materials (items that drop on death in survival)
        // ingots and crafting materials

        // all blocks are materials
        for (int i = 0; i < Block.currentID; i++) {
            var block = Block.get(i);
            if (block != null) {
                var item = Item.block(i);
                if (item != null) {
                    material[item.idx] = true;
                }
            }
        }
    }

    public virtual UVPair getTexture(ItemStack stack) {
        if (isBlock()) {
            var blockID = getBlockID();
            if (Block.renderItemLike[blockID]) {
                return getBlock().getTexture(0, stack.metadata);
            }
        }

        return tex;
    }

    /**
     * What a meaty method lol. Called when the player uses an item on a block.
     */
    public virtual void useBlock(ItemStack stack, World world, Player player, int x, int y, int z, RawDirection dir) {
    }

    /**
     * Called when the player uses an item in the air (not on a block).
     */
    public virtual void use(ItemStack stack, World world, Player player) {
    }

    /**
     * How fast this item can break the given block. 1.0 is default, higher is faster, lower is slower.
     */
    public virtual double getBreakSpeed(ItemStack stack, Block block) {
        return 1.0;
    }

    /**
     * Can this item actually break the given block and get drops?
     */
    public virtual bool canBreak(ItemStack stack, Block block) {
        return true;
    }

    public virtual int getMaxStackSize() => 64;

    public override string ToString() {
        return "Item{id=" + id + ", name=" + name + "}";
    }
}

public class Tool : Item {
    public ToolType type;
    public MaterialTier tier;
    public double speed;

    public Tool(int id, string name, ToolType type, MaterialTier tier, double speed) : base(id, name) {
        this.type = type;
        this.tier = tier;
        this.speed = speed;
    }

    public override double getBreakSpeed(ItemStack stack, Block block) {
        if (Block.tool[block.id] != type) {
            return 1.0;
        }

        double basePower = 1.0 + double.Log(1 + tier.level);
        double targetPower = 1.0 + double.Log(1 + Block.tier[block.id].level);

        if (tier.level < Block.tier[block.id].level) {
            return double.Max(1.0, basePower / targetPower); // Penalty but never < 1.0!!
        }

        return speed * (basePower / targetPower); // Efficiency bonus for overtiered tools
    }

    public override bool canBreak(ItemStack stack, Block block) {
        if (Block.tool[block.id] == type) {
            return tier.level >= Block.tier[block.id].level;
        }

        return Block.tool[block.id] == ToolType.NONE;
    }
}