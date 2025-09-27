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

        var goldPickaxe = new Item(Items.GOLD_PICKAXE, "Gold Pickaxe");
        goldPickaxe.tex = new UVPair(3, 2);
        register(goldPickaxe);

        var ironPickaxe = new Item(Items.IRON_PICKAXE, "Iron Pickaxe");
        ironPickaxe.tex = new UVPair(1, 2);
        register(ironPickaxe);

        var woodPickaxe = new Item(Items.WOOD_PICKAXE, "Wood Pickaxe");
        woodPickaxe.tex = new UVPair(2, 2);
        register(woodPickaxe);

        var stonePickaxe = new Item(Items.STONE_PICKAXE, "Stone Pickaxe");
        stonePickaxe.tex = new UVPair(0, 2);
        register(stonePickaxe);

        var woodAxe = new Item(Items.WOOD_AXE, "Wood Axe");
        woodAxe.tex = new UVPair(2, 3);
        register(woodAxe);

        var stoneShovel = new Item(Items.STONE_SHOVEL, "Stone Shovel");
        stoneShovel.tex = new UVPair(4, 2);
        register(stoneShovel);

        var stoneSword = new Item(Items.STONE_SWORD, "Stone Sword");
        stoneSword.tex = new UVPair(5, 2);
        register(stoneSword);

        var stoneHoe = new Item(Items.STONE_HOE, "Stone Hoe");
        stoneHoe.tex = new UVPair(6, 2);
        register(stoneHoe);

        var stoneScythe = new Item(Items.STONE_SCYTHE, "Stone Scythe");
        stoneScythe.tex = new UVPair(7 , 2);
        register(stoneScythe);
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

    public virtual double getBreakSpeed(ItemStack stack, Block block) {
        return 1.0;
    }

    public virtual bool canBreak(ItemStack stack, Block block) {
        return true;
    }

    public virtual int getMaxStackSize() => 64;

    public override string ToString() {
        return "Item{id=" + id + ", name=" + name + "}";
    }
}