namespace BlockGame.util.path;

public class PathNode : IEquatable<PathNode> {
    public int x;
    public int y;
    public int z;
    public float f;
    public float g;
    public float h;

    public PathNode? prev;
    public PathNode? next;

    public PathNode(int x, int y, int z) {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public void reset(int x, int y, int z) {
        this.x = x;
        this.y = y;
        this.z = z;
        f = g = h = 0;
        prev = next = null;
    }

    public float cost() {
        return g + h;
    }

    public float dist(PathNode other) {
        return float.Sqrt((x - other.x) * (x - other.x) +
                          (y - other.y) * (y - other.y) +
                          (z - other.z) * (z - other.z));
    }

    public float distsq(PathNode other) {
        return (x - other.x) * (x - other.x) +
               (y - other.y) * (y - other.y) +
               (z - other.z) * (z - other.z);
    }

    public bool Equals(PathNode? other) {
        if (other == null) {
            return false;
        }
        return x == other.x && y == other.y && z == other.z;
    }

    public override bool Equals(object? obj) {
        return Equals(obj as PathNode);
    }

    public override int GetHashCode() {
        return x * 73856093 ^ y * 19349663 ^ z * 83492791;
    }

    public override string ToString() {
        return $"PathNode({x}, {y}, {z})";
    }

}