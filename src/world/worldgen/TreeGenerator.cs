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
                if (x1 == 0 && z1 == 0) {
                    continue;
                }

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
        var oak = new ProceduralTree(world, random, x, y, z, height);
        oak.prepareRound(psiF);
        oak.generate(roots:false, rootButtresses:false);
    }

    public static void placeMapleTree(World world, XRandom random, int x, int y, int z) {
        int height = random.Next(5, 5 + random.Next(3));
        var maple = new ProceduralTree(world, random, x, y, z, height) {
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

        var maple = new ProceduralTree(world, random, x, y, z, height) {
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

    /** place a palm tree with fan-shaped foliage at top */
    public static void placePalmTree(World world, XRandom random, int x, int y, int z, int height = 7) {
        // trunk bottom with logs
        for (int i = 0; i < height; i++) {
            world.setBlockSilent(x, y + i, z, Block.PALM_LOG.id);
        }
        //trunk top with leaves
        for (int i = 0; i <= 1; i++) {
            world.setBlockSilent(x, y + height + i, z, Block.PALM_LEAVES.id);
        }
        
        int h = y + height - 3;
        
        // level 1
        world.setBlockSilent(x - 1, h, z, Block.BANANAFRUIT.id);
        world.setBlockSilent(x + 1, h, z, Block.BANANAFRUIT.id);
        world.setBlockSilent(x, h, z - 1, Block.PALM_LEAVES.id);
        world.setBlockSilent(x, h, z + 1, Block.PALM_LEAVES.id);

        // level 2
        world.setBlockSilent(x, h + 1, z + 2, Block.BANANAFRUIT.id);
        world.setBlockSilent(x - 1, h + 1, z, Block.PALM_LEAVES.id);
        world.setBlockSilent(x + 1, h + 1, z, Block.PALM_LEAVES.id);
        world.setBlockSilent(x, h + 1, z - 1, Block.PALM_LEAVES.id);
        world.setBlockSilent(x, h + 1, z + 1, Block.PALM_LEAVES.id);
        

        // level 3
        for (int xoff = -2; xoff <= -1; xoff++) {
            world.setBlockSilent(x + xoff, h + 2, z, Block.PALM_LEAVES.id);
        }
        for (int xoff = 1; xoff <= 2; xoff++) {
            world.setBlockSilent(x + xoff, h + 2, z, Block.PALM_LEAVES.id);
        }
        for (int zoff = -2; zoff <= -1; zoff++) {
            world.setBlockSilent(x, h + 2, z + zoff, Block.PALM_LEAVES.id);
        }
        for (int zoff = 1; zoff <= 2; zoff++) {
            world.setBlockSilent(x, h + 2, z + zoff, Block.PALM_LEAVES.id);
        }
        
        //level 4
        for (int xoff = -3; xoff <= -2; xoff++) {
            world.setBlockSilent(x + xoff, h + 3, z, Block.PALM_LEAVES.id);
        }
        for (int xoff = 2; xoff <= 3; xoff++) {
            world.setBlockSilent(x + xoff, h + 3, z, Block.PALM_LEAVES.id);
        }
        for (int zoff = -3; zoff <= -2; zoff++) {
            world.setBlockSilent(x, h + 3, z + zoff, Block.PALM_LEAVES.id);
        }
        for (int zoff = 2; zoff <= 3; zoff++) {
            world.setBlockSilent(x, h + 3, z + zoff, Block.PALM_LEAVES.id);
        }
    }
    
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
                    if (xo == 0 && zo == 0 && layer > 0) {
                        continue;
                    }

                    // simple circular shape
                    if (xo * xo + zo * zo > radius * radius) {
                        continue;
                    }

                    // random gaps for naturalness
                    if (random.NextSingle() < 0.2f) {
                        continue;
                    }

                    world.setBlockSilent(x + xo, layerY, z + zo, Block.MAHOGANY_LEAVES.id);
                }
            }
        }
    }

    /** place medium mahogany tree - improved with mid-height branches */
    public static void placeMediumMahogany(World world, XRandom random, int x, int y, int z) {
        int height = random.Next(7, 16);

        var mahogany = new ProceduralTree(world, random, x, y, z, height) {
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

    public static void placeHugeMahogany(World world, XRandom random, int x, int y, int z) {
        int height = random.Next(25, 36);

        var t = new ProceduralTree(world, random, x, y, z, height) {
            trunkThickness = random.NextSingle() * 0.8f + 2.2f,
            foliageDensity = 2.2f,
            branchDensity = 1.4f,
            leafMat = Block.MAHOGANY_LEAVES.id,
            logMat = Block.MAHOGANY_LOG.id
        };
        t.prepareMahoganyHuge();
        t.generate(roots: false, rootButtresses: true);
    }

    /** small fern - 1-2 blocks tall, sparse leaves */
    public static void placeSmallFern(World world, XRandom random, int x, int y, int z) {
        world.setBlockSilent(x, y, z, Block.FERN_LOG.id);

        int topY = y + 1;
        int radius = random.Next(2, 4);

        for (int xo = -radius; xo <= radius; xo++) {
            for (int zo = -radius; zo <= radius; zo++) {
                if (xo == 0 && zo == 0) {
                    continue;
                }

                // rough distance check
                if (xo * xo + zo * zo > radius * radius) {
                    continue;
                }

                if (Block.log[world.getBlock(x + xo, topY, z + zo)]) {
                    continue;
                }

                world.setBlockSilent(x + xo, y, z + zo, Block.MAHOGANY_LEAVES.id);
                world.setBlockSilent(x + xo, topY, z + zo, Block.MAHOGANY_LEAVES.id);
            }
        }

        world.setBlockSilent(x, topY, z, Block.MAHOGANY_LEAVES.id);
    }

    /** dense bush - short but very leafy */
    public static void placeDenseBush(World world, XRandom random, int x, int y, int z) {
        world.setBlockSilent(x, y, z, Block.MAHOGANY_LOG.id);

        for (int h = 0; h <= 2; h++) {
            int layerY = y + h;
            int radius = random.Next(1, 3);

            for (int xo = -radius; xo <= radius; xo++) {
                for (int zo = -radius; zo <= radius; zo++) {
                    if (xo * xo + zo * zo > radius * radius) {
                        continue;
                    }

                    if (Block.log[world.getBlock(x + xo, layerY, z + zo)]) {
                        continue;
                    }

                    world.setBlockSilent(x + xo, layerY, z + zo, Block.MAHOGANY_LEAVES.id);
                }
            }
        }
    }

    /** create a circular cross-section perpendicular to dirAxis */
    private static void crossSection(World world, int cx, int cy, int cz, float radius, int dirAxis, ushort block) {
        int rad = (int)(radius + psiF);
        if (rad <= 0) {
            return;
        }

        for (int off1 = -rad; off1 <= rad; off1++) {
            for (int off2 = -rad; off2 <= rad; off2++) {
                float dist = MathF.Sqrt((MathF.Abs(off1) + 0.5f) * (MathF.Abs(off1) + 0.5f) +
                                        (MathF.Abs(off2) + 0.5f) * (MathF.Abs(off2) + 0.5f));
                if (dist > radius) {
                    continue;
                }

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

        if (maxdist == 0) {
            return;
        }

        // find primary axis (largest delta)
        int primidx;
        if (Math.Abs(delta.X) == maxdist) {
            primidx = 0;
        }
        else if (Math.Abs(delta.Y) == maxdist) {
            primidx = 1;
        }
        else {
            primidx = 2;
        }

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
    private static int cast(World world, float sx, float sy, float sz, float vx, float vy, float vz,
        ushort leafMat, bool wantAir, float limit) {
        float cx = sx + 0.5f;
        float cy = sy + 0.5f;
        float cz = sz + 0.5f;
        int iterations = 0;

        while (iterations < limit) {
            ushort block = world.getBlock((int)cx, (int)cy, (int)cz);
            var hit = wantAir
                ? block == Block.AIR.id
                : block != Block.AIR.id && block != leafMat;
            if (hit) {
                break;
            }

            cx += vx;
            cy += vy;
            cz += vz;
            iterations++;
        }

        return iterations;
    }

    /** procedural tree generator */
    private class ProceduralTree {
        /**
         * max horizontal distance of a foliage cluster from the trunk so we don't go outside the chunk
         */
        private const float MAX_SPREAD = 16 * World.POPULATE_REACH - 5;

        private World world;
        private XRandom random;
        private int x, y, z;
        private int height;

        public float trunkThickness = 1.0f;
        public float foliageDensity = 1.0f;
        public float branchDensity = 1.0f;
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


        private static readonly float[] roundShape = [2f, 3f, 3f, 2.5f, 1.6f];
        private static readonly float[] mahoganyShape = [3.0f, 2.5f, 2.0f, 1.5f];
        private static readonly float[] mapleShape = [2f, 2f, 1f];

        private void prepare(float slope, float radiusMult, float trunkHeightMult, float[] shape,
            Func<ProceduralTree, int, float?> shapeFunc) {
            branchSlope = slope;
            trunkRadius = MathF.Max(1, psiF * MathF.Sqrt(height * trunkThickness) * radiusMult);
            trunkHeight = height * trunkHeightMult;
            foliageShape = shape;

            prepareFoliageClusters(shapeFunc, height);
        }

        /** round deciduous tree */
        public void prepareRound(float trunkHeightMult) {
            prepare(rhoF, 0.8f, trunkHeightMult, roundShape, roundShapeFunc);
        }

        public void prepareMahogany() {
            prepare(1.0f, rhoF, psiF * 0.8f, mahoganyShape, rainforestShapeFunc);
        }

        /** medium mahogany - branches start at 50% height instead of 80% */
        public void prepareMahoganyMedium() {
            prepare(1.0f, rhoF, psiF * 0.7f, mahoganyShape, mahoganyMediumShapeFunc);
        }

        /** huge mahogany */
        public void prepareMahoganyHuge() {
            prepare(1.0f, rhoF, psiF * 0.9f, mahoganyShape, rainforestShapeFunc);
        }

        public void prepareMaple() {
            branchSlope = 0.15f;
            trunkRadius = MathF.Max(1, psiF * MathF.Sqrt(height * trunkThickness) * 0.5f);
            trunkHeight = height;
            foliageShape = mapleShape;

            foliageCords.Clear();
            int numClusters = (int)(foliageDensity * height * 2.5f);
            for (int i = 0; i < numClusters; i++) {
                rand: ;
                // favour lower heights - more bushy at bottom
                float yFac = MathF.Pow(random.NextSingle(), 0.6f); // bias toward 0
                int cy = y + (int)(yFac * height);

                // wider spread lower down
                float maxRadius = (1 - yFac) * height + 0.5f;
                float r = float.Min(MathF.Sqrt(random.NextSingle()) * maxRadius, MAX_SPREAD);
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

            if (adj == 0) {
                return radius * psiF;
            }

            if (MathF.Abs(adj) >= radius) {
                return null;
            }

            float dist = MathF.Sqrt(radius * radius - adj * adj);
            return dist * psiF;
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
            if (radius < 1) {
                return null;
            }

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

        /** prepare foliage cluster positions using shape function */
        private void prepareFoliageClusters(Func<ProceduralTree, int, float?> shapeFunc, int effectiveHeight) {
            foliageCords.Clear();

            int topY = y + effectiveHeight;
            int clustersPerY = (int)(1.5f + MathF.Pow(foliageDensity * height / 19f, 2));
            if (clustersPerY < 1) {
                clustersPerY = 1;
            }

            // iterate from top down, EXCLUDING base
            for (int cy = topY; cy > y; cy--) {
                int yOff = cy - y;
                for (int i = 0; i < clustersPerY; i++) {
                    float? shapeFac = shapeFunc(this, yOff);
                    if (shapeFac == null) {
                        continue;
                    }

                    float r = float.Min((MathF.Sqrt(random.NextSingle()) + 0.328f) * shapeFac.Value, MAX_SPREAD);
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
                        int matDist = cast(world, x, startY, z, vx, vy, vz, leafMat, false, offlength + 3);

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
            if (endRad < 1.0f) {
                endRad = 1.0f;
            }

            if (midRad < endRad) {
                midRad = endRad;
            }

            float startRad = trunkRadius;
            rootBases.Clear();

            // root buttresses
            if (rootButtresses) {
                startRad = trunkRadius * 0.8f;
                rootBases.Add((x, z, startRad));

                float buttressRad = trunkRadius * rhoF;
                float posRadius = trunkRadius;
                int numButtresses = (int)(MathF.Sqrt(trunkRadius) + 3.5f);

                for (int i = 0; i < numButtresses; i++) {
                    float ang = random.NextSingle() * 2 * PI;
                    float thisPosRadius = posRadius * (0.9f + random.NextSingle() * 0.2f);
                    int bx = x + (int)(thisPosRadius * MathF.Sin(ang));
                    int bz = z + (int)(thisPosRadius * MathF.Cos(ang));
                    float thisRad = buttressRad * (psiF + random.NextSingle());
                    if (thisRad < 1.0f) {
                        thisRad = 1.0f;
                    }

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
                if (value < random.NextSingle()) {
                    continue;
                }

                float slope = branchSlope + (0.5f - random.NextSingle()) * 0.16f;

                int branchY;
                float baseSize;

                if (coord.Y - dist * slope > topY) {
                    float threshold = 1f / height;
                    if (random.NextSingle() < threshold) {
                        continue;
                    }

                    branchY = topY;
                    baseSize = endRad;
                }
                else {
                    branchY = (int)(coord.Y - dist * slope);
                    baseSize = endRad + (trunkRadius - endRad) * (topY - branchY) / trunkHeight;
                }

                float startSize = baseSize * (1 + random.NextSingle()) * psiF * MathF.Pow(dist / height, psiF);
                if (startSize < 1.0f) {
                    startSize = 1.0f;
                }

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
                    if (value < random.NextSingle()) {
                        continue;
                    }

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

                    int endx = startx + offx;
                    int endy = starty + offy;
                    int endz = startz + offz;

                    float rootStartSize = rootbaseRadius * psiF * MathF.Abs(offy) / (height * psiF);
                    if (rootStartSize < 1.0f) {
                        rootStartSize = 1.0f;
                    }

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
                        int raydist = startdist + cast(world, searchx, searchy, searchz, vx, vy, vz,
                            leafMat, true, offlength);

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

        }

        /** place a foliage cluster */
        private void placeFoliageCluster(int cx, int cy, int cz) {
            for (int i = 0; i < foliageShape.Length; i++) {
                crossSection(world, cx, cy + i, cz, foliageShape[i], 1, leafMat);
            }
        }
    }
}