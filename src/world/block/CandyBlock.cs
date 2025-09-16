namespace BlockGame.world.block;

public class CandyBlock : Block {
    public CandyBlock(ushort id, string name) : base(id, name) {
        renderType[id] = RenderType.CUBE_DYNTEXTURE;
    }

    private static readonly string[] colorNames = [
        "Light Blue", "Cyan", "Turquoise", "Dark Green", "Light Green", 
        "Orange", "Yellow", "Light Red", "Pink", "Purple", 
        "Violet", "Red", "Dark Blue", "White", "Grey", "Black"
    ];
    
    public override byte maxValidMetadata() => 15;
    
    public string getName(byte metadata) => $"{colorNames[metadata]} Candy";
    
    public override UVPair getTexture(int faceIdx, int metadata) {
        return new UVPair(metadata, 2);
    }
}