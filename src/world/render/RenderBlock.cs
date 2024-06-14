using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BlockGame.GUI;
using BlockGame.util;
using Silk.NET.Maths;

namespace BlockGame;

/// <summary>
/// Render blocks manually!
/// </summary>
public class RenderBlock {

    public static bool neighbourTest(World world, Vector3D<int> pos, RawDirection direction) {
        var neighbour = Blocks.get(world.getBlock(pos + Direction.getDirection(direction)));
        var isTranslucent = Blocks.get(world.getBlock(pos)).type == BlockType.TRANSLUCENT;
        var flag = false;
        switch (isTranslucent) {
            case false:
                flag = Blocks.notSolid(neighbour) || !neighbour.isFullBlock;
                break;
            case true:
                flag = !Blocks.isTranslucent(neighbour) && (Blocks.notSolid(neighbour) || !neighbour.isFullBlock);
                break;
        }
        return flag;
    }

    public static void addVertexWithAO(World world, Vector3D<int> pos, List<BlockVertex> vertexBuffer, List<ushort> indexBuffer,
        float x, float y, float z, UVPair uv, RawDirection direction, int currentIndex) {

        ref var offsetArray = ref MemoryMarshal.GetArrayDataReference<sbyte>(ChunkSectionRenderer.offsetTable);

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
                FourShorts o;
                Unsafe.SkipInit(out o);
                // bx, by, bz
                FourSBytes b;
                // lx, ly, lz, lo
                FourBytes l;

                ao = 0;
                light.Whole = 0;

                for (int j = 0; j < 4; j++) {
                    //mult = dirIdx * 36 + j * 9 + vert * 3;
                    o.Fourth = (ushort)((int)direction * 36 + j * 9);
                    // premultiply cuz its faster that way
                    b.First = (sbyte)(x + Unsafe.Add(ref offsetArray, o.Fourth));
                    o.Fourth++;
                    b.Second = (sbyte)(y + Unsafe.Add(ref offsetArray, o.Fourth));
                    o.Fourth++;
                    b.Third = (sbyte)(z + Unsafe.Add(ref offsetArray, o.Fourth));
                    o.Fourth++;
                    o.First = chunk.blocks[b.First, b.Second, b.Third];
                    l.First = chunk.blocks.getLight(b.First, b.Second, b.Third);

                    b.First = (sbyte)(x + Unsafe.Add(ref offsetArray, o.Fourth));
                    o.Fourth++;
                    b.Second = (sbyte)(y + Unsafe.Add(ref offsetArray, o.Fourth));
                    o.Fourth++;
                    b.Third = (sbyte)(z + Unsafe.Add(ref offsetArray, o.Fourth));
                    o.Fourth++;
                    o.Second = chunk.blocks[b.First, b.Second, b.Third];
                    l.Second = chunk.blocks.getLight(b.First, b.Second, b.Third);

                    b.First = (sbyte)(x + Unsafe.Add(ref offsetArray, o.Fourth));
                    o.Fourth++;
                    b.Second = (sbyte)(y + Unsafe.Add(ref offsetArray, o.Fourth));
                    o.Fourth++;
                    b.Third = (sbyte)(z + Unsafe.Add(ref offsetArray, o.Fourth));
                    //mult++;
                    o.Third = chunk.blocks[b.First, b.Second, b.Third];
                    l.Third = chunk.blocks.getLight(b.First, b.Second, b.Third);

                    // construct flags
                    var flags = (byte)(ChunkSectionRenderer.toByte(Blocks.isFullBlock(o.First)) |
                                       ChunkSectionRenderer.toByte(Blocks.isFullBlock(o.Second)) << 1 |
                                ChunkSectionRenderer.toByte(Blocks.isFullBlock(o.Third)) << 2);

                    // only apply AO if enabled
                    if (Settings.instance.AO) {
                        ao |= (byte)((ChunkSectionRenderer.calculateAOFixed(o.First, o.Second, o.Third) & 0x3) << j * 2);
                        //Console.Out.WriteLine(ao);
                    }
                    if (Settings.instance.smoothLighting) {
                        // if smooth lighting enabled, average light from neighbour face + the 3 other ones
                        // calculate average
                        l.Fourth = world.getLight(neighbour.X, neighbour.Y, neighbour.Z);


                        // this averages the four light values. If the block is opaque, it ignores the light value.
                        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
                        byte average(byte lx, byte ly, byte lz, byte lo, FourShorts o) {
                            byte flags = 0;
                            // check ox
                            if (o.First == 0) {
                                flags = 1;
                            }
                            if (o.Second == 0) {
                                flags |= 2;
                            }
                            // if both sides are blocked, don't check the corner, won't be visible anyway
                            if (o.Third == 0 && flags != 0) {
                                flags |= 4;
                            }
                            return (byte)((lx * (flags & 1) + ly * ((flags & 2) >> 1) + lz * ((flags & 4) >> 2) + lo) / (BitOperations.PopCount(flags) + 1f));
                        }

                        // split light and reassemble it again
                        light.Whole |= (uint)((byte)(
                            average((byte)(l.First >> 4), (byte)(l.Second >> 4), (byte)(l.Third >> 4), (byte)(l.Fourth >> 4),
                                o)
                            << 4 |
                            average((byte)(l.First & 0xF), (byte)(l.Second & 0xF), (byte)(l.Third & 0xF), (byte)(l.Fourth & 0xF),
                                o)
                        ) << j * 8);
                    }
                }
            }
        }

        data = Block.packData((byte)direction, (byte)(ao & 0x3), light.First);
        vertexBuffer.Add(new BlockVertex(pos.X + x, pos.Y + y, pos.Z + z, Block.texU(uv.u), Block.texV(uv.v), data));
        indexBuffer.Add((ushort)currentIndex);
    }
}