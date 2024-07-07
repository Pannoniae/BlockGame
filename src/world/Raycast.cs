using System.Numerics;
using System.Runtime.InteropServices;
using BlockGame.util;
using Silk.NET.Maths;

namespace BlockGame;

public class Raycast {
    /// <summary>
    /// This piece of shit raycast breaks when the player goes outside the world. Solution? Don't go outside the world (will be prevented in the future with barriers)
    /// </summary>
    /// <param name="previous">The previous block (used for placing)</param>
    /// <returns></returns>
    public static RayCollision raycast(World world) {
        // raycast
        var cameraPos = world.player.camera.position;
        var forward = world.player.camera.forward;
        var cameraForward = new Vector3D<double>(forward.X, forward.Y, forward.Z);
        var currentPos = new Vector3D<double>(cameraPos.X, cameraPos.Y, cameraPos.Z);

        // don't round!!
        //var blockPos = toBlockPos(currentPos);
        var dist = 0.0;

        var previous = currentPos.toBlockPos();
        for (int i = 0; i < 1 / Constants.RAYCASTSTEP * Constants.RAYCASTDIST; i++) {
            dist += (cameraForward * Constants.RAYCASTSTEP).Length;
            currentPos += cameraForward * Constants.RAYCASTSTEP;
            var blockPos = currentPos.toBlockPos();
            var block = Blocks.get(world.getBlock(blockPos));
            if (world.isSelectableBlock(blockPos.X, blockPos.Y, blockPos.Z)) {
                // we also need to check if it's inside the selection of the block
                if (AABB.isCollision(world.getSelectionAABB(blockPos.X, blockPos.Y, blockPos.Z, block) ?? AABB.empty, currentPos)) {
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

[StructLayout(LayoutKind.Sequential)]
public struct RayCollision {

    /// <summary>
    /// Point of the nearest hit
    /// </summary>
    public Vector3D<double> point;


    /// <summary>
    /// The block position of the hit
    /// </summary>
    public Vector3D<int> block;


    /// <summary>
    /// The previous block which was hit
    /// </summary>
    public Vector3D<int> previous;

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