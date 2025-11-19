namespace BlockGame.world.block;

public class GlowingOre : Block {
    public GlowingOre(string name) : base(name) {
    }


    public override UVPair getTexture(int faceIdx, int metadata) {
        return uvs[0] + new UVPair(metadata, 0);
    }
}