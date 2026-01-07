using BlockGame.util;
using BlockGame.world.block;
using Molten;
using static BlockGame.util.Meth;

namespace BlockGame.world.worldgen;

/**
 * A tree generation class implementing Spooner's procedural algorithm.
 * God bless him and his work.
 */
public class TreeGenerator {
    private static ProceduralTree oak;
    private static ProceduralTree maple;
    private static ProceduralTree mahogany;
    private static ProceduralTree palm;

    private const float PI = MathF.PI;

    /** place a simple oak tree - DO NOT TOUCH THIS */
    public static void placeOakTree(World world, XRandom random, int x, int y, int z) {
        int trunkHeight = random.Next(5, 8);

        // trunk
        for (int i = 0; i < trunkHeight; i++) {
            world.setBlockSilent(x, y + i, z, Block.OAK_LOG.id);
        }

        // leaves, thick
        for (int x1 = -2; x1 <= 2; x1++) {
            for (int z1 = -2; z1 <= 2; z1++) {
                if (x1 == 0 && z1 == 0) continue;
                for (int y1 = trunkHeight - 2; y1 <= trunkHeight - 1; y1++) {
                    world.setBlockSilent(x + x1, y + y1, z + z1, Block.LEAVES.id);
                }
            }
        }

        // leaves, thin on top
        for (int x1 = -1; x1 <= 1; x1++) {
            for (int z1 = -1; z1 <= 1; z1++) {
                for (int y1 = trunkHeight; y1 <= trunkHeight + 1; y1++) {
                    world.setBlockSilent(x + x1, y + y1, z + z1, Block.LEAVES.id);
                }
            }
        }
    }

    /** This one is real simple and procedural. */
    public static void placeCandyTree(World world, XRandom random, int x, int y, int z) {
        int trunkHeight = random.Next(4, 6);

        var randomColour = random.Next(0, 16);
        var randomCandy = ((uint)Block.CANDY.id).setMetadata((byte)randomColour);

        // trunk
        for (int i = 0; i < trunkHeight; i++) {
            world.setBlockSilent(x, y + i, z, Block.CANDY.id);
        }

        // top
        for (int x1 = -2; x1 <= 2; x1++) {
            for (int z1 = -2; z1 <= 2; z1++) {
                // skip corners
                if (Math.Abs(x1) == 2 && Math.Abs(z1) == 2) {
                    continue;
                }

                // todo we could skip even more checks and implement a "setBlockMetadataDumb" method but this is fine for now
                world.setBlockMetadataSilent(x + x1, y + trunkHeight, z + z1, randomCandy);
            }
        }
    }

    /** place a fancy tree - uses Spooner's round tree */
    public static void placeFancyTree(World world, XRandom random, int x, int y, int z) {
        int height = random.Next(4, 4 + random.Next(16));
        oak = new ProceduralTree(world, random, x, y, z, height);
        oak.prepareRound(rootButtresses:false, trunkHeightMult:psiF);
        oak.generate(roots:false, rootButtresses:false);
    }

    public static void placeMapleTree(World world, XRandom random, int x, int y, int z) {
        int height = random.Next(5, 5 + random.Next(3));
        maple = new ProceduralTree(world, random, x, y, z, height) {
            trunkThickness = 0.8f,
            foliageDensity = 1.2f,
            branchDensity = 0.0f,
            leafMat = Block.MAPLE_LEAVES.id,
            logMat = Block.MAPLE_LOG.id
        };
        maple.prepareMaple();
        maple.generate(roots:false, rootButtresses:false);
    }

    public static void placeMahoganyTree(World world, XRandom random, int x, int y, int z) {
        int height = random.Next(6, 6 + random.Next(11));

        var t = random.NextSingle() * 1.25f + 1.25f; // trunk thickness 1.25 - 2.5

        maple = new ProceduralTree(world, random, x, y, z, height) {
            trunkThickness = t,
            foliageDensity = 1.5f + t,
            branchDensity = 0.8f + t * 0.5f,
            leafMat = Block.MAHOGANY_LEAVES.id,
            logMat = Block.MAHOGANY_LOG.id
        };
        maple.prepareMahogany();
        maple.generate(roots:false, rootButtresses:false);
    }

    public static void placePineTree(World world, XRandom random, int x, int y, int z) {
        int height = random.Next(6, 10);

        // trunk
        for (int i = 0; i < height; i++) {
            world.setBlockSilent(x, y + i, z, Block.PINE_LOG.id);
        }

        int startY = y + 2;
        int foliageHeight = height - 2;

        for (int dy = 0; dy < foliageHeight; dy++) {
            int currentY = startY + dy;
            // alternate: even layers wide (2), odd layers narrow (1)
            int radius = (dy % 2 == 0) ? 2 : 0;

            // taper at top - reduce radius
            if (dy >= foliageHeight - 2) {
                radius = 1;
            }

            for (int xo = -radius; xo <= radius; xo++) {
                for (int zo = -radius; zo <= radius; zo++) {
                    // skip centre where trunk is
                    if (xo == 0 && zo == 0) {
                        continue;
                    }

                    // skip corners on wide layers for more natural look
                    if (radius == 2 && Math.Abs(xo) == 2 && Math.Abs(zo) == 2) {
                        continue;
                    }

                    world.setBlockSilent(x + xo, currentY, z + zo, Block.PINE_LEAVES.id);
                }
            }
        }

        // pointy top
        world.setBlockSilent(x, y + height, z, Block.PINE_LEAVES.id);
        world.setBlockSilent(x, y + height + 1, z, Block.PINE_LEAVES.id);
    }

    /** place a normal tree with foliage bulb */
    public static void placeNormalTree(World world, XRandom random, int x, int y, int z, int height = 5) {
        // trunk
        for (int i = 0; i < height; i++) {
            world.setBlockSilent(x, y + i, z, Block.OAK_LOG.id);
        }

        // foliage bulb from (top-2) to (top+1)
        int topY = y + height - 1;
        for (int cy = topY - 2; cy < topY + 2; cy++) {
            int rad = (cy > topY - 1) ? 1 : 2;

            for (int xoff = -rad; xoff <= rad; xoff++) {
                for (int zoff = -rad; zoff <= rad; zoff++) {
                    // randomly skip corners
                    if (random.NextSingle() > psiF &&
                        Math.Abs(xoff) == Math.Abs(zoff) &&
                        Math.Abs(xoff) == rad) {
                        continue;
                    }

                    world.setBlockSilent(x + xoff, cy, z + zoff, Block.LEAVES.id);
                }
            }
        }
    }

    /** place a bamboo-style tree with sparse foliage along trunk */
    public static void placeBambooTree(World world, XRandom random, int x, int y, int z, int height = 7) {
        // trunk
        for (int i = 0; i < height; i++) {
            world.setBlockSilent(x, y + i, z, Block.OAK_LOG.id);
        }

        // sparse foliage adjacent to trunk from base to top+1
        for (int cy = y; cy < y + height + 1; cy++) {
            for (int i = 0; i < 2; i++) {
                int xoff = random.Next(0, 2) == 0 ? -1 : 1;
                int zoff = random.Next(0, 2) == 0 ? -1 : 1;
                world.setBlockSilent(x + xoff, cy, z + zoff, Block.LEAVES.id);
            }
        }
    }

    /** place a palm tree with fan-shaped foliage at top */
    public static void placePalmTree(World world, XRandom random, int x, int y, int z, int height = 5) {
        // trunk
        for (int i = 0; i < height; i++) {
            world.setBlockSilent(x, y + i, z, Block.PALM_LOG.id);
        }

        int h = y + height;
        for (int i = 0; i < height + 1; i++) {
            world.setBlockSilent(x, h + i, z, Block.PALM_LEAVES.id);
        }

        // level 1
        world.setBlockSilent(x, h, z + 1, Block.BANANAFRUIT.id);

        // level 2
        for (int xoff = -1; xoff <= 1; xoff++) {
            for (int zoff = -1; zoff <= 1; zoff++) {
                if (Math.Abs(xoff) != Math.Abs(zoff)) {
                    world.setBlockSilent(x + xoff, h + 1, z + zoff, Block.PALM_LEAVES.id);
                }
            }
        }
        world.setBlockSilent(x, h + 1, z - 2, Block.BANANAFRUIT.id);

        // level 3
        world.setBlockSilent(x - 2, h + 2, z, Block.PALM_LEAVES.id);
        world.setBlockSilent(x - 1, h + 2, z, Block.PALM_LOG.id);
        world.setBlockSilent(x, h + 2, z + 1, Block.PALM_LEAVES.id);
        world.setBlockSilent(x, h + 2, z - 1, Block.PALM_LOG.id);
        world.setBlockSilent(x, h + 2, z - 2, Block.PALM_LEAVES.id);
        for (int zoff = -1; zoff <= 0; zoff++) {
            world.setBlockSilent(x + 1, h + 2, z + zoff, Block.PALM_LEAVES.id);
        }

        //level 4
        for (int zoff = -3; zoff <= -2; zoff++) {
            world.setBlockSilent(x, h + 3, z + zoff, Block.PALM_LEAVES.id);
        }
        for (int zoff = 1; zoff <= 2; zoff++) {
            world.setBlockSilent(x, h + 3, z + zoff, Block.PALM_LEAVES.id);
        }
        for (int zoff = -1; zoff <= 0; zoff++) {
            world.setBlockSilent(x + 2, h + 3, z + zoff, Block.PALM_LEAVES.id);
        }
        for (int xoff = -3; xoff <= -2; xoff++) {
            world.setBlockSilent(x + xoff, h + 3, z, Block.PALM_LEAVES.id);
        }
        world.setBlockSilent(x + 1, h + 3, z, Block.PALM_LOG.id);
        world.setBlockSilent(x + 3, h + 3, z, Block.BANANAFRUIT.id);

        // level 5
        for (int xoff = 2; xoff <= 3; xoff++) {
            world.setBlockSilent(x + xoff, h + 4, z, Block.PALM_LEAVES.id);
        }
        world.setBlockSilent(x, h + 4, z - 3, Block.PALM_LEAVES.id);
        world.setBlockSilent(x, h + 4, z + 2, Block.PALM_LEAVES.id);
        world.setBlockSilent(x - 3, h + 4, z, Block.PALM_LEAVES.id);
    }



    /*int h2 = y + height;
        for (int cy = h2 - 2; cy < h2 + 2; cy++) {
            int rad = (cy > topY - 1) ? 1 : 2;

            for (int xoff = -2; xoff <= 2; xoff++) {
                for (int zoff = -2; zoff <= 2; zoff++) {
                if (Math.Abs(xoff) == Math.Abs(zoff)) {
                    world.setBlockSilent(x + xoff, topY, z + zoff, Block.PALM_LEAVES.id);
                }
            }
        }
    }*/


    /** place small mahogany tree - 4-6 blocks, simple round crown */
    public static void placeSmallMahogany(World world, XRandom random, int x, int y, int z) {
        int height = random.Next(4, 7);

        // trunk
        for (int h = 0; h < height; h++) {
            world.setBlockSilent(x, y + h, z, Block.MAHOGANY_LOG.id);
        }

        // simple round crown at top - 2 layers
        for (int layer = 0; layer < 2; layer++) {
            int layerY = y + height - 1 - layer;
            int radius = layer == 0 ? 1 : 2;

            for (int xo = -radius; xo <= radius; xo++) {
                for (int zo = -radius; zo <= radius; zo++) {
                    if (xo == 0 && zo == 0 && layer > 0) continue;

                    // simple circular shape
                    if (xo * xo + zo * zo > radius * radius) continue;

                    // random gaps for naturalness
                    if (random.NextSingle() < 0.2f) continue;

                    world.setBlockSilent(x + xo, layerY, z + zo, Block.MAHOGANY_LEAVES.id);
                }
            }
        }
    }

    /** place medium mahogany tree - improved with mid-height branches */
    public static void placeMediumMahogany(World world, XRandom random, int x, int y, int z) {
        int height = random.Next(7, 16);

        mahogany = new ProceduralTree(world, random, x, y, z, height) {
            trunkThickness = random.NextSingle() * 0.5f + 1.0f, // 1.0 - 1.5
            foliageDensity = 1.5f,
            branchDensity = 1.2f,
            leafMat = Block.MAHOGANY_LEAVES.id,
            logMat = Block.MAHOGANY_LOG.id
        };
        // improved rainforest shape - branches start at 50% height instead of 80%
        mahogany.prepareMahoganyMedium();
        mahogany.generate(roots:false, rootButtresses:false);
    }

    /** place huge mahogany tree - 25-35 blocks, 2x2 trunk, multi-canopy */
    public static void placeHugeMahogany(World world, XRandom random, int x, int y, int z) {
        int height = random.Next(25, 36);

        // 2x2 trunk base
        for (int h = 0; h < height; h++) {
            float taper = 1.0f - (h / (float)height) * 0.3f; // taper from 1.0 to 0.7
            if (taper > 0.8f) {
                // full 2x2 at base
                world.setBlockSilent(x, y + h, z, Block.MAHOGANY_LOG.id);
                world.setBlockSilent(x + 1, y + h, z, Block.MAHOGANY_LOG.id);
                world.setBlockSilent(x, y + h, z + 1, Block.MAHOGANY_LOG.id);
                world.setBlockSilent(x + 1, y + h, z + 1, Block.MAHOGANY_LOG.id);
            }
            else {
                // single trunk higher up
                world.setBlockSilent(x, y + h, z, Block.MAHOGANY_LOG.id);
            }
        }

        // buttress roots at base (4 roots spreading out)
        for (int i = 0; i < 4; i++) {
            float angle = i * MathF.PI / 2f + random.NextSingle() * 0.3f;
            int rootLength = random.Next(3, 6);
            for (int r = 1; r <= rootLength; r++) {
                int rx = x + (int)(MathF.Sin(angle) * r);
                int rz = z + (int)(MathF.Cos(angle) * r);
                int ry = y + rootLength - r; // slope down
                if (ry < y) ry = y;
                world.setBlockSilent(rx, ry, rz, Block.MAHOGANY_LOG.id);
            }
        }

        // multi-layer canopy - 3 levels
        Span<int> canopyHeights = [
            (int)(height * 0.5f), // lower canopy
            (int)(height * 0.7f), // mid canopy
            height - 3            // top canopy
        ];

        foreach (var canopyY in canopyHeights) {
            int canopyRadius = random.Next(4, 7);
            int canopyHeight = random.Next(3, 5);

            for (int dy = 0; dy < canopyHeight; dy++) {
                int layerY = y + canopyY + dy;
                float layerFactor = 1.0f - (dy / (float)canopyHeight);
                int layerRadius = (int)(canopyRadius * layerFactor);

                for (int xo = -layerRadius; xo <= layerRadius; xo++) {
                    for (int zo = -layerRadius; zo <= layerRadius; zo++) {
                        int distSq = xo * xo + zo * zo;
                        if (distSq > layerRadius * layerRadius) {
                            continue;
                        }

                        // check if not trunk
                        int leafX = x + xo;
                        int leafZ = z + zo;
                        if (leafX >= x && leafX <= x + 1 && leafZ >= z && leafZ <= z + 1 && dy < canopyHeight - 1) {
                            continue; // keep trunk clear
                        }

                        world.setBlockSilent(leafX, layerY, leafZ, Block.MAHOGANY_LEAVES.id);
                    }
                }
            }
        }

        // add a few branches connecting canopy layers
        int numBranches = random.Next(4, 8);
        for (int i = 0; i < numBranches; i++) {
            float angle = random.NextSingle() * 2 * MathF.PI;
            int branchLength = random.Next(4, 8);
            int branchStartY = y + random.Next(height / 2, height - 5);

            for (int b = 1; b < branchLength; b++) {
                int bx = x + (int)(MathF.Sin(angle) * b);
                int bz = z + (int)(MathF.Cos(angle) * b);
                int by = branchStartY + b / 3; // slight upward angle
                world.setBlockSilent(bx, by, bz, Block.MAHOGANY_LOG.id);

                // leaves around branch end
                if (b >= branchLength - 2) {
                    for (int xo = -1; xo <= 1; xo++) {
                        for (int zo = -1; zo <= 1; zo++) {
                            world.setBlockSilent(bx + xo, by, bz + zo, Block.MAHOGANY_LEAVES.id);
                        }
                    }
                }
            }
        }
    }

    /** create a circular cross-section perpendicular to dirAxis */
    private static void crossSection(World world, int cx, int cy, int cz, float radius, int dirAxis, ushort block) {
        int rad = (int)(radius + psiF);
        if (rad <= 0) return;

        for (int off1 = -rad; off1 <= rad; off1++) {
            for (int off2 = -rad; off2 <= rad; off2++) {
                float dist = MathF.Sqrt((MathF.Abs(off1) + 0.5f) * (MathF.Abs(off1) + 0.5f) +
                                        (MathF.Abs(off2) + 0.5f) * (MathF.Abs(off2) + 0.5f));
                if (dist > radius) continue;

                int px = cx, py = cy, pz = cz;
                if (dirAxis == 0) {
                    py += off1;
                    pz += off2;
                }
                else if (dirAxis == 1) {
                    px += off1;
                    pz += off2;
                }
                else {
                    px += off1;
                    py += off2;
                }

                world.setBlockSilent(px, py, pz, block);
            }
        }
    }

    /** create a tapered cylinder from (sx,sy,sz) to (ex,ey,ez) */
    private static void taperedCylinder(World world, int sx, int sy, int sz, int ex, int ey, int ez,
        float startSize, float endSize, ushort block) {
        var delta = new Vector3I(ex - sx, ey - sy, ez - sz);
        var maxdist = Math.Max(Math.Abs(delta.X), Math.Max(Math.Abs(delta.Y), Math.Abs(delta.Z)));

        if (maxdist == 0) return;

        // find primary axis (largest delta)
        int primidx;
        if (Math.Abs(delta.X) == maxdist) primidx = 0;
        else if (Math.Abs(delta.Y) == maxdist) primidx = 1;
        else primidx = 2;

        var secidx1 = Meth.mod(primidx - 1, 3);
        var secidx2 = Meth.mod((1 + primidx), 3);
        var primsign = Math.Sign(delta[primidx]);
        var secfac1 = (float)(delta[secidx1]) / delta[primidx];
        var secfac2 = (float)(delta[secidx2]) / delta[primidx];
        var coord = new Vector3I(0, 0, 0);
        var endoffset = delta[primidx] + primsign;

        for (int primoffset = 0; primoffset != endoffset; primoffset += primsign) {
            var start = new Vector3I(sx, sy, sz);
            var primloc = start[primidx] + primoffset;
            var secloc1 = (int)(start[secidx1] + primoffset * secfac1);
            var secloc2 = (int)(start[secidx2] + primoffset * secfac2);
            coord[primidx] = primloc;
            coord[secidx1] = secloc1;
            coord[secidx2] = secloc2;
            var primdist = Math.Abs(delta[primidx]);
            var radius = endSize + (startSize - endSize) * MathF.Abs(primdist - primoffset) / primdist;
            crossSection(world, coord.X, coord.Y, coord.Z, radius, primidx, block);
        }
    }

    /** raycast along vec from start, return distance to first block matching predicate (or limit) */
    private static int distToMat(World world, float sx, float sy, float sz, float vx, float vy, float vz,
        Func<ushort, bool> predicate, float limit) {
        float cx = sx + 0.5f;
        float cy = sy + 0.5f;
        float cz = sz + 0.5f;
        int iterations = 0;

        while (iterations < limit) {
            int bx = (int)cx;
            int by = (int)cy;
            int bz = (int)cz;

            ushort block = world.getBlock(bx, by, bz);
            if (predicate(block)) break;

            cx += vx;
            cy += vy;
            cz += vz;
            iterations++;
        }

        return iterations;
    }

    /** procedural tree generator */
    private class ProceduralTree {
        private World world;
        private XRandom random;
        private int x, y, z;
        private int height;

        public float trunkThickness = 1.0f;
        public float foliageDensity = 1.0f;
        public float branchDensity = 1.0f;
        public bool brokenTrunk = false;
        public bool hollowTrunk = false;
        public bool isMangrove = false;
        public ushort leafMat = Block.LEAVES.id;
        public ushort logMat = Block.OAK_LOG.id;

        private float trunkRadius;
        private float trunkHeight;
        private float branchSlope;
        private float[] foliageShape = null!;
        private readonly List<Vector3I> foliageCords = [];
        private readonly List<(int x, int z, float r)> rootBases = [];

        public ProceduralTree(World world, XRandom random, int x, int y, int z, int height) {
            this.world = world;
            this.random = random;
            this.x = x;
            this.y = y;
            this.z = z;
            this.height = height;
        }

        /** prepare a round deciduous tree */
        public void prepareRound(bool rootButtresses, float trunkHeightMult) {
            branchSlope = rhoF;

            trunkRadius = psiF * MathF.Sqrt(height * trunkThickness) * 0.8f;
            if (trunkRadius < 1) trunkRadius = 1;

            float foliageHeight = height;
            if (brokenTrunk) {
                foliageHeight = height * (0.3f + random.NextSingle() * 0.4f);
            }

            trunkHeight = foliageHeight * trunkHeightMult;
            foliageShape = [2f, 3f, 3f, 2.5f, 1.6f];

            prepareFoliageClusters(roundShapeFunc, (int)(foliageHeight + 0.5f));
        }

        /** prepare a conical pine tree */
        public void prepareCone() {
            branchSlope = 0.15f;
            trunkRadius = psiF * MathF.Sqrt(height * trunkThickness) * 0.5f;
            if (trunkRadius < 1) trunkRadius = 1;

            float foliageHeight = height;
            if (brokenTrunk) {
                foliageHeight = height * (0.3f + random.NextSingle() * 0.4f);
            }

            trunkHeight = foliageHeight;
            foliageShape = [2.5f, 1.6f, 1f];

            prepareFoliageClusters(coneShapeFunc, (int)(foliageHeight + 0.5f));
        }

        /** prepare a really spread-to-the-side maple tree */
        public void prepareMaple() {
            branchSlope = 0.15f;
            trunkRadius = psiF * MathF.Sqrt(height * trunkThickness) * 0.5f;
            if (trunkRadius < 1) trunkRadius = 1;

            float foliageHeight = height;
            if (brokenTrunk) {
                foliageHeight = height * (0.3f + random.NextSingle() * 0.4f);
            }

            trunkHeight = foliageHeight;
            foliageShape = [2f, 2f, 1f];

            // add random bushy clusters
            foliageCords.Clear();
            int numClusters = (int)(foliageDensity * height * 2.5f);
            for (int i = 0; i < numClusters; i++) {
                rand: ;
                // favour lower heights - more bushy at bottom
                float yFac = MathF.Pow(random.NextSingle(), 0.6f); // bias toward 0
                int cy = y + (int)(yFac * foliageHeight);

                // wider spread lower down
                float maxRadius = (1 - yFac) * height + 0.5f;
                float r = MathF.Sqrt(random.NextSingle()) * maxRadius;
                float theta = random.NextSingle() * 2 * PI;
                int cx = (int)(r * MathF.Sin(theta)) + x;
                int cz = (int)(r * MathF.Cos(theta)) + z;

                // don't add it close to the ground!!
                if (cy - y < height * 0.3f) {
                    goto rand;
                }

                foliageCords.Add(new Vector3I(cx, cy, cz));
            }
        }

        public void prepareMahogany() {
            branchSlope = 1.0f;
            trunkRadius = psiF * MathF.Sqrt(height * trunkThickness) * rhoF;
            if (trunkRadius < 1) trunkRadius = 1;

            float foliageHeight = height;
            if (brokenTrunk) {
                foliageHeight = height * (0.3f + random.NextSingle() * 0.4f);
            }

            trunkHeight = foliageHeight * psiF * 0.8f;
            foliageShape = [3.0f, 2.5f, 2.0f, 1.5f];

            prepareFoliageClusters(rainforestShapeFunc, (int)(foliageHeight + 0.5f));
        }

        /** prepare medium mahogany - improved with mid-height branches */
        public void prepareMahoganyMedium() {
            branchSlope = 1.0f;
            trunkRadius = psiF * MathF.Sqrt(height * trunkThickness) * rhoF;
            if (trunkRadius < 1) trunkRadius = 1;

            float foliageHeight = height;
            if (brokenTrunk) {
                foliageHeight = height * (0.3f + random.NextSingle() * 0.4f);
            }

            trunkHeight = foliageHeight * psiF * 0.7f;
            foliageShape = [3.0f, 2.5f, 2.0f, 1.5f];

            // branches start at 50% instead of 80%!
            prepareFoliageClusters(mahoganyMediumShapeFunc, (int)(foliageHeight + 0.5f));
        }

        /** prepare a rainforest tree */
        public void prepareRainforest() {
            branchSlope = 1.0f;
            trunkRadius = psiF * MathF.Sqrt(height * trunkThickness) * rhoF;
            if (trunkRadius < 1) trunkRadius = 1;

            float foliageHeight = height;
            if (brokenTrunk) {
                foliageHeight = height * (0.3f + random.NextSingle() * 0.4f);
            }

            trunkHeight = foliageHeight * psiF * 0.9f;
            foliageShape = [3.4f, 2.6f];

            prepareFoliageClusters(rainforestShapeFunc, (int)(foliageHeight + 0.5f));
        }

        /** prepare a mangrove tree */
        public void prepareMangrove() {
            branchSlope = 1.0f;
            trunkRadius = psiF * MathF.Sqrt(height * trunkThickness) * psiF * 0.8f;
            if (trunkRadius < 1) trunkRadius = 1;

            float foliageHeight = height;
            if (brokenTrunk) {
                foliageHeight = height * (0.3f + random.NextSingle() * 0.4f);
            }

            trunkHeight = foliageHeight * psiF;
            foliageShape = [2f, 3f, 3f, 2.5f, 1.6f];

            prepareFoliageClusters(mangroveShapeFunc, (int)(foliageHeight + 0.5f));
        }

        /** shape function for round trees */
        private static float? roundShapeFunc(ProceduralTree tree, int yOff) {
            if (yOff < tree.height * 0.3f) {
                return null;
            }

            // occasional twigs low down
            if (tree.random.NextSingle() < 100f / (tree.height * tree.height) && yOff < tree.trunkHeight) {
                return tree.height * 0.12f;
            }

            if (yOff < tree.height * (0.282f + 0.1f * MathF.Sqrt(tree.random.NextSingle()))) {
                return null;
            }

            float radius = tree.height / 2f;
            float adj = tree.height / 2f - yOff;

            if (adj == 0) return radius * psiF;
            if (MathF.Abs(adj) >= radius) return null;

            float dist = MathF.Sqrt(radius * radius - adj * adj);
            return dist * psiF;
        }

        /** shape function for conical trees */
        private static float? coneShapeFunc(ProceduralTree tree, int yOff) {
            if (yOff < tree.height * 0.5f) {
                return null;
            }

            // occasional twigs low down
            if (tree.random.NextSingle() < 100f / (tree.height * tree.height) && yOff < tree.trunkHeight) {
                return tree.height * 0.12f;
            }

            if (yOff < tree.height * (0.25f + 0.05f * tree.random.NextSingle() * tree.random.NextSingle())) {
                return null;
            }

            float radius = (tree.height - yOff) * rhoF;
            return radius < 0 ? null : radius;
        }

        /** shape function for maples trees */
        private static float? mapleShapeFunc(ProceduralTree tree, int yOff) {
            if (yOff < tree.height * 0.3f) {
                return null;
            }

            // occasional twigs low down
            if (tree.random.NextSingle() < 100f / (tree.height * tree.height) && yOff < tree.trunkHeight) {
                return tree.height * 0.12f;
            }

            if (yOff < tree.height * (0.25f + 0.05f * MathF.Sqrt(tree.random.NextSingle()))) {
                return null;
            }

            // bushy at bottom, thin top
            yOff -= (int)(tree.height * 0.3f);
            float t = (tree.height - yOff) / (float)tree.height; // 1 at base, 0 at top
            float radius = MathF.Pow(t, 3f) * tree.height * 1.8f;
            if (radius < 1) return null;

            return radius * psiF;
        }

        /** shape function for rainforest trees */
        private static float? rainforestShapeFunc(ProceduralTree tree, int yOff) {
            if (yOff < tree.height * 0.8f) {
                // occasional low twigs only
                if (tree.random.NextSingle() < 100f / (tree.height * tree.height) && yOff < tree.trunkHeight &&
                    tree.random.NextSingle() < 0.07f) {
                    return tree.height * 0.12f;
                }

                return null;
            }

            float width = tree.height * rhoF;
            float topDist = (tree.height - yOff) / (tree.height * 0.2f);
            float dist = width * (psiF + topDist) * (psiF + tree.random.NextSingle()) * rhoF;
            return dist;
        }

        /** shape function for medium mahogany - branches from 50% height */
        private static float? mahoganyMediumShapeFunc(ProceduralTree tree, int yOff) {
            if (yOff < tree.height * 0.5f) {
                // occasional low twigs
                if (tree.random.NextSingle() < 100f / (tree.height * tree.height) && yOff < tree.trunkHeight &&
                    tree.random.NextSingle() < 0.1f) {
                    return tree.height * 0.12f;
                }

                return null;
            }

            float width = tree.height * rhoF;
            float topDist = (tree.height - yOff) / (tree.height * 0.5f);
            float dist = width * (psiF + topDist) * (psiF + tree.random.NextSingle()) * rhoF;
            return dist;
        }

        /** shape function for mangrove trees - wider version of round */
        private static float? mangroveShapeFunc(ProceduralTree tree, int yOff) {
            var val = roundShapeFunc(tree, yOff);
            if (val == null) return null;
            return val.Value * phiF;
        }

        /** prepare foliage cluster positions using shape function */
        private void prepareFoliageClusters(Func<ProceduralTree, int, float?> shapeFunc, int effectiveHeight) {
            foliageCords.Clear();

            int topY = y + effectiveHeight;
            int clustersPerY = (int)(1.5f + MathF.Pow(foliageDensity * height / 19f, 2));
            if (clustersPerY < 1) clustersPerY = 1;

            // iterate from top down, EXCLUDING base
            for (int cy = topY; cy > y; cy--) {
                int yOff = cy - y;
                for (int i = 0; i < clustersPerY; i++) {
                    float? shapeFac = shapeFunc(this, yOff);
                    if (shapeFac == null) continue;

                    float r = (MathF.Sqrt(random.NextSingle()) + 0.328f) * shapeFac.Value;
                    float theta = random.NextSingle() * 2 * PI;
                    int cx = (int)(r * MathF.Sin(theta)) + x;
                    int cz = (int)(r * MathF.Cos(theta)) + z;

                    // collision check: raycast from branch start to cluster position
                    float dist = MathF.Sqrt((cx - x) * (cx - x) + (cz - z) * (cz - z));
                    int trunkTopY = y + (int)(trunkHeight + 0.5f);

                    // determine where branch would start
                    int startY;
                    if (cy - dist * branchSlope > trunkTopY) {
                        startY = trunkTopY;
                    }
                    else {
                        startY = (int)(cy - dist * branchSlope);
                    }

                    // raycast from branch start to cluster
                    float offx = cx - x;
                    float offy = cy - startY;
                    float offz = cz - z;
                    float offlength = MathF.Sqrt(offx * offx + offy * offy + offz * offz);

                    if (offlength >= 1) {
                        float vx = offx / offlength;
                        float vy = offy / offlength;
                        float vz = offz / offlength;

                        // check for solid blocks (anything not air/leaves)
                        int matDist = distToMat(world, x, startY, z, vx, vy, vz, b => b != Block.AIR.id && b != leafMat, offlength + 3);

                        // skip this cluster if we hit terrain before reaching it
                        if (matDist < offlength + 2) {
                            continue;
                        }
                    }

                    foliageCords.Add(new Vector3I(cx, cy, cz));
                }
            }
        }

        /** generate the tree */
        public void generate(bool roots, bool rootButtresses) {
            // normalize branch density by foliage density
            var normBranchDens = branchDensity / foliageDensity;

            // foliage first
            foreach (var coord in foliageCords) {
                placeFoliageCluster(coord.X, coord.Y, coord.Z);
            }

            // trunk and branches
            int topY = y + (int)(trunkHeight + 0.5f);
            int midY = y + (int)(trunkHeight * rhoF);

            float endSizeFactor = trunkHeight / height;
            float midRad = trunkRadius * (1 - endSizeFactor * 0.5f);
            float endRad = trunkRadius * (1 - endSizeFactor);
            if (endRad < 1.0f) endRad = 1.0f;
            if (midRad < endRad) midRad = endRad;

            float startRad = trunkRadius;
            rootBases.Clear();

            // root buttresses
            if (rootButtresses) {
                startRad = trunkRadius * 0.8f;
                rootBases.Add((x, z, startRad));

                float buttressRad = trunkRadius * rhoF;
                // normal trees use trunkRadius, mangrove extends 2.618x
                float posRadius = isMangrove ? trunkRadius * (phiF + 1) : trunkRadius;
                int numButtresses = (int)(MathF.Sqrt(trunkRadius) + 3.5f);

                for (int i = 0; i < numButtresses; i++) {
                    float ang = random.NextSingle() * 2 * PI;
                    float thisPosRadius = posRadius * (0.9f + random.NextSingle() * 0.2f);
                    int bx = x + (int)(thisPosRadius * MathF.Sin(ang));
                    int bz = z + (int)(thisPosRadius * MathF.Cos(ang));
                    float thisRad = buttressRad * (psiF + random.NextSingle());
                    if (thisRad < 1.0f) thisRad = 1.0f;

                    taperedCylinder(world, bx, y, bz, x, midY, z, thisRad, thisRad, logMat);
                    rootBases.Add((bx, bz, thisRad));
                }
            }
            else {
                rootBases.Add((x, z, startRad));
            }

            // main trunk
            taperedCylinder(world, x, y, z, x, midY, z, startRad, midRad, logMat);
            taperedCylinder(world, x, midY, z, x, topY, z, midRad, endRad, logMat);

            // branches
            foreach (var coord in foliageCords) {
                float dist = MathF.Sqrt((coord.X - x) * (coord.X - x) + (coord.Z - z) * (coord.Z - z));
                float ydist = coord.Y - y;

                float value = (normBranchDens * 220 * height) / MathF.Pow(ydist + dist, 3);
                if (value < random.NextSingle()) continue;

                float slope = branchSlope + (0.5f - random.NextSingle()) * 0.16f;

                int branchY;
                float baseSize;

                if (coord.Y - dist * slope > topY) {
                    float threshold = 1f / height;
                    if (random.NextSingle() < threshold) continue;
                    branchY = topY;
                    baseSize = endRad;
                }
                else {
                    branchY = (int)(coord.Y - dist * slope);
                    baseSize = endRad + (trunkRadius - endRad) * (topY - branchY) / trunkHeight;
                }

                float startSize = baseSize * (1 + random.NextSingle()) * psiF * MathF.Pow(dist / height, psiF);
                if (startSize < 1.0f) startSize = 1.0f;

                float rndr = MathF.Sqrt(random.NextSingle()) * baseSize * psiF;
                float rndang = random.NextSingle() * 2 * PI;
                int rndx = (int)(rndr * MathF.Sin(rndang) + 0.5f);
                int rndz = (int)(rndr * MathF.Cos(rndang) + 0.5f);

                taperedCylinder(world, x + rndx, branchY, z + rndz, coord.X, coord.Y, coord.Z,
                    startSize, 1.0f, logMat);
            }

            // roots with proper collision detection
            if (roots) {
                foreach (var coord in foliageCords) {
                    float dist = MathF.Sqrt((coord.X - x) * (coord.X - x) + (coord.Z - z) * (coord.Z - z));
                    float ydist = coord.Y - y;

                    float value = (normBranchDens * 220 * height) / MathF.Pow(ydist + dist, 3);
                    if (value < random.NextSingle()) continue;

                    var rootBase = rootBases[random.Next(rootBases.Count)];
                    int rootx = rootBase.x;
                    int rootz = rootBase.z;
                    float rootbaseRadius = rootBase.r;

                    float rndr = MathF.Sqrt(random.NextSingle()) * rootbaseRadius * psiF;
                    float rndang = random.NextSingle() * 2 * PI;
                    int rndx = (int)(rndr * MathF.Sin(rndang) + 0.5f);
                    int rndz = (int)(rndr * MathF.Cos(rndang) + 0.5f);
                    int rndy = (int)(random.NextSingle() * rootbaseRadius * 0.5f);

                    int startx = rootx + rndx;
                    int starty = y + rndy;
                    int startz = rootz + rndz;

                    int offx = startx - coord.X;
                    int offy = starty - coord.Y;
                    int offz = startz - coord.Z;

                    // mangrove roots are 1.618x longer
                    if (isMangrove) {
                        offx = (int)(offx * phiF - 1.5f);
                        offy = (int)(offy * phiF - 1.5f);
                        offz = (int)(offz * phiF - 1.5f);
                    }

                    int endx = startx + offx;
                    int endy = starty + offy;
                    int endz = startz + offz;

                    float rootStartSize = rootbaseRadius * psiF * MathF.Abs(offy) / (height * psiF);
                    if (rootStartSize < 1.0f) rootStartSize = 1.0f;

                    // hanging roots: raycast to find where they hit air, then hang down
                    float offlength = MathF.Sqrt(offx * offx + offy * offy + offz * offz);
                    if (offlength >= 1) {
                        float vx = offx / offlength;
                        float vy = offy / offlength;
                        float vz = offz / offlength;

                        int startdist = (int)(random.NextSingle() * 3.6f * MathF.Sqrt(rootStartSize) + 2.8f);
                        float searchx = startx + startdist * vx;
                        float searchy = starty + startdist * vy;
                        float searchz = startz + startdist * vz;

                        // search for air blocks (hanging roots)
                        int raydist = startdist + distToMat(world, searchx, searchy, searchz, vx, vy, vz,
                            static b => b == Block.AIR.id, offlength);

                        if (raydist < offlength) {
                            // found air, root stops here then hangs down
                            float rootMid = 1.0f + (rootStartSize - 1.0f) * (1 - raydist / offlength);
                            int midx = (int)(startx + vx * raydist);
                            int midy = (int)(starty + vy * raydist);
                            int midz = (int)(startz + vz * raydist);

                            // remaining distance hangs straight down
                            float remainingDist = offlength - raydist;
                            int bottomy = midy - (int)remainingDist;

                            // angled part to air
                            taperedCylinder(world, startx, starty, startz, midx, midy, midz,
                                rootStartSize, rootMid, logMat);
                            // hanging part straight down
                            taperedCylinder(world, midx, midy, midz, midx, bottomy, midz,
                                rootMid, 1.0f, logMat);
                        }
                        else {
                            // no air found, root goes all the way
                            taperedCylinder(world, startx, starty, startz, endx, endy, endz,
                                rootStartSize, 1.0f, logMat);
                        }
                    }
                }
            }

            // hollow trunk with proper wall thickness and tapering
            if (hollowTrunk && trunkRadius > 2) {
                float wallThickness = 1 + trunkRadius * 0.1f * random.NextSingle();
                if (wallThickness < 1.3f) wallThickness = 1.3f;

                float baseRadius = trunkRadius - wallThickness;
                if (baseRadius < 1) baseRadius = 1.0f;
                float midRadius = midRad - wallThickness;
                float topRadius = endRad - wallThickness;

                // offset for asymmetric hollow
                int baseOffset = (int)wallThickness;
                int startX = x + random.Next(-baseOffset, baseOffset + 1);
                int startZ = z + random.Next(-baseOffset, baseOffset + 1);

                // hollow out bottom and middle sections with taper
                taperedCylinder(world, startX, y, startZ, x, midY, z, baseRadius, midRadius, Block.AIR.id);

                // extend hollow above trunk top
                int hollowTopY = (int)(topY + trunkRadius + 1.5f);
                taperedCylinder(world, x, midY, z, x, hollowTopY, z, midRadius, topRadius, Block.AIR.id);
            }
        }

        /** place a foliage cluster */
        private void placeFoliageCluster(int cx, int cy, int cz) {
            for (int i = 0; i < foliageShape.Length; i++) {
                crossSection(world, cx, cy + i, cz, foliageShape[i], 1, leafMat);
            }
        }
    }
}