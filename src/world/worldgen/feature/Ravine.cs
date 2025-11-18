using System.Diagnostics.CodeAnalysis;
using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.chunk;

namespace BlockGame.world.worldgen.feature;

public class Ravine : OverlayFeature {
    public override int radius => 4;

    // Current ravine head position
    public double cx, cy, cz;
    public double hAngle;
    public double baseWidth;
    public int depth;

    // Number of ledges to create
    public int numLedges;
    public int[] ledges = new int[4];

    public override void generate(World world, XRandom rand, ChunkCoord coord, ChunkCoord origin) {

        //var count = 1 / (1 * (freq + 0.05f));

        // 1 in 20 chance for a ravine (orig was 1/15, WAY TOO COMMON, shit should be special)
        if (rand.Next(20) != 0) {
            return;
        }


        
        var x = coord.x * Chunk.CHUNKSIZE + rand.Next(Chunk.CHUNKSIZE);
        var z = coord.z * Chunk.CHUNKSIZE + rand.Next(Chunk.CHUNKSIZE);
        var y = rand.Next(10, World.WORLDHEIGHT - 20);

        genRavine(world, rand, x, y, z, origin);
    }

    private void genRavine(World world, XRandom rand, int x, int y, int z, ChunkCoord origin) {
        cx = x;
        cy = y;
        cz = z;

        hAngle = rand.NextDouble() * Math.PI * 2;
        baseWidth = rand.NextDouble() + 3;
        depth = rand.Next(8, 15);

        numLedges = (int)rand.ApproxGaussian(0.5, 1.5);
        numLedges = int.Clamp(numLedges, 0, 4); // Clamp to 0-4 ledges
        for (var i = 0; i < numLedges; i++) {
            ledges[i] = rand.Next(2, depth - 2);
        }

        var steps = rand.Next(20, 42);

        for (int i = 0; i < steps; i++) {
            genRavineStep(world, rand, i, steps, origin);
        }
    }

    [SuppressMessage("ReSharper", "LocalVariableHidesMember")]
    private void genRavineStep(World world, XRandom rand, int step, int totalSteps, ChunkCoord origin) {
        var cx = this.cx;
        var cy = this.cy;
        var cz = this.cz;
        var hAngle = this.hAngle;
        var baseWidth = this.baseWidth;
        var depth = this.depth;
        var numLedges = this.numLedges;
        var ledges = this.ledges;

        // Vary direction slightly
        double rotationChance = rand.NextDouble();
        if (rotationChance < 0.1) {
            hAngle += Meth.deg2rad(rand.Next(60) - 30);
        }
        else {
            hAngle += Meth.deg2rad(rand.Next(20) - 10);
        }

        // Move mostly horizontally
        var moveDistance = 1.0;
        cx += Math.Cos(hAngle) * moveDistance;
        cz += Math.Sin(hAngle) * moveDistance;
        cy += (rand.NextDouble() - 0.5) * 0.2;

        // Taper at ends
        double widthMultiplier = 1.0;
        /*int taperSteps = totalSteps / 8;
        if (step < taperSteps) {
            widthMultiplier = (double)step / taperSteps;
        }
        else if (step > totalSteps - taperSteps) {
            widthMultiplier = (double)(totalSteps - step) / taperSteps;
        }*/

        // Carve the ravine
        var xMin = cx - baseWidth * widthMultiplier;
        var xMax = cx + baseWidth * widthMultiplier;
        var zMin = cz - baseWidth * widthMultiplier;
        var zMax = cz + baseWidth * widthMultiplier;

        // Check bounds
        if (xMax < origin.x * Chunk.CHUNKSIZE ||
            xMin > (origin.x + 1) * Chunk.CHUNKSIZE ||
            zMax < origin.z * Chunk.CHUNKSIZE ||
            zMin > (origin.z + 1) * Chunk.CHUNKSIZE) {
            goto cleanup;
        }

        // Carve from top down
        var topY = (int)cy;
        var bottomY = Math.Max(2, topY - depth);

        // if the area has water, bail
        if (world.anyWater((int)xMin, bottomY, (int)zMin, (int)xMax, topY, (int)zMax)) {
            return;
        }

        // Cap to chunk boundaries
        xMin = Math.Max(xMin, origin.x * Chunk.CHUNKSIZE);
        xMax = Math.Min(xMax, (origin.x + 1) * Chunk.CHUNKSIZE);
        zMin = Math.Max(zMin, origin.z * Chunk.CHUNKSIZE);
        zMax = Math.Min(zMax, (origin.z + 1) * Chunk.CHUNKSIZE);

        for (int xx = (int)xMin; xx < (int)xMax; xx++) {
            for (int zz = (int)zMin; zz < (int)zMax; zz++) {
                bool hasGrass = false;

                // Carve from top to bottom
                for (int yy = Math.Min(topY, World.WORLDHEIGHT - 1); yy >= bottomY; yy--) {
                    // Calculate ledge width at this depth
                    double ledgeWidth = baseWidth * widthMultiplier;
                    for (int ledgeIndex = 0; ledgeIndex < numLedges; ledgeIndex++) {
                        if (yy == topY - ledges[ledgeIndex] || yy == topY - ledges[ledgeIndex] + 1) {
                            ledgeWidth += 1.5;
                        }
                    }

                    // Distance calculation - cylinder with rounded caps
                    double dist;
                    if (yy < bottomY + 4) {
                        // Near bottom - flattened floor
                        var ccy = bottomY + 2;
                        dist = (cx - xx) * (cx - xx) +
                               ((ccy - yy) * 2) * ((ccy - yy) * 2) +
                               (cz - zz) * (cz - zz);
                    }
                    else if (yy > topY - 3) {
                        var ccy = topY - 2;
                        // Near top - rounded ceiling
                        dist = (cx - xx) * (cx - xx) +
                               (ccy - yy) * (ccy - yy) +
                               (cz - zz) * (cz - zz);
                    }
                    else {
                        // Main body - cylinder (no Y component)
                        dist = (cx - xx) * (cx - xx) +
                               (cz - zz) * (cz - zz);
                    }

                    if (dist < ledgeWidth * ledgeWidth) {
                        var block = world.getBlock(xx, yy, zz);

                        if (block ==  Block.GRASS.id) {
                            hasGrass = true;
                        }

                        if (Block.fullBlock[block]) {
                            // if y low, LAVA TIME
                            if (yy < 12) {
                                world.setBlockDumb(xx, yy, zz,  Block.LAVA.id);
                                // we have to unironically place light here though.
                                world.setBlockLight(xx, yy, zz, Block.lightLevel[Block.LAVA.id]);
                            }
                            else {
                                world.setBlockDumb(xx, yy, zz, Block.AIR.id);
                            }

                            // Replace exposed dirt with grass
                            if (hasGrass && world.getBlock(xx, yy - 1, zz) ==  Block.DIRT.id) {
                                world.setBlockDumb(xx, yy - 1, zz,  Block.GRASS.id);
                            }
                        }
                    }
                }
            }
        }

        cleanup:
        this.cx = cx;
        this.cy = cy;
        this.cz = cz;
        this.hAngle = hAngle;
        this.baseWidth = baseWidth;
        this.depth = depth;
        this.numLedges = numLedges;
        this.ledges = ledges;
    }
}