using BlockGame.world.block;

namespace BlockGame.world.item;

public class FenceItem : Item {
    private readonly Block Fence;

    public FenceItem(string name, Block block) : base(name) {
        Fence = block;
    }
}