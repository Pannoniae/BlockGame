using System.Runtime.InteropServices;
using Silk.NET.OpenGL.Legacy;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.GL;

/// <summary>
/// Buffer for managing bindless multi draw indirect commands
/// </summary>
public unsafe class BindlessIndirectBuffer : IDisposable {
    public readonly Silk.NET.OpenGL.Legacy.GL GL;
    public uint handle;
    public DrawElementsIndirectBindlessCommandNV* data;
    public int commands;
    public int capacity;
    public int size;
    public int offset;

    public BindlessIndirectBuffer(Silk.NET.OpenGL.Legacy.GL gl, int initialCapacity) {
        GL = gl;
        capacity = initialCapacity;
        
        // Create OpenGL buffer
        handle = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.DrawIndirectBuffer, handle);
        GL.BufferData(BufferTargetARB.DrawIndirectBuffer, (nuint)capacity, null, BufferUsageARB.DynamicDraw);
        
        // Allocate staging memory
        data = (DrawElementsIndirectBindlessCommandNV*)NativeMemory.AlignedAlloc((nuint)capacity, 16);
        
        // sizeof
        Console.WriteLine("Size of structure: " + sizeof(DrawElementsIndirectBindlessCommandNV));
        
        Console.WriteLine($"BindlessIndirectBuffer created with capacity {capacity} bytes");
    }

    /// <summary>
    /// Clear the buffer for new commands
    /// </summary>
    public void clear() {
        offset = 0;
        size = 0;
        commands = 0;
    }

    /// <summary>
    /// Upload the staged data to the GPU buffer
    /// </summary>
    public void upload() {
        if (size > 0) {
            GL.InvalidateBufferData(handle);
            GL.NamedBufferSubData(handle, 0, (nuint)size, data);
        }
    }

    /// <summary>
    /// Execute all queued bindless multi draw indirect commands
    /// </summary>
    public void executeDrawCommands() {
        if (size == 0 || commands == 0) {
            return;
        }

        upload();
        
        //GL.MemoryBarrier(MemoryBarrierMask.CommandBarrierBit);
        GL.BindBuffer(BufferTargetARB.DrawIndirectBuffer, handle);
        
        // Execute bindless multi draw elements indirect
        // stride = 0 means tightly packed
        // vertexBufferCount = 1 (we have 1 vertex buffer per draw call)
        const int vertexBufferCount = 1;
        
        //Console.WriteLine($"Executing {drawCount} bindless draw commands, stride={stride}, vertexBufferCount={vertexBufferCount}");
        
        // update metrics
        Game.metrics.renderedSubChunks = commands;
        
        Game.bmdi.MultiDrawElementsIndirectBindles(PrimitiveType.Triangles, DrawElementsType.UnsignedShort, 
            (void*)null, (uint)commands, 0, vertexBufferCount);
        
        // unbind so it's not tracked anymore!
        //GL.BindBuffer(BufferTargetARB.DrawIndirectBuffer, 0);
    }

    /// <summary>
    /// Get the maximum number of commands that can fit in this buffer
    /// </summary>
    public int getMaxCommands() {
        return capacity / 72;
    }

    public void Dispose() {
        if (data != null) {
            NativeMemory.AlignedFree(data);
            data = null;
        }

        if (handle != 0) {
            GL.DeleteBuffer(handle);
            handle = 0;
        }

        GC.SuppressFinalize(this);
    }

    ~BindlessIndirectBuffer() {
        Dispose();
    }
}

/// <summary>
/// Buffer for managing bindless multi draw indirect commands
/// </summary>
public unsafe class BindlessArraysIndirectBuffer : IDisposable {
    public readonly Silk.NET.OpenGL.Legacy.GL GL;
    public uint handle;
    public DrawArraysIndirectBindlessCommandNV* data;
    public int commands;
    public int capacity;
    public int size;
    public int offset;

    public BindlessArraysIndirectBuffer(Silk.NET.OpenGL.Legacy.GL gl, int initialCapacity) {
        GL = gl;
        capacity = initialCapacity;
        
        // Create OpenGL buffer
        handle = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.DrawIndirectBuffer, handle);
        GL.BufferData(BufferTargetARB.DrawIndirectBuffer, (nuint)capacity, null, BufferUsageARB.DynamicDraw);
        
        // Allocate staging memory
        data = (DrawArraysIndirectBindlessCommandNV*)NativeMemory.AlignedAlloc((nuint)capacity, 16);
        
        // sizeof
        Console.WriteLine("Size of structure: " + sizeof(DrawArraysIndirectBindlessCommandNV));
        
        Console.WriteLine($"BindlessIndirectBuffer created with capacity {capacity} bytes");
    }

    /// <summary>
    /// Clear the buffer for new commands
    /// </summary>
    public void clear() {
        offset = 0;
        size = 0;
        commands = 0;
    }

    /// <summary>
    /// Upload the staged data to the GPU buffer
    /// </summary>
    public void upload() {
        if (size > 0) {
            GL.InvalidateBufferData(handle);
            GL.NamedBufferSubData(handle, 0, (nuint)size, data);
        }
    }

    /// <summary>
    /// Execute all queued bindless multi draw indirect commands
    /// </summary>
    public void executeDrawCommands() {
        if (size == 0 || commands == 0) {
            return;
        }

        upload();
        
        //GL.MemoryBarrier(MemoryBarrierMask.CommandBarrierBit);
        GL.BindBuffer(BufferTargetARB.DrawIndirectBuffer, handle);
        
        // Execute bindless multi draw elements indirect
        // stride = 0 means tightly packed
        // vertexBufferCount = 1 (we have 1 vertex buffer per draw call)
        const int vertexBufferCount = 1;
        
        //Console.WriteLine($"Executing {drawCount} bindless draw commands, stride={stride}, vertexBufferCount={vertexBufferCount}");
        
        // update metrics
        Game.metrics.renderedSubChunks = commands;
        
        Game.bmdi.MultiDrawArraysIndirectBindles(PrimitiveType.Quads, (void*)null, (uint)commands, 0, vertexBufferCount);
        
        // unbind so it's not tracked anymore!
        //GL.BindBuffer(BufferTargetARB.DrawIndirectBuffer, 0);
    }

    /// <summary>
    /// Get the maximum number of commands that can fit in this buffer
    /// </summary>
    public int getMaxCommands() {
        return capacity / 72;
    }

    public void Dispose() {
        if (data != null) {
            NativeMemory.AlignedFree(data);
            data = null;
        }

        if (handle != 0) {
            GL.DeleteBuffer(handle);
            handle = 0;
        }

        GC.SuppressFinalize(this);
    }

    ~BindlessArraysIndirectBuffer() {
        Dispose();
    }
}