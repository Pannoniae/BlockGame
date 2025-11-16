using BlockGame.util;
using BlockGame.world.chunk;
using K4os.Compression.LZ4;

namespace BlockGame.net.packet;

/** S→C: 0x10 - sends chunk data */
public class ChunkDataPacket : Packet {
    public ChunkCoord coord;
    public SubChunkData[] subChunks;

    public void write(PacketBuffer buf) {
        buf.writeInt(coord.x);
        buf.writeInt(coord.z);

        // serialize to temp buffer
        using var tempMs = new MemoryStream();
        using var tempWriter = new BinaryWriter(tempMs);
        var tempBuf = new PacketBuffer(tempWriter);

        // write number of non-empty subchunks
        tempBuf.writeByte((byte)subChunks.Length);

        // write each subchunk
        foreach (var sub in subChunks) {
            tempBuf.writeByte(sub.y);

            // block data
            tempBuf.writeInt(sub.vertices.Length);
            foreach (var v in sub.vertices) {
                tempBuf.writeUInt(v);
            }

            tempBuf.writeInt(sub.blockRefs.Length);
            foreach (var r in sub.blockRefs) {
                tempBuf.writeUShort(r);
            }

            tempBuf.writeBool(sub.indices != null);
            if (sub.indices != null) {
                tempBuf.writeInt(sub.indices.Length);
                tempBuf.writeRawBytes(sub.indices);
            }

            tempBuf.writeInt(sub.count);
            tempBuf.writeInt(sub.vertCount);
            tempBuf.writeInt(sub.density);

            // light data
            tempBuf.writeInt(sub.lightVertices.Length);
            tempBuf.writeRawBytes(sub.lightVertices);

            tempBuf.writeInt(sub.lightRefs.Length);
            foreach (var r in sub.lightRefs) {
                tempBuf.writeUShort(r);
            }

            tempBuf.writeBool(sub.lightIndices != null);
            if (sub.lightIndices != null) {
                tempBuf.writeInt(sub.lightIndices.Length);
                tempBuf.writeRawBytes(sub.lightIndices);
            }

            tempBuf.writeInt(sub.lightCount);
            tempBuf.writeInt(sub.lightVertCount);
            tempBuf.writeInt(sub.lightDensity);

            // metadata
            tempBuf.writeInt(sub.blockCount);
            tempBuf.writeInt(sub.translucentCount);
            tempBuf.writeInt(sub.fullBlockCount);
            tempBuf.writeInt(sub.randomTickCount);
            tempBuf.writeInt(sub.renderTickCount);
        }

        // compress
        var uncompressed = tempMs.ToArray();
        var compressed = new byte[LZ4Codec.MaximumOutputSize(uncompressed.Length)];
        int compressedLen = LZ4Codec.Encode(uncompressed, compressed, LZ4Level.L00_FAST);

        // write uncompressed size + compressed data
        buf.writeInt(uncompressed.Length);
        buf.writeInt(compressedLen);
        buf.writeRawBytes(compressed, 0, compressedLen);
    }

    public void read(PacketBuffer buf) {
        coord = new ChunkCoord(buf.readInt(), buf.readInt());

        // read compressed data
        int uncompressedSize = buf.readInt();
        int compressedSize = buf.readInt();
        var compressed = buf.readBytes(compressedSize);

        // decompress
        var uncompressed = new byte[uncompressedSize];
        int decoded = LZ4Codec.Decode(compressed, uncompressed);
        if (decoded != uncompressedSize) {
            InputException.throwNew($"LZ4 decompression failed: expected {uncompressedSize} bytes, got {decoded}");
        }

        // deserialize from uncompressed buffer
        using var tempMs = new MemoryStream(uncompressed);
        using var tempReader = new BinaryReader(tempMs);
        var tempBuf = new PacketBuffer(tempReader);

        // read number of subchunks
        int numSubChunks = tempBuf.readByte();
        subChunks = new SubChunkData[numSubChunks];

        // read each subchunk
        for (int i = 0; i < numSubChunks; i++) {
            var sub = new SubChunkData();
            sub.y = tempBuf.readByte();

            // block data
            int verticesLen = tempBuf.readInt();
            sub.vertices = new uint[verticesLen];
            for (int j = 0; j < verticesLen; j++) {
                sub.vertices[j] = tempBuf.readUInt();
            }

            int blockRefsLen = tempBuf.readInt();
            sub.blockRefs = new ushort[blockRefsLen];
            for (int j = 0; j < blockRefsLen; j++) {
                sub.blockRefs[j] = tempBuf.readUShort();
            }

            bool hasIndices = tempBuf.readBool();
            if (hasIndices) {
                int indicesLen = tempBuf.readInt();
                sub.indices = tempBuf.readBytes(indicesLen);
            }

            sub.count = tempBuf.readInt();
            sub.vertCount = tempBuf.readInt();
            sub.density = tempBuf.readInt();

            // light data
            int lightVerticesLen = tempBuf.readInt();
            sub.lightVertices = tempBuf.readBytes(lightVerticesLen);

            int lightRefsLen = tempBuf.readInt();
            sub.lightRefs = new ushort[lightRefsLen];
            for (int j = 0; j < lightRefsLen; j++) {
                sub.lightRefs[j] = tempBuf.readUShort();
            }

            bool hasLightIndices = tempBuf.readBool();
            if (hasLightIndices) {
                int lightIndicesLen = tempBuf.readInt();
                sub.lightIndices = tempBuf.readBytes(lightIndicesLen);
            }

            sub.lightCount = tempBuf.readInt();
            sub.lightVertCount = tempBuf.readInt();
            sub.lightDensity = tempBuf.readInt();

            // metadata
            sub.blockCount = tempBuf.readInt();
            sub.translucentCount = tempBuf.readInt();
            sub.fullBlockCount = tempBuf.readInt();
            sub.randomTickCount = tempBuf.readInt();
            sub.renderTickCount = tempBuf.readInt();

            subChunks[i] = sub;
        }
    }

    /** subchunkdata - see PaletteBlockData */
    public class SubChunkData {
        public byte y; // which subchunk (0-7)

        public uint[] vertices;
        public byte[]? indices;
        public ushort[] blockRefs;
        public int count;
        public int vertCount;
        public int vertCapacity;
        public int density;

        // light vertices
        public byte[] lightVertices;
        public byte[]? lightIndices;
        public ushort[] lightRefs;
        public int lightCount;
        public int lightVertCount;
        public int lightVertCapacity;
        public int lightDensity;

        public int blockCount;
        public int translucentCount;
        public int fullBlockCount;
        public int randomTickCount;
        public int renderTickCount;
    }
}