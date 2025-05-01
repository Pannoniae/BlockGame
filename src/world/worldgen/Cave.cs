using System.Numerics;
using BlockGame.util;
using Molten.DoublePrecision;

namespace BlockGame;

public class Cave : OverlayFeature {
    /// <summary>
    /// The current cave width
    /// </summary>
    public double width;

    // Current worm head
    public double cx;
    public double cy;

    public double cz;

    // Direction vectors
    public Vector3D d;

    public double hAngle;
    public double vAngle;

    public override void generate(World world, ChunkCoord coord, ChunkCoord origin) {
        var chunk = world.getChunk(coord);

        // if this chunk isn't lucky, it gets murdered
        if (rand.Next(5) != 0) {
            return;
        }

        // decide how many to generate (totally randomly, trust)
        var count = rand.Next(10, 50);

        // trim
        //count = rand.Next(count);

        for (int i = 0; i < count; i++) {
            // get random position to start with
            var x = coord.x * Chunk.CHUNKSIZE + rand.Next(Chunk.CHUNKSIZE);
            var z = coord.z * Chunk.CHUNKSIZE + rand.Next(Chunk.CHUNKSIZE);
            var y = rand.Next(World.WORLDHEIGHT);
            genCave(world, x, y, z, origin);
        }
    }

    /// <summary>
    /// Generate a cave at the given position. This is recursive - we decide how many steps to generate then
    /// we call the inner function X number of times to carve our way through the terrain.
    /// This should ALWAYS be capped to the original chunk because otherwise we just waste time carving out the same blocks again and again lol
    /// </summary>
    private void genCave(World world, int x, int y, int z, ChunkCoord origin) {
        var chunk = world.getChunk(origin);

        width = rand.NextDouble() * 2 + 1.5; // random width between 1 and 3
        var steps = rand.Next(World.WORLDHEIGHT); // yesn't
        steps -= rand.Next((int)(World.WORLDHEIGHT / 3d));

        // if it's wide, have fewer steps
        var fat = double.Max(width - 3, 0);
        steps = (int)(steps / double.Max(fat, 1));

        var xd = rand.NextDouble() * 2 - 1;
        var yd = rand.NextDouble() * 2 - 1;
        var zd = rand.NextDouble() * 2 - 1;
        d = new Vector3D(xd, yd, zd);
        d.Normalize();
        cx = x;
        cy = y;
        cz = z;
        hAngle = rand.NextDouble() * Math.PI * 2;
        vAngle = (rand.NextDouble() - 0.5) * Math.PI * 0.25;
        //Console.Out.WriteLine("Cave: " + cx + ", " + cy + ", " + cz);
        for (int i = 0; i < steps; i++) {
            genCaveInner(world, x, y, z, i, steps, origin);
        }
    }

    private void genCaveInner(World world, int x, int y, int z, int step, int steps, ChunkCoord origin) {
        // in the beginning/end, we want to be narrower to "tail it off"
        int taperingSteps = steps / 10; // 10% at each end
        double appliedWidth = width;
        if (step < taperingSteps) {
            appliedWidth = 1 + (width - 1) * step / taperingSteps;
        }
        // Last few blocks - taper from normalWidth to 1
        else if (step > steps - taperingSteps) {
            appliedWidth = 1 + (width - 1) * (steps - step) / taperingSteps;
        }

        //Console.Out.WriteLine("Step: " + step + ", Width: " + appliedWidth);

        if (rand.Next(10 + (int)width) == 0) {
            width += 0.5;
        }

        if (rand.Next(10) == 0) {
            width -= 0.5;
        }

        if (width < 1.5) {
            return;
        }

        // vary the direction or something unfinished idk

        // angle shit

        // if the cave is very narrow, we want to move less so it doesn't look too blocky TODO?
        var offset = appliedWidth;

        // vary the direction with rotations
        double rotationChance = rand.NextDouble();
        
        // if we are vertical, mellow it out
        if (Math.Abs(Vector3D.Dot(d, new Vector3D(0, 1, 0))) > 0.9) {
            d.Y *= 0.76;
        }

        if (rotationChance < 0.03) {
            // 1% chance for a vertical drop
            // Keep small horizontal movement but add strong downward component
            d = new Vector3D(d.X * 0.2, -2.0 + d.Y * 0.1, d.Z * 0.2);
            //Console.Out.WriteLine("VERTICAL DROP");
        }
        else if (rotationChance < 0.1) {
            // 9% chance for a drastic turn (30-70 degrees)
            double angle = Meth.deg2rad(30 + rand.Next(40));

            // Create random rotation axis (perpendicular to current direction for maximum turning effect)
            Vector3D up = new Vector3D(0, 1, 0);
            Vector3D rotationAxis;

            // If direction is mostly vertical, use a different reference vector
            if (Math.Abs(Vector3D.Dot(d, up)) > 0.9) {
                rotationAxis = Vector3D.Cross(d, new Vector3D(1, 0, 0));
            }
            else {
                rotationAxis = Vector3D.Cross(d, up);
            }

            rotationAxis = Vector3D.Normalize(rotationAxis);

            // Apply rotation
            d = rot(d, rotationAxis, angle);
        }
        else {
            // 90% chance for a small turn (1-15 degrees)
            double angle = Meth.deg2rad(1 + rand.Next(15));

            // Create random rotation axis for more naturalistic movement
            Vector3D rotationAxis = new Vector3D(
                rand.NextDouble() * 2 - 1,
                rand.NextDouble() * 2 - 1,
                rand.NextDouble() * 2 - 1
            );
            rotationAxis = Vector3D.Normalize(rotationAxis);

            // Apply rotation
            d = rot(d, rotationAxis, angle);
        }

        // Keep direction normalized for consistent "speed"
        d = Vector3D.Normalize(d) * offset;

        // update position
        cx += d.X;
        cy += d.Y;
        cz += d.Z;


        // actually carve
        var xMin = cx - appliedWidth;
        var xMax = cx + appliedWidth;
        var yMin = cy - appliedWidth;
        var yMax = cy + appliedWidth;
        var zMin = cz - appliedWidth;
        var zMax = cz + appliedWidth;

        // cap them to the original chunk
        xMin = Math.Max(xMin, origin.x * Chunk.CHUNKSIZE);
        xMax = Math.Min(xMax, (origin.x + 1) * Chunk.CHUNKSIZE);
        yMin = Math.Max(yMin, 1);
        yMax = Math.Min(yMax, World.WORLDHEIGHT - 4);
        zMin = Math.Max(zMin, origin.z * Chunk.CHUNKSIZE);
        zMax = Math.Min(zMax, (origin.z + 1) * Chunk.CHUNKSIZE);

        for (int xd = (int)xMin; xd < (int)xMax; xd++) {
                for (int zd = (int)zMin; zd < (int)zMax; zd++) {
                    bool hasGrass = false;
                    
                    // IMPORTANT do it upside down because the whole grass replacing stuff
                    // if you do it the right way around we won't know whether we've ruined terrain or not
                    for (int yd = (int)yMax - 1; yd >= (int)yMin; yd--) {
                    // distance check
                    var dist = Math.Sqrt(
                        (cx - xd) * (cx - xd) +
                        (cy - yd) * (cy - yd) +
                        (cz - zd) * (cz - zd));
                    if (dist < (int)appliedWidth) {
                        var block = world.getBlock(xd, yd, zd);
                        
                        // if it's grass, exchange below
                        if (block == Block.GRASS.id) {
                            hasGrass = true;
                        }
                        
                        // if it's a solid block, remove it
                        if (Block.isSolid(block)) {
                            world.setBlock(xd, yd, zd, Block.AIR.id);
                            if (hasGrass && world.getBlock(xd, yd - 1, zd) == Block.DIRT.id) {
                                world.setBlock(xd, yd - 1, zd, Block.GRASS.id);
                            }
                        }
                    }
                }
            }
        }
    }

    private static Vector3D rot(Vector3D vector, Vector3D axis, double angle) {
        // Implementation of Rodrigues' rotation formula
        // v_rot = v * cos(θ) + (axis × v) * sin(θ) + axis * (axis · v) * (1 - cos(θ))
        double cosAngle = Math.Cos(angle);
        double sinAngle = Math.Sin(angle);

        Vector3D cross = Vector3D.Cross(axis, vector);
        double dot = Vector3D.Dot(axis, vector);

        return vector * cosAngle +
               cross * sinAngle +
               axis * dot * (1 - cosAngle);
    }
}