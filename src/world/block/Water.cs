using System.Numerics;
using BlockGame.GL.vertexformats;
using Molten;

namespace BlockGame.util;

public class Water : Block {
    public Water(ushort id, string name, byte tickRate) : base(id, name) {
        lightAbsorption[id] = 1;
        renderType[id] = RenderType.CUSTOM;
        customCulling[id] = true;
        updateDelay[id] = tickRate;
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

    public override void update(World world, Vector3I pos) {
        var metadata = world.getBlockMetadata(pos);
        
        // if static water is being updated externally, wake it up
        if (!isDynamic(metadata)) {
            wakeUpWater(world, pos);
            // don't worry it will update next tick!
            return;
        }
        
        var currentLevel = getWaterLevel(metadata);
        var isFallingWater = isFalling(metadata);
        
        var newLevel = calculateFlowLevel(world, pos);
        var shouldBeFalling = shouldFall(world, pos);
        
        // update metadata if changed
        if (currentLevel != newLevel || isFallingWater != shouldBeFalling) {
            var newMetadata = setWaterLevel(metadata, newLevel);
            newMetadata = setFalling(newMetadata, shouldBeFalling);
            newMetadata = setDynamic(newMetadata, true); // keep dynamic while changing
            world.setBlockMetadataRemesh(pos.X, pos.Y, pos.Z, ((uint)Blocks.WATER).setMetadata(newMetadata));
            
            // wake up water neighbors
            foreach(var neighbor in Direction.directions) {
                if (world.getBlock(neighbor) == Blocks.WATER) {
                    wakeUpWater(world, neighbor);
                }
            }
        }
        
        // try to flow to neighbors
        bool hasFlowed = false;
        foreach(var dir in Direction.directionsWaterSpread) {
            var neighbor = pos + dir;
            if (canFlowTo(world, neighbor, currentLevel, isFallingWater)) {
                var flowLevel = calculateFlowLevelAt(currentLevel, isFallingWater, dir);
                var fallingFlag = (dir == Direction.DOWN);
                
                setWater(world, neighbor, flowLevel, fallingFlag);
                wakeUpWater(world, neighbor);
                hasFlowed = true;
            }
        }
        
        // become static if stable, otherwise reschedule
        if (hasFlowed || isUnstable(world, pos)) {
            world.scheduleBlockUpdate(pos);
        } else {
            // become static - clear dynamic flag
            var staticMetadata = setDynamic(metadata, false);
            world.setBlockMetadataRemesh(pos.X, pos.Y, pos.Z, ((uint)Blocks.WATER).setMetadata(staticMetadata));
        }
    }
    
    public byte calculateFlowLevelAt(byte fromLevel, bool isFallingWater, Vector3I dir) {
        if (dir == Direction.DOWN) {
            return fromLevel; // falling water keeps same level
        }
        
        // horizontal spread
        if (isFallingWater) {
            return fromLevel; // falling water spreads at same level (prevents infinite spread)
        } else {
            return (byte)Math.Min(fromLevel + 1, 7); // normal horizontal flow decreases by 1
        }
    }
    
    public byte calculateFlowLevel(World world, Vector3I pos) {
        // check for source conditions (2+ adjacent source blocks)
        if (shouldBecomeSource(world, pos)) return 0;
        
        // find best (lowest) level that can flow into this position
        byte bestLevel = 7; // weakest possible flow
        
        // check water above (falls down at same level)
        var above = pos + Direction.UP;
        if (world.getBlock(above) == Blocks.WATER) {
            var aboveLevel = getWaterLevel(world.getBlockMetadata(above));
            if (aboveLevel < 7) { // can flow down
                bestLevel = Math.Min(bestLevel, aboveLevel);
            }
        }
        
        // check horizontal neighbors (they flow at level+1)
        foreach(var dir in Direction.directionsHorizontal) {
            var neighbor = pos + dir;
            if (world.getBlock(neighbor) == Blocks.WATER) {
                var neighborLevel = getWaterLevel(world.getBlockMetadata(neighbor));
                if (neighborLevel < 7) { // can flow horizontally
                    bestLevel = Math.Min(bestLevel, (byte)(neighborLevel + 1));
                }
            }
        }
        
        return bestLevel;
    }
    
    public bool shouldBecomeSource(World world, Vector3I pos) {
        int adjacentSources = 0;
        int adjacentWater = 0;
        foreach(var dir in Direction.directionsHorizontal) {
            var neighbor = pos + dir;
            if (world.getBlock(neighbor) == Blocks.WATER) {
                adjacentWater++;
                var neighborMetadata = world.getBlockMetadata(neighbor);
                if (getWaterLevel(neighborMetadata) == 0) { // source block
                    adjacentSources++;
                }
            }
        }
        // also check for water above as additional source condition
        var above = pos + Direction.UP;
        if (world.getBlock(above) == Blocks.WATER) {
            var aboveMetadata = world.getBlockMetadata(above);
            if (getWaterLevel(aboveMetadata) == 0) {
                adjacentSources++;
            }
        }
        
        // become source if: 2+ adjacent sources OR surrounded by water (corner case)
        return adjacentSources >= 2 || (adjacentWater == 4 && adjacentSources >= 1);
    }
    
    public bool shouldFall(World world, Vector3I pos) {
        var below = pos + Direction.DOWN;
        return world.getBlock(below) == Blocks.AIR;
    }
    
    public bool canFlowTo(World world, Vector3I pos, byte fromLevel, bool fromFalling) {
        var block = world.getBlock(pos);
        if (block == Blocks.AIR) return fromLevel < 7; // can't create level 7 water (too weak)
        if (block != Blocks.WATER) return false;
        
        var existingLevel = getWaterLevel(world.getBlockMetadata(pos));
        return fromLevel < existingLevel; // can only flow to higher levels (less water)
    }
    
    public bool isUnstable(World world, Vector3I pos) {
        var metadata = world.getBlockMetadata(pos);
        var currentLevel = getWaterLevel(metadata);
        var calculatedLevel = calculateFlowLevel(world, pos);
        
        // unstable if calculated level differs from current
        if (currentLevel != calculatedLevel) return true;
        
        // unstable if can still spread to new areas  
        var isFallingWater = isFalling(metadata);
        foreach (var dir in Direction.directionsWaterSpread) {
            if (canFlowTo(world, pos + dir, currentLevel, isFallingWater)) return true;
        }
        
        return false;
    }
    
    public void setWater(World world, Vector3I pos, byte level, bool falling) {
        var existingBlock = world.getBlock(pos);
        if (existingBlock == Blocks.AIR) {
            var metadata = setWaterLevel(0, level);
            metadata = setFalling(metadata, falling);
            metadata = setDynamic(metadata, true); // new water is dynamic
            world.setBlockRemesh(pos.X, pos.Y, pos.Z, Blocks.WATER);
            world.setBlockMetadataRemesh(pos.X, pos.Y, pos.Z, ((uint)Blocks.WATER).setMetadata(metadata));
        } else if (existingBlock == Blocks.WATER) {
            // update existing water metadata only
            var metadata = setWaterLevel(0, level);
            metadata = setFalling(metadata, falling);
            metadata = setDynamic(metadata, true); // updating makes it dynamic
            world.setBlockMetadataRemesh(pos.X, pos.Y, pos.Z, ((uint)Blocks.WATER).setMetadata(metadata));
        }
    }
    
    public void wakeUpWater(World world, Vector3I pos) {
        if (world.getBlock(pos) == Blocks.WATER) {
            var metadata = world.getBlockMetadata(pos);
            if (!isDynamic(metadata)) {
                // wake up static water
                var dynamicMetadata = setDynamic(metadata, true);
                world.setBlockMetadataRemesh(pos.X, pos.Y, pos.Z, ((uint)Blocks.WATER).setMetadata(dynamicMetadata));
                world.scheduleBlockUpdate(pos);
            } else if (world.blockUpdateQueue.All(u => u.position != pos)) {
                // already dynamic but not scheduled
                world.scheduleBlockUpdate(pos);
            }
        }
    }
    
    public static Vector3 getWaterFlow(World world, Vector3I pos) {
        var metadata = world.getBlockMetadata(pos);
        var level = getWaterLevel(metadata);
        if (level == 0) return Vector3.Zero; // sources don't flow
        
        if (isFalling(metadata)) {
            return new Vector3(0, -1, 0); // falling water flows down
        }
        
        // calculate horizontal gradient
        var flow = Vector3.Zero;
        foreach(var dir in Direction.directionsHorizontal) {
            var neighbor = pos + dir;
            if (world.getBlock(neighbor) == Blocks.WATER) {
                var neighborLevel = getWaterLevel(world.getBlockMetadata(neighbor));
                var gradient = level - neighborLevel;
                if (gradient > 0) {
                    flow += dir.toVec3() * gradient;
                }
            }
        }
        
        flow.normi(); // normalize in-place
        return flow;
    }
    
    /** Water doesn't get rendered next to water, but always gets rendered on the top face */
    public override bool cullFace(BlockRenderer br, int x, int y, int z, RawDirection dir) {
        var direction = Direction.getDirection(dir);
        var same = br.getBlockCached(direction.X, direction.Y, direction.Z).getID() == br.getBlock().getID();
        if (same) {
            return false;
        }
        return dir == RawDirection.UP || true || base.cullFace(br, x, y, z, dir);
    }

    /** Calculate water height at corner based on neighboring water levels */
    public float getWaterHeightAt(World world, Vector3I pos, float cornerX, float cornerZ) {
        var metadata = world.getBlockMetadata(pos);
        var level = getWaterLevel(metadata);
        var baseHeight = level == 0 ? 15f / 16f : Math.Max((15f - level * 2f) / 16f, 1f / 16f);
        
        // determine which neighbors to sample based on corner position
        Vector3I[] neighbors = [];
        if (cornerX == 0 && cornerZ == 0) { // southwest corner
            neighbors = [pos + Direction.WEST, pos + Direction.SOUTH, pos + Direction.WEST + Direction.SOUTH];
        } else if (cornerX == 1 && cornerZ == 0) { // southeast corner  
            neighbors = [pos + Direction.EAST, pos + Direction.SOUTH, pos + Direction.EAST + Direction.SOUTH];
        } else if (cornerX == 0 && cornerZ == 1) { // northwest corner
            neighbors = [pos + Direction.WEST, pos + Direction.NORTH, pos + Direction.WEST + Direction.NORTH];
        } else if (cornerX == 1 && cornerZ == 1) { // northeast corner
            neighbors = [pos + Direction.EAST, pos + Direction.NORTH, pos + Direction.EAST + Direction.NORTH];
        }
        
        float totalHeight = baseHeight;
        int samples = 1;
        
        foreach (var neighbor in neighbors) {
            if (world.getBlock(neighbor) == Blocks.WATER) {
                var neighborMetadata = world.getBlockMetadata(neighbor);
                var neighborLevel = getWaterLevel(neighborMetadata);
                var neighborHeight = neighborLevel == 0 ? 15f / 16f : Math.Max((15f - neighborLevel * 2f) / 16f, 1f / 16f);
                totalHeight += neighborHeight;
                samples++;
            }
        }
        
        return totalHeight / samples; // average of available heights
    }
    
    public override void render(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices) {
        base.render(br, x, y, z, vertices);
        
        // Calculate water height based on level (0 = source/full, 7 = nearly empty)
        var block = br.getBlock();
        var metadata = block.getMetadata();
        var level = getWaterLevel(metadata);
        var height = level == 0 ? 15f / 16f : Math.Max((15f - level * 2f) / 16f, 1f / 16f); // Source = full, flowing gets shorter, min 1/16
        
        // Calculate corner heights for sloped rendering
        var world = br.world;
        var blockPos = new Vector3I(x, y, z);
        var h00 = getWaterHeightAt(world, blockPos, 0, 0); // southwest
        var h10 = getWaterHeightAt(world, blockPos, 1, 0); // southeast  
        var h01 = getWaterHeightAt(world, blockPos, 0, 1); // northwest
        var h11 = getWaterHeightAt(world, blockPos, 1, 1); // northeast
        
        // Get texture coordinates for water
        var texMin = level > 0 ? uvs[1] : uvs[0]; // flowing texture for levels > 0, still texture for source
        var texMax = texMin + 1;
        var min = texCoords(texMin.u, texMin.v);
        var max = texCoords(texMax.u, texMax.v);
        
        var uMin = min.X;
        var vMin = min.Y;
        var uMax = max.X;
        var vMax = max.Y;
        
        Span<BlockVertexPacked> cache = stackalloc BlockVertexPacked[4];
        Span<Vector4> colourCache = stackalloc Vector4[4];
        Span<byte> lightColourCache = stackalloc byte[4];
        
        x &= 15;
        y &= 15;
        z &= 15;

        for (RawDirection d = 0; d < RawDirection.MAX; d++) {
            
            if (cullFace(br, x, y, z, d)) {
                br.applyFaceLighting(d, colourCache, lightColourCache);
                br.begin(cache);
                
                switch (d) {
                    case RawDirection.WEST:
                        // use interpolated heights for west face
                        br.vertex(x + 0, y + h01, z + 1, uMin, vMax - (vMax - vMin) * (h01 / (15f / 16f)));
                        br.vertex(x + 0, y + 0, z + 1, uMin, vMax);
                        br.vertex(x + 0, y + 0, z + 0, uMax, vMax);
                        br.vertex(x + 0, y + h00, z + 0, uMax, vMax - (vMax - vMin) * (h00 / (15f / 16f)));
                        break;
                    case RawDirection.EAST:
                        br.vertex(x + 1, y + h10, z + 0, uMin, vMax - (vMax - vMin) * (h10 / (15f / 16f)));
                        br.vertex(x + 1, y + 0, z + 0, uMin, vMax);
                        br.vertex(x + 1, y + 0, z + 1, uMax, vMax);
                        br.vertex(x + 1, y + h11, z + 1, uMax, vMax - (vMax - vMin) * (h11 / (15f / 16f)));
                        break;
                    case RawDirection.SOUTH:
                        br.vertex(x + 0, y + h00, z + 0, uMin, vMax - (vMax - vMin) * (h00 / (15f / 16f)));
                        br.vertex(x + 0, y + 0, z + 0, uMin, vMax);
                        br.vertex(x + 1, y + 0, z + 0, uMax, vMax);
                        br.vertex(x + 1, y + h10, z + 0, uMax, vMax - (vMax - vMin) * (h10 / (15f / 16f)));
                        break;
                    case RawDirection.NORTH:
                        br.vertex(x + 1, y + h11, z + 1, uMin, vMax - (vMax - vMin) * (h11 / (15f / 16f)));
                        br.vertex(x + 1, y + 0, z + 1, uMin, vMax);
                        br.vertex(x + 0, y + 0, z + 1, uMax, vMax);
                        br.vertex(x + 0, y + h01, z + 1, uMax, vMax - (vMax - vMin) * (h01 / (15f / 16f)));
                        break;
                    case RawDirection.DOWN:
                        br.vertex(x + 1, y + 0, z + 1, uMin, vMin);
                        br.vertex(x + 1, y + 0, z + 0, uMin, vMax);
                        br.vertex(x + 0, y + 0, z + 0, uMax, vMax);
                        br.vertex(x + 0, y + 0, z + 1, uMax, vMin);
                        break;
                    case RawDirection.UP:
                        // sloped top face with corner heights
                        br.vertex(x + 0, y + h01, z + 1, uMin, vMin);
                        br.vertex(x + 0, y + h00, z + 0, uMin, vMax);
                        br.vertex(x + 1, y + h10, z + 0, uMax, vMax);
                        br.vertex(x + 1, y + h11, z + 1, uMax, vMin);
                        break;
                }
                br.end(vertices);
            }
        }
    }
}