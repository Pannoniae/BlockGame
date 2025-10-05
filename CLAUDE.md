# CLAUDE.md

## Response Style

- No tutorial bullshit unless asked
- Expert-level, high-performance code only!
- Challenge bad ideas, demand clarification when vague
- Short responses, Socratic method when useful
- Push back on lazy questions - send specs/links instead of walls of text
- No sycophancy or glazing

### Building and Running
Use `dotnet build BlockGame.slnx -c Release /property:WarningLevel=0` to build the entire solution in Release mode.
Use `dotnet run --project BlockGame.csproj -c Release /property:WarningLevel=0` to run the main project in Release mode.
(This is so the output doesn't get spammed with superfluous warnings.)
If you want to run tests, do `dotnet test BlockGameTesting\BlockGameTesting.csproj`.

## Architecture

BlockGame is a 3D block-based game (+engine) written in C# targeting .NET 10.0 preview.

**READ @GUIDE.MD EVERY TIME** for IMPORTANT code structuring tips.

**Are you not finding something?** - We use partial classes HEAVILY, search the entire project, not just one file.

The documentation is in `docs/` and various debugging shit is in `debug/`.

### Core Systems

**Entry Point**: `src/main/Program.cs` → `src/main/Game.cs` (singleton pattern)
- Uses Silk.NET framework for cross-platform windowing, input, and OpenGL
- Event-driven initialization through window callbacks

**Rendering System** (`src/render/`, `src/GL/`):
- `WorldRenderer`: Multi-pass rendering (opaque → transparent depth pre-pass → translucent)
- `Graphics`: SpriteBatch system and OpenGL abstraction layer
- `CommandBuffer`: NVIDIA command list renderer for high-performance rendering
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

### Dependencies

- **Silk.NET**: Windowing, input, OpenGL
- **FontStashSharp**: Font rendering
- **FastNoiseLite**: Noise generation
- **ImageSharp**: Image processing
- **TrippyGL**: Dead dependency in `lib/` (we wrote our own state tracker), check for reference tricks

### Directory Structure

```
src/
├── main/        # Entry point and main game class
├── render/      # 3D rendering, world renderer, particle systems
├── world/       # World management, chunks, entities, world gen
├── ui/          # GUI system, screens, menus, elements
├── GL/          # OpenGL abstraction, VAOs, shaders, textures
├── util/        # Utilities, math, NBT, fonts, constants
├── snd/         # Sound engine
└── assets/
    ├── shaders/     # GLSL shaders (including cmdl variants)
    ├── textures/    # Game textures and atlases
    └── fonts/       # Fonts

docs/            # Design documents and architecture specs
lib/             # Custom libraries and dependencies
```

## Key Facts

.NET 10.0 preview with latest C# features. Unsafe code enabled and encouraged. Custom homemade libraries over premade game engines and 3rd party stuff. Performance-first with memory pooling.

**WHEN IN DOUBT, ASK. REFUSE TO DO ANYTHING UNLESS THE TASK IS CLEAR.**