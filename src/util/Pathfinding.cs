using BlockGame.util;
using BlockGame.world;
using BlockGame.world.block;
using Molten;

namespace Core.util;

public class Pathfinding {

    public static PriorityQueue<PathNode, float> openSet = new PriorityQueue<PathNode, float>();
    public static HashSet<PathNode> closedSet = new HashSet<PathNode>();
    public static XIntMap<PathNode> openSetLookup = new XIntMap<PathNode>();
    private static List<PathNode> neighborBuffer = new List<PathNode>(10);

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
        openSet.Clear();
        closedSet.Clear();
        openSetLookup.Clear();

        var startNode = new PathNode(start.X, start.Y, start.Z);
        startNode.g = 0;
        startNode.h = heuristic(start, goal);
        startNode.f = startNode.h;

        openSet.Enqueue(startNode, startNode.f);
        openSetLookup.Set(startNode.GetHashCode(), startNode);

        int iterations = 0;

        while (openSet.Count > 0 && iterations < MAX_ITERATIONS) {
            iterations++;

            // get node with lowest f score
            var current = openSet.Dequeue();
            openSetLookup.Remove(current.GetHashCode());

            // reached goal?
            if (current.x == goal.X && current.y == goal.Y && current.z == goal.Z) {
                return reconstructPath(current, maxLength);
            }

            closedSet.Add(current);

            // check neighbours
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

                    openSet.Enqueue(neighbor, neighbor.f);
                    openSetLookup.Set(key, neighbor);
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

    private enum Type {
        Blocked,  // solid blocks
        Air,      // empty space
        Water,    // water (can swim)
        Lava      // lava (avoid)
    }

    private static List<PathNode> getNeighbours(Entity e, PathNode node) {
        neighborBuffer.Clear();

        // 8 horizontal directions + up/down
        ReadOnlySpan<Vector3I> directions = [
            new(1, 0, 0), new(-1, 0, 0),
            new(0, 0, 1), new(0, 0, -1),
            new(1, 0, 1), new(-1, 0, 1),
            new(1, 0, -1), new(-1, 0, -1),
        ];

        var current = fits(e, node.x, node.y, node.z);

        foreach (var dir in directions) {
            var nx = node.x + dir.X;
            var ny = node.y + dir.Y;
            var nz = node.z + dir.Z;

            var fit = fits(e, nx, ny, nz);

            // check if entity fits at this position
            if (fit is Type.Air or Type.Water) {
                neighborBuffer.Add(new PathNode(nx, ny, nz));
            }
            // try stepping up one block
            else if (fit == Type.Blocked) {
                var stepFit = fits(e, nx, ny + 1, nz);
                if (stepFit is Type.Air or Type.Water) {
                    neighborBuffer.Add(new PathNode(nx, ny + 1, nz));
                }
            }
        }

        // can fall down?
        var downFit = fits(e, node.x, node.y - 1, node.z);
        if (downFit is Type.Air or Type.Water) {
            neighborBuffer.Add(new PathNode(node.x, node.y - 1, node.z));
        }

        // can swim up through water?
        if (current == Type.Water) {
            var upFit = fits(e, node.x, node.y + 1, node.z);
            if (upFit is Type.Air or Type.Water) {
                neighborBuffer.Add(new PathNode(node.x, node.y + 1, node.z));
            }
        }

        return neighborBuffer;
    }

    private static Type fits(Entity e, int x, int y, int z) {
        var aabb = e.calcAABB(new Molten.DoublePrecision.Vector3D(x + 0.5, y, z + 0.5));

        // check all blocks entity would occupy
        var min = aabb.min.toBlockPos();
        var max = aabb.max.toBlockPos();

        if (min.Y < 0 || max.Y >= World.WORLDHEIGHT) {
            return Type.Blocked;
        }

        bool hasWater = false;
        bool hasLava = false;
        
        int chunkX = min.X >> 4;
        int chunkZ = min.Z >> 4;
        int maxChunkX = max.X >> 4;
        int maxChunkZ = max.Z >> 4;

        // fast path: all blocks in same chunk (common case for small entities)
        if (chunkX == maxChunkX && chunkZ == maxChunkZ) {
            if (!e.world.getChunkMaybe(min.X, min.Z, out var chunk)) {
                return Type.Blocked;
            }

            for (int bx = min.X; bx <= max.X; bx++) {
                for (int by = min.Y; by <= max.Y; by++) {
                    for (int bz = min.Z; bz <= max.Z; bz++) {
                        var block = chunk!.getBlock(bx & 0xF, by, bz & 0xF);

                        if (Block.collision[block]) {
                            return Type.Blocked;
                        }

                        if (Block.liquid[block]) {
                            if (block == Block.LAVA.id) {
                                hasLava = true;
                            } else {
                                hasWater = true;
                            }
                        }
                    }
                }
            }
        }
        // slow path: more chunks lol
        else {
            for (int bx = min.X; bx <= max.X; bx++) {
                for (int by = min.Y; by <= max.Y; by++) {
                    for (int bz = min.Z; bz <= max.Z; bz++) {
                        var block = e.world.getBlock(bx, by, bz);

                        if (Block.collision[block]) {
                            return Type.Blocked;
                        }

                        if (Block.liquid[block]) {
                            if (block == Block.LAVA.id) {
                                hasLava = true;
                            } else {
                                hasWater = true;
                            }
                        }
                    }
                }
            }
        }

        if (hasLava) return Type.Lava;
        if (hasWater) return Type.Water;
        return Type.Air;
    }

    private static float heuristic(Vector3I a, Vector3I b) {
        // manhattan distance
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z);
    }
}