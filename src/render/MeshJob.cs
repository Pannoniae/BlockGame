using BlockGame.GL.vertexformats;
using BlockGame.util;
using BlockGame.world.chunk;

namespace BlockGame.render;

public sealed class MeshJob : XJob {
    public readonly BlockRenderer br = new();

    public SubChunk? section;
    public SubChunkCoord coord;
    public bool doTranslucent;

    public readonly XList<BlockVertexPacked> opaque = new(2048);
    public readonly XList<BlockVertexPacked> translucent = new(512);

    public override void run() {
        br.build(section!, opaque, translucent, doTranslucent);
    }
}
