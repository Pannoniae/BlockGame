namespace BlockGame.world.block;

public class CandyBlock : Block {
    public CandyBlock(ushort id, string name) : base(id, name) {
        renderType[id] = RenderType.CUBE_DYNTEXTURE;
    }

    public static readonly string[] colourNames = [
        "White", "Gray", "Black", "Yellow",
        "Orange", "Red", "Dark Red", "Pink",
        "Purple", "Violet",  "Light Green", "Turquoise",
        "Blue", "Sky Blue", "Dark Green", "Dark Blue",

        // extended colours
        "Beige", "Light Orange", "Neon", "Apple Green",
        "Cyan", "Light Purple", "Dark Violet", "Brown",
    ];

    public static readonly Color[] colours = [
        Color.FromBgra(0xFFf2f2f2), // White
        Color.FromBgra(0xFF858585), // Gray
        Color.FromBgra(0xFF404040), // Black
        Color.FromBgra(0xFFe4e81a), // Yellow
        Color.FromBgra(0xFFff9e42), // Orange
        Color.FromBgra(0xFFfe0909), // Red
        Color.FromBgra(0xFFbe0505), // Dark Red
        Color.FromBgra(0xFFff8ee1), // Pink
        Color.FromBgra(0xFFd55df1), // Purple
        Color.FromBgra(0xFF9d5bd7), // Violet
        Color.FromBgra(0xFF0df415), // Light Green
        Color.FromBgra(0xFF3fdccc), // Turquoise
        Color.FromBgra(0xFF1542F8), // Blue
        Color.FromBgra(0xFF0db5e3), // Sky Blue
        Color.FromBgra(0xFF084e0b), // Dark Green
        Color.FromBgra(0xFF0014c7), // Dark Blue

        Color.FromBgra(0xFFfdecd8), // Beige
        Color.FromBgra(0xFFffbe42), // Light Orange
        Color.FromBgra(0xFFb0fa00), // Neon
        Color.FromBgra(0xFF00f8a6), // Apple Green
        Color.FromBgra(0xFF06e9ed), // Cyan
        Color.FromBgra(0xFFf3aafc), // Light Purple
        Color.FromBgra(0xFF8018a0), // Dark Violet
        Color.FromBgra(0xFF89440b), // Brown
    ];
    
    public override byte maxValidMetadata() => 23;
    
    public string getName(byte metadata) => $"{colourNames[metadata]} Candy";
    
    public override UVPair getTexture(int faceIdx, int metadata) {
        // handle two rows
        return new UVPair(metadata & 0xF, 6 + (metadata >> 4));
    }
}