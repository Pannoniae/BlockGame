using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using BlockGame.id;
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

    /// <summary>
    /// Between -PI and PI
    /// </summary>
    public double hAngle;

    /// <summary>
    /// Between -PI/2 and PI/2
    /// </summary>
    public double vAngle;

    public bool isRoom = false;

    public override void generate(World world, ChunkCoord coord, ChunkCoord origin) {
        var chunk = world.getChunk(coord);

        // if this chunk isn't lucky, it gets murdered
        if (rand.Next(10) != 0) {
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

            // if above the terrain heightmap, skip
            if (y > world.getHeight(x, z) + 6) {
                continue;
            }

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
        isRoom = false;
        //Console.Out.WriteLine("Cave: " + cx + ", " + cy + ", " + cz);
        for (int i = 0; i < steps; i++) {
            genCaveInner(world, i, steps, origin);
        }
    }

    // I know we are hiding them, THAT'S THE ENTIRE FUCKING POINT
    [SuppressMessage("ReSharper", "LocalVariableHidesMember")]
    private void genCaveInner(World world, int step, int steps, ChunkCoord origin) {
        // copy shit into locals
        // idk JIT probably doesn't do that
        var width = this.width;
        var rand = this.rand;
        var d = this.d;
        var hAngle = this.hAngle;
        var vAngle = this.vAngle;
        var cx = this.cx;
        var cy = this.cy;
        var cz = this.cz;

        //Console.Out.WriteLine("Step: " + step + ", Width: " + appliedWidth);

        var widthVar = rand.NextDouble();
        // if wide, don't increase further
        if (widthVar < 0.02) {
            width += 0.3;
        }

        if (widthVar > 0.98) {
            width -= 0.3;
        }

        if (width < 2) {
            goto cleanup;
        }

        double appliedWidth = width;

        // if room, continue being a room, if not, 0.1% chance of flipping into one
        if (isRoom || (widthVar > 0.45 && widthVar < 0.451)) {
            // create a room
            // make D small & random
            d = new Vector3D(rand.NextDouble() * 0.3, rand.NextDouble() * 0.05, rand.NextDouble() * 0.3);
            width = rand.NextDouble() * 4 + 5;
            isRoom = true;
            goto gen;
        }

        // in the beginning/end, we want to be narrower to "tail it off"
        int taperingSteps = steps / 10; // 10% at each end
        if (step < taperingSteps) {
            appliedWidth = 1 + (width - 1) * step / taperingSteps;
        }
        // Last few blocks - taper from normalWidth to 1
        else if (step > steps - taperingSteps) {
            appliedWidth = 1 + (width - 1) * (steps - step) / taperingSteps;
        }

        // vary the direction or something unfinished idk

        // angle shit

        // if the cave is very narrow, we want to move less so it doesn't look too blocky TODO?
        var offset = appliedWidth / 2;

        // vary the direction with rotations
        double rotationChance = rand.NextDouble();

        // if we are vertical, mellow it out
        vAngle *= 0.76;

        if (rotationChance < 0.01) {
            // 1% chance for a vertical drop
            // Keep small horizontal movement but add strong downward component
            vAngle = Meth.deg2rad(rand.Next(-70, 70));
            //Console.Out.WriteLine("VERTICAL DROP");
        }
        else if (rotationChance < 0.05) {
            // 5% chance for a drastic turn (-90..+90 degrees)
            double angle = Meth.deg2rad(rand.Next(180) - 90);
            hAngle += angle;
            double v = Meth.deg2rad(rand.Next(90) - 45);
            vAngle += v;
        }
        else if (rotationChance < 0.4) {
            double angle = Meth.deg2rad(rand.Next(90) - 45);
            hAngle += angle;
            double v = Meth.deg2rad(rand.Next(90) - 45);
            vAngle += v;
        }
        else {
            // 90% chance for a small turn (1-15 degrees)
            double angle = Meth.deg2rad(rand.Next(15) - 7.5f);
            hAngle += angle;
            double v = Meth.deg2rad(rand.Next(15) - 7.5f);
            vAngle += v;
        }

        // update d vector with the angles
        d.X = Math.Cos(hAngle) * Math.Cos(vAngle);
        d.Y = Math.Sin(vAngle);
        d.Z = Math.Sin(hAngle) * Math.Cos(vAngle);

        // Keep direction normalized for consistent "speed"
        d = Vector3D.Normalize(d) * offset;

        gen: ;

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


        // if we are outside the chunk, bail
        if (xMax < origin.x * Chunk.CHUNKSIZE ||
            xMin > (origin.x + 1) * Chunk.CHUNKSIZE ||
            yMax < 2 ||
            yMin > World.WORLDHEIGHT - 4 ||
            zMax < origin.z * Chunk.CHUNKSIZE ||
            zMin > (origin.z + 1) * Chunk.CHUNKSIZE) {
            goto cleanup;
        }

        // cap them to the original chunk
        xMin = Math.Max(xMin, origin.x * Chunk.CHUNKSIZE);
        xMax = Math.Min(xMax, (origin.x + 1) * Chunk.CHUNKSIZE);
        yMin = Math.Max(yMin, 2);
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

                    double dist;
                    // if it's a room, don't do a circle - make the bottom flat
                    if (isRoom && yd < cy) {
                        dist = (cx - xd) * (cx - xd) +
                               ((cy - yd) * 2d) * ((cy - yd) * 2d) +
                               (cz - zd) * (cz - zd);
                    }
                    else {
                        dist = (cx - xd) * (cx - xd) +
                               (cy - yd) * (cy - yd) +
                               (cz - zd) * (cz - zd);
                    }


                    if (dist < (int)(appliedWidth * appliedWidth)) {
                        var block = world.getBlock(xd, yd, zd);

                        // if it's grass, exchange below
                        if (block == Blocks.GRASS) {
                            hasGrass = true;
                        }

                        // if it's a solid block, remove it
                        if (Block.fullBlock[block]) {
                            world.setBlock(xd, yd, zd, Blocks.AIR);
                            if (hasGrass && world.getBlock(xd, yd - 1, zd) == Blocks.DIRT) {
                                world.setBlock(xd, yd - 1, zd, Blocks.GRASS);
                            }
                        }
                    }
                }
            }
        }

        cleanup: ;
        this.width = width;
        this.d = d;
        this.hAngle = hAngle;
        this.vAngle = vAngle;
        this.cx = cx;
        this.cy = cy;
        this.cz = cz;
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