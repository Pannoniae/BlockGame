using System.IO.Compression;
using System.Numerics;
using SharpNBT;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace BlockGame;

public class World {
    public const int WORLDSIZE = 6;
    public const int WORLDHEIGHT = Chunk.CHUNKHEIGHT * ChunkSection.CHUNKSIZE;

    public Chunk[,] chunks;
    public Shader shader;
    public int uView;

    public int uProjection;

    //public int uColor;
    public int blockTexture;

    public GL GL;

    public Player player;

    public Shader outline;
    private uint outlineVao;
    private uint outlineCount;
    private int outline_uModel;
    private int outline_uView;
    private int outline_uProjection;

    public World() {
        GL = Game.instance.GL;
        player = new Player(this, 0, 20, 0);
        shader = new Shader(GL, "shader.vert", "shader.frag");
        uView = shader.getUniformLocation("uView");
        uProjection = shader.getUniformLocation("uProjection");
        //uColor = shader.getUniformLocation("uColor");
        blockTexture = shader.getUniformLocation("blockTexture");

        chunks = new Chunk[WORLDSIZE, WORLDSIZE];
        outline = new Shader(Game.instance.GL, "outline.vert", "outline.frag");
        for (int x = 0; x < WORLDSIZE; x++) {
            for (int z = 0; z < WORLDSIZE; z++) {
                chunks[x, z] = new Chunk(this, shader, x, z);
            }
        }

        // create terrain
        genTerrain();

        // separate loop so all data is there
        meshChunks();

        Console.Out.WriteLine(chunks.Length);

        meshBlockOutline();
    }

    private void meshChunks() {
        for (int x = 0; x < WORLDSIZE; x++) {
            for (int z = 0; z < WORLDSIZE; z++) {
                chunks[x, z].meshChunk();
            }
        }
    }

    private void genTerrain() {
        for (int x = 0; x < WORLDSIZE * ChunkSection.CHUNKSIZE; x++) {
            for (int z = 0; z < WORLDSIZE * ChunkSection.CHUNKSIZE; z++) {
                for (int y = 0; y < 2; y++) {
                    setBlock(x, y, z, 1, false);
                }
            }
        }

        for (int x = 0; x < WORLDSIZE * ChunkSection.CHUNKSIZE; x++) {
            for (int z = 0; z < WORLDSIZE * ChunkSection.CHUNKSIZE; z++) {
                for (int y = 2; y < 3; y++) {
                    setBlock(x, y, z, 3, false);
                }
            }
        }

        for (int x = 0; x < WORLDSIZE * ChunkSection.CHUNKSIZE; x++) {
            for (int z = 0; z < WORLDSIZE * ChunkSection.CHUNKSIZE; z++) {
                for (int y = 3; y < 4; y++) {
                    setBlock(x, y, z, 4, false);
                }
            }
        }

        for (int x = 0; x < WORLDSIZE * ChunkSection.CHUNKSIZE; x++) {
            for (int z = 0; z < WORLDSIZE * ChunkSection.CHUNKSIZE; z++) {
                for (int y = 15; y < 19; y++) {
                    setBlock(x, y, z, 2, false);
                }
            }
        }

        /*var h = 19;
        for (int x = 0; x < WORLDSIZE * ChunkSection.CHUNKSIZE; x++) {
            for (int z = 0; z < WORLDSIZE * ChunkSection.CHUNKSIZE; z++) {
                h = 19 + x + z;
                setBlock(x, h, z, 4, false);
            }
        }*/
    }

    public void save(string filename) {

        if (!Directory.Exists("world")) {
            Directory.CreateDirectory("world");
        }

        var tag = new TagBuilder("world");
        tag.AddDouble("posX", player.position.X);
        tag.AddDouble("posY", player.position.Y);
        tag.AddDouble("posZ", player.position.Z);
        tag.BeginList(TagType.Compound, "chunks");
        foreach (var chunk in chunks) {
            tag.BeginCompound("chunk");
            tag.AddInt("posX", chunk.x);
            tag.AddInt("posZ", chunk.z);
            // blocks
            tag.BeginList(TagType.List, "blocks");
            for (int x = 0; x < ChunkSection.CHUNKSIZE; x++) {
                for (int z = 0; z < ChunkSection.CHUNKSIZE; z++) {
                    for (int y = 0; y < ChunkSection.CHUNKSIZE * Chunk.CHUNKHEIGHT; y++) {
                        tag.BeginList(TagType.Int);
                        tag.AddInt(x);
                        tag.AddInt(y);
                        tag.AddInt(z);
                        tag.AddInt(chunk.block[x, y, z]);
                        tag.EndList();
                    }
                }
            }
            tag.EndList();
            tag.EndCompound();
        }
        tag.EndList();
        var fileTag = tag.Create();
        NbtFile.Write($"world/{filename}.nbt", fileTag, FormatOptions.LittleEndian, CompressionType.ZLib, CompressionLevel.Optimal);
    }

    public void load(string filename) {
        CompoundTag tag = NbtFile.Read($"world/{filename}.nbt", FormatOptions.LittleEndian, CompressionType.ZLib);
        player.position.X = tag.Get<DoubleTag>("posX");
        player.position.Y = tag.Get<DoubleTag>("posY");
        player.position.Z = tag.Get<DoubleTag>("posZ");
        var chunkTags = tag.Get<ListTag>("chunks");
        foreach (var chunkTag in chunkTags) {
            var chunk = (CompoundTag)chunkTag;
            var chunkX = chunk.Get<IntTag>("posX").Value;
            var chunkZ = chunk.Get<IntTag>("posZ").Value;
            chunks[chunkX, chunkZ] = new Chunk(this, shader, chunkX, chunkZ);
            var blocks = chunk.Get<ListTag>("blocks");
            foreach (var block in blocks) {
                var fields = (ListTag)block;
                var x = ((IntTag)fields[0]).Value;
                var y = ((IntTag)fields[1]).Value;
                var z = ((IntTag)fields[2]).Value;
                var id = ((IntTag)fields[3]).Value;
                chunks[chunkX, chunkZ].block[x, y, z] = id;
            }
        }

        meshChunks();
    }

    public Vector3D<int> getWorldSize() {
        var c = ChunkSection.CHUNKSIZE;
        return new Vector3D<int>(c * WORLDSIZE, c * WORLDHEIGHT, c * WORLDSIZE);
    }

    public bool isBlock(int x, int y, int z) {
        if (!inWorld(x, y, z)) {
            return false;
        }

        var blockPos = getPosInChunk(x, y, z);
        var chunk = getChunk(x, z);
        return chunk.block[blockPos.X, y, blockPos.Z] != 0;
    }

    public int getBlock(int x, int y, int z) {
        if (!inWorld(x, y, z)) {
            return 0;
        }

        var blockPos = getPosInChunk(x, y, z);
        var chunk = getChunk(x, z);
        return chunk.block[blockPos.X, y, blockPos.Z];
    }

    public int getBlock(Vector3D<int> pos) {
        return getBlock(pos.X, pos.Y, pos.Z);
    }

    public AABB? getAABB(int x, int y, int z, int id) {
        if (id == 0) {
            return null;
        }

        var block = Blocks.get(id);
        var aabb = block.aabb;
        return new AABB(new Vector3D<double>(x + aabb.minX, y + aabb.minY, z + aabb.minZ),
            new Vector3D<double>(x + aabb.maxX, y + aabb.maxY, z + aabb.maxZ));
    }

    public void setBlock(int x, int y, int z, int block, bool remesh = true) {
        if (!inWorld(x, y, z)) {
            return;
        }

        var blockPos = getPosInChunk(x, y, z);
        var chunk = getChunk(x, z);
        chunk.block[blockPos.X, blockPos.Y, blockPos.Z] = block;

        if (remesh) {
            chunk.meshChunk();

            var chunkPos = getChunkSectionPos(new Vector3D<int>(x, y, z));

            foreach (var dir in Direction.directions) {
                var neighbourSection = getChunkSectionPos(new Vector3D<int>(x, y, z) + dir);
                if (isChunkSectionInWorld(neighbourSection) && neighbourSection != chunkPos) {
                    getChunkByChunkPos(new Vector2D<int>(neighbourSection.X, neighbourSection.Z)).chunks[neighbourSection.Y].meshChunk();
                }
            }
        }
    }

    private bool inWorld(int x, int y, int z) {
        var chunkpos = getChunkPos(x, z);
        return chunkpos.X is >= 0 and < WORLDSIZE &&
               y is >= 0 and < WORLDHEIGHT &&
               chunkpos.Y is >= 0 and < WORLDSIZE;
    }

    private Vector3D<int> getChunkSectionPos(Vector3D<int> pos) {
        return new Vector3D<int>(
            (int)MathF.Floor(pos.X / (float)ChunkSection.CHUNKSIZE),
            (int)MathF.Floor(pos.Y / (float)ChunkSection.CHUNKSIZE),
            (int)MathF.Floor(pos.Z / (float)ChunkSection.CHUNKSIZE));
    }

    private Vector2D<int> getChunkPos(Vector2D<int> pos) {
        return new Vector2D<int>(
            (int)MathF.Floor(pos.X / (float)ChunkSection.CHUNKSIZE),
            (int)MathF.Floor(pos.Y / (float)ChunkSection.CHUNKSIZE));
    }

    private Vector2D<int> getChunkPos(int x, int z) {
        return new Vector2D<int>(
            (int)MathF.Floor(x / (float)ChunkSection.CHUNKSIZE),
            (int)MathF.Floor(z / (float)ChunkSection.CHUNKSIZE));
    }

    private Vector3D<int> getPosInChunk(int x, int y, int z) {
        return new Vector3D<int>(
            x % ChunkSection.CHUNKSIZE,
            y,
            z % ChunkSection.CHUNKSIZE);
    }

    private Vector3D<int> getPosInChunk(Vector3D<int> pos) {
        return new Vector3D<int>(
            pos.X % ChunkSection.CHUNKSIZE,
            pos.Y,
            pos.Z % ChunkSection.CHUNKSIZE);
    }

    private bool isChunkInWorld(int x, int z) {
        return x >= 0 && x < WORLDSIZE && z >= 0 && z < WORLDSIZE;
    }

    private bool isChunkInWorld(Vector2D<int> pos) {
        return pos.X >= 0 && pos.X < WORLDSIZE && pos.Y >= 0 && pos.Y < WORLDSIZE;
    }

    private bool isChunkSectionInWorld(Vector3D<int> pos) {
        return pos.X >= 0 && pos.X < WORLDSIZE && pos.Y >= 0 && pos.Y < Chunk.CHUNKHEIGHT && pos.Z >= 0 && pos.Z < WORLDSIZE;
    }

    private Chunk getChunk(int x, int z) {
        var pos = getChunkPos(x, z);
        return chunks[pos.X, pos.Y];
    }

    private Chunk getChunk(Vector2D<int> position) {
        var pos = getChunkPos(position);
        return chunks[pos.X, pos.Y];
    }

    private Chunk getChunkByChunkPos(Vector2D<int> position) {
        return chunks[position.X, position.Y];
    }

    public void mesh() {
        foreach (var chunk in chunks) {
            chunk.meshChunk();
        }
    }

    public void draw(double interp) {
        var tex = Game.instance.blockTexture;
        //var fChunk = chunks[0, 0, 0];
        tex.bind();
        shader.use();
        shader.setUniform(uView, player.camera.getViewMatrix(interp));
        shader.setUniform(uProjection, player.camera.getProjectionMatrix());
        shader.setUniform(blockTexture, 0);
        foreach (var chunk in chunks) {
            chunk.drawChunk();
        }
    }

    public Vector3D<int> toWorldPos(int chunkX, int chunkY, int chunkZ, int x, int y, int z) {
        return new Vector3D<int>(chunkX * ChunkSection.CHUNKSIZE + x,
            chunkY * ChunkSection.CHUNKSIZE + y,
            chunkZ * ChunkSection.CHUNKSIZE + z);
    }

    /// <summary>
    /// This piece of shit raycast breaks when the player goes outside the world. Solution? Don't go outside the world (will be prevented in the future with barriers)
    /// </summary>
    /// <param name="previous">The previous block (used for placing)</param>
    /// <returns></returns>
    public Vector3D<int>? naiveRaycastBlock(out Vector3D<int>? previous) {
        // raycast
        var cameraPos = player.camera.position;
        var forward = player.camera.forward;
        var cameraForward = new Vector3D<double>(forward.X, forward.Y, forward.Z);
        var currentPos = new Vector3D<double>(cameraPos.X, cameraPos.Y, cameraPos.Z);

        // don't round!!
        //var blockPos = toBlockPos(currentPos);

        previous = toBlockPos(currentPos);
        for (int i = 0; i < 1 / Constants.RAYCASTSTEP * Constants.RAYCASTDIST; i++) {
            currentPos += cameraForward * Constants.RAYCASTSTEP;
            var blockPos = toBlockPos(currentPos);
            if (isBlock(blockPos.X, blockPos.Y, blockPos.Z)) {
                //Console.Out.WriteLine("getblock:" + getBlock(blockPos.X, blockPos.Y, blockPos.Z));
                return blockPos;
            }

            previous = blockPos;
        }

        previous = null;
        return null;
    }

    public Vector3D<int> toBlockPos(Vector3D<double> currentPos) {
        return new Vector3D<int>((int)Math.Floor(currentPos.X), (int)Math.Floor(currentPos.Y),
            (int)Math.Floor(currentPos.Z));
    }

    public void meshBlockOutline() {
        unsafe {
            var GL = Game.instance.GL;
            const float OFFSET = 0.005f;
            var minX = 0f - OFFSET;
            var minY = 0f - OFFSET;
            var minZ = 0f - OFFSET;
            var maxX = 1f + OFFSET;
            var maxY = 1f + OFFSET;
            var maxZ = 1f + OFFSET;

            outlineVao = GL.GenVertexArray();
            GL.BindVertexArray(outlineVao);


            float[] vertices = [
                // bottom
                minX, minY, minZ,
                minX, minY, maxZ,
                minX, minY, maxZ,
                maxX, minY, maxZ,
                maxX, minY, maxZ,
                maxX, minY, minZ,
                maxX, minY, minZ,
                minX, minY, minZ,

                // top
                minX, maxY, minZ,
                minX, maxY, maxZ,
                minX, maxY, maxZ,
                maxX, maxY, maxZ,
                maxX, maxY, maxZ,
                maxX, maxY, minZ,
                maxX, maxY, minZ,
                minX, maxY, minZ,

                // sides
                minX, minY, minZ,
                minX, maxY, minZ,
                maxX, minY, minZ,
                maxX, maxY, minZ,
                minX, minY, maxZ,
                minX, maxY, maxZ,
                maxX, minY, maxZ,
                maxX, maxY, maxZ
            ];
            var vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            fixed (float* data = vertices) {
                GL.BufferData(BufferTargetARB.ArrayBuffer, (uint)(vertices.Length * sizeof(float)), data,
                    BufferUsageARB.StreamDraw);
            }

            outlineCount = (uint)vertices.Length / 3;
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
            GL.EnableVertexAttribArray(0);

            outline_uModel = outline.getUniformLocation("uModel");
            outline_uView = outline.getUniformLocation("uView");
            outline_uProjection = outline.getUniformLocation("uProjection");
        }
    }

    public void drawBlockOutline(double interp) {
        var GL = Game.instance.GL;
        var block = Game.instance.targetedPos!.Value;
        GL.BindVertexArray(outlineVao);
        outline.use();
        outline.setUniform(outline_uModel, Matrix4x4.CreateTranslation(block.X, block.Y, block.Z));
        outline.setUniform(outline_uView, player.camera.getViewMatrix(interp));
        outline.setUniform(outline_uProjection, player.camera.getProjectionMatrix());
        GL.DrawArrays(PrimitiveType.Lines, 0, outlineCount);
    }

    public List<Vector3D<int>> getBlocksInBox(Vector3D<int> min, Vector3D<int> max) {
        var l = new List<Vector3D<int>>();
        for (int x = min.X; x <= max.X; x++) {
            for (int y = min.Y; y <= max.Y; y++) {
                for (int z = min.Z; z <= max.Z; z++) {
                    l.Add(new Vector3D<int>(x, y, z));
                }
            }
        }

        return l;
    }
}