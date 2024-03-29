using Silk.NET.Maths;

namespace BlockGame;

public class ChunkComparer : IComparer<ChunkSection> {
    public Camera camera;

    public ChunkComparer(Camera camera) {
        this.camera = camera;
    }
    public int Compare(ChunkSection x, ChunkSection y) {
        return (int)(Vector3D.Distance(x.worldPos.As<float>(), camera.position.toVec3F()) -
               Vector3D.Distance(y.worldPos.As<float>(), camera.position.toVec3F()));
    }
}