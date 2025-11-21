using BlockGame.GL.vertexformats;
using BlockGame.render;
using BlockGame.util;
using BlockGame.world.item;

namespace BlockGame.world.block;

public class CandySlab : Slabs {
    public CandySlab(string name) : base(name) {
    }

    protected override BlockItem createItem() {
        return new CandySlabItem(this);
    }

    protected override void onRegister(int id) {
        base.onRegister(id);

        // set uvs
        // return new UVPair(color & 0xF, 6 + (color >> 4));
        uvs = new UVPair[maxValidMetadata() + 1];
        for (int i = 0; i <= maxValidMetadata(); i++) {
            int color = getColour((byte)i);
            int row = color / 16;
            int col = color % 16;
            uvs[i] = uv("blocks.png", col, 6 + row);
        }
    }

    /**
     * Metadata encoding:
     * Bits 0-1: inherited from Slabs (position/double)
     * Bits 2-7: candy colour (0-23)
     */
    public static byte getColour(byte metadata) => (byte)((metadata >> 2) & 0x3F);
    public static byte setColour(byte metadata, byte color) => (byte)((metadata & 0b11) | ((color & 0x3F) << 2));

    public override UVPair getTexture(int faceIdx, int metadata) {
        return uvs[metadata];
    }

    public override byte maxValidMetadata() {
        // 2 bits for slab state (0-3) + 6 bits for colour (0-63)
        // but candy only has 24 colours, so max colour is 23
        return (23 << 2) | 3; // = 95
    }

    public override void place(World world, int x, int y, int z, byte metadata, Placement info) {
        var color = getColour(metadata);
        var existingBlock = world.getBlockRaw(x, y, z);

        byte finalMeta;

        // check if combining with existing slab of same color
        if (existingBlock.getID() == id) {
            var existingMeta = existingBlock.getMetadata();
            if (!isDouble(existingMeta) && getColour(existingMeta) == color) {
                // make double slab, preserve color
                finalMeta = setColour(setDouble(setTop(0, false), true), color);
            } else {
                return; // can't place
            }
        } else {

            var hitPoint = info.hitPoint;
            bool placeOnTop = info.face == RawDirection.DOWN || info.face != RawDirection.UP && (hitPoint.Y - y) > 0.5;

            finalMeta = setColour(setTop(0, placeOnTop), color);
        }

        world.setBlockMetadata(x, y, z, ((uint)id).setMetadata(finalMeta));
        world.blockUpdateNeighbours(x, y, z);
    }

    public string getName(byte metadata) {
        var color = getColour(metadata);
        return $"{CandyBlock.colourNames[color]} Candy Slab";
    }

    public override void render(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices) {
        x &= 15;
        y &= 15;
        z &= 15;

        var block = br.getBlock();
        var metadata = block.getMetadata();
        var top = isTop(metadata);
        var doubleSlab = isDouble(metadata);

        // use getTexture() instead of uvs[0]
        var min = getTexture(0, metadata);
        var max = min + 1;

        if (br.forceTex.u >= 0 && br.forceTex.v >= 0) {
            min = br.forceTex;
            max = br.forceTex + 1;
        }

        var uv0 = UVPair.texCoords(min);
        var uv1 = UVPair.texCoords(max);
        float u0 = uv0.X;
        float v0 = uv0.Y;
        float u1 = uv1.X;
        float v1 = uv1.Y;

        float y0;
        float y1;

        if (doubleSlab) {
            y0 = 0f;
            y1 = 1f;
        } else if (top) {
            y0 = 0.5f;
            y1 = 1f;
        } else {
            y0 = 0f;
            y1 = 0.5f;
        }

        br.renderCube(x, y, z, vertices, 0f, y0, 0f, 1f, y1, 1f, u0, v0, u1, v1);
    }
}
