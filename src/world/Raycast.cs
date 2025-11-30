using System.Runtime.InteropServices;
using BlockGame.main;
using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.entity;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.world;

public class Raycast {
    private static readonly List<AABB> AABBList = [];
    private static readonly List<Entity> l = [];

    /// <summary>
    /// This piece of shit raycast breaks when the player goes outside the world. Solution? Don't go outside the world (will be prevented in the future with barriers)
    /// </summary>
    /// <returns></returns>
    public static RayCollision raycast(World world, RaycastType type) {
        // raycast from player eye position in player look direction (not camera direction)
        var player = Game.player;
        var basePos = player.position;
        var trueEyeHeight = player.sneaking ? Player.sneakingEyeHeight : Player.eyeHeight;
        var raycastPos = basePos + new Vector3D(0, trueEyeHeight, 0);

        // calculate player look direction based on player rotation (not camera rotation)
        var yaw = player.rotation.Y;
        var pitch = player.rotation.X;
        var playerForward = new Vector3D {
            X = MathF.Sin(Meth.deg2rad(yaw)) * MathF.Cos(Meth.deg2rad(pitch)),
            Y = MathF.Sin(Meth.deg2rad(pitch)),
            Z = MathF.Cos(Meth.deg2rad(yaw)) * MathF.Cos(Meth.deg2rad(pitch))
        };
        playerForward = Vector3D.Normalize(playerForward);

        var currentPos = raycastPos;

        // don't round!!
        //var blockPos = toBlockPos(currentPos);
        var dist = 0.0;

        var previous = currentPos.toBlockPos();
        for (int i = 0; i < 1 / Constants.RAYCASTSTEP * player.gameMode.reach; i++) {
            dist += (playerForward * Constants.RAYCASTSTEP).Length();
            currentPos += playerForward * Constants.RAYCASTSTEP;
            var blockPos = currentPos.toBlockPos();

            // if entities enabled, check that first
            if (type is RaycastType.ALL or RaycastType.ENTITIES) {
                world.getEntitiesInBox(l, new AABB(currentPos - new Vector3D(0.3, 0.3, 0.3), currentPos + new Vector3D(0.3, 0.3, 0.3)));
                foreach (var entity in l) {
                    if (entity == player) {
                        continue;
                    }

                    // if itementity and player has non-empty hand, skip
                    if (entity is ItemEntity && player.inventory.getSelected() != ItemStack.EMPTY) {
                        continue;
                    }

                    // calculate hit face
                    RawDirection f;
                    var toEntity = Vector3D.Normalize(entity.position + new Vector3D(0, (entity.aabb.y1 - entity.aabb.y0) / 2, 0) - raycastPos);
                    var dx = Vector3D.Dot(toEntity, new Vector3D(1, 0, 0));
                    var dy = Vector3D.Dot(toEntity, new Vector3D(0, 1, 0));
                    var dz = Vector3D.Dot(toEntity, new Vector3D(0, 0, 1));
                    var adx = Math.Abs(dx);
                    var ady = Math.Abs(dy);
                    var adz = Math.Abs(dz);
                    if (adx >= ady && adx >= adz) {
                        f = dx > 0 ? RawDirection.EAST : RawDirection.WEST;
                    } else if (ady >= adx && ady >= adz) {
                        f = dy > 0 ? RawDirection.UP : RawDirection.DOWN;
                    } else {
                        f = dz > 0 ? RawDirection.SOUTH : RawDirection.NORTH;
                    }

                    var entityAABB = entity.aabb;
                    if (AABB.isCollision(entityAABB, currentPos)) {
                        return new RayCollision {
                            type = Result.ENTITY,

                            point = currentPos,
                            previous = previous,
                            block = blockPos,
                            entity = entity,
                            hit = true,
                            distance = dist,
                            face = f
                        };
                    }
                }
            }


            if (world.isSelectableBlock(blockPos.X, blockPos.Y, blockPos.Z) || (type == RaycastType.BLOCKSLIQUIDS && Block.liquid[world.getBlock(blockPos.X, blockPos.Y, blockPos.Z)])) {
                // we also need to check if it's inside the selection of the block
                world.getAABBs(AABBList, blockPos.X, blockPos.Y, blockPos.Z);
                foreach (AABB aabb in AABBList) {
                    if (AABB.isCollision(aabb, currentPos)) {
                        var rayDir = playerForward;
                        var rayOrigin = raycastPos;

                        // calculate the intersection distances for each axis-aligned face
                        var txn = (aabb.min.X - rayOrigin.X) / rayDir.X;
                        var txx = (aabb.max.X - rayOrigin.X) / rayDir.X;
                        var tyn = (aabb.min.Y - rayOrigin.Y) / rayDir.Y;
                        var tyx = (aabb.max.Y - rayOrigin.Y) / rayDir.Y;
                        var tzn = (aabb.min.Z - rayOrigin.Z) / rayDir.Z;
                        var tzx = (aabb.max.Z - rayOrigin.Z) / rayDir.Z;

                        if (txn > txx) {
                            txn = txx;
                        }

                        if (tyn > tyx) {
                            tyn = tyx;
                        }

                        if (tzn > tzx) {
                            tzn = tzx;
                        }

                        // figure out which face was hit first (smallest t > 0)
                        double te = Math.Max(Math.Max(txn, tyn), tzn);

                        RawDirection f;
                        const double epsilon = 0.0001;
                        if (Math.Abs(te - txn) < epsilon) {
                            f = rayDir.X > 0 ? RawDirection.WEST : RawDirection.EAST;
                        } else if (Math.Abs(te - tyn) < epsilon) {
                            f = rayDir.Y > 0 ? RawDirection.DOWN : RawDirection.UP;
                        } else {
                            f = rayDir.Z > 0 ? RawDirection.SOUTH : RawDirection.NORTH;
                        }

                        var col = new RayCollision {
                            type = Result.BLOCK,
                            point = currentPos,
                            previous = previous,
                            block = blockPos,
                            hit = true,
                            distance = dist,
                            face = f,
                            hitAABB = aabb
                        };
                        return col;
                    }
                }
            }

            previous = blockPos;
        }
        return new RayCollision {
            type = Result.MISS,
            point = default,
            block = default,
            previous = default,
            hit = false,
            distance = dist,
            face = RawDirection.NONE
        };
    }
}

[StructLayout(LayoutKind.Auto)]
public struct RayCollision {

    /// <summary>
    /// Type of the raycast hit
    /// </summary>
    public Result type;

    /// <summary>
    /// Point of the nearest hit
    /// </summary>
    public Vector3D point;


    /// <summary>
    /// The block position of the hit
    /// </summary>
    public Vector3I block;

    /// <summary>
    /// The previous block which was hit
    /// </summary>
    public Vector3I previous;

    /// <summary>
    /// The entity that was hit (if any)
    /// </summary>
    public Entity? entity;

    /// <summary>
    /// Which face of the block was hit?
    /// </summary>
    public RawDirection face;

    /// <summary>
    /// Distance to the nearest hit
    /// </summary>
    public double distance;

    /// <summary>
    /// Did the ray hit something?
    /// </summary>
    public bool hit;

    /// <summary>
    /// The specific AABB that was hit (for blocks with multiple/custom AABBs)
    /// </summary>
    public AABB? hitAABB;

}

public enum Result {
    MISS,
    BLOCK,
    ENTITY
}

public enum RaycastType {
    ALL,
    BLOCKS,
    BLOCKSLIQUIDS,
    ENTITIES
}