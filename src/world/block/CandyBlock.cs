namespace BlockGame.world.block;

public class CandyBlock : Block {
    public CandyBlock(ushort id, string name) : base(id, name) {
        renderType[id] = RenderType.CUBE_DYNTEXTURE;
    }

    public static readonly string[] colourNames = [
        "Blue", "Sky Blue", "Turquoise", "Dark Green", "Light Green",
        "Orange", "Yellow", "Light Red", "Pink", "Purple", 
        "Violet", "Red", "Dark Blue", "White", "Gray", "Black",
        "Cyan", "Apple Green", "Beige", "Neon", "Light Orange", "Brown", "Light Purple", "Dark Violet"
    ];

    public static readonly Color[] colours = [
        Color.FromBgra(0xFF1542F8),
        Color.FromBgra(0xFF0db5e3),
        Color.FromBgra(0xFF3fdccc),
        Color.FromBgra(0xFF084e0b),
        Color.FromBgra(0xFF0df415),
        Color.FromBgra(0xFFff9e42),
        Color.FromBgra(0xFFe4e81a),
        Color.FromBgra(0xFFfe0909),
        Color.FromBgra(0xFFff8ee1),
        Color.FromBgra(0xFFd55df1),
        Color.FromBgra(0xFF9d5bd7),
        Color.FromBgra(0xFFbe0505),
        Color.FromBgra(0xFF0014c7),
        Color.FromBgra(0xFFf2f2f2),
        Color.FromBgra(0xFF858585),
        Color.FromBgra(0xFF404040),
        Color.FromBgra(0xFF06e9ed),
        Color.FromBgra(0xFF00f8a6),
        Color.FromBgra(0xFFfdecd8),
        Color.FromBgra(0xFFb0fa00),
        Color.FromBgra(0xFFffbe42),
        Color.FromBgra(0xFF89440b),
        Color.FromBgra(0xFFf3aafc),
        Color.FromBgra(0xFF8018a0),
    ];
    
    public override byte maxValidMetadata() => 23;
    
    public string getName(byte metadata) => $"{colourNames[metadata]} Candy";
    
    public override UVPair getTexture(int faceIdx, int metadata) {
        // handle two rows
        return new UVPair(metadata & 0xF, 6 + (metadata >> 4));
    }
}