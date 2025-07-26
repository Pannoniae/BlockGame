# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

### Building and Running
Don't run the project, you are in WSL.

### Testing
See above, WSL.

## Architecture Overview

BlockGame is a 3D block-based game (+engine) written in C# targeting .NET 10.0 preview. The architecture follows a layered, event-driven design optimized for real-time 3D rendering.
Consult @GUIDE.MD on coding stuff and architecture stuff. (FYI)

### Core Systems

**Entry Point**: `src/main/Program.cs` → `src/main/Game.cs` (singleton pattern)
- Uses Silk.NET framework for cross-platform windowing, input, and OpenGL
- Event-driven initialization through window callbacks

**Rendering System** (`src/render/`, `src/GL/`):
- `WorldRenderer`: Multi-pass rendering (opaque → transparent depth pre-pass → translucent)
- `Graphics`: SpriteBatch system and OpenGL abstraction layer
- Frustum culling for chunks and specialized shaders for world/water/outlines

**World Management** (`src/world/`):
- Chunk-based world with 16x16x16 sub-chunks in 16x(8*16)x16 chunks
- Status-based loading pipeline: Generated → Populated → Lighted → Meshed
- Asynchronous chunk loading with frame-time budgeting (7.5ms max)
- Separate lighting system with sky light and block light propagation

**UI System** (`src/ui/`):
- Hierarchical: Screen → Menu → GUIElement
- Scalable with `guiScale` factor
- Uses FontStashSharp for font rendering

**Block System**

- Blocks are defined in `src/id/Blocks.cs` with static registration
- Block models and rendering data in `src/util/Block.cs` and `src/util/BlockModel.cs`
- Texture atlasing system in `src/util/TextureManager.cs`

**World Generation**

- Located in `src/world/worldgen/`
- Multiple generators: `SimpleWorldGenerator`, `PerlinWorldGenerator`, `OverworldWorldGenerator`. `PerlinWorldGenerator` is the actually used one.
- Feature system for caves, ores, ravines in `src/world/worldgen/feature/`

### Key Dependencies

- **Silk.NET**: Cross-platform windowing, input, OpenGL
- **FontStashSharp**: Font rendering with multiple rasterizers
- **TrippyGL**: Custom OpenGL wrapper (included in lib/). Used to exist but the game doesn't use it anymore (we wrote our own state tracker). Useful to consult for tricks.
- **FastNoiseLite**: Noise generation for world generation
- **SixLabors.ImageSharp**: Image processing

### Directory Structure

```
src/
├── main/        # Entry point and main game class
├── render/      # 3D rendering, world renderer, particle systems
├── world/       # World management, chunks, entities, world gen
├── ui/          # GUI system, screens, menus, elements
├── GL/          # OpenGL abstraction, VAOs, shaders, textures
├── util/        # Utilities, math, NBT, fonts, constants
└── snd/         # Sound engine

shaders/         # GLSL shaders
textures/        # Game textures and atlases
fonts/           # BDF and TTF fonts
lib/             # Custom libraries and dependencies
```

### Performance Considerations

- Frame-time budgeted chunk loading
- Frustum culling at chunk and sub-chunk level
- Vertex buffer reuse with shared indices
- Memory pooling for temporary allocations
- SIMD optimizations where applicable (pretty much everywhere lol)

### Modding Architecture

TODO, indicating a design that could support modding through reflection or similar mechanisms.

## Important Notes

- This is a .NET 10.0 preview project using latest C# language features
- Unsafe code is enabled for performance-critical operations
- The project includes extensive custom libraries rather than relying on third-party game engines
- Strong focus on performance with custom memory management and pooling
- Uses event-driven architecture for decoupled system communication

## Code Style Guidelines
- Java-style naming conventions (PascalCase for types, camelCase for variables)
- Generally short names and abbreviations, prefer brevity to clarity, e.g. `pos`, `xx`, `xp`, `c00`, `sp`, `title`
- Comment the "why" and the "how", not the "what" - leave the tutorial-level comments out of the codebase. Prefer starting with lowercase for one-line comments.
- Use /** (Java)-style doc comments instead of ///.
- When designing features, aim for simplicity, minimalism and performance. When they conflict, prefer performance.