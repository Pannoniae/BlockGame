using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace BlockGame;

public class World {
    private const int WORLDSIZE = 6;
    private const int WORLDHEIGHT = 3;

    private const float RAYCASTSTEP = 0.2f;

    public Chunk[,,] chunks;

    public Shader outline;

    public World() {
        chunks = new Chunk[WORLDSIZE, WORLDHEIGHT, WORLDSIZE];
        outline = new Shader(Game.instance.GL, "outline.vert", "outline.frag");
        for (int x = 0; x < WORLDSIZE; x++) {
            for (int y = 0; y < WORLDHEIGHT; y++) {
                for (int z = 0; z < WORLDSIZE; z++) {
                    chunks[x, y, z] = new Chunk(this, x, y, z);
                }

            }
        }

        // separate loop so all data is there
        for (int x = 0; x < WORLDSIZE; x++) {
            for (int y = 0; y < WORLDHEIGHT; y++) {
                for (int z = 0; z < WORLDSIZE; z++) {
                    chunks[x, y, z].meshChunk();
                }
            }
        }
    }

    public bool isBlock(int x, int y, int z) {
        if (!inWorld(x, y, z)) {
            return false;
        }

        var blockPos = getBlockPos(x, y, z);
        var chunk = getChunk(x, y, z);
        return chunk.block[blockPos.X, blockPos.Y, blockPos.Z] != 0;
    }

    public int getBlock(int x, int y, int z) {
        if (!inWorld(x, y, z)) {
            return 0;
        }

        var blockPos = getBlockPos(x, y, z);
        var chunk = getChunk(x, y, z);
        return chunk.block[blockPos.X, blockPos.Y, blockPos.Z];
    }

    public void setBlock(int x, int y, int z, int block, bool remesh = true) {
        if (!inWorld(x, y, z)) {
            return;
        }

        var blockPos = getBlockPos(x, y, z);
        var chunk = getChunk(x, y, z);
        chunk.block[blockPos.X, blockPos.Y, blockPos.Z] = block;

        if (remesh) {
            chunk.meshChunk();
        }
    }

    private bool inWorld(int x, int y, int z) {
        var chunkpos = getChunkPos(x, y, z);
        return chunkpos.X is >= 0 and < WORLDSIZE &&
               chunkpos.Y is >= 0 and < WORLDHEIGHT &&
               chunkpos.Z is >= 0 and < WORLDSIZE;
    }

    private Vector3D<int> getChunkPos(Vector3D<int> pos) {
        return new Vector3D<int>(
            (int)MathF.Floor(pos.X / (float)Chunk.CHUNKSIZE),
            (int)MathF.Floor(pos.Y / (float)Chunk.CHUNKSIZE),
            (int)MathF.Floor(pos.Z / (float)Chunk.CHUNKSIZE));
    }

    private Vector3D<int> getChunkPos(int x, int y, int z) {
        return new Vector3D<int>(
            (int)MathF.Floor(x / (float)Chunk.CHUNKSIZE),
            (int)MathF.Floor(y / (float)Chunk.CHUNKSIZE),
            (int)MathF.Floor(z / (float)Chunk.CHUNKSIZE));
    }
    
    private Vector3D<int> getBlockPos(int x, int y, int z) {
        return new Vector3D<int>(
            x % Chunk.CHUNKSIZE,
            y % Chunk.CHUNKSIZE,
            z % Chunk.CHUNKSIZE);
    }

    private Vector3D<int> getBlockPos(Vector3D<int> pos) {
        return new Vector3D<int>(
            pos.X % Chunk.CHUNKSIZE,
            pos.Y % Chunk.CHUNKSIZE,
            pos.Z % Chunk.CHUNKSIZE);
    }

    private Chunk getChunk(int x, int y, int z) {
        var pos = getChunkPos(x, y, z);
        return chunks[pos.X, pos.Y, pos.Z];
    }

    private Chunk getChunk(Vector3D<int> position) {
        var pos = getChunkPos(position);
        return chunks[pos.X, pos.Y, pos.Z];
    }

    public void mesh() {
        foreach (var chunk in chunks) {
            chunk.meshChunk();
        }
    }

    public void draw() {
        foreach (var chunk in chunks) {
            chunk.drawChunk();
        }
    }

    public Vector3D<int> toWorldPos(int chunkX, int chunkY, int chunkZ, int x, int y, int z) {
        return new Vector3D<int>(chunkX * Chunk.CHUNKSIZE + x,
            chunkY * Chunk.CHUNKSIZE + y,
            chunkZ * Chunk.CHUNKSIZE + z);
    }

    public Vector3D<int>? getTargetedBlock(out Vector3D<int>? previous) {
        // raycast
        var cameraPos = Game.instance.camera.position;
        var cameraForward = Game.instance.camera.forward;
        var currentPos = new Vector3(cameraPos.X, cameraPos.Y, cameraPos.Z);

        // don't round!!
        //var blockPos = toBlockPos(currentPos);

        previous = toBlockPos(currentPos);
        for (int i = 0; i < 200; i++) {
            currentPos += cameraForward * RAYCASTSTEP;
            var blockPos = toBlockPos(currentPos);
            if (isBlock(blockPos.X, blockPos.Y, blockPos.Z)) {
                //Console.Out.WriteLine("getblock:" + getBlock(blockPos.X, blockPos.Y, blockPos.Z));
                return blockPos;
            }
            previous = blockPos;
        }

        return null;
    }

    private Vector3D<int> toBlockPos(Vector3 currentPos) {
        return new Vector3D<int>((int)MathF.Round(currentPos.X), (int)MathF.Round(currentPos.Y), (int)MathF.Round(currentPos.Z));
    }

    public void drawBlockOutline() {
        unsafe {
            var GL = Game.instance.GL;
            const float OFFSET = 0.001f;
            var block = Game.instance.targetedPos!.Value;
            var minX = block.X - 0.5f - OFFSET;
            var minY = block.Y - 0.5f - OFFSET;
            var minZ = block.Z - 0.5f - OFFSET;
            var maxX = block.X + 0.5f + OFFSET;
            var maxY = block.Y + 0.5f + OFFSET;
            var maxZ = block.Z + 0.5f + OFFSET;

            var vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);


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
                maxX, maxY, maxZ,

            ];
            var vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            fixed (float* data = vertices) {
                GL.BufferData(BufferTargetARB.ArrayBuffer, (uint)(vertices.Length * sizeof(float)), data,
                    BufferUsageARB.StreamDraw);
            }
            var count = (uint)vertices.Length;
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
            GL.EnableVertexAttribArray(0);
            outline.use();
            outline.setUniform("uModel", Matrix4x4.Identity);
            outline.setUniform("uView", Game.instance.camera.getViewMatrix());
            outline.setUniform("uProjection", Game.instance.camera.getProjectionMatrix());
            GL.DrawArrays(PrimitiveType.Lines, 0, count);
        }
    }
}