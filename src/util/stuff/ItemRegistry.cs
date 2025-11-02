using BlockGame.world.item;

namespace BlockGame.util.stuff;

public class ItemRegistry : Registry<Item> {

    /** is this item armour? */
    public readonly XUList<bool> armour;

    /** is this item an accessory? */
    public readonly XUList<bool> accessory;

    /**
     * is this item a material (used for crafting or a placeable building block)
     * if true, player drops it on death.
     */
    public readonly XUList<bool> material;

    /**
     * If true, item doesn't show up in the creative inventory (or anywhere else really)
     */
    public readonly XUList<bool> blackList;

    /**
     * Fuel burn time in ticks (0 = not fuel).
     */
    public readonly XUList<int> fuelValue;

    public ItemRegistry() {
        armour = track(false);
        accessory = track(false);
        material = track(false);
        blackList = track(false);
        fuelValue = track(0);
    }
}