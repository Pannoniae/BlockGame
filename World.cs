using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace BlockGame;

public class World {
    public const int WORLDSIZE = 12;
    public const int WORLDHEIGHT = Chunk.CHUNKHEIGHT * ChunkSection.CHUNKSIZE;

    public Chunk[,] chunks;
    public List<ChunkSection> sortedTransparentChunks = [];
    public Shader shader;
    public Shader dummyShader;

    public int uProjection;

    //public int uColor;
    public int blockTexture;
    public int uMVP;

    public GL GL;

    public Player player;

    public FastNoiseLite noise;
    public FastNoiseLite treenoise;

    public Shader outline;
    private uint outlineVao;
    private uint outlineCount;
    private int outline_uModel;
    private int outline_uView;
    private int outline_uProjection;

    public double worldTime;

    public Random random;

    /// <summary>
    /// Random ticks per chunk section per tick. Normally 3 but let's test with 50
    /// </summary>
    public const int numTicks = 50;

    public World(bool loaded = false) {
        GL = Game.instance.GL;
        player = new Player(this, 6 * ChunkSection.CHUNKSIZE, 20, 6 * ChunkSection.CHUNKSIZE);
        shader = new Shader(GL, "shaders/shader.vert", "shaders/shader.frag");
        dummyShader = new Shader(GL, "shaders/dummyShader.vert");
        blockTexture = shader.getUniformLocation("blockTexture");
        uMVP = shader.getUniformLocation("uMVP");
        random = new Random();
        noise = new FastNoiseLite(Environment.TickCount);
        treenoise = new FastNoiseLite(random.Next(Environment.TickCount));
        worldTime = 0;

        chunks = new Chunk[WORLDSIZE, WORLDSIZE];
        outline = new Shader(Game.instance.GL, "shaders/outline.vert", "shaders/outline.frag");
        for (int x = 0; x < WORLDSIZE; x++) {
            for (int z = 0; z < WORLDSIZE; z++) {
                chunks[x, z] = new Chunk(this, shader, x, z);
            }
        }

        if (!loaded) {
            // create terrain
            genTerrainNoise();
            // separate loop so all data is there
            meshChunks();
        }

        meshBlockOutline();
    }

    public void meshChunks() {
        for (int x = 0; x < WORLDSIZE; x++) {
            for (int z = 0; z < WORLDSIZE; z++) {
                chunks[x, z].meshChunk();
            }
        }
    }

    private void genTerrainSine() {
        for (int x = 0; x < WORLDSIZE * ChunkSection.CHUNKSIZE; x++) {
            for (int z = 0; z < WORLDSIZE * ChunkSection.CHUNKSIZE; z++) {
                for (int y = 0; y < 3; y++) {
                    setBlock(x, y, z, 2, false);
                }
            }
        }

        var sinMin = 2;
        for (int x = 0; x < WORLDSIZE * ChunkSection.CHUNKSIZE; x++) {
            for (int z = 0; z < WORLDSIZE * ChunkSection.CHUNKSIZE; z++) {
                var sin = Math.Sin(x / 3f) * 2 + sinMin + 1 + Math.Cos(z / 3f) * 2 + sinMin + 1;
                for (int y = sinMin; y < sin; y++) {
                    setBlock(x, y, z, 5, false);
                }

                if (sin < 4) {
                    for (int y = 3; y < 4; y++) {
                        setBlock(x, y, z, Blocks.WATER.id, false);
                    }
                }
            }
        }
    }

    private void genTerrainNoise() {
        noise.SetFrequency(0.03f);
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalLacunarity(2f);
        noise.SetFractalGain(0.5f);
        treenoise.SetFrequency(1f);
        for (int x = 0; x < WORLDSIZE * ChunkSection.CHUNKSIZE; x++) {
            for (int z = 0; z < WORLDSIZE * ChunkSection.CHUNKSIZE; z++) {
                // -1 to 1
                // transform to the range 5 - 10
                var height = noise.GetNoise(x, z) * 2.5 + 7.5;
                for (int y = 0; y < height; y++) {
                    setBlock(x, y, z, Blocks.DIRT.id, false);
                }
                setBlock(x, (int)(height + 1), z, Blocks.GRASS.id, false);

                // TREES
                if (MathF.Abs(treenoise.GetNoise(x, z) - 1) < 0.01f) {
                    placeTree(x, (int)(height + 1), z);
                }
            }
        }
    }

    private void placeTree(int x, int y, int z) {
        // tree
        for (int i = 0; i < 7; i++) {
            setBlock(x, y + i, z, Blocks.LOG.id, false);
        }
        // leaves, thick
        for (int x1 = -2; x1 <= 2; x1++) {
            for (int z1 = -2; z1 <= 2; z1++) {
                // don't overwrite the trunk
                if (x1 == 0 && z1 == 0) {
                    continue;
                }
                for (int y1 = 4; y1 < 6; y1++) {
                    setBlock(x + x1, y + y1, z + z1, Blocks.LEAVES.id, false);
                }
            }
        }
        // leaves, thin on top
        for (int x2 = -1; x2 <= 1; x2++) {
            for (int z2 = -1; z2 <= 1; z2++) {
                for (int y2 = 6; y2 <= 7; y2++) {
                    // don't overwrite the trunk
                    if (x2 == 0 && z2 == 0 && y == 6) {
                        continue;
                    }
                    setBlock(x + x2, y + y2, z + z2, Blocks.LEAVES.id, false);
                }
            }
        }
    }

    public void update(double dt) {
        worldTime += dt;
        /*if (Vector3D.DistanceSquared(player.position, player.lastSort) > 64) {
            sortedTransparentChunks.Sort(new ChunkComparer(player.camera));
            player.lastSort = player.position;
        }*/

        // random block updates!
        foreach (var chunk in chunks) {
            foreach (var chunksection in chunk.chunks) {
                for (int i = 0; i < numTicks; i++) {
                    var x = random.Next(16);
                    var y = random.Next(16);
                    var z = random.Next(16);
                    chunksection.tick(x, y, z);
                }
            }
        }
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

    /// <summary>
    /// getBlock but returns -1 if OOB
    /// </summary>
    public int getBlockUnsafe(int x, int y, int z) {
        if (!inWorld(x, y, z)) {
            return -1;
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
        if (aabb == null) {
            return null;
        }
        return new AABB(new Vector3D<double>(x + aabb.minX, y + aabb.minY, z + aabb.minZ),
            new Vector3D<double>(x + aabb.maxX, y + aabb.maxY, z + aabb.maxZ));
    }

    public void setBlock(int x, int y, int z, ushort block, bool remesh = true) {
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

    public bool inWorld(int x, int y, int z) {
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

    public void render(double interp) {
        var tex = Game.instance.blockTexture;
        tex.bind();
        var viewProj = player.camera.getViewMatrix(interp) * player.camera.getProjectionMatrix();
        // OPAQUE PASS
        shader.use();
        shader.setUniform(uMVP, viewProj);
        shader.setUniform(blockTexture, 0);
        foreach (var chunk in chunks) {
            chunk.drawOpaque(player.camera);
        }
        // TRANSLUCENT DEPTH PRE-PASS
        dummyShader.use();
        dummyShader.setUniform(uMVP, viewProj);
        GL.ColorMask(false, false, false, false);
        foreach (var chunk in chunks) {
            chunk.drawTransparent(player.camera);
        }
        // TRANSLUCENT PASS
        shader.use();
        GL.ColorMask(true, true, true, true);
        GL.DepthMask(false);
        GL.Disable(EnableCap.CullFace);
        GL.DepthFunc(DepthFunction.Lequal);
        foreach (var chunk in chunks) {
            chunk.drawTransparent(player.camera);
        }
        GL.DepthMask(true);
        GL.DepthFunc(DepthFunction.Less);
        GL.Enable(EnableCap.CullFace);

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
            if (isBlock(blockPos.X, blockPos.Y, blockPos.Z) && Blocks.get(getBlock(blockPos)).selection) {
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