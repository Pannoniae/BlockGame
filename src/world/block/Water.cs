using System.Numerics;
using BlockGame.GL.vertexformats;
using Molten;

namespace BlockGame.util;

/**
 * TODO this will be generic liquid later or something, we'll have other liquids like lava too!
 */
public class Water : Block {
    private readonly byte maxFlow;

    /**
     * tickRate = how many ticks between updates (lower = faster flow)
     * maxFlow = maximum flow distance from source (not currently used)
     */
    public Water(ushort id, string name, byte tickRate, byte maxFlow) : base(id, name) {
        lightAbsorption[id] = 1;
        renderType[id] = RenderType.CUSTOM;
        customCulling[id] = true;
        updateDelay[id] = tickRate;
        this.maxFlow = maxFlow;
    }

    /**
     * Metadata encoding for water:
     * Bits 0-2: 0=source, 1-7=flowing water levels (1=full flow, 7=nearly empty)
     * Bit 3: Falling flag (1=falling, 0=not falling)
     * Bit 4: Dynamic flag (1=actively updating, 0=static)
     * Bits 5-7: Reserved
     */
    public static byte getWaterLevel(byte metadata) => (byte)(metadata & 0x7);

    public static bool isFalling(byte metadata) => (metadata & 0x08) != 0;
    public static bool isDynamic(byte metadata) => (metadata & 0x10) != 0;
    public static byte setWaterLevel(byte metadata, byte level) => (byte)((metadata & 0xF8) | (level & 0x7));
    public static byte setFalling(byte metadata, bool falling) => (byte)((metadata & 0xF7) | (falling ? 0x08 : 0));
    public static byte setDynamic(byte metadata, bool dynamic) => (byte)((metadata & 0xEF) | (dynamic ? 0x10 : 0));


    public override void update(World world, int x, int y, int z) {
        var data = world.getBlockMetadata(x, y, z);
        var pos = new Vector3I(x, y, z);

        // if static water is being updated externally, wake it up
        if (!isDynamic(data)) {
            wakeUpWater(world, pos);
            // don't worry it will update next tick!
        }
    }


    /**
     * This method has been in DIRE need of a rework. So here it is.
     * This is a piece of shit method which breaks, gets into an infinite loop, blows your house up, etc. if you sneeze on it.
     * I know that Rider complains that half the method is logically impossible, that the expression is always true, etc.
     * That's right, but I'm afraid to touch it now, it's so cursed.
     * Only touch if you're smarter than me.
     */
    public override void scheduledUpdate(World world, int x, int y, int z) {
        var data = world.getBlockMetadata(x, y, z);
        var pos = new Vector3I(x, y, z);

        int currentLevel = getWaterLevel(data);
        // includes falling!
        currentLevel = setFalling((byte)currentLevel, isFalling(data));
        var falling = isFalling(data);


        bool newFalling = false;
        int newLevel;

        // is this a source? (any kind)
        // if falling, update it anyway because we might have to remove
        // is this a source? great, sleep well
        if (getWaterLevel((byte)currentLevel) == 0 && !falling) {
            data = setDynamic(data, false);
            world.setBlockMetadataSilent(pos.X, pos.Y, pos.Z, ((uint)Blocks.WATER).setMetadata(data));
            newLevel = 0;
            // DON'T RETURN WE CAN STILL SPREAD
        }
        else {
            var bestLevel = 999;
            bool hasFalling = false;
            // find the highest water level (lowest number, since 0=source has most water)
            var w1 = getWater(world, x - 1, y, z, ref hasFalling);
            var w2 = getWater(world, x + 1, y, z, ref hasFalling);
            var w3 = getWater(world, x, y, z - 1, ref hasFalling);
            var w4 = getWater(world, x, y, z + 1, ref hasFalling);

            if (w1 >= 0) bestLevel = Math.Min(bestLevel, w1);
            if (w2 >= 0) bestLevel = Math.Min(bestLevel, w2);
            if (w3 >= 0) bestLevel = Math.Min(bestLevel, w3);
            if (w4 >= 0) bestLevel = Math.Min(bestLevel, w4);

            // no water neighbors found?
            if (bestLevel == 999) {
                // do we still need this? IDK
                newLevel = -1;
            }

            // EXCEPTION: if one of the neighbours is falling water with data 7, give it one more chance so it doesn't look shite
            if (hasFalling && bestLevel == maxFlow - 1) {
                bestLevel = maxFlow - 2;
            }

            newLevel = bestLevel + 1;
            if (newLevel >= maxFlow) {
                newLevel = -1; // too weak to exist
            }

            // check above
            var above = world.getBlockRaw(x, y + 1, z);
            // if there is water above, this should be falling
            // WE DON'T USE GETWATER because that swallows the level for falling water...
            if (above.getID() == Blocks.WATER) {
                byte aboveMetadata = getWaterLevel(above.getMetadata());

                // if above is falling, become falling
                newLevel = setFalling(aboveMetadata, true);

                newFalling = true;
            }

            // check for adjacent sources, if 2+ then become source
            int adjacentSources = 0;
            foreach (var dir in Direction.directionsHorizontal) {
                var neighbor = pos + dir;
                if (world.getBlock(neighbor) == Blocks.WATER) {
                    var neighborMetadata = world.getBlockMetadata(neighbor);
                    // if neighbor is a source and not falling (not sure how the second one could happen but whatever)
                    if (getWaterLevel(neighborMetadata) == 0 && !isFalling(neighborMetadata)) {
                        // source block
                        adjacentSources++;
                    }
                }
            }

            if (adjacentSources >= 2) {
                newLevel = 0; // become source
            }

            // if no change needed
            // sadly this is a bit more complex because we have schizophrenia in mixing up metadata, water level, falling, etc.
            // this is ALREADY a fucking mess but oh well
            // newLevel is water level + falling
            if (newLevel == currentLevel) {
                // go to sleep, goodnight!
                data = setDynamic(data, false);
                world.setBlockMetadataSilent(pos.X, pos.Y, pos.Z, ((uint)Blocks.WATER).setMetadata(data));
            }
            else {
                // update current level
                currentLevel = newLevel;
                // if newLevel is -1, remove
                if (newLevel == -1) {
                    world.setBlockRemesh(pos.X, pos.Y, pos.Z, Blocks.AIR);
                }
                else {
                    data = setWaterLevel(data, (byte)newLevel);
                    data = setDynamic(data, true); // keep dynamic while changing
                    data = setFalling(data, newFalling);
                    world.setBlockMetadata(pos.X, pos.Y, pos.Z, ((uint)Blocks.WATER).setMetadata(data));

                    world.scheduleBlockUpdate(pos);
                    // update neighbours
                    // todo do we really tho? it doesn't seem to break if we do, but also if we don't?
                    // revisit this when more survival shit is added and see if stuff breaks hahahaha
                    world.blockUpdateNeighboursOnly(pos.X, pos.Y, pos.Z);
                }
            }
        }

        // Step 10-12: Spreading logic
        // where's the other steps? I rewrote the whole thing 4 times already, they've disappeared, sorry
        if (currentLevel == -1) {
            return; // Can't spread if evaporated
        }

        // okay, time to actually spread!
        // first, down
        // sadly currentLevel seems to be -1 sometimes?
        // hackjob time!
        // this is outdated but I'm afraid to touch it
        var fallingLevel = currentLevel >= 0 ? currentLevel : 9;
        if (canSpread(world, x, y - 1, z)) {
            // force-fall
            spread(world, x, y - 1, z, (byte)(fallingLevel & 0x7), true);
        }

        // didn't spread down? good, we can try sides
        else {
            // if == -1 now, we're gone
            if (currentLevel == -1) {
                return;
            }

            // only do the sides if this is either a source or there's something under it (if it can flow down, then do that instead of going to the sides lol)
            // if it's NOT solid below, don't even bother (otherwise the fucking waterfalls will go off to the side endlessly because there's water below)
            var isSolid = fullBlock[world.getBlock(x, y - 1, z)];
            if (currentLevel == 0 || (isSolid && !canSpread(world, x, y - 1, z))) {
                // if it's falling, it should be at least level 1 when spreading sideways
                // todo is this really necessary?

                var isFalling = Water.isFalling((byte)currentLevel);

                // alternatively, chop it down!
                currentLevel = setFalling((byte)currentLevel, false);

                newLevel = currentLevel + 1;

                // EXCEPTION: if falling water with data 7, give it one more chance so it doesn't look shite
                if (isFalling && currentLevel == maxFlow - 1) {
                    newLevel = maxFlow - 1;
                }

                // if >= max, don't bother
                if (newLevel >= maxFlow) {
                    return;
                }

                if (canSpread(world, x - 1, y, z)) {
                    spread(world, x - 1, y, z, (byte)newLevel, false);
                }

                if (canSpread(world, x + 1, y, z)) {
                    spread(world, x + 1, y, z, (byte)newLevel, false);
                }

                if (canSpread(world, x, y, z - 1)) {
                    spread(world, x, y, z - 1, (byte)newLevel, false);
                }

                if (canSpread(world, x, y, z + 1)) {
                    spread(world, x, y, z + 1, (byte)newLevel, false);
                }
            }
        }
    }

    /**
     * Get the water level for this block. -1 if no water :(
     */
    public int getWater(World world, int x, int y, int z, ref bool hasFalling) {
        var block = world.getBlock(x, y, z);
        if (block == Blocks.WATER) {
            var data = world.getBlockMetadata(x, y, z);

            // if falling water, always full height
            // not anymore, we want to preserve the water volume!
            if (isFalling(data)) {
                hasFalling = true;
                return getWaterLevel(data);
            }

            // if source, full height
            if (getWaterLevel(data) == 0) {
                return 0;
            }

            return getWaterLevel(data);
        }

        return -1;
    }

    /**
     * Why is this necessary to be separate? I DON'T KNOW
     * however, this is only used by the rendering! otherwise falling water is treated as source which is no good.
     */
    public int getRenderWater(World world, int x, int y, int z) {
        var block = world.getBlock(x, y, z);
        if (block == Blocks.WATER) {
            var data = world.getBlockMetadata(x, y, z);

            // if falling water, always full height
            if (isFalling(data)) {
                return 0;
            }

            // if source, full height
            if (getWaterLevel(data) == 0) {
                return 0;
            }

            return getWaterLevel(data);
        }

        return -1;
    }

    /**
     * Get how much liquid this water block has.
     */
    public float getHeight(byte data) {
        // if falling water, always full height
        if (isFalling(data)) {
            return 1.0f;
        }

        data = getWaterLevel(data);
        // if full, return 15/16
        if (data == 0) {
            return 15 / 16f;
        }

        return 1.0f - data * 0.125f;
    }

    /**
     * What's the height of the water at this vertex?
     * x = -1..1 (corner x)
     * z = -1..1 (corner z)
     */
    public float getRenderHeight(World world, int x, int y, int z, sbyte ox, sbyte oz) {
        // if there's water above ANY of the 4 corner blocks, return full height
        if (getRenderWater(world, x, y + 1, z) >= 0 ||
            getRenderWater(world, x + ox, y + 1, z) >= 0 ||
            getRenderWater(world, x, y + 1, z + oz) >= 0 ||
            getRenderWater(world, x + ox, y + 1, z + oz) >= 0) {
            return 1.0f;
        }

        float totalHeight = 0;
        int samples = 0;

        // sample the 4 blocks around this corner
        totalHeight += sampleBlockHeight(world, x, y, z, ref samples);
        totalHeight += sampleBlockHeight(world, x + ox, y, z, ref samples);
        totalHeight += sampleBlockHeight(world, x, y, z + oz, ref samples);
        totalHeight += sampleBlockHeight(world, x + ox, y, z + oz, ref samples);

        if (samples == 0) {
            return 0; // no water or air found, solid blocks only
        }

        return totalHeight / samples;
    }

    private float sampleBlockHeight(World world, int x, int y, int z, ref int samples) {
        var block = world.getBlock(x, y, z);
        if (block == Blocks.WATER) {
            var data = world.getBlockMetadata(x, y, z);
            samples++;
            return getHeight(data);
        }

        if (!fullBlock[block]) {
            // air or non-full block contributes 0 height
            // todo is this right?
            samples++;
            return 0f;
        }

        // solid blocks don't contribute to samples
        return 0.0f;
    }

    public override void interact(World world, int x, int y, int z, Entity e) {
        e.inLiquid = true;
        // push entity in flow direction
        var flow = getFlow(world, x, y, z);
        if (flow != Vector3.Zero) {
            flow = Vector3.Normalize(flow) * 0.04f; // tweak strength here?
            e.velocity += flow.toVec3D(); // small push
        }
    }

    /**
     * Get the flow vector for this water block.
     * This is used for rendering flow direction and for pushing entities.
     */
    public Vector3 getFlow(World world, int x, int y, int z) {
        var block = world.getBlock(x, y, z);
        if (block != Blocks.WATER) {
            return Vector3.Zero;
        }

        var metadata = world.getBlockMetadata(x, y, z);
        var currentHeight = getHeight(metadata);
        var flow = Vector3.Zero;

        // Check all 6 directions for height differences
        // Down
        var blockBelow = world.getBlock(x, y - 1, z);
        if (blockBelow == Blocks.AIR || (blockBelow == Blocks.WATER && getRenderWater(world, x, y - 1, z) > 0)) {
            flow.Y -= 1.0f; // Strong downward flow
        }

        // Horizontal directions
        Vector3I[] horizontalDirs = [
            new Vector3I(-1, 0, 0), new Vector3I(1, 0, 0),
            new Vector3I(0, 0, -1), new Vector3I(0, 0, 1)
        ];

        foreach (var dir in horizontalDirs) {
            var neighborBlock = world.getBlock(x + dir.X, y + dir.Y, z + dir.Z);

            if (neighborBlock == Blocks.AIR) {
                // Flow toward empty space
                flow += new Vector3(dir.X * 0.5f, dir.Y * 0.5f, dir.Z * 0.5f);
            }
            else if (neighborBlock == Blocks.WATER) {
                var neighborWater = getRenderWater(world, x + dir.X, y + dir.Y, z + dir.Z);
                if (neighborWater > 0) {
                    var neighborHeight = 1.0f - neighborWater * 0.125f;
                    var heightDiff = currentHeight - neighborHeight;
                    if (heightDiff > 0) {
                        // Flow from high to low
                        flow += new Vector3(dir.X * heightDiff, 0, dir.Z * heightDiff);
                    }
                }
            }
        }

        // Check if water above is pushing down
        if (world.getBlock(x, y + 1, z) == Blocks.WATER) {
            flow.Y -= 0.5f;
        }

        return flow;
    }

    public bool canSpread(World world, int x, int y, int z) {
        var block = world.getBlock(x, y, z);

        if (block == Blocks.AIR) {
            return true; // can always spread to air
        }

        if (block == Blocks.WATER) {
            // can't spread to water, can you? :)
            return false;
        }

        return !fullBlock[block]; // other non-full blocks
    }

    public void spread(World world, int x, int y, int z, byte level, bool falling) {
        // actually do it!
        var metadata = setWaterLevel(0, level);
        metadata = setFalling(metadata, falling);
        metadata = setDynamic(metadata, true);
        world.setBlockMetadata(x, y, z, ((uint)Blocks.WATER).setMetadata(metadata));

        // tick it
        world.scheduleBlockUpdate(new Vector3I(x, y, z));
    }

    public void wakeUpWater(World world, Vector3I pos) {
        if (world.getBlock(pos) == Blocks.WATER) {
            var metadata = world.getBlockMetadata(pos);
            // wake up static water
            var dynamicMetadata = setDynamic(metadata, true);
            // no updates!!
            world.setBlockMetadataSilent(pos.X, pos.Y, pos.Z, ((uint)Blocks.WATER).setMetadata(dynamicMetadata));

            // scheduleBlockUpdate handles duplicate protection internally
            world.scheduleBlockUpdate(pos);
        }
    }


    /** Water doesn't get rendered next to water, but always gets rendered on the top face */
    public override bool cullFace(BlockRenderer br, int x, int y, int z, RawDirection dir) {
        var direction = Direction.getDirection(dir);
        var same = br.getBlockCached(direction.X, direction.Y, direction.Z).getID() == br.getBlock().getID();
        if (same) {
            return false;
        }

        var notTransparent = !transparent[br.getBlockCached(direction.X, direction.Y, direction.Z)];

        return dir == RawDirection.UP || (notTransparent && base.cullFace(br, x, y, z, dir));
    }

    public override void render(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices) {
        base.render(br, x, y, z, vertices);

        var block = br.getBlock();
        var metadata = block.getMetadata();
        var level = getWaterLevel(metadata);
        var falling = isFalling(metadata);

        // Calculate corner heights for sloped rendering
        var world = br.world;
        var h00 = getRenderHeight(world, x, y, z, -1, -1); // southwest
        var h10 = getRenderHeight(world, x, y, z, 1, -1); // southeast
        var h01 = getRenderHeight(world, x, y, z, -1, 1); // northwest
        var h11 = getRenderHeight(world, x, y, z, 1, 1); // northeast

        // Calculate flow direction for UV rotation
        var flow = getFlow(world, x, y, z);
        float flowAngle = 0;
        if (flow.X != 0 || flow.Z != 0) {
            flowAngle = MathF.Atan2(flow.Z, flow.X);
        }

        float flowCos = MathF.Cos(flowAngle);
        // why negative? because UV Y axis is inverted
        float flowSin = -MathF.Sin(flowAngle);

        // Use center 16x16 of the 32x32 flowing texture
        var flowingUV = uvs[1] + 0.25f; // offset to center 16x16
        const float flowingUVSize = 1f;

        // Get texture coordinates for water
        var texMin = (level > 0 || falling) ? flowingUV : uvs[0];
        var texMax = (level > 0 || falling) ? flowingUV + flowingUVSize : uvs[0] + 1;
        var min = texCoords(texMin.u, texMin.v);
        var max = texCoords(texMax.u, texMax.v);

        var uMin = min.X;
        var vMin = min.Y;
        var uMax = max.X;
        var vMax = max.Y;
        
        // Cache v range for interpolation
        var vRange = vMax - vMin;

        Span<BlockVertexPacked> cache = stackalloc BlockVertexPacked[4];
        Span<Vector4> colourCache = stackalloc Vector4[4];
        Span<byte> lightColourCache = stackalloc byte[4];

        x &= 15;
        y &= 15;
        z &= 15;

        // Calculate UVs relative to center for rotation
        float uCenter = (uMin + uMax) * 0.5f;
        float vCenter = (vMin + vMax) * 0.5f;

        // --- Top Face UV Rotation ---
        float u0 = uMin - uCenter;
        float u1 = uMax - uCenter;
        float v0 = vMin - vCenter;
        float v1 = vMax - vCenter;
        
        var uv00 = rotateUV(u0, v1, flowCos, flowSin) + new Vector2(uCenter, vCenter);
        var uv01 = rotateUV(u0, v0, flowCos, flowSin) + new Vector2(uCenter, vCenter);
        var uv10 = rotateUV(u1, v0, flowCos, flowSin) + new Vector2(uCenter, vCenter);
        var uv11 = rotateUV(u1, v1, flowCos, flowSin) + new Vector2(uCenter, vCenter);

        // --- Side Face UV Rotation (-90 degrees) ---
        // Transformation: T(u,v) = (v - vCenter + uCenter, -u + uCenter + vCenter)
        float uNewSideOffset = uCenter - vCenter;
        float vNewSideOffset = uCenter + vCenter;
        
        float vNewSideUMin = -uMin + vNewSideOffset;
        float vNewSideUMax = -uMax + vNewSideOffset;

        for (RawDirection d = 0; d < RawDirection.MAX; d++) {
            if (cullFace(br, x, y, z, d)) {
                br.applyFaceLighting(d, colourCache, lightColourCache);

                // if dynamic, tint red (debug)
                if (isDynamic(metadata)) {
                    for (int i = 0; i < 4; i++) {
                        colourCache[i] = new Vector4(1.0f, 0.5f, 0.5f, 1.0f);
                    }
                }

                br.begin(cache);

                switch (d) {
                    case RawDirection.WEST:
                        br.vertex(x + 0, y + h01, z + 1, (vMax - vRange * h01) + uNewSideOffset, vNewSideUMin);
                        br.vertex(x + 0, y + 0, z + 1, vMax + uNewSideOffset, vNewSideUMin);
                        br.vertex(x + 0, y + 0, z + 0, vMax + uNewSideOffset, vNewSideUMax);
                        br.vertex(x + 0, y + h00, z + 0, (vMax - vRange * h00) + uNewSideOffset, vNewSideUMax);
                        break;
                    case RawDirection.EAST:
                        br.vertex(x + 1, y + h10, z + 0, (vMax - vRange * h10) + uNewSideOffset, vNewSideUMin);
                        br.vertex(x + 1, y + 0, z + 0, vMax + uNewSideOffset, vNewSideUMin);
                        br.vertex(x + 1, y + 0, z + 1, vMax + uNewSideOffset, vNewSideUMax);
                        br.vertex(x + 1, y + h11, z + 1, (vMax - vRange * h11) + uNewSideOffset, vNewSideUMax);
                        break;
                    case RawDirection.SOUTH:
                        br.vertex(x + 0, y + h00, z + 0, (vMax - vRange * h00) + uNewSideOffset, vNewSideUMin);
                        br.vertex(x + 0, y + 0, z + 0, vMax + uNewSideOffset, vNewSideUMin);
                        br.vertex(x + 1, y + 0, z + 0, vMax + uNewSideOffset, vNewSideUMax);
                        br.vertex(x + 1, y + h10, z + 0, (vMax - vRange * h10) + uNewSideOffset, vNewSideUMax);
                        break;
                    case RawDirection.NORTH:
                        br.vertex(x + 1, y + h11, z + 1, (vMax - vRange * h11) + uNewSideOffset, vNewSideUMin);
                        br.vertex(x + 1, y + 0, z + 1, vMax + uNewSideOffset, vNewSideUMin);
                        br.vertex(x + 0, y + 0, z + 1, vMax + uNewSideOffset, vNewSideUMax);
                        br.vertex(x + 0, y + h01, z + 1, (vMax - vRange * h01) + uNewSideOffset, vNewSideUMax);
                        break;
                    case RawDirection.DOWN:
                        br.vertex(x + 1, y + 0, z + 1, uMin, vMin);
                        br.vertex(x + 1, y + 0, z + 0, uMin, vMax);
                        br.vertex(x + 0, y + 0, z + 0, uMax, vMax);
                        br.vertex(x + 0, y + 0, z + 1, uMax, vMin);
                        break;
                    case RawDirection.UP:
                        br.vertex(x + 0, y + h01, z + 1, uv00.X, uv00.Y);
                        br.vertex(x + 0, y + h00, z + 0, uv01.X, uv01.Y);
                        br.vertex(x + 1, y + h10, z + 0, uv10.X, uv10.Y);
                        br.vertex(x + 1, y + h11, z + 1, uv11.X, uv11.Y);

                        break;
                }

                br.end(vertices);
            }
        }
    }

    private static Vector2 rotateUV(float u, float v, float cos, float sin) {
        return new Vector2(u * cos - v * sin, u * sin + v * cos);
    }
}