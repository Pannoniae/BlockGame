namespace BlockGame.item;

/**
 * Items have IDs from 1 and up,
 * blocks have IDs from -1 and down.
 * 
 * itemID = -blockID
 */
public class Item {
    public int id;
    public int metadata;
    public string name;
    
    
    public static Item[] items = new Item[256];
}