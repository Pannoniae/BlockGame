using System.Runtime.InteropServices;
using BlockGame.util;
using Silk.NET.OpenGL.Legacy;
using Buffer = System.Buffer;

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
        Log.debug("Size of structure: " + sizeof(DrawElementsIndirectBindlessCommandNV));
        
        Log.info($"BindlessIndirectBuffer created with capacity {capacity} bytes");
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

    /// <summary>
    /// Add a command to the buffer, resizing if necessary
    /// </summary>
    public unsafe DrawElementsIndirectBindlessCommandNV* addCommand() {
        const int commandSize = 72; // sizeof(DrawElementsIndirectBindlessCommandNV)
        
        if (offset + commandSize > capacity) {
            resize(Math.Max(capacity * 2, offset + commandSize));
        }
        
        DrawElementsIndirectBindlessCommandNV* commandPtr = 
            (DrawElementsIndirectBindlessCommandNV*)((byte*)data + offset);
        
        offset += commandSize;
        size = Math.Max(size, offset);
        commands++;
        
        return commandPtr;
    }

    private void resize(int newCapacity) {
        Log.debug($"BindlessIndirectBuffer resizing from {capacity} to {newCapacity} bytes");
        
        // allocate new GPU buffer
        var oldHandle = handle;
        handle = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.DrawIndirectBuffer, handle);
        GL.BufferData(BufferTargetARB.DrawIndirectBuffer, (nuint)newCapacity, null, BufferUsageARB.DynamicDraw);
        
        // allocate new staging memory
        var oldData = data;
        data = (DrawElementsIndirectBindlessCommandNV*)NativeMemory.AlignedAlloc((nuint)newCapacity, 16);
        
        // copy existing data if any
        if (size > 0 && oldData != null) {
            Buffer.MemoryCopy(oldData, data, newCapacity, size);
        }
        
        // cleanup old resources
        GL.DeleteBuffer(oldHandle);
        if (oldData != null) {
            NativeMemory.AlignedFree(oldData);
        }
        
        capacity = newCapacity;
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
        Log.debug($"Size of structure: {sizeof(DrawArraysIndirectBindlessCommandNV)}");
        
        Log.info($"BindlessIndirectBuffer created with capacity {capacity} bytes");
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

    /// <summary>
    /// Add a command to the buffer, resizing if necessary
    /// </summary>
    public unsafe DrawArraysIndirectBindlessCommandNV* addCommand() {
        const int commandSize = 32; // sizeof(DrawArraysIndirectBindlessCommandNV)
        
        if (offset + commandSize > capacity) {
            resize(Math.Max(capacity * 2, offset + commandSize));
        }
        
        DrawArraysIndirectBindlessCommandNV* commandPtr = 
            (DrawArraysIndirectBindlessCommandNV*)((byte*)data + offset);
        
        offset += commandSize;
        size = Math.Max(size, offset);
        commands++;
        
        return commandPtr;
    }

    private void resize(int newCapacity) {
        Log.debug($"BindlessArraysIndirectBuffer resizing from {capacity} to {newCapacity} bytes");
        
        // allocate new GPU buffer
        var oldHandle = handle;
        handle = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.DrawIndirectBuffer, handle);
        GL.BufferData(BufferTargetARB.DrawIndirectBuffer, (nuint)newCapacity, null, BufferUsageARB.DynamicDraw);
        
        // allocate new staging memory
        var oldData = data;
        data = (DrawArraysIndirectBindlessCommandNV*)NativeMemory.AlignedAlloc((nuint)newCapacity, 16);
        
        // copy existing data if any
        if (size > 0 && oldData != null) {
            Buffer.MemoryCopy(oldData, data, newCapacity, size);
        }
        
        // cleanup old resources
        GL.DeleteBuffer(oldHandle);
        if (oldData != null) {
            NativeMemory.AlignedFree(oldData);
        }
        
        capacity = newCapacity;
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