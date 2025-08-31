using BlockGame.util;

namespace BlockGame.item;

/**
 * Items have IDs from 1 and up,
 * blocks have IDs from -1 and down.
 * 
 * itemID = -blockID
 */
public class Item {
    public int id;
    private string name;

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
    public static Item WOOD_PICKAXE;

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
    
    public static Item getBlockItem(int blockID) {
        return get(-blockID);
    }
    
    public static int getBlockItemID(int blockID) {
        return -blockID;
    }
    
    public bool isBlock() => id < 0 && -id < Block.currentID && Block.blocks[-id] != null;
    
    public bool isItem() => id > 0 && id < currentID && items[id] != null;
    
    public int getBlockID() => isBlock() ? -id : 0;
    
    public Block getBlock() => isBlock() ? Block.blocks[-id] : null!;

    public static void preLoad() {
        AIR = register(new Item(Items.AIR, "Air"));
        WOOD_PICKAXE = register(new Item(Items.WOOD_PICKAXE, "Wooden Pickaxe"));
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