using BlockGame.util;
using BlockGame.world;
using BlockGame.world.block;
using Molten;

namespace Core.util;

public class Pathfinding {

    public const int MAX_PATH_LENGTH = 32;
    private const int MAX_ITERATIONS = 512;

    public static Path find(Entity e, int x, int y, int z, int max = MAX_PATH_LENGTH) {
        var start = e.position.toBlockPos();
        var goal = new Vector3I(x, y, z);

        return aStar(e, start, goal, max);
    }

    public static Path find(Entity e, Entity target, int max = MAX_PATH_LENGTH) {
        var start = e.position.toBlockPos();
        var goal = target.position.toBlockPos();

        return aStar(e, start, goal, max);
    }

    private static Path aStar(Entity e, Vector3I start, Vector3I goal, int maxLength) {
        var openSet = new List<PathNode>();
        var closedSet = new HashSet<PathNode>();
        var openSetLookup = new XIntMap<PathNode>();

        var startNode = new PathNode(start.X, start.Y, start.Z);
        startNode.g = 0;
        startNode.h = heuristic(start, goal);
        startNode.f = startNode.h;

        openSet.Add(startNode);
        openSetLookup[startNode.GetHashCode()] = startNode;

        int iterations = 0;

        while (openSet.Count > 0 && iterations < MAX_ITERATIONS) {
            iterations++;

            // get node with lowest f score
            openSet.Sort((a, b) => a.f.CompareTo(b.f));
            var current = openSet[0];
            openSet.RemoveAt(0);
            openSetLookup.Remove(current.GetHashCode());

            // reached goal?
            if (current.x == goal.X && current.y == goal.Y && current.z == goal.Z) {
                return reconstructPath(current, maxLength);
            }

            closedSet.Add(current);

            // check neighbors
            foreach (var neighbor in getNeighbours(e, current)) {
                if (closedSet.Contains(neighbor)) continue;

                float tentativeG = current.g + current.dist(neighbor);

                // skip if path already too long
                if (tentativeG > maxLength) continue;

                var key = neighbor.GetHashCode();
                if (openSetLookup.TryGetValue(key, out var existing)) {
                    if (tentativeG < existing.g) {
                        existing.g = tentativeG;
                        existing.f = existing.g + existing.h;
                        existing.prev = current;
                    }
                }
                else {
                    neighbor.g = tentativeG;
                    neighbor.h = heuristic(new Vector3I(neighbor.x, neighbor.y, neighbor.z), goal);
                    neighbor.f = neighbor.g + neighbor.h;
                    neighbor.prev = current;

                    openSet.Add(neighbor);
                    openSetLookup[key] = neighbor;
                }
            }
        }

        // no path found
        return new Path();
    }

    private static Path reconstructPath(PathNode goal, int maxLength) {
        var pathList = new XList<PathNode>();
        var current = goal;

        while (current != null) {
            pathList.Add(current);
            current = current.prev;
        }

        pathList.Reverse();

        return new Path(pathList);
    }

    private static List<PathNode> getNeighbours(Entity e, PathNode node) {
        var neighbors = new List<PathNode>();

        // 8 horizontal directions + up/down
        ReadOnlySpan<Vector3I> directions = [
            new(1, 0, 0), new(-1, 0, 0),
            new(0, 0, 1), new(0, 0, -1),
            new(1, 0, 1), new(-1, 0, 1),
            new(1, 0, -1), new(-1, 0, -1),
        ];

        foreach (var dir in directions) {
            var nx = node.x + dir.X;
            var ny = node.y + dir.Y;
            var nz = node.z + dir.Z;

            // check if entity fits at this position
            if (fits(e, nx, ny, nz)) {
                neighbors.Add(new PathNode(nx, ny, nz));
            }
            // try stepping up one block
            else if (fits(e, nx, ny + 1, nz)) {
                neighbors.Add(new PathNode(nx, ny + 1, nz));
            }
        }

        // can fall down?
        if (fits(e, node.x, node.y - 1, node.z)) {
            neighbors.Add(new PathNode(node.x, node.y - 1, node.z));
        }

        return neighbors;
    }

    private static bool fits(Entity e, int x, int y, int z) {
        var aabb = e.calcAABB(new Molten.DoublePrecision.Vector3D(x + 0.5, y, z + 0.5));

        // check all blocks entity would occupy
        var min = aabb.min.toBlockPos();
        var max = aabb.max.toBlockPos();

        for (int bx = min.X; bx <= max.X; bx++) {
            for (int by = min.Y; by <= max.Y; by++) {
                for (int bz = min.Z; bz <= max.Z; bz++) {
                    var block = e.world.getBlock(bx, by, bz);
                    // if block has collision, entity doesn't fit
                    if (Block.collision[block]) {
                        return false;
                    }
                }
            }
        }

        // mobs are stupid and will walk off cliffs ;)
        return true;
    }

    private static float heuristic(Vector3I a, Vector3I b) {
        // manhattan distance
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z);
    }
}