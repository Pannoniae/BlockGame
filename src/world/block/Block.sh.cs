using System.Runtime.CompilerServices;
using BlockGame.main;
using BlockGame.util;
using Molten.DoublePrecision;

namespace BlockGame.world.block;

public partial class Block {
    public static bool notSolid(int block) {
        return block == 0 || get(block).layer != RenderLayer.SOLID;
    }

    public static bool isTranslucent(int block) {
        return translucent[block];
    }

    public static UVPair[] cubeUVs(int x, int y) {
        var c = uv("blocks.png", x, y);
        return [c, c, c, c, c, c];
    }

    public static UVPair[] grassUVs(int topX, int topY, int sideX, int sideY, int bottomX, int bottomY) {
        var side = uv("blocks.png", sideX, sideY);
        var bottom = uv("blocks.png", bottomX, bottomY);
        var top = uv("blocks.png", topX, topY);
        return [side, side, side, side, bottom, top];
    }

    public static UVPair[] furnaceUVs(int frontX, int frontY, int litX, int litY, int sideX, int sideY, int top_bottomX, int top_bottomY) {
        return [
            uv("blocks.png", frontX, frontY),
            uv("blocks.png", litX, litY),
            uv("blocks.png", sideX, sideY),
            uv("blocks.png", top_bottomX, top_bottomY)];
    }

    public static UVPair[] CTUVs(int topX, int topY, int xx, int xy, int zx, int zy, int bottomX, int bottomY) {
        var x = uv("blocks.png", xx, xy);
        var z = uv("blocks.png", zx, zy);
        var bottom = uv("blocks.png", bottomX, bottomY);
        var top = uv("blocks.png", topX, topY);
        return [x, x, z, z, bottom, top];
    }

    public static UVPair[] chestUVs(int topX, int topY, int xx, int xy, int zx, int zy, int bottomX, int bottomY) {
        var x = uv("blocks.png", xx, xy);
        var z = uv("blocks.png", zx, zy);
        var bottom = uv("blocks.png", bottomX, bottomY);
        var top = uv("blocks.png", topX, topY);
        return [x, x, z, x, bottom, top];
    }

    public static UVPair[] ldetectorUVs(int topX, int topY, int xx, int xy, int zx, int zy) {
        var x = uv("blocks.png", xx, xy);
        var z = uv("blocks.png", zx, zy);
        var top = uv("blocks.png", topX, topY);
        return [x, z, top, top, top, top];
    }

    public static UVPair[] crossUVs(int x, int y) {
        var c = uv("blocks.png", x, y);
        return [c, c];
    }

    public static UVPair[] HeadUVs(int leftX, int leftY, int rightX, int rightY, int frontX, int frontY, int backX,
        int backY, int bottomX, int bottomY, int topX, int topY) {
        return [
            uv("blocks.png", leftX, leftY),
            uv("blocks.png", rightX, rightY),
            uv("blocks.png", frontX, frontY),
            uv("blocks.png", backX, backY),
            uv("blocks.png", bottomX, bottomY),
            uv("blocks.png", topX, topY)
        ];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort packData(byte direction, byte ao, byte light) {
        // idx[0] = texU == 1, idx[1] = texV == 1

        // if none, treat it as an up (strip 4th byte)
        var a = 2;
        return (ushort)(light << 8 | ao << 3 | direction & 0b111);
    }

    public static Color packColour(byte direction, byte ao, byte light) {
        Span<float> aoArray = [1.0f, 0.75f, 0.5f, 0.25f];
        Span<float> a = [0.8f, 0.8f, 0.6f, 0.6f, 0.6f, 1];

        direction = (byte)(direction & 0b111);
        var blocklight = (byte)(light >> 4);
        var skylight = (byte)(light & 0xF);
        var lightVal = Game.textures.light(blocklight, skylight);
        float tint = a[direction] * aoArray[ao];
        var ab = new Color(lightVal.R / 255f * tint, lightVal.G / 255f * tint, lightVal.B / 255f * tint, 1);
        return ab;
    }

    public static Color packColour(RawDirection direction, byte ao, byte light) {
        return packColour((byte)direction, ao, light);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color packColour(byte direction, byte ao) {
        Span<float> aoArray = [1.0f, 0.75f, 0.5f, 0.25f];
        Span<float> a = [0.8f, 0.8f, 0.6f, 0.6f, 0.6f, 1];

        direction &= 0b111;
        byte tint = (byte)(a[direction] * aoArray[ao] * 255);
        return new Color(tint, tint, tint, (byte)255);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint packColourB(byte direction, byte ao) {
        // can we just inline the array?

        Span<float> aoArray = [1.0f, 0.75f, 0.5f, 0.25f];
        Span<float> a = [0.8f, 0.8f, 0.6f, 0.6f, 0.6f, 1];


        direction &= 0b111;
        byte tint = (byte)(a[direction] * aoArray[ao] * 255);
        return (uint)(tint | (tint << 8) | (tint << 16) | (255 << 24));
    }

    public static AABB fullBlockAABB() {
        return new AABB(new Vector3D(0, 0, 0), new Vector3D(1, 1, 1));
    }

    public Block flowerAABB() {
        var offset = 6 / 16f;
        AABB[id] = new AABB(new Vector3D(0 + offset, 0, 0 + offset), new Vector3D(1 - offset, 0.5, 1 - offset));
        return this;
    }

    public Block shortGrassAABB() {
        var offset = 4 / 16f;
        AABB[id] = new AABB(new Vector3D(0, 0, 0), new Vector3D(1, offset, 1));
        return this;
    }

    public Block torchAABB() {
        var offset = 6 / 16f;
        AABB[id] = new AABB(new Vector3D(0 + offset, 0, 0 + offset), new Vector3D(1 - offset, 1, 1 - offset));
        noCollision();
        return this;
    }

    public Block transparency() {
        transparent[id] = true;
        fullBlock[id] = false;
        return this;
    }

    public Block translucency() {
        layer = RenderLayer.TRANSLUCENT;
        translucent[id] = true;
        fullBlock[id] = false;
        return this;
    }

    public Block waterTransparent() {
        waterSolid[id] = false;
        return this;
    }

    public Block noCollision() {
        collision[id] = false;
        //AABB[id] = null;
        return this;
    }

    public Block noSelection() {
        selection[id] = false;
        return this;
    }

    public Block partialBlock() {
        fullBlock[id] = false;
        return this;
    }

    public Block makeLiquid() {
        translucency();
        noCollision();
        noSelection();
        waterTransparent();
        liquid[id] = true;
        fullBlock[id] = false;
        return this;
    }

    public Block setCustomRender() {
        renderType[id] = RenderType.CUSTOM;
        return this;
    }

    public Block light(byte amount) {
        lightLevel[id] = amount;
        return this;
    }

    public Block setLightAbsorption(byte amount) {
        lightAbsorption[id] = amount;
        return this;
    }

    public Block air() {
        noCollision();
        noSelection();
        fullBlock[id] = false;
        return this;
    }

    public Block tick() {
        randomTick[id] = true;
        return this;
    }

    public Block itemLike() {
        renderItemLike[id] = true;
        return this;
    }

    public Block setHardness(double hards) {
        hardness[id] = hards;
        return this;
    }

    public Block setFlammable(double value) {
        flammable[id] = value;
        return this;
    }

    public Block material(Material mat) {
        this.mat = mat;
        tool[id] = mat.toolType;
        tier[id] = mat.tier;
        hardness[id] = mat.hardness;
        return this;
    }

    public Block setTier(MaterialTier t) {
        tier[id] = t;
        return this;
    }

    private void setFriction(float f) {
        friction[id] = f;
    }
}