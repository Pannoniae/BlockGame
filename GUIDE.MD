# Guide to the code/concepts

## This guide is suitable for both humans and AIs.

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
- Java-style naming conventions (PascalCase for types, camelCase for variables)
- Generally short names and abbreviations, prefer brevity to expressivity, e.g. `pos`, `xx`, `xp`, `c00`, `sp`, `title`. This is so the code doesn't get cluttered. You can leave a comment to explain it fully.
- Comment the "why" and the "how", not the "what" - leave the tutorial-level comments out of the codebase. Prefer starting with lowercase for one-line comments.
- Use /** (Java)-style doc comments instead of ///.
- When designing features, aim for simplicity, minimalism and performance. When they conflict, prefer performance.


## Footguns/noobtraps
- NEVER use the built-in `Random` class. The no-arg constructor is good but you can't control the seed -
the seeded constructor has backwards compatibility bullshit, so it's doggone slow. Use `XRandom` instead.
- NEVER use built-in hashcodes, especially in worldgen/where you want consistent results. IT IS LITERALLY RANDOM ON PURPOSE.
Shitty-ass design. Use `XHash` instead for deterministic hashing.
- Silk.NET is a walking skill issue, it's so full of footguns. More specifically:
- Avoid generic (`<T0>`) overloads in Silk.NET. They DO NOT DO WHAT YOU EXPECT and WILL SEGFAULT EVERYTHING. I warned you.
- When an API requires shit like (void*)0, DO NOT SKIP THE CAST. It will use the IntPtr overload, which is... guess what, segfaulting again, trying to access 0x0000.
- The only correct option to use 99% of the time are the raw overloads taking pointers. If you don't know how to use pointers, ask me.
- Similarly, NEVER use the string overloads either. This one is violated a few times in the code (tell me if you find them lol), but they allocate the string on the native heap every single time, so they are shit.
Especially in render code.
