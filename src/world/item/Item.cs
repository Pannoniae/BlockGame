using BlockGame.main;
using BlockGame.util;
using BlockGame.util.stuff;
using BlockGame.world.block;

namespace BlockGame.world.item;

public class Item {
    public int id;
    public string name;

    public UVPair tex = new UVPair(0, 0);

    /**
     * Problem is, we can't get the actual name without an itemstack, because the metadata might change the name (e.g. different colours of candy).
     */
    public virtual string getName(ItemStack stack) => name;

    /** compatibility: access registry property arrays */
    public static XUList<bool> armour => Registry.ITEMS.armour;

    public static XUList<bool> accessory => Registry.ITEMS.accessory;
    public static XUList<bool> material => Registry.ITEMS.material;
    
    public static XUList<int> durability => Registry.ITEMS.durability;

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
    public static Weapon STONE_SWORD;
    public static Tool STONE_HOE;
    public static Tool STONE_SCYTHE;
    public static Tool WOOD_PICKAXE;
    public static Tool WOOD_AXE;
    public static Tool WOOD_SHOVEL;
    public static Weapon WOOD_SWORD;
    public static Item STICK;
    public static Tool COPPER_PICKAXE;
    public static Tool COPPER_AXE;
    public static Tool COPPER_SHOVEL;
    public static Weapon COPPER_SWORD;
    public static Tool COPPER_HOE;
    public static Tool COPPER_SCYTHE;
    public static Tool IRON_AXE;
    public static Tool IRON_SHOVEL;
    public static Weapon IRON_SWORD;
    public static Tool IRON_HOE;
    public static Tool IRON_SCYTHE;
    public static Tool GOLD_AXE;
    public static Tool GOLD_SHOVEL;
    public static Weapon GOLD_SWORD;
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
    public static Item BOTTLE;
    public static Item BOTTLE_MILK;
    public static Item RAW_BEEF;
    public static Item STEAK;
    public static Item PORKCHOP;
    public static Item COOKED_PORKCHOP;
    public static Item DOOR;
    public static Item MAHOGANY_DOOR;
    public static Item BUCKET;
    public static Item WATER_BUCKET;
    public static Item LAVA_BUCKET;
    public static Item LIGHTER;
    public static Item SIGN_ITEM;
    public static Item FLINT;
    public static Item BOW_WOOD;
    public static Item ARROW_WOOD;


    public Item(string name) {
        this.name = name;
    }

    /** register item with string ID */
    public static Item register(string stringID, Item item) {
        int id = Registry.ITEMS.register(stringID, item);
        item.id = id;
        item.onRegister(id);
        return item;
    }

    public static T register<T>(string stringID, T item) where T : Item {
        int id = Registry.ITEMS.register(stringID, item);
        item.id = id;
        item.onRegister(id);
        return item;
    }

    /** called after ID assignment, override to set properties */
    protected virtual void onRegister(int id) {
    }

    /** lookup by runtime ID */
    public static Item? get(int id) => Registry.ITEMS.get(id);

    /** lookup by string ID */
    public static Item? get(string stringID) => Registry.ITEMS.get(stringID);

    /** get block item by block string ID */
    public static Item? block(string blockID) => Registry.ITEMS.get(blockID);

    /** is this a block item? */
    public bool isBlock() => this is BlockItem;

    /** is this a non-block item? */
    public bool isItem() => !isBlock();

    /** get the block if this is a block item */
    public Block? getBlock() => (this as BlockItem)?.block;

    /** get block ID if this is a block item, 0 otherwise */
    public int getBlockID() => getBlock()?.id ?? 0;

    /** helper for old Item.blockID(blockID) pattern - converts block ID to item ID */
    public static int blockID(int blockID) => Block.get(blockID)?.item.id ?? 0;

    public static void preLoad() {
        AIR = register("iair", new Item("Air"));

        GOLD_INGOT = register("goldIngot", new Item("Gold Ingot"));
        GOLD_INGOT.tex = new UVPair(0, 0);
        material[GOLD_INGOT.id] = true;

        IRON_INGOT = register("ironIngot", new Item("Iron Ingot"));
        IRON_INGOT.tex = new UVPair(1, 0);
        material[IRON_INGOT.id] = true;

        TIN_INGOT = register("tinIngot", new Item("Tin Ingot"));
        TIN_INGOT.tex = new UVPair(2, 0);
        material[TIN_INGOT.id] = true;


        SILVER_INGOT = register("silverIngot", new Item("Silver Ingot"));
        SILVER_INGOT.tex = new UVPair(3, 0);
        material[SILVER_INGOT.id] = true;


        COPPER_INGOT = register("copperIngot", new Item("Copper Ingot"));
        COPPER_INGOT.tex = new UVPair(4, 0);
        material[COPPER_INGOT.id] = true;


        GOLD_PICKAXE = register("goldPickaxe", new Tool("Gold Pickaxe", ToolType.PICKAXE, MaterialTier.GOLD, 2f));
        GOLD_PICKAXE.tex = new UVPair(2, 7);

        IRON_PICKAXE = register("ironPickaxe", new Tool("Iron Pickaxe", ToolType.PICKAXE, MaterialTier.IRON, 1.7f));
        IRON_PICKAXE.tex = new UVPair(2, 6);

        STONE_PICKAXE = register("stonePickaxe", new Tool("Stone Pickaxe", ToolType.PICKAXE, MaterialTier.STONE, 1.25));
        STONE_PICKAXE.tex = new UVPair(2, 3);

        STONE_AXE = register("stoneAxe", new Tool("Stone Axe", ToolType.AXE, MaterialTier.STONE, 1.25));
        STONE_AXE.tex = new UVPair(3, 3);

        STONE_SHOVEL = register("stoneShovel", new Tool("Stone Shovel", ToolType.SHOVEL, MaterialTier.STONE, 1.25));
        STONE_SHOVEL.tex = new UVPair(4, 3);

        STONE_SWORD = register("stoneSword", new Weapon("Stone Sword", MaterialTier.STONE, 5.0));
        STONE_SWORD.tex = new UVPair(5, 3);

        STONE_HOE = register("stoneHoe", new Tool("Stone Hoe", ToolType.HOE, MaterialTier.STONE, 1.25));
        STONE_HOE.tex = new UVPair(6, 3);

        STONE_SCYTHE = register("stoneScythe", new Tool("Stone Scythe", ToolType.HOE, MaterialTier.STONE, 1.25));
        STONE_SCYTHE.tex = new UVPair(7, 3);

        WOOD_PICKAXE = register("woodPickaxe", new Tool("Wood Pickaxe", ToolType.PICKAXE, MaterialTier.WOOD, 1.0));
        WOOD_PICKAXE.tex = new UVPair(2, 4);

        WOOD_AXE = register("woodAxe", new Tool("Wood Axe", ToolType.AXE, MaterialTier.WOOD, 1.0));
        WOOD_AXE.tex = new UVPair(3, 4);

        WOOD_SHOVEL = register("woodShovel", new Tool("Wood Shovel", ToolType.SHOVEL, MaterialTier.WOOD, 1.0));
        WOOD_SHOVEL.tex = new UVPair(4, 4);

        WOOD_SWORD = register("woodSword", new Weapon("Wood Sword", MaterialTier.WOOD, 4.0));
        WOOD_SWORD.tex = new UVPair(5, 4);

        STICK = register("stick", new Item("Stick"));
        STICK.tex = new UVPair(0, 8);
        material[STICK.id] = true;

        COPPER_PICKAXE = register("copperPickaxe", new Tool("Copper Pickaxe", ToolType.PICKAXE, MaterialTier.COPPER, 1.5));
        COPPER_PICKAXE.tex = new UVPair(2, 5);

        COPPER_AXE = register("copperAxe", new Tool("Copper Axe", ToolType.AXE, MaterialTier.COPPER, 1.5));
        COPPER_AXE.tex = new UVPair(3, 5);

        COPPER_SHOVEL = register("copperShovel", new Tool("Copper Shovel", ToolType.SHOVEL, MaterialTier.COPPER, 1.5));
        COPPER_SHOVEL.tex = new UVPair(4, 5);

        COPPER_SWORD = register("copperSword", new Weapon("Copper Sword", MaterialTier.COPPER, 6.0));
        COPPER_SWORD.tex = new UVPair(5, 5);

        COPPER_HOE = register("copperHoe", new Tool("Copper Hoe", ToolType.HOE, MaterialTier.COPPER, 1.5));
        COPPER_HOE.tex = new UVPair(6, 5);

        COPPER_SCYTHE = register("copperScythe", new Tool("Copper Scythe", ToolType.HOE, MaterialTier.COPPER, 1.5));
        COPPER_SCYTHE.tex = new UVPair(7, 5);

        IRON_AXE = register("ironAxe", new Tool("Iron Axe", ToolType.AXE, MaterialTier.IRON, 2.5));
        IRON_AXE.tex = new UVPair(3, 6);

        IRON_SHOVEL = register("ironShovel", new Tool("Iron Shovel", ToolType.SHOVEL, MaterialTier.IRON, 2.5));
        IRON_SHOVEL.tex = new UVPair(4, 6);

        IRON_SWORD = register("ironSword", new Weapon("Iron Sword", MaterialTier.IRON, 7.0));
        IRON_SWORD.tex = new UVPair(5, 6);

        IRON_HOE = register("ironHoe", new Tool("Iron Hoe", ToolType.HOE, MaterialTier.IRON, 2.5));
        IRON_HOE.tex = new UVPair(6, 6);

        IRON_SCYTHE = register("ironScythe", new Tool("Iron Scythe", ToolType.HOE, MaterialTier.IRON, 2.5));
        IRON_SCYTHE.tex = new UVPair(7, 6);

        GOLD_AXE = register("goldAxe", new Tool("Gold Axe", ToolType.AXE, MaterialTier.GOLD, 3.0));
        GOLD_AXE.tex = new UVPair(3, 7);

        GOLD_SHOVEL = register("goldShovel", new Tool("Gold Shovel", ToolType.SHOVEL, MaterialTier.GOLD, 3.0));
        GOLD_SHOVEL.tex = new UVPair(4, 7);

        GOLD_SWORD = register("goldSword", new Weapon("Gold Sword", MaterialTier.GOLD, 8.0));
        GOLD_SWORD.tex = new UVPair(5, 7);

        GOLD_HOE = register("goldHoe", new Tool("Gold Hoe", ToolType.HOE, MaterialTier.GOLD, 3.0));
        GOLD_HOE.tex = new UVPair(6, 7);

        GOLD_SCYTHE = register("goldScythe", new Tool("Gold Scythe", ToolType.HOE, MaterialTier.GOLD, 3.0));
        GOLD_SCYTHE.tex = new UVPair(7, 7);

        DYE = register("dye", new DyeItem("Dye"));

        APPLE = register("apple", new Item("Apple"));
        APPLE.tex = new UVPair(0, 10);

        MAPLE_SYRUP = register("mapleSyrup", new Item("Maple Syrup"));
        MAPLE_SYRUP.tex = new UVPair(1, 10);

        COAL = register("coal", new Item("Coal"));
        COAL.tex = new UVPair(1, 9);
        material[COAL.id] = true;

        BRICK = register("brick", new Item("Brick"));
        BRICK.tex = new UVPair(5, 0);
        material[BRICK.id] = true;

        CLAY = register("clay", new Item("Clay"));
        CLAY.tex = new UVPair(2, 9);
        material[CLAY.id] = true;

        DIAMOND = register("diamond", new Item("Diamond"));
        DIAMOND.tex = new UVPair(3, 9);
        material[DIAMOND.id] = true;

        CINNABAR = register("cinnabar", new Item("Cinnabar"));
        CINNABAR.tex = new UVPair(4, 9);
        material[CINNABAR.id] = true;

        BOTTLE = register("bottle", new Item("Empty Bottle"));
        BOTTLE.tex = new UVPair(2, 10);

        BOTTLE_MILK = register("milk", new Item("Bottle of Milk"));
        BOTTLE_MILK.tex = new UVPair(3, 10);

        RAW_BEEF = register("rawBeef", new Item("Raw Beef"));
        RAW_BEEF.tex = new UVPair(4, 10);

        STEAK = register("steak", new Item("Steak"));
        STEAK.tex = new UVPair(5, 10);

        PORKCHOP = register("porkchop", new Item("Porkchop"));
        PORKCHOP.tex = new UVPair(6, 10);

        COOKED_PORKCHOP = register("cookedPorkchop", new Item("Cooked Porkchop"));
        COOKED_PORKCHOP.tex = new UVPair(7, 10);

        DOOR = register("doorItem", new DoorItem("Door", Block.DOOR));
        DOOR.tex = new UVPair(4, 8);

        MAHOGANY_DOOR = register("mahoganyDoorItem", new DoorItem("Mahogany Door", Block.MAHOGANY_DOOR));
        MAHOGANY_DOOR.tex = new UVPair(5, 8);

        BUCKET = register("bucket", new BucketItem("Bucket"));
        BUCKET.tex = new UVPair(6, 4);

        WATER_BUCKET = register("waterBucket", new BucketItem("Water Bucket", Block.WATER));
        WATER_BUCKET.tex = new UVPair(7, 4);

        LAVA_BUCKET = register("lavaBucket", new BucketItem("Lava Bucket", Block.LAVA));
        LAVA_BUCKET.tex = new UVPair(8, 4);

        LIGHTER = register("lighter", new Lighter("Lighter"));
        LIGHTER.tex = new UVPair(0, 7);

        SIGN_ITEM = register("signItem", new SignItem("Sign", Block.SIGN));
        SIGN_ITEM.tex = new UVPair(1, 7);

        FLINT = register("flint", new Item("Flint"));
        FLINT.tex = new UVPair(0, 9);

        BOW_WOOD = register("bow_wood", new Item("Wooden Bow"));
        BOW_WOOD.tex = new UVPair(1, 4);

        ARROW_WOOD = register("arrow_wood", new Item("Wooden Arrow"));
        ARROW_WOOD.tex = new UVPair(0, 4);


        // all blocks are already marked as materials during Block.register() lol

        Registry.ITEMS.blackList[AIR.id] = true;
        Registry.ITEMS.blackList[Block.WATER.item.id] = true;
        Registry.ITEMS.blackList[Block.LAVA.item.id] = true;
        Registry.ITEMS.blackList[Block.FIRE.item.id] = true;
        Registry.ITEMS.blackList[Block.SIGN.item.id] = true;
        Registry.ITEMS.blackList[Block.DOOR.item.id] = true;
        Registry.ITEMS.blackList[Block.MAHOGANY_DOOR.item.id] = true;

        // fuel values
        Registry.ITEMS.fuelValue[COAL.id] = 3600; // 60 seconds
        Registry.ITEMS.fuelValue[Block.OAK_PLANKS.item.id] = 900; // 15 seconds
        Registry.ITEMS.fuelValue[Block.MAHOGANY_PLANKS.item.id] = 900;
        Registry.ITEMS.fuelValue[Block.MAPLE_PLANKS.item.id] = 900;
        Registry.ITEMS.fuelValue[STICK.id] = 300; // 5 seconds
        Registry.ITEMS.fuelValue[Block.OAK_LOG.item.id] = 1800; // 30 seconds
        Registry.ITEMS.fuelValue[Block.MAHOGANY_LOG.item.id] = 1800;
        Registry.ITEMS.fuelValue[Block.MAPLE_LOG.item.id] = 1800;
        Registry.ITEMS.fuelValue[LAVA_BUCKET.id] = 12000; // 200 seconds (lava bucket op)
        Registry.ITEMS.fuelValue[SIGN_ITEM.id] = 300; // 5 seconds
    }

    public virtual UVPair getTexture(ItemStack stack) {
        if (this is BlockItem bi) {
            if (Block.renderItemLike[bi.block.id]) {
                return bi.block.getTexture(0, stack.metadata);
            }
        }

        return tex;
    }

    /**
     * What a meaty method lol. Called when the player uses an item on a block.
     * Returns an optional replacement ItemStack (e.g., empty bucket -> water bucket).
     */
    public virtual ItemStack? useBlock(ItemStack stack, World world, Player player, int x, int y, int z, RawDirection dir) {
        return null;
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
        // bare hands and random items can only break blocks with 0 tier
        return Block.tier[block.id] == MaterialTier.NONE;
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

    public Tool(string name, ToolType type, MaterialTier tier, double speed) : base(name) {
        this.type = type;
        this.tier = tier;
        this.speed = speed;
    }

    protected override void onRegister(int id) {
        durability[id] = tier.durability;
    }

    public override int getMaxStackSize() => 1;

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

        // wrong tool... can only break if block has no tier requirement
        return Block.tier[block.id] == MaterialTier.NONE;
    }
}