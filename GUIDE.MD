# Guide to the code/concepts

## Terminology
(note, half of this is wrong in the code rn, this is more like an aspiration lol)

- **World**: A world folder with multiple dimensions.
- **Dimension**: A 3D world with blocks, entities, etc.
- **Chunk**: A 2D chunk, 16x16x128 blocks.
- **SubChunk**: A 3D chunk, 16x16x16 blocks.
- **Block**: A single block in the world.
- **Entity**: A single entity in the world.
- **ChunkCoord**: A 2D coordinate for a chunk, with x, z coordinates.
- **SubChunkCoord**: A 3D coordinate for a subchunk, with x, y, z coordinates.


## Code style/guidelines/conventions

- If possible, don't refer back to stuff! It creates a mess. For example, if you have a `Slot`, don't create an `Inventory` field for it. 
Or if you have an `Entity`, don't create a `World` field for it. Methods which require *both* should live in the parent. (e.g. in `World`).

  This avoids a huge spaghetti mess of references.


## Footguns/noobtraps
- NEVER use the built-in `Random` class. The no-arg constructor is good but you can't control the seed -
the seeded constructor has backwards compatibility bullshit, so it's doggone slow. Use `XRandom` instead.
- NEVER use built-in hashcodes, especially in worldgen/where you want consistent results. IT IS LITERALLY RANDOM ON PURPOSE.
Shitty-ass design. I'll cook something up later but for now, DIY homebrew a solution with XOR or something