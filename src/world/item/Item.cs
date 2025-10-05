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
[SuppressMessage("Compiler", "CS8618:Non-nullable field must contain a non-null value when exiting constructor. Consider adding the \'required\' modifier or declaring as nullable.")]
public class Item {
    public int id;
    public string name;

    public UVPair tex = new UVPair(0, 0);

    /**
     * Problem is, we can't get the actual name without an itemstack, because the metadata might change the name (e.g. different colours of candy).
     */
    public virtual string getName(ItemStack stack) => name;
    
    private const int MAXITEMS = 4096;
    
    /**
     * We "cheat", half of this goes for blocks (the negatives), half for items.
     * We shift it automatically in register, shh.
     */
    public static readonly Item[] items = new Item[MAXITEMS * 2 + 1];

    

    public static int currentID = 0;

    public static Item AIR;

    public Item(int id, string name) {
        this.id = id;
        this.name = name;
    }
    
    public static Item register(Item item) {
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
        if (i is <= -MAXITEMS or >= MAXITEMS) {
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

        var goldIngot = new Item(Items.GOLD_INGOT, "Gold Ingot");
        goldIngot.tex = new UVPair(0, 0);
        register(goldIngot);

        var ironIngot = new Item(Items.IRON_INGOT, "Iron Ingot");
        ironIngot.tex = new UVPair(1, 0);
        register(ironIngot);

        var tinIngot = new Item(Items.TIN_INGOT, "Tin Ingot");
        tinIngot.tex = new UVPair(2, 0);
        register(tinIngot);

        var silverIngot = new Item(Items.SILVER_INGOT, "Silver Ingot");
        silverIngot.tex = new UVPair(3, 0);
        register(silverIngot);

        var goldPickaxe = new Tool(Items.GOLD_PICKAXE, "Gold Pickaxe", ToolType.PICKAXE, MaterialTier.GOLD, 6.0);
        goldPickaxe.tex = new UVPair(2, 2);
        register(goldPickaxe);

        var ironPickaxe = new Tool(Items.IRON_PICKAXE, "Iron Pickaxe", ToolType.PICKAXE, MaterialTier.IRON, 5.0);
        ironPickaxe.tex = new UVPair(1, 2);
        register(ironPickaxe);

        var stonePickaxe = new Tool(Items.STONE_PICKAXE, "Stone Pickaxe", ToolType.PICKAXE, MaterialTier.STONE, 4.0);
        stonePickaxe.tex = new UVPair(0, 2);
        register(stonePickaxe);

        var stoneAxe = new Tool(Items.STONE_AXE, "Stone Axe", ToolType.AXE, MaterialTier.STONE, 4.0);
        stoneAxe.tex = new UVPair(3, 2);
        register(stoneAxe);

        var stoneShovel = new Tool(Items.STONE_SHOVEL, "Stone Shovel", ToolType.SHOVEL, MaterialTier.STONE, 2.0);
        stoneShovel.tex = new UVPair(4, 2);
        register(stoneShovel);

        var stoneSword = new Item(Items.STONE_SWORD, "Stone Sword");
        stoneSword.tex = new UVPair(5, 2);
        register(stoneSword);

        var stoneHoe = new Tool(Items.STONE_HOE, "Stone Hoe", ToolType.HOE, MaterialTier.STONE, 2.0);
        stoneHoe.tex = new UVPair(6, 2);
        register(stoneHoe);

        var stoneScythe = new Tool(Items.STONE_SCYTHE, "Stone Scythe", ToolType.HOE, MaterialTier.STONE, 2.0);
        stoneScythe.tex = new UVPair(7 , 2);
        register(stoneScythe);

        var woodPickaxe = new Tool(Items.WOOD_PICKAXE, "Wood Pickaxe", ToolType.PICKAXE, MaterialTier.WOOD, 2.0);
        woodPickaxe.tex = new UVPair(1, 3);
        register(woodPickaxe);

        var woodAxe = new Tool(Items.WOOD_AXE, "Wood Axe", ToolType.AXE, MaterialTier.WOOD, 1.5);
        woodAxe.tex = new UVPair(3, 3);
        register(woodAxe);

        var woodShovel = new Tool(Items.WOOD_SHOVEL, "Wood Shovel", ToolType.SHOVEL, MaterialTier.WOOD, 1.0);
        woodShovel.tex = new UVPair(4, 3);
        register(woodShovel);

        var woodSword = new Item(Items.WOOD_SWORD, "Wood Sword");
        woodSword.tex = new UVPair(5, 3);
        register(woodSword);




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