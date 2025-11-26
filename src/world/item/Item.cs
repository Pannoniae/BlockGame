using BlockGame.GL;
using BlockGame.main;
using BlockGame.util;
using BlockGame.util.stuff;
using BlockGame.world.block;
using BlockGame.world.entity;

namespace BlockGame.world.item;

public class Item {
    public int id;
    public string name;

    public UVPair tex = new UVPair(0, 0);

    public static BTextureAtlas atlas;

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
    public static Item COPPER_INGOT;
    public static Item IRON_INGOT;
    public static Item GOLD_INGOT;
    public static Item DIAMOND;

    public static Item CINNABAR;

    public static Item TIN_INGOT;
    //public static Item SILVER_INGOT;
    public static Item COAL;
    public static Item FLINT;
    public static Item CLAY;
    public static Item BRICK;
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
    public static Tool IRON_PICKAXE;
    public static Tool IRON_AXE;
    public static Tool IRON_SHOVEL;
    public static Weapon IRON_SWORD;
    public static Tool IRON_HOE;
    public static Tool IRON_SCYTHE;
    public static Tool GOLD_PICKAXE;
    public static Tool GOLD_AXE;
    public static Tool GOLD_SHOVEL;
    public static Weapon GOLD_SWORD;
    public static Tool GOLD_HOE;
    public static Tool GOLD_SCYTHE;
    public static DyeItem DYE;
    public static Item APPLE;
    public static Item MAPLE_SYRUP;
    public static Item BOTTLE;
    public static Item BOTTLE_MILK;
    public static Item RAW_BEEF;
    public static Item STEAK;
    public static Item PORKCHOP;
    public static Item COOKED_PORKCHOP;
    public static Item OAK_DOOR;
    public static Item MAHOGANY_DOOR;
    public static Item SIGN_ITEM;
    public static Item BUCKET;
    public static Item WATER_BUCKET;
    public static Item LAVA_BUCKET;
    public static Item LIGHTER;

    public static Item WHEAT_SEEDS;
    public static Item WHEAT;
    public static Item BREAD;
    public static Item CARROT_SEEDS;
    public static Item CARROT;
    //public static Item SUGAR;
    public static Item APPLE_PIE;

    public static Item BOW_WOOD;
    public static Item ARROW_WOOD;
    public static Item FEATHER;
    public static Item STRING;
    public static Item SNOWBALL;
    public static Item HAND_GRENADE;
    public static Item SNOWBALL_SPITTER;

    public static UVPair uv(string source, int x, int y) {
        if (Net.mode.isDed()) {
            return new UVPair(0, 0);
        }
        return atlas.uv(source, x, y);
    }


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
        if (!Net.mode.isDed()) {
            atlas = Game.textures.itemTexture;
        }

        AIR = register("iair", new Item("Air"));

        STICK = register("stick", new Item("Stick"));
        STICK.tex = uv("items.png", 0, 8);
        material[STICK.id] = true;

        SIGN_ITEM = register("signItem", new SignItem("Sign", Block.SIGN));
        SIGN_ITEM.tex = uv("items.png", 1, 7);

        OAK_DOOR = register("oakdoorItem", new DoorItem("Oak Door", Block.OAK_DOOR));
        OAK_DOOR.tex = uv("items.png", 4, 8);

        MAHOGANY_DOOR = register("mahoganyDoorItem", new DoorItem("Mahogany Door", Block.MAHOGANY_DOOR));
        MAHOGANY_DOOR.tex = uv("items.png", 5, 8);

        COPPER_INGOT = register("copperIngot", new Item("Copper Ingot"));
        COPPER_INGOT.tex = uv("items.png", 4, 0);
        material[COPPER_INGOT.id] = true;

        IRON_INGOT = register("ironIngot", new Item("Iron Ingot"));
        IRON_INGOT.tex = uv("items.png", 1, 0);
        material[IRON_INGOT.id] = true;

        GOLD_INGOT = register("goldIngot", new Item("Gold Ingot"));
        GOLD_INGOT.tex = uv("items.png", 0, 0);
        material[GOLD_INGOT.id] = true;

        DIAMOND = register("diamond", new Item("Diamond"));
        DIAMOND.tex = uv("items.png", 3, 9);
        material[DIAMOND.id] = true;

        CINNABAR = register("cinnabar", new Item("Cinnabar"));
        CINNABAR.tex = uv("items.png", 4, 9);
        material[CINNABAR.id] = true;

        COAL = register("coal", new Item("Coal"));
        COAL.tex = uv("items.png", 1, 9);
        material[COAL.id] = true;

        FLINT = register("flint", new Item("Flint"));
        FLINT.tex = uv("items.png", 0, 9);

        CLAY = register("clay", new Item("Clay"));
        CLAY.tex = uv("items.png", 2, 9);
        material[CLAY.id] = true;

        BRICK = register("brick", new Item("Brick"));
        BRICK.tex = uv("items.png", 5, 0);
        material[BRICK.id] = true;

        TIN_INGOT = register("tinIngot", new Item("Tin Ingot"));
        TIN_INGOT.tex = uv("items.png", 2, 0);
        material[TIN_INGOT.id] = true;

        //SILVER_INGOT = register("silverIngot", new Item("Silver Ingot"));
        //SILVER_INGOT.tex = uv("items.png", 3, 0);
        //material[SILVER_INGOT.id] = true;

        WOOD_PICKAXE = register("woodPickaxe", new Tool("Wood Pickaxe", ToolType.PICKAXE, MaterialTier.WOOD, 1.0));
        WOOD_PICKAXE.tex = uv("items.png", 2, 4);

        WOOD_AXE = register("woodAxe", new Tool("Wood Axe", ToolType.AXE, MaterialTier.WOOD, 1.0));
        WOOD_AXE.tex = uv("items.png", 3, 4);

        WOOD_SHOVEL = register("woodShovel", new Tool("Wood Shovel", ToolType.SHOVEL, MaterialTier.WOOD, 1.0));
        WOOD_SHOVEL.tex = uv("items.png", 4, 4);

        WOOD_SWORD = register("woodSword", new Weapon("Wood Sword", MaterialTier.WOOD, 4.0));
        WOOD_SWORD.tex = uv("items.png", 5, 4);

        STONE_PICKAXE = register("stonePickaxe", new Tool("Stone Pickaxe", ToolType.PICKAXE, MaterialTier.STONE, 1.25));
        STONE_PICKAXE.tex = uv("items.png", 2, 3);

        STONE_AXE = register("stoneAxe", new Tool("Stone Axe", ToolType.AXE, MaterialTier.STONE, 1.25));
        STONE_AXE.tex = uv("items.png", 3, 3);

        STONE_SHOVEL = register("stoneShovel", new Tool("Stone Shovel", ToolType.SHOVEL, MaterialTier.STONE, 1.25));
        STONE_SHOVEL.tex = uv("items.png", 4, 3);

        STONE_SWORD = register("stoneSword", new Weapon("Stone Sword", MaterialTier.STONE, 5.0));
        STONE_SWORD.tex = uv("items.png", 5, 3);

        STONE_HOE = register("stoneHoe", new Tool("Stone Hoe", ToolType.HOE, MaterialTier.STONE, 1.25));
        STONE_HOE.tex = uv("items.png", 6, 3);

        STONE_SCYTHE = register("stoneScythe", new Tool("Stone Scythe", ToolType.SCYTHE, MaterialTier.STONE, 1.25));
        STONE_SCYTHE.tex = uv("items.png", 7, 3);

        COPPER_PICKAXE = register("copperPickaxe", new Tool("Copper Pickaxe", ToolType.PICKAXE, MaterialTier.COPPER, 1.5));
        COPPER_PICKAXE.tex = uv("items.png", 2, 5);

        COPPER_AXE = register("copperAxe", new Tool("Copper Axe", ToolType.AXE, MaterialTier.COPPER, 1.5));
        COPPER_AXE.tex = uv("items.png", 3, 5);

        COPPER_SHOVEL = register("copperShovel", new Tool("Copper Shovel", ToolType.SHOVEL, MaterialTier.COPPER, 1.5));
        COPPER_SHOVEL.tex = uv("items.png", 4, 5);

        COPPER_SWORD = register("copperSword", new Weapon("Copper Sword", MaterialTier.COPPER, 6.0));
        COPPER_SWORD.tex = uv("items.png", 5, 5);

        COPPER_HOE = register("copperHoe", new Tool("Copper Hoe", ToolType.HOE, MaterialTier.COPPER, 1.5));
        COPPER_HOE.tex = uv("items.png", 6, 5);

        COPPER_SCYTHE = register("copperScythe", new Tool("Copper Scythe", ToolType.SCYTHE, MaterialTier.COPPER, 1.5));
        COPPER_SCYTHE.tex = uv("items.png", 7, 5);

        IRON_PICKAXE = register("ironPickaxe", new Tool("Iron Pickaxe", ToolType.PICKAXE, MaterialTier.IRON, 2f));
        IRON_PICKAXE.tex = uv("items.png", 2, 6);

        IRON_AXE = register("ironAxe", new Tool("Iron Axe", ToolType.AXE, MaterialTier.IRON, 2f));
        IRON_AXE.tex = uv("items.png", 3, 6);

        IRON_SHOVEL = register("ironShovel", new Tool("Iron Shovel", ToolType.SHOVEL, MaterialTier.IRON, 2f));
        IRON_SHOVEL.tex = uv("items.png", 4, 6);

        IRON_SWORD = register("ironSword", new Weapon("Iron Sword", MaterialTier.IRON, 7.0));
        IRON_SWORD.tex = uv("items.png", 5, 6);

        IRON_HOE = register("ironHoe", new Tool("Iron Hoe", ToolType.HOE, MaterialTier.IRON, 2f));
        IRON_HOE.tex = uv("items.png", 6, 6);

        IRON_SCYTHE = register("ironScythe", new Tool("Iron Scythe", ToolType.SCYTHE, MaterialTier.IRON, 2f));
        IRON_SCYTHE.tex = uv("items.png", 7, 6);

        GOLD_PICKAXE = register("goldPickaxe", new Tool("Gold Pickaxe", ToolType.PICKAXE, MaterialTier.GOLD, 3f));
        GOLD_PICKAXE.tex = uv("items.png", 2, 7);

        GOLD_AXE = register("goldAxe", new Tool("Gold Axe", ToolType.AXE, MaterialTier.GOLD, 3f));
        GOLD_AXE.tex = uv("items.png", 3, 7);

        GOLD_SHOVEL = register("goldShovel", new Tool("Gold Shovel", ToolType.SHOVEL, MaterialTier.GOLD, 3f));
        GOLD_SHOVEL.tex = uv("items.png", 4, 7);

        GOLD_SWORD = register("goldSword", new Weapon("Gold Sword", MaterialTier.GOLD, 8.0));
        GOLD_SWORD.tex = uv("items.png", 5, 7);

        GOLD_HOE = register("goldHoe", new Tool("Gold Hoe", ToolType.HOE, MaterialTier.GOLD, 3f));
        GOLD_HOE.tex = uv("items.png", 6, 7);

        GOLD_SCYTHE = register("goldScythe", new Tool("Gold Scythe", ToolType.SCYTHE, MaterialTier.GOLD, 3f));
        GOLD_SCYTHE.tex = uv("items.png", 7, 7);

        DYE = register("dye", new DyeItem("Dye"));
        material[DYE.id] = true;

        APPLE = register("apple", new Food("Apple", 5));
        APPLE.tex = uv("items.png", 0, 10);
        material[APPLE.id] = true;

        MAPLE_SYRUP = register("mapleSyrup", new Food("Maple Syrup", 10));
        MAPLE_SYRUP.tex = uv("items.png", 1, 10);
        material[MAPLE_SYRUP.id] = true;

        BOTTLE = register("bottle", new Item("Empty Bottle"));
        BOTTLE.tex = uv("items.png", 2, 10);
        material[BOTTLE.id] = true;

        BOTTLE_MILK = register("milk", new Food("Bottle of Milk", 10));
        BOTTLE_MILK.tex = uv("items.png", 3, 10);
        material[BOTTLE_MILK.id] = true;

        RAW_BEEF = register("rawBeef", new Food("Raw Beef", 10));
        RAW_BEEF.tex = uv("items.png", 4, 10);

        STEAK = register("steak", new Food("Steak", 30));
        STEAK.tex = uv("items.png", 5, 10);

        PORKCHOP = register("porkchop", new Food("Porkchop", 10));
        PORKCHOP.tex = uv("items.png", 6, 10);

        COOKED_PORKCHOP = register("cookedPorkchop", new Food("Cooked Porkchop", 20));
        COOKED_PORKCHOP.tex = uv("items.png", 7, 10);

        BUCKET = register("bucket", new BucketItem("Bucket"));
        BUCKET.tex = uv("items.png", 6, 4);

        WATER_BUCKET = register("waterBucket", new BucketItem("Water Bucket", Block.WATER));
        WATER_BUCKET.tex = uv("items.png", 7, 4);

        LAVA_BUCKET = register("lavaBucket", new BucketItem("Lava Bucket", Block.LAVA));
        LAVA_BUCKET.tex = uv("items.png", 8, 4);

        LIGHTER = register("lighter", new Lighter("Lighter"));
        LIGHTER.tex = uv("items.png", 0, 7);

        WHEAT_SEEDS = register("wheatseeds", new SeedItem("Wheat Seeds", Block.CROP_WHEAT, Block.FARMLAND));
        WHEAT_SEEDS.tex = uv("items.png", 8, 10);
        material[WHEAT_SEEDS.id] = true;

        WHEAT = register("wheat", new Item("Wheat"));
        WHEAT.tex = uv("items.png", 9, 10);
        material[WHEAT.id] = true;
        ((Crop)Block.CROP_WHEAT).product = WHEAT;
        ((Crop)Block.CROP_WHEAT).seedItem = WHEAT_SEEDS;

        CARROT_SEEDS = register("carrotseeds", new SeedItem("Carrot Seeds", Block.CROP_CARROT, Block.FARMLAND));
        CARROT_SEEDS.tex = uv("items.png", 12, 10);
        material[CARROT_SEEDS.id] = true;

        CARROT = register("carrot", new Item("Carrot"));
        CARROT.tex = uv("items.png", 11, 10);
        material[CARROT.id] = true;
        ((Crop)Block.CROP_CARROT).product = CARROT;
        ((Crop)Block.CROP_CARROT).seedItem = CARROT_SEEDS;


        BREAD = register("bread", new Food("Bread", 25));
        BREAD.tex = uv("items.png", 10, 10);
        material[BREAD.id] = true;

        //SUGAR = register("sugar", new Item("Sugar"));
        //SUGAR.tex = uv("items.png", 12, 11);
        //material[SUGAR.id] = true;

        APPLE_PIE = register("applePie", new Food("Apple Pie", 40));
        APPLE_PIE.tex = uv("items.png", 13, 10);
        material[APPLE_PIE.id] = true;

        BOW_WOOD = register("bow_wood", new BowItem("Wooden Bow"));
        BOW_WOOD.tex = uv("items.png", 1, 4);

        ARROW_WOOD = register("arrow_wood", new Item("Wooden Arrow"));
        ARROW_WOOD.tex = uv("items.png", 0, 4);

        FEATHER = register("feather", new Item("Feather"));
        FEATHER.tex = uv("items.png", 0, 3);

        STRING = register("string", new Item("String"));
        STRING.tex = uv("items.png", 1, 3);

        SNOWBALL = register("snowball", new Item("Snowball"));
        SNOWBALL.tex = uv("items.png", 0, 5);
        material[SNOWBALL.id] = true;

        HAND_GRENADE = register("handGrenade", new Item("Hand Grenade"));
        HAND_GRENADE.tex = uv("items.png", 1, 5);

        SNOWBALL_SPITTER = register("snowballspitter", new Item("Snowball Spitter"));
        SNOWBALL_SPITTER.tex = uv("items.png", 1, 6);





        // all blocks are already marked as materials during Block.register() lol

        Registry.ITEMS.blackList[AIR.id] = true;
        Registry.ITEMS.blackList[Block.WATER.item.id] = true;
        Registry.ITEMS.blackList[Block.LAVA.item.id] = true;
        Registry.ITEMS.blackList[Block.FIRE.item.id] = true;
        Registry.ITEMS.blackList[Block.SIGN.item.id] = true;
        Registry.ITEMS.blackList[Block.OAK_DOOR.item.id] = true;
        Registry.ITEMS.blackList[Block.MAHOGANY_DOOR.item.id] = true;
        Registry.ITEMS.blackList[Block.FURNACE_LIT.item.id] = true;
        Registry.ITEMS.blackList[Block.BRICK_FURNACE_LIT.item.id] = true;
        Registry.ITEMS.blackList[Block.CROP_WHEAT.item.id] = true;
        Registry.ITEMS.blackList[Block.FARMLAND.item.id] = true;


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
        Registry.ITEMS.fuelValue[OAK_DOOR.id] = 1200; // 20 seconds
        Registry.ITEMS.fuelValue[MAHOGANY_DOOR.id] = 1200; // 20 seconds
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
    public virtual ItemStack? useBlock(ItemStack stack, World world, Player player, int x, int y, int z, Placement info) {
        return null;
    }

    /**
     * Called when the player uses an item in the air (not on a block).
     */
    public virtual ItemStack? use(ItemStack stack, World world, Player player) {
        return stack;
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

    /**
     * How much damage does this item deal when used as a weapon?
     */
    public virtual double getDamage(ItemStack stack) {
        return 2; // base damage for non-weapon items (fists, blocks, etc)
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
        bool rightTool = Block.tool[block.id] == type || Block.tool[block.id] == ToolType.NONE;
        bool rightTier = tier.level >= Block.tier[block.id].level;

        // Right tool + right tier = full speed
        if (rightTool && rightTier) {
            return speed;
        }

        return 1;
    }

    public override bool canBreak(ItemStack stack, Block block) {
        if (Block.tool[block.id] == type) {
            return tier.level >= Block.tier[block.id].level;
        }

        // wrong tool... can only break if block has no tier requirement
        return Block.tier[block.id] == MaterialTier.NONE;
    }

    public override double getDamage(ItemStack stack) {
        return 2 + tier.level;
    }

    public override ItemStack? useBlock(ItemStack stack, World world, Player player, int x, int y, int z, Placement info) {
        // hoe tilling: dirt/grass -> farmland
        // todo move this to a more appropriate place later maybe?
        if (type == ToolType.HOE) {

            // problem is, x y z are the *prev* block coords, we need to get the block being clicked
            switch (info.face) {
                case RawDirection.UP:
                    y -= 1;
                    break;
                case RawDirection.DOWN:
                    y += 1;
                    break;
                case RawDirection.WEST:
                    x += 1;
                    break;
                case RawDirection.EAST:
                    x -= 1;
                    break;
                case RawDirection.NORTH:
                    z -= 1;
                    break;
                case RawDirection.SOUTH:
                    z += 1;
                    break;
            }

            var block = world.getBlock(x, y, z);
            if (block == Block.DIRT.id || block == Block.GRASS.id || block == Block.SNOW_GRASS.id) {
                world.setBlock(x, y, z, Block.FARMLAND.id);
                // damage hoe
                stack.damageItem(player, 1);
                return stack;
            }
        }

        return null;
    }
}