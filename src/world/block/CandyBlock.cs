namespace BlockGame.world.block;

public class CandyBlock : Block {
    public CandyBlock(ushort id, string name) : base(id, name) {
        renderType[id] = RenderType.CUBE_DYNTEXTURE;
    }

    public static readonly string[] colourNames = [
        "Blue", "Sky Blue", "Turquoise", "Dark Green", "Light Green",
        "Orange", "Yellow", "Light Red", "Pink", "Purple", 
        "Violet", "Red", "Dark Blue", "White", "Gray", "Black",
        "Cyan", "Apple Green", "Lime", "Neon", "Light Orange", "Brown", "Light Purple", "Dark Violet"
    ];
    
    public override byte maxValidMetadata() => 23;
    
    public string getName(byte metadata) => $"{colourNames[metadata]} Candy";
    
    public override UVPair getTexture(int faceIdx, int metadata) {
        // handle two rows
        return new UVPair(metadata & 0xF, 6 + (metadata >> 4));
    }
}