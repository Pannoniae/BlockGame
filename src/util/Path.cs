using BlockGame.util;
using Molten.DoublePrecision;

namespace Core.util;

public class Path {
    public readonly XList<PathNode> nodes;
    public int current;

    public Path() {
        nodes = [];
        current = 0;
    }

    public Path(XList<PathNode> nodes) {
        this.nodes = nodes;
        current = 0;
    }

    public bool isFinished() {
        return current >= nodes.Count;
    }

    public bool isEmpty() {
        return nodes.Count == 0;
    }

    public PathNode? getCurrent() {
        if (isFinished()) return null;
        return nodes[current];
    }

    public PathNode? getNext() {
        if (current + 1 >= nodes.Count) return null;
        return nodes[current + 1];
    }

    public void advance() {
        if (!isFinished()) {
            current++;
        }
    }

    public Vector3D? getTarget() {
        if (isEmpty()) return null;
        var node = nodes[^1];
        return new Vector3D(node.x + 0.5, node.y, node.z + 0.5);
    }

    public Vector3D? getCurrentTarget() {
        var node = getCurrent();
        if (node == null) return null;
        return new Vector3D(node.x + 0.5, node.y, node.z + 0.5);
    }

    public void reset() {
        current = 0;
    }
}