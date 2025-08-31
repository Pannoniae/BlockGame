namespace BlockGame.item;

/**
 * An item which is a "wrapper" for a block.
 */
public class BlockItem : Item {
    public BlockItem(int id, string name) : base(-id, name) {
    }
}