using BlockGame.GL.vertexformats;
using BlockGame.render;
using BlockGame.util;
using BlockGame.world.item;

namespace BlockGame.world.block;

public class CandyStairs : Stairs {
    public CandyStairs(string name) : base(name) {
    }

    protected override BlockItem createItem() {
        return new CandyStairsItem(this);
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
     * Bits 0-2: inherited from Stairs (facing, upside-down)
     * Bits 3-7: candy color (0-23)
     */
    public static byte getColour(byte metadata) => (byte)((metadata >> 3) & 0x1F);
    public static byte setColour(byte metadata, byte color) => (byte)((metadata & 0b111) | ((color & 0x1F) << 3));

    public override UVPair getTexture(int faceIdx, int metadata) {
        return uvs[metadata];
    }

    public override void place(World world, int x, int y, int z, byte metadata, Placement info) {
        var color = getColour(metadata);
        var opposite = Direction.getOpposite(info.hfacing);
        var finalMeta = setColour(0, color);
        finalMeta = (byte)((finalMeta & ~0b11) | ((byte)opposite & 0b11));

        world.setBlockMetadata(x, y, z, ((uint)id).setMetadata(finalMeta));
        world.blockUpdateNeighbours(x, y, z);
    }

    public override byte maxValidMetadata() {
        // 3 bits for stair state (0-7) + 5 bits for colour (0-31)
        // but candy only has 24 colours, so max colour is 23
        return (23 << 3) | 7; // = 191
    }

    public override bool same(ItemStack self, ItemStack other) {
        return other.id == self.id && getColour((byte)other.metadata) == getColour((byte)self.metadata);
    }

    public override ItemStack getCanonical(byte metadata) {
        return new ItemStack(id, 1, setColour(0, getColour(metadata)));
    }

    public override void getDrop(List<ItemStack> drops, World world, int y, int z, int i, byte metadata, bool canBreak) {
        if (canBreak) {
            drops.Add(new ItemStack(getItem(), 1, setColour(0, getColour(metadata))));
        }
    }

    public string getName(byte metadata) {
        var color = getColour(metadata);
        return $"{CandyBlock.colourNames[color]} Candy Stairs";
    }

    public override void render(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices) {
        x &= 15; y &= 15; z &= 15;

        var block = br.getBlock();
        var metadata = block.getMetadata();
        var facing = (byte)(metadata & 0b11);

        // use getTexture() instead of uvs[0]
        var min = getTexture(0, metadata);
        var max = min + 1;

        if (br.forceTex.u >= 0 && br.forceTex.v >= 0) {
            min = new UVPair(br.forceTex.u, br.forceTex.v);
            max = min + 1;
        }

        var uv0 = UVPair.texCoords(min);
        var uv1 = UVPair.texCoords(max);
        var u0 = uv0.X;
        var v0 = uv0.Y;
        var u1 = uv1.X;
        var v1 = uv1.Y;

        var (tx1, tz1, tx2, tz2) = facing switch {
            0 => (0.5f, 0f, 1f, 1f),
            1 => (0f, 0f, 0.5f, 1f),
            2 => (0f, 0.5f, 1f, 1f),
            _ => (0f, 0f, 1f, 0.5f)
        };

        br.renderCube(x, y, z, vertices, 0f, 0f, 0f, 1f, 0.5f, 1f, u0, v0, u1, v1);
        br.renderCube(x, y, z, vertices, tx1, 0.5f, tz1, tx2, 1f, tz2, u0, v0, u1, v1);
    }
}
