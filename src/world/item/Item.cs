using BlockGame.util;
using BlockGame.world.block;

namespace BlockGame.world.item;

/**
 * Items have IDs from 1 and up,
 * blocks have IDs from -1 and down.
 * 
 * itemID = -blockID
 */
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

    

    public static int currentID = 1;

    public static Item AIR;

    public Item(int id, string name) {
        this.id = id;
        this.name = name;
    }
    
    public static Item register(Item item) {
        // handles negatives correctly!
        currentID = Math.Max(currentID, item.id);
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
}