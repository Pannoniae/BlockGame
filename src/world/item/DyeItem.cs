using BlockGame.util;
using BlockGame.world.block;

namespace BlockGame.world.item;

public class DyeItem : Item {
    public DyeItem(int id, string name) : base(id, name) {
    }

    private static readonly string[] colourNames = [
        "Blue", "Sky Blue", "Turquoise", "Dark Green", "Light Green",
        "Orange", "Yellow", "Light Red", "Pink", "Purple",
        "Violet", "Red", "Dark Blue", "White", "Gray", "Black"
    ];

    public override string getName(ItemStack stack) {
        int meta = stack.metadata & 0xF;
        if (meta >= colourNames.Length) return name;
        return $"{colourNames[meta]} Dye";
    }

    public override UVPair getTexture(ItemStack stack) {
        int meta = stack.metadata & 0xF;
        if (meta < 14) {
            // v=1, u=2 to 15
            return new UVPair(2 + meta, 1);
        } else {
            // v=2, u=0 to 1
            return new UVPair(meta - 14, 2);
        }
    }
}