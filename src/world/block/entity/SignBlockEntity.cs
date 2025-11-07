using BlockGame.util.xNBT;

namespace BlockGame.world.block.entity;

public class SignBlockEntity : BlockEntity {

    public readonly List<string> lines = ["", "", "", ""];

    // (16px = 1 block)
    public const float TEXT_HEIGHT_WORLD = 2f;
    public const float LINE_SPACING_WORLD = 2f;
    public const float TEXT_PADDING_TOP_WORLD = 0.5f; // padding from top of sign
    public const int SIGN_WIDTH_WORLD = 16;
    public const int MAX_TEXT_WIDTH_WORLD = 16; // maxwidth on block

    public SignBlockEntity() : base("sign") {
    }

    public override void update(World world, int x, int y, int z) {
        
    }

    protected override void readx(NBTCompound data) {
        if (data.has("line0")) {
            lines[0] = data.getString("line0");
        }

        if (data.has("line1")) {
            lines[1] = data.getString("line1");
        }

        if (data.has("line2")) {
            lines[2] = data.getString("line2");
        }

        if (data.has("line3")) {
            lines[3] = data.getString("line3");
        }
    }

    protected override void writex(NBTCompound data) {
        data.addString("line0", lines[0]);
        data.addString("line1", lines[1]);
        data.addString("line2", lines[2]);
        data.addString("line3", lines[3]);
    }
}