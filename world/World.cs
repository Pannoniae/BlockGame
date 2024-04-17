using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace BlockGame;

public class World {
    public const int WORLDSIZE = 12;
    public const int WORLDHEIGHT = Chunk.CHUNKHEIGHT * Chunk.CHUNKSIZE;

    public Dictionary<ChunkCoord, Chunk> chunks;
    public List<ChunkSection> sortedTransparentChunks = [];

    public WorldRenderer renderer;

    public Player player;

    public FastNoiseLite noise;
    public FastNoiseLite treenoise;

    public double worldTime;

    public Random random;

    /// <summary>
    /// Random ticks per chunk section per tick. Normally 3 but let's test with 50
    /// </summary>
    public const int numTicks = 3;

    public World() {
        renderer = new WorldRenderer(this);
        player = new Player(this, 6 * Chunk.CHUNKSIZE, 20, 6 * Chunk.CHUNKSIZE);

        random = new Random();
        noise = new FastNoiseLite(Environment.TickCount);
        treenoise = new FastNoiseLite(random.Next(Environment.TickCount));
        worldTime = 0;

        chunks = new Dictionary<ChunkCoord, Chunk>();
        for (int x = 0; x < WORLDSIZE; x++) {
            for (int z = 0; z < WORLDSIZE; z++) {
                chunks[new ChunkCoord(x, z)] = new Chunk(this, x, z);
            }
        }

        renderer.meshBlockOutline();
    }

    public void generate() {
        noise.SetFrequency(0.03f);
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalLacunarity(2f);
        noise.SetFractalGain(0.5f);
        treenoise.SetFrequency(1f);
        // create terrain
        genTerrainNoise();
        // separate loop so all data is there
        renderer.meshChunks();
    }

    private void genTerrainSine() {
        for (int x = 0; x < WORLDSIZE * Chunk.CHUNKSIZE; x++) {
            for (int z = 0; z < WORLDSIZE * Chunk.CHUNKSIZE; z++) {
                for (int y = 0; y < 3; y++) {
                    setBlock(x, y, z, 2, false);
                }
            }
        }

        var sinMin = 2;
        for (int x = 0; x < WORLDSIZE * Chunk.CHUNKSIZE; x++) {
            for (int z = 0; z < WORLDSIZE * Chunk.CHUNKSIZE; z++) {
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
        // generate terrain for all loaded chunks
        foreach (var chunk in chunks.Values) {
            chunk.generator.generate();
            chunk.generator.populate();
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
            foreach (var chunksection in chunk.Value.chunks) {
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
        var c = Chunk.CHUNKSIZE;
        return new Vector3D<int>(c * WORLDSIZE, c * WORLDHEIGHT, c * WORLDSIZE);
    }

    public bool isBlock(int x, int y, int z) {
        if (!inWorld(x, y, z)) {
            return false;
        }

        var blockPos = getPosInChunk(x, y, z);
        var chunk = getChunk(x, z);
        return chunk.blocks[blockPos.X, y, blockPos.Z] != 0;
    }

    public ushort getBlock(int x, int y, int z) {
        if (!inWorld(x, y, z)) {
            return 0;
        }

        var blockPos = getPosInChunk(x, y, z);
        var chunk = getChunk(x, z);
        return chunk.blocks[blockPos.X, y, blockPos.Z];
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
        return chunk.blocks[blockPos.X, y, blockPos.Z];
    }

    public ushort getBlock(Vector3D<int> pos) {
        return getBlock(pos.X, pos.Y, pos.Z);
    }

    public AABB? getAABB(int x, int y, int z, ushort id) {
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
        chunk.setBlock(blockPos.X, blockPos.Y, blockPos.Z, block, remesh);
    }

    public bool inWorld(int x, int y, int z) {
        var chunkpos = getChunkPos(x, z);
        return chunkpos.x is >= 0 and < WORLDSIZE &&
               y is >= 0 and < WORLDHEIGHT &&
               chunkpos.z is >= 0 and < WORLDSIZE;
    }

    public ChunkSectionCoord getChunkSectionPos(Vector3D<int> pos) {
        return new ChunkSectionCoord(
            (int)MathF.Floor(pos.X / (float)Chunk.CHUNKSIZE),
            (int)MathF.Floor(pos.Y / (float)Chunk.CHUNKSIZE),
            (int)MathF.Floor(pos.Z / (float)Chunk.CHUNKSIZE));
    }

    public ChunkCoord getChunkPos(Vector2D<int> pos) {
        return new ChunkCoord(
            (int)MathF.Floor(pos.X / (float)Chunk.CHUNKSIZE),
            (int)MathF.Floor(pos.Y / (float)Chunk.CHUNKSIZE));
    }

    public ChunkCoord getChunkPos(int x, int z) {
        return new ChunkCoord(
            (int)MathF.Floor(x / (float)Chunk.CHUNKSIZE),
            (int)MathF.Floor(z / (float)Chunk.CHUNKSIZE));
    }

    public Vector3D<int> getPosInChunk(int x, int y, int z) {
        return new Vector3D<int>(
            x % Chunk.CHUNKSIZE,
            y,
            z % Chunk.CHUNKSIZE);
    }

    public Vector3D<int> getPosInChunk(Vector3D<int> pos) {
        return new Vector3D<int>(
            pos.X % Chunk.CHUNKSIZE,
            pos.Y,
            pos.Z % Chunk.CHUNKSIZE);
    }

    public bool isChunkInWorld(int x, int z) {
        return x >= 0 && x < WORLDSIZE && z >= 0 && z < WORLDSIZE;
    }

    public bool isChunkInWorld(Vector2D<int> pos) {
        return pos.X >= 0 && pos.X < WORLDSIZE && pos.Y >= 0 && pos.Y < WORLDSIZE;
    }

    public bool isChunkSectionInWorld(ChunkSectionCoord pos) {
        return pos.x >= 0 && pos.x < WORLDSIZE && pos.y >= 0 && pos.y < Chunk.CHUNKHEIGHT && pos.z >= 0 && pos.z < WORLDSIZE;
    }

    public Chunk getChunk(int x, int z) {
        var pos = getChunkPos(x, z);
        return chunks[pos];
    }

    public Chunk getChunk(Vector2D<int> position) {
        var pos = getChunkPos(position);
        return chunks[pos];
    }

    public Chunk getChunkByChunkPos(ChunkCoord position) {
        return chunks[position];
    }

    public void mesh() {
        foreach (var chunk in chunks) {
            chunk.Value.meshChunk();
        }
    }

    /// <summary>
    /// For sections
    /// </summary>
    public Vector3D<int> toWorldPos(int chunkX, int chunkY, int chunkZ, int x, int y, int z) {
        return new Vector3D<int>(chunkX * Chunk.CHUNKSIZE + x,
            chunkY * Chunk.CHUNKSIZE + y,
            chunkZ * Chunk.CHUNKSIZE + z);
    }

    /// <summary>
    /// For chunks
    /// </summary>
    public Vector3D<int> toWorldPos(int chunkX, int chunkZ, int x, int y, int z) {
        return new Vector3D<int>(chunkX * Chunk.CHUNKSIZE + x,
            y,
            chunkZ * Chunk.CHUNKSIZE + z);
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