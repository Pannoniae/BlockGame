using System.Runtime.InteropServices;
using BlockGame.main;
using BlockGame.util;
using BlockGame.util.meth;
using BlockGame.world.block;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.world;

public class Raycast {
    private static readonly List<AABB> AABBList = [];

    /// <summary>
    /// This piece of shit raycast breaks when the player goes outside the world. Solution? Don't go outside the world (will be prevented in the future with barriers)
    /// </summary>
    /// <returns></returns>
    public static RayCollision raycast(World world, bool liquids = false) {
        // raycast from player eye position in player look direction (not camera direction)
        var player = Game.player;
        var basePos = player.position;
        var trueEyeHeight = player.sneaking ? Player.sneakingEyeHeight : Player.eyeHeight;
        var raycastPos = basePos + new Vector3D(0, trueEyeHeight, 0);

        // calculate player look direction based on player rotation (not camera rotation)
        var yaw = player.rotation.Y;
        var pitch = player.rotation.X;
        var playerForward = new Vector3D();
        playerForward.X = MathF.Sin(Meth.deg2rad(yaw)) * MathF.Cos(Meth.deg2rad(pitch));
        playerForward.Y = MathF.Sin(Meth.deg2rad(pitch));
        playerForward.Z = MathF.Cos(Meth.deg2rad(yaw)) * MathF.Cos(Meth.deg2rad(pitch));
        playerForward = Vector3D.Normalize(playerForward);

        var currentPos = raycastPos;

        // don't round!!
        //var blockPos = toBlockPos(currentPos);
        var dist = 0.0;

        var previous = currentPos.toBlockPos();
        for (int i = 0; i < 1 / Constants.RAYCASTSTEP * Game.gamemode.reach; i++) {
            dist += (playerForward * Constants.RAYCASTSTEP).Length();
            currentPos += playerForward * Constants.RAYCASTSTEP;
            var blockPos = currentPos.toBlockPos();
            if (world.isSelectableBlock(blockPos.X, blockPos.Y, blockPos.Z) || (liquids && Block.liquid[world.getBlock(blockPos.X, blockPos.Y, blockPos.Z)])) {
                // we also need to check if it's inside the selection of the block
                world.getAABBs(AABBList, blockPos.X, blockPos.Y, blockPos.Z);
                foreach (AABB aabb in AABBList) {
                    if (AABB.isCollision(aabb, currentPos)) {
                        //Console.Out.WriteLine("getblock:" + getBlock(blockPos.X, blockPos.Y, blockPos.Z));
                        // the hit face is the one where the change is the greatest
                        var dx = blockPos.X - previous.X;
                        var dy = blockPos.Y - previous.Y;
                        var dz = blockPos.Z - previous.Z;
                        var adx = Math.Abs(dx);
                        var ady = Math.Abs(dy);
                        var adz = Math.Abs(dz);
                        RawDirection f;
                        if (adx > ady) {
                            if (adx > adz) {
                                f = dx > 0 ? RawDirection.WEST : RawDirection.EAST;
                            }
                            else {
                                f = dz > 0 ? RawDirection.SOUTH : RawDirection.NORTH;
                            }
                        }
                        else {
                            if (ady > adz) {
                                f = dy > 0 ? RawDirection.DOWN : RawDirection.UP;
                            }
                            else {
                                f = dz > 0 ? RawDirection.SOUTH : RawDirection.NORTH;
                            }
                        }

                        var col = new RayCollision {
                            point = currentPos,
                            previous = previous,
                            block = blockPos,
                            hit = true,
                            distance = dist,
                            face = f
                        };
                        return col;
                    }
                }
            }

            previous = blockPos;
        }
        return new RayCollision {
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

}