using BlockGame.world.block;

namespace BlockGame.world.item;

/**
 * An item which is a "wrapper" for a block.
 */
public class BlockItem : Item {
    public readonly Block block;

    public BlockItem(Block block) : base(block.name) {
        this.block = block;
    }
}