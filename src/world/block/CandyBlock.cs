using BlockGame.world.item;

namespace BlockGame.world.block;

public class CandyBlock : Block {
    public CandyBlock(string name) : base(name) {
    }

    protected override void onRegister(int id) {
        renderType[id] = RenderType.CUBE_DYNTEXTURE;

        // set uvs
        // return new UVPair(metadata & 0xF, 6 + (metadata >> 4));
        uvs = new UVPair[maxValidMetadata() + 1];
        for (int i = 0; i <= maxValidMetadata(); i++) {
            int row = i / 16;
            int col = i % 16;
            uvs[i] = atlas.uv("blocks.png", col, 6 + row);
        }
    }

    protected override BlockItem createItem() {
        return new CandyBlockItem(this);
    }

    public static readonly string[] colourNames = [
        "White", "Gray", "Black", "Dark Red",
        "Red", "Orange", "Yellow", "Light Green",
        "Dark Green", "Turquoise", "Sky Blue", "Blue",
        "Dark Blue", "Violet", "Purple", "Pink",


        // extended colours
        "Beige", "Light Orange", "Neon", "Apple Green",
        "Cyan", "Light Purple", "Dark Violet", "Brown",
    ];

    public static readonly Color[] colours = [
        //Color.FromBgra(0xFFf2f2f2), // White
        Color.FromBgra(0xFFf5f5f5), // White
        //Color.FromBgra(0xFF858585), // Gray
        Color.FromBgra(0xFF818181), // Gray
        //Color.FromBgra(0xFF404040), // Black
        Color.FromBgra(0xFF242424), // Black
        Color.FromBgra(0xFF840101), // Dark Red

        //Color.FromBgra(0xFFfe0909), // Red
        Color.FromBgra(0xFFff1414), // Red
        //Color.FromBgra(0xFFff9e42), // Orange
        Color.FromBgra(0xFFff8616), //Orange
        //Color.FromBgra(0xFFe4e81a), // Yellow
        Color.FromBgra(0xFFf1ed1e), // Yellow
        //Color.FromBgra(0xFF0df415), // Light Green
        Color.FromBgra(0xFF11f019), // Light Green

        //Color.FromBgra(0xFF084e0b), // Dark Green
        Color.FromBgra(0xFF023a04), // Dark Green
        //Color.FromBgra(0xFF3fdccc), // Turquoise
        Color.FromBgra(0xFF39e1d1), // Turquoise
        //Color.FromBgra(0xFF0db5e3), // Sky Blue
        Color.FromBgra(0xFF14bbe5), // Sky Blue
        //Color.FromBgra(0xFF1542F8), // Blue
        Color.FromBgra(0xFF1638f8), // Blue

        //Color.FromBgra(0xFF0014c7), // Dark Blue
        Color.FromBgra(0xFF05058f), // Dark Blue
        Color.FromBgra(0xFF9d5bd7), // Violet
        Color.FromBgra(0xFFd553f0), // Purple
        //Color.FromBgra(0xFFff8ee1), // Pink
        Color.FromBgra(0xFFff81dc), // Pink

        Color.FromBgra(0xFFfdecd8), // Beige
        //Color.FromBgra(0xFFffbe42), // Light Orange
        Color.FromBgra(0xFFffba38), // Light Orange
        //Color.FromBgra(0xFFb0fa00), // Neon
        Color.FromBgra(0xFFb8ff05), // Neon
        //Color.FromBgra(0xFF00f8a6), // Apple Green
        Color.FromBgra(0xFF05ffad), // Apple Green
        //Color.FromBgra(0xFF06e9ed), // Cyan
        Color.FromBgra(0xFF0ceef3), // Cyan
        Color.FromBgra(0xFFf3aafc), // Light Purple
        Color.FromBgra(0xFF8018a0), // Dark Violet
        //Color.FromBgra(0xFF89440b), // Brown
        Color.FromBgra(0xFF7c3d08), // Brown
    ];

    public override byte maxValidMetadata() => 23;

    public string getName(byte metadata) => $"{colourNames[metadata]} Candy";

    public override UVPair getTexture(int faceIdx, int metadata) {
        // handle two rows
        return uvs[metadata];
    }
}