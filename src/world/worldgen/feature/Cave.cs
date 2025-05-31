using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using BlockGame.id;
using BlockGame.util;
using Molten.DoublePrecision;

namespace BlockGame;

public class Cave : OverlayFeature {

    public override int radius => 8;

    /// <summary>
    /// The current cave width
    /// </summary>
    public float width;

    // Current worm head
    public double cx;
    public double cy;

    public double cz;

    // Direction vectors
    public Vector3 d;

    /// <summary>
    /// Between -PI and PI
    /// </summary>
    public float hAngle;

    /// <summary>
    /// Between -PI/2 and PI/2
    /// </summary>
    public float vAngle;

    public bool isRoom = false;
    private Chunk chunk;


    /// <summary>
    /// TODO generate less caves in plains biomes / flat terrain
    /// especially less caves which cut the surface so we don't have a fucking warzone of holes everywhere
    /// There was a RNG desync bug with getHeight() which caused caves to desync and generate differently even with the same seed/get cut off randomly.
    /// This was because generated chunks didn't have trees and stuff, populated chunks did, and getHeight() used the heightmap which is obviously modified by the trees. (and in the future, the other terrain structures)
    /// Solution? Create a "base" terrain heightmap (which will be saved!!) and use that for cave generation and all the stuff which should get the "raw" terrain...
    /// </summary>
    public override void generate(World world, XRandom rand, ChunkCoord coord, ChunkCoord origin) {
        // if this chunk isn't lucky, it gets murdered
        var luck = rand.Next(20);
        //Console.WriteLine($"Chunk {coord}: luck={luck}");
        if (luck != 0) {
            return;
        }

        // decide how many to generate (totally randomly, trust)
        var count = rand.Next(10, 50);

        //Console.Out.WriteLine("Cave count: " + count);

        // trim
        //count = rand.Next(count);

        chunk = world.getChunk(origin);

        for (int i = 0; i < count; i++) {
            // get random position to start with
            var x = coord.x * Chunk.CHUNKSIZE + rand.Next(Chunk.CHUNKSIZE);
            var z = coord.z * Chunk.CHUNKSIZE + rand.Next(Chunk.CHUNKSIZE);
            var y = rand.Next(8, 96);

            // if above the terrain heightmap, skip
            // TODO this completely fucks cavegen. Why? TIME TO FIND OUT
            //if (y > world.getHeight(x, z) + 6) {
            //continue;
            //}

            genCave(world, rand, x, y, z, origin);
        }
    }

    /// <summary>
    /// Generate a cave at the given position. This is recursive - we decide how many steps to generate then
    /// we call the inner function X number of times to carve our way through the terrain.
    /// This should ALWAYS be capped to the original chunk because otherwise we just waste time carving out the same blocks again and again lol
    /// </summary>
    private void genCave(World world, XRandom rand, int x, int y, int z, ChunkCoord origin) {

        width = rand.NextSingle() * 2 + 1.5f; // random width between 1 and 3
        var steps = rand.Next(32, World.WORLDHEIGHT); // yesn't
        steps -= rand.Next((int)(World.WORLDHEIGHT / 4d));

        // if it's wide, have fewer steps
        var fat = double.Max(width - 3, 0);
        steps = (int)(steps / double.Max(fat, 1));
        d = new Vector3();
        cx = x;
        cy = y;
        cz = z;
        hAngle = rand.NextSingle() * MathF.PI * 2;
        vAngle = (rand.NextSingle() - 0.5f) * MathF.PI * 0.25f;
        isRoom = false;
        //Console.Out.WriteLine("Cave: " + cx + ", " + cy + ", " + cz + ", Steps: " + steps + ", Width: " + width + ", Direction: " + d + ", hAngle: " + hAngle + ", vAngle: " + vAngle);
        for (int i = 0; i < steps; i++) {
            genCaveInner(rand, i, origin);
        }
    }

    // I know we are hiding them, THAT'S THE ENTIRE FUCKING POINT
    [SuppressMessage("ReSharper", "LocalVariableHidesMember")]
    private void genCaveInner(XRandom rand, int step, ChunkCoord origin) {
        // copy shit into locals
        // idk JIT probably doesn't do that
        var width = this.width;
        var d = this.d;
        var hAngle = this.hAngle;
        var vAngle = this.vAngle;
        var cx = this.cx;
        var cy = this.cy;
        var cz = this.cz;
        var isRoom = this.isRoom;
        var chunk = this.chunk;


        //Console.Out.WriteLine("Step: " + step + ", Width: " + appliedWidth);

        var widthVar = rand.NextDouble();
        switch (widthVar) {
            // if wide, don't increase further
            case < 0.02:
                width += 0.3f;
                break;
            case > 0.98:
                width -= 0.3f;
                break;
        }

        // min width is 1
        width = float.Max(1, width);

        //if (width < 1) {
        //    Console.Out.WriteLine(width);
        //}

        //if (width < 2) {
        //goto cleanup;
        //}

        //double appliedWidth = width;

        // if step 0, just carve
        if (step == 0) {
            goto gen;
        }

        // if room, continue being a room, if not, 0.1% chance of flipping into one
        if (isRoom || widthVar is > 0.45 and < 0.451) {
            // create a room
            // make D small & random
            //d = new Vector3D(rand.NextDouble() * 0.3, rand.NextDouble() * 0.05, rand.NextDouble() * 0.3);
            d = rand.NextUnitVector3();
            d.X *= 0.3f;
            d.Y *= 0.05f;
            d.Z *= 0.3f;
            width = rand.NextSingle() * 4 + 5;
            isRoom = true;
            cx += d.X;
            cy += d.Y;
            cz += d.Z;
            goto gen;
        }

        // in the beginning/end, we want to be narrower to "tail it off"
        /*int taperingSteps = steps / 10; // 10% at each end
        if (step < taperingSteps) {
            appliedWidth = 1 + (width - 1) * step / taperingSteps;
        }
        // Last few blocks - taper from normalWidth to 1
        else if (step > steps - taperingSteps) {
            appliedWidth = 1 + (width - 1) * (steps - step) / taperingSteps;
        }*/

        // vary the direction or something unfinished idk

        // angle shit

        // if the cave is very narrow, we want to move less so it doesn't look too blocky TODO?

        // vary the direction with rotations
        //double rotationChance = rand.NextDouble();

        // if we are vertical, mellow it out
        vAngle *= 0.76f;

        // Generate two random values once
        var r1 = rand.NextSingle(); // [0, 1)
        var r2 = rand.NextSingle(); // [0, 1)

        switch (rand.NextDouble()) {
            case < 0.01: {
                // 1% chance for a vertical drop
                vAngle = Meth.deg2rad(r1 * 140 - 70); // map to [-70, 70)
                break;
            }
            case < 0.05: {
                // 5% chance for a drastic turn
                hAngle += Meth.deg2rad(r1 * 180 - 90); // map to [-90, 90)
                vAngle += Meth.deg2rad(r2 * 90 - 45); // map to [-45, 45)
                break;
            }
            case < 0.4: {
                hAngle += Meth.deg2rad(r1 * 90 - 45); // map to [-45, 45)
                vAngle += Meth.deg2rad(r2 * 90 - 45); // map to [-45, 45)
                break;
            }
            default: {
                // 90% chance for a small turn
                hAngle += Meth.deg2rad(r1 * 15 - 7.5f); // map to [-7.5, 7.5)
                vAngle += Meth.deg2rad(r2 * 15 - 7.5f); // map to [-7.5, 7.5)
                break;
            }
        }

        // update d vector with the angles

        (float hSin, float hCos) = float.SinCos(hAngle);
        (float vSin, float vCos) = float.SinCos(vAngle);
        d.X = hCos * vCos;
        d.Y = vSin;
        d.Z = hSin * vCos;

        // Keep direction normalized for consistent "speed"
        // var offset = width / 2;
        d.normi();
        d *= width * (1 / 2f);

        // update position
        cx += d.X;
        cy += d.Y;
        cz += d.Z;

        gen: ;

        // actually carve
        var xMin = (int)(cx - width);
        var xMax = (int)(cx + width);
        var yMin = (int)(cy - width);
        var yMax = (int)(cy + width);
        var zMin = (int)(cz - width);
        var zMax = (int)(cz + width);


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

        // create chunk-relative coordinates

        //var icx = (int)cx;
        //var icy = (int)cy;
        //var icz = (int)cz;

        double widthSq = width * width;

        for (int xx = xMin; xx < xMax; xx++) {
            var cxx = xx & 0xF; // chunk-relative x
            for (int zz = zMin; zz < zMax; zz++) {
                var czz = zz & 0xF; // chunk-relative z
                bool hasGrass = false;

                // IMPORTANT do it upside down because the whole grass replacing stuff
                // if you do it the right way around we won't know whether we've ruined terrain or not
                for (int yy = yMax - 1; yy >= yMin; yy--) {
                    // distance check

                    if (!isRoom) {
                        var relativePos = new Vector3((float)(xx - cx), (float)(yy - cy), (float)(zz - cz));
                        // todo if you fuck this up deliberately, you get "rougher" caves
                        // could be a worldgen option?
                        if (relativePos.dot(d) < -0.15) {
                            continue; // Block is behind the direction vector, don't bother
                        }
                    }

                    double dist = (cx - xx) * (cx - xx) +
                                  (cy - yy) * (cy - yy) +
                                  (cz - zz) * (cz - zz);

                    // if it's a room, don't do a circle - make the bottom flat
                    if (isRoom && yy < cy) {
                        dist += (cy - yy) * (cy - yy);
                    }
                    //else {
                    // nothing!
                    //dist += ;
                    //}


                    if (dist < widthSq) {
                        var block = chunk.getBlock(cxx, yy, czz);
                        //if (block == 69 && yy < 60) {
                        //    Console.Out.WriteLine("wtf?");
                        //} 

                        // if it's grass, exchange below
                        if (block == Blocks.GRASS) {
                            hasGrass = true;
                        }

                        // if it's a solid block, remove it
                        if (Block.fullBlock[block]) {
                            chunk.setBlock(cxx, yy, czz, Blocks.AIR);
                            if (hasGrass && chunk.getBlock(cxx, yy - 1, czz) == Blocks.DIRT) {
                                chunk.setBlock(cxx, yy - 1, czz, Blocks.GRASS);
                            }
                        }
                    }
                    // 1750 27 -131
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
        this.isRoom = isRoom;
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
