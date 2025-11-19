using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.entity;
using Molten;
using Molten.DoublePrecision;
using Entity = BlockGame.world.entity.Entity;

namespace BlockGame.util.path;

public static class Pathfinding {

    public static readonly PriorityQueue<PathNode, float> openSet = new();
    public static readonly HashSet<PathNode> closedSet = [];
    public static readonly XIntMap<PathNode> openSetLookup = [];
    private static readonly XUList<PathNode> neighbourBuffer = new(10);


    private static readonly XIntMap<PathNode> nodeCache = new(128);
    private static readonly XUList<PathNode> nodePool = new(128);
    private static readonly XUList<Path> pathPool = [];

    public const int MAX_PATH_LENGTH = 32;
    private const int MAX_ITERATIONS = 512;

    private static PathNode get(int x, int y, int z) {
        int hash = x * 73856093 ^ y * 19349663 ^ z * 83492791;
        if (nodeCache.TryGetValue(hash, out var node)) {
            return node;
        }

        if (nodePool.Count > 0) {
            node = nodePool[^1];
            nodePool.RemoveAt(nodePool.Count - 1);
            node.reset(x, y, z);
            nodeCache.Set(hash, node);
            return node;
        }

        node = new PathNode(x, y, z);
        nodeCache.Set(hash, node);
        return node;
    }

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

    /**
     * Returns path nodes to the pool for reuse.
     * Call this after you're done with a path so we won't have to allocate new nodes next time!
     */
    public static void ret(Path path) {
        // Add all the nodes
        foreach (var node in path.nodes) {
            nodeCache.Remove(node.GetHashCode());
            node.reset(0, 0, 0);
            nodePool.Add(node);
        }
        path.reset();

        // Add the path itself
        pathPool.Add(path);
    }

    private static Path aStar(Entity e, Vector3I start, Vector3I goal, int maxLength) {
        openSet.Clear();
        closedSet.Clear();
        openSetLookup.Clear();
        nodeCache.Clear();

        var startNode = get(start.X, start.Y, start.Z);
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
            foreach (var neighbour in getNeighbours(e, current)) {
                if (closedSet.Contains(neighbour)) {
                    continue;
                }

                float tentativeG = current.g + current.dist(neighbour);

                // skip if path already too long
                if (tentativeG > maxLength) continue;

                var key = neighbour.GetHashCode();
                if (openSetLookup.TryGetValue(key, out var existing)) {
                    if (tentativeG < existing.g) {
                        existing.g = tentativeG;
                        existing.f = existing.g + existing.h;
                        existing.prev = current;
                    }
                }
                else {
                    neighbour.g = tentativeG;
                    neighbour.h = heuristic(new Vector3I(neighbour.x, neighbour.y, neighbour.z), goal);
                    neighbour.f = neighbour.g + neighbour.h;
                    neighbour.prev = current;

                    openSet.Enqueue(neighbour, neighbour.f);
                    openSetLookup.Set(key, neighbour);
                }
            }
        }

        // no path found
        return new Path();
    }

    private static Path reconstructPath(PathNode goal, int maxLength) {

        Path path;

        if (pathPool.Count > 0) {
            path = pathPool[^1];
            pathPool.RemoveAt(pathPool.Count - 1);
            path.reset();
        } else {
            path = new Path();
        }
        var current = goal;

        while (current != null) {
            path.nodes.Add(current);
            current = current.prev;
        }

        path.nodes.Reverse();

        return path;
    }

    private enum Type {
        BLOCKED,  // solid blocks
        AIR,      // empty space
        WATER,    // water (can swim)
        LAVA      // lava (avoid)
    }

    private static XUList<PathNode> getNeighbours(Entity e, PathNode node) {
        neighbourBuffer.Clear();

        // hostile mobs shouldn't pathfind to water
        bool isHostile = e is Mob mob && mob.hostile;

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
            if (fit == Type.AIR || (fit == Type.WATER && !isHostile)) {
                neighbourBuffer.Add(get(nx, ny, nz));
            }
            // try stepping up one block
            else if (fit == Type.BLOCKED) {
                var stepFit = fits(e, nx, ny + 1, nz);
                if (stepFit == Type.AIR || (stepFit == Type.WATER && !isHostile)) {
                    neighbourBuffer.Add(get(nx, ny + 1, nz));
                }
            }
        }

        // can fall down?
        var downFit = fits(e, node.x, node.y - 1, node.z);
        if (downFit == Type.AIR || (downFit == Type.WATER && !isHostile)) {
            neighbourBuffer.Add(get(node.x, node.y - 1, node.z));
        }

        // can swim up through water?
        if (current == Type.WATER && !isHostile) {
            var upFit = fits(e, node.x, node.y + 1, node.z);
            if (upFit is Type.AIR or Type.WATER) {
                neighbourBuffer.Add(get(node.x, node.y + 1, node.z));
            }
        }

        return neighbourBuffer;
    }

    private static Type fits(Entity e, int x, int y, int z) {
        var aabb = e.calcAABB(new Vector3D(x + 0.5, y, z + 0.5));

        // check all blocks entity would occupy
        var min = aabb.min.toBlockPos();
        var max = aabb.max.toBlockPos();

        if (min.Y < 0 || max.Y >= World.WORLDHEIGHT) {
            return Type.BLOCKED;
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
                return Type.BLOCKED;
            }

            for (int bx = min.X; bx <= max.X; bx++) {
                for (int by = min.Y; by <= max.Y; by++) {
                    for (int bz = min.Z; bz <= max.Z; bz++) {
                        var block = chunk!.getBlock(bx & 0xF, by, bz & 0xF);

                        if (Block.collision[block]) {
                            return Type.BLOCKED;
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
                            return Type.BLOCKED;
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

        if (hasLava) return Type.LAVA;
        if (hasWater) return Type.WATER;
        return Type.AIR;
    }

    private static float heuristic(Vector3I a, Vector3I b) {
        // manhattan distance
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z);
    }
}