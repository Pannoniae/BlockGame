# Guide to the code/concepts

## This guide is suitable for both humans and AIs.

## COORDINATE SYSTEM (READ THIS EVERY TIME)
**-X = West, +X = East**
**-Y = Down, +Y = Up**
**+Z = North, -Z = South**

This is NOT fucking minecraft! South is -Z, North is +Z.

## Terminology
- **Level**: World folder with multiple dimensions
- **World**: 3D dimension with blocks/entities
- **Chunk**: 2D chunk column, 16x16x128 blocks
- **SubChunk**: 3D chunk section, 16x16x16 blocks
- **ChunkCoord**: 2D chunk position (x, z)
- **SubChunkCoord**: 3D subchunk position (x, y, z)


## Code style/guidelines/conventions
- Java-style naming conventions (PascalCase for types, camelCase for variables, no IInterface). I WON'T ACCEPT C#-STYLED SLOP. This is to ensure quality control. I warned you.
- Generally short names and abbreviations, prefer brevity to expressivity, e.g. `pos`, `xx`, `xp`, `c00`, `sp`, `title`. This is so the code doesn't get cluttered. You can leave a comment to explain it fully.
- Comment the "why" and the "how", not the "what" - leave the tutorial-level comments out of the codebase. Prefer starting with lowercase for one-line comments.
- Use /** (Java)-style doc comments instead of ///. You *can* use XML tags, but NEVER use ///-styled comments. If you find one, fix it.
- When designing features, aim for simplicity, minimalism and performance. When they conflict, prefer performance.

### NO ENTERPRISE SLOP

This is game dev, not corporate CRUD. We have ZERO external consumers, ZERO backwards compatibility requirements, and ZERO team members who need "clean" interfaces. **Just write direct, fast code.**

**BANNED:**
- `IFooService`, `IBarManager`, `IThingProvider` - interfaces with single implementations are mental illness. You need to do 10 pushups for every interface you add.
- `SomethingContext`, `FooFactory`, `BarHandler` - enterprise naming cancer
- Layers for the sake of layers - data should flow obviously, not through 7 abstraction levels
- Generic everything `ConfigurableDoer<T, K, V>` - make specific classes for specific jobs
- Future-proofing bullshit - we can modify the code, that's the point
- Design patterns as goals instead of tools - Strategy/Factory/Observer are means, not ends

**DO:**
- Concrete classes doing concrete things
- Inheritance only when behavior genuinely differs (not for "extensibility")
- Short, direct names: `Renderer` not `IRenderingServiceProvider`
- Obvious call chains: `player.inv.getStack(0)` not `contextProvider.getInventoryService().querySlot(request)`
- Performance-first design - this runs at 180fps AT LEAST, not once per fucking HTTP request
- I repeat, USE SHORT NAMES! Most names you can think of are way too long. Don't be like that. We have fucking namespaces for a reason.

**DON'T DO THIS SLOP:**
```csharp
public interface ISlotOperationHandler<TContext> where TContext : IInventoryContext {
    OperationResult<TItem> HandleOperation<TItem>(ISlotRequest<TItem, TContext> request)
        where TItem : IStackable;
}
```

**DO THIS INSTEAD:**
```csharp
public abstract class ItemSlot {
    public abstract ItemStack? take(int count);
    public abstract ItemStack? place(ItemStack stack);
}
```

Remember: this isn't your fucking day job. Write code like your performance review depends on the FPS, not "clean architecture" buzzwords.


## Footguns/gotchas

**Random/hashing:**
- `Random` class: seeded constructor has backwards compat overhead making it slow as shit. Use `XRandom`.
- Built-in `GetHashCode()`: randomized by design for "security". Use `XHash` for deterministic results (worldgen or anywhere else you need it).

**Silk.NET noobtraps (prepare for pain):**
- Generic `<T>` overloads: they DON'T do what you expect, it will segfault
- Casting `(void*)0`: ALWAYS cast or it uses the IntPtr overload → the game will segfault at 0x0000
- String overloads: allocate on native heap every call → it generates HUGE amounts of heap garbage in the render loop
- Solution: use raw pointer overloads exclusively. Ask me if you don't know pointers.
