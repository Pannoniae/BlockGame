using BlockGame.world.item;

namespace BlockGame.util.stuff;

public class ItemRegistry : Registry<Item> {

    /** is this item armour? */
    public XUList<bool> armour;

    /** is this item an accessory? */
    public XUList<bool> accessory;

    /**
     * is this item a material (used for crafting or a placeable building block)
     * if true, player drops it on death.
     */
    public XUList<bool> material;

    public ItemRegistry() {
        armour = track(false);
        accessory = track(false);
        material = track(false);
    }
}