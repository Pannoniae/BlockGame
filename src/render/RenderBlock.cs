using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using BlockGame.ui;
using BlockGame.util;
using Molten;

namespace BlockGame;

/// <summary>
/// Render blocks manually!
/// </summary>
public class RenderBlock {

    public static bool neighbourTest(World world, Vector3I pos, RawDirection direction) {
        var neighbour = world.getBlock(pos + Direction.getDirection(direction));
        var isTranslucent = Blocks.get(world.getBlock(pos)).type == BlockType.TRANSLUCENT;
        var flag = false;
        switch (isTranslucent) {
            case false:
                flag = Blocks.notSolid(neighbour) || !Blocks.isFullBlock(neighbour);
                break;
            case true:
                flag = !Blocks.isTranslucent(neighbour) && (Blocks.notSolid(neighbour) || !Blocks.isFullBlock(neighbour));
                break;
        }
        return flag;
    }

    public static void addVertexWithAO(World world, Vector3I pos, List<BlockVertexPacked> vertexBuffer, List<ushort> indexBuffer,
        float vx, float vy, float vz, UVPair uv, RawDirection direction, int currentIndex) {

        ref var offsetArray = ref MemoryMarshal.GetReference(SubChunkRenderer.offsetTable);

        var chunk = world.getChunkSection(World.getChunkSectionPos(pos));

        byte ao = 0;
        ushort data;
        FourBytes light;
        Unsafe.SkipInit(out light);

        var neighbour = pos + Direction.getDirection(direction);

        if (!Settings.instance.smoothLighting) {
            light.First = light.Second = light.Third = light.Fourth = world.getLight(neighbour.X, neighbour.Y, neighbour.Z);
        }
        // AO requires smooth lighting. Otherwise don't need to deal with sampling any of this
        if (Settings.instance.smoothLighting || Settings.instance.AO) {
            if (direction != RawDirection.NONE) {

                // ox, oy, oz, mult
                byte o;
                // need to store 9 sbytes so it's a 16-element vector
                Vector128<sbyte> vector;
                // lx, ly, lz, lo
                FourBytes l;
                Unsafe.SkipInit(out l);

                ao = 0;
                light.Whole = 0;

                for (int j = 0; j < 4; j++) {
                    //mult = dirIdx * 36 + j * 9 + vert * 3;

                    // load the vector with the offsets
                    vector = Vector128.LoadUnsafe(ref Unsafe.Add(ref offsetArray, (int)direction * 36 + j * 9));

                    // premultiply cuz its faster that way
                    o = SubChunkRenderer.toByte(Blocks.isFullBlock(chunk.blocks[pos.X + vector[0], pos.Y + vector[1], pos.Z + vector[2]]));
                    l.First = chunk.blocks.getLight(pos.X + vector[0], pos.Y + vector[1], pos.Z + vector[2]);

                    o |= (byte)(SubChunkRenderer.toByte(Blocks.isFullBlock(chunk.blocks[pos.X + vector[3], pos.Y + vector[4], pos.Z + vector[5]])) << 1);
                    l.Second = chunk.blocks.getLight(pos.X + vector[3], pos.Y + vector[4], pos.Z + vector[5]);

                    o |= (byte)(SubChunkRenderer.toByte(Blocks.isFullBlock(chunk.blocks[pos.X + vector[6], pos.Y + vector[7], pos.Z + vector[8]])) << 2);
                    l.Third = chunk.blocks.getLight(pos.X + vector[6], pos.Y + vector[7], pos.Z + vector[8]);

                    // only apply AO if enabled
                    if (Settings.instance.AO) {
                        ao |= (byte)((SubChunkRenderer.calculateAOFixed(o) & 0x3) << j * 2);
                        //Console.Out.WriteLine(ao);
                    }
                    if (Settings.instance.smoothLighting) {
                        // if smooth lighting enabled, average light from neighbour face + the 3 other ones
                        // calculate average
                        l.Fourth = world.getLight(neighbour.X, neighbour.Y, neighbour.Z);

                        // split light and reassemble it again
                        light.Whole |= (uint)((byte)(
                            SubChunkRenderer.average(Unsafe.BitCast<uint, FourBytes>((l.Whole >> 4) & 0x0F0F0F0F),
                                o)
                            << 4 |
                            SubChunkRenderer.average(Unsafe.BitCast<uint, FourBytes>(l.Whole & 0x0F0F0F0F),
                                o)
                        ) << j * 8);
                    }
                }
            }
        }

        data = Block.packData((byte)direction, (byte)(ao & 0x3), light.First);
        vertexBuffer.Add(new BlockVertexPacked(pos.X + vx, pos.Y + vy, pos.Z + vz, Block.texU(uv.u), Block.texV(uv.v), data));
        indexBuffer.Add((ushort)currentIndex);
    }
}