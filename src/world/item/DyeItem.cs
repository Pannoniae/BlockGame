using BlockGame.util;
using BlockGame.world.block;

namespace BlockGame.world.item;

public class DyeItem : Item {
    public DyeItem(int id, string name) : base(id, name) {
    }

    public override string getName(ItemStack stack) {
        int meta = stack.metadata & 0xF;
        return meta >= CandyBlock.colourNames.Length ? name : $"{CandyBlock.colourNames[meta]} Dye";
    }

    public override UVPair getTexture(ItemStack stack) {
        int meta = stack.metadata & 0xF;
        if (meta < 16) {
            // v=1, u=0 to 15
            return new UVPair(meta, 1);
        } else {
            // v=2, u=0 to 3
            return new UVPair(meta - 16, 2);
        }
    }
}