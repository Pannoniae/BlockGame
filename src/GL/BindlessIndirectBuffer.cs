using System;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;

namespace BlockGame.GL;

/// <summary>
/// Buffer for managing bindless multi draw indirect commands
/// </summary>
public unsafe class BindlessIndirectBuffer : IDisposable {
    public readonly Silk.NET.OpenGL.GL GL;
    public uint handle;
    public DrawElementsIndirectBindlessCommandNV* data;
    public int capacity;
    public int size;
    public int offset;

    public BindlessIndirectBuffer(Silk.NET.OpenGL.GL gl, int initialCapacity) {
        GL = gl;
        capacity = initialCapacity;
        
        // Create OpenGL buffer
        handle = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.DrawIndirectBuffer, handle);
        GL.BufferData(BufferTargetARB.DrawIndirectBuffer, (nuint)capacity, null, BufferUsageARB.DynamicDraw);
        
        // Allocate staging memory
        data = (DrawElementsIndirectBindlessCommandNV*)NativeMemory.AlignedAlloc((nuint)capacity, 16);
        
        Console.WriteLine($"BindlessIndirectBuffer created with capacity {capacity} bytes");
    }

    /// <summary>
    /// Clear the buffer for new commands
    /// </summary>
    public void clear() {
        offset = 0;
        size = 0;
    }

    /// <summary>
    /// Upload the staged data to the GPU buffer
    /// </summary>
    public void upload() {
        if (size > 0) {
            GL.BindBuffer(BufferTargetARB.DrawIndirectBuffer, handle);
            GL.BufferSubData(BufferTargetARB.DrawIndirectBuffer, 0, (nuint)size, data);
        }
    }

    /// <summary>
    /// Execute all queued bindless multi draw indirect commands
    /// </summary>
    public void executeDrawCommands(int drawCount) {
        if (size == 0 || drawCount == 0) {
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
        
        Game.bmdi.MultiDrawElementsIndirectBindles(PrimitiveType.Triangles, DrawElementsType.UnsignedShort, 
            (void*)null, (uint)drawCount, 0, vertexBufferCount);
    }

    /// <summary>
    /// Get the maximum number of commands that can fit in this buffer
    /// </summary>
    public int getMaxCommands() {
        return capacity / SharedBlockVAO.getStride();
    }

    /// <summary>
    /// Get the current number of commands in the buffer
    /// </summary>
    public int getCommandCount() {
        return size / SharedBlockVAO.getStride();
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