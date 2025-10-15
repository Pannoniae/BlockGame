using System.Runtime.InteropServices;
using BlockGame.main;
using BlockGame.ui;
using BlockGame.util;
using BlockGame.util.log;
using Silk.NET.OpenGL.Legacy;
using Silk.NET.OpenGL.Legacy.Extensions.NV;
using Buffer = System.Buffer;

namespace BlockGame.GL;

/// <summary>
/// Shader Storage Buffer Object for dynamic arrays in shaders
/// </summary>
public unsafe class ShaderStorageBuffer : IDisposable {
    private readonly Silk.NET.OpenGL.Legacy.GL GL;
    public uint handle;
    private readonly uint bindingPoint;
    private int size;
    private int capacity;
    private void* data;

    public ShaderStorageBuffer(Silk.NET.OpenGL.Legacy.GL GL, int initialCapacity, uint bindingPoint) {
        this.GL = GL;
        this.bindingPoint = bindingPoint;
        this.capacity = initialCapacity;
        this.size = 0;

        // create buffer
        handle = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, handle);

        // allocate buffer storage
        GL.BufferStorage(BufferStorageTarget.ShaderStorageBuffer, (nuint)capacity, null, BufferStorageMask.DynamicStorageBit);

        // bind to binding point
        GL.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, bindingPoint, handle);

        // allocate managed memory for staging data
        data = NativeMemory.AlignedAlloc((nuint)capacity, 16);
        
        Log.info($"ShaderStorageBuffer created with capacity {capacity} bytes and binding point {bindingPoint}");
    }

    public void updateData<T>(ReadOnlySpan<T> values) where T : unmanaged {
        int requiredSize = values.Length * sizeof(T);
        
        if (requiredSize > capacity) {
            resize(Math.Max(requiredSize, capacity * 2));
        }

        // copy to staging memory
        fixed (T* src = values) {
            Buffer.MemoryCopy(src, data, capacity, requiredSize);
        }
        
        size = requiredSize;
    }

    private void resize(int newCapacity) {
        Log.info($"ShaderStorageBuffer resizing from {capacity} to {newCapacity} bytes");
        
        // allocate new GPU buffer
        var oldHandle = handle;
        handle = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, handle);
        GL.BufferStorage(BufferStorageTarget.ShaderStorageBuffer, (nuint)newCapacity, null, BufferStorageMask.DynamicStorageBit);
        GL.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, bindingPoint, handle);
        
        // allocate new staging memory
        var oldData = data;
        data = NativeMemory.AlignedAlloc((nuint)newCapacity, 16);
        
        // copy existing data if any
        if (size > 0 && oldData != null) {
            Buffer.MemoryCopy(oldData, data, newCapacity, size);
        }

        // make resident!
        makeResident(out Game.renderer.ssboaddr);
        
        // cleanup old resources
        GL.DeleteBuffer(oldHandle);
        if (oldData != null) {
            NativeMemory.AlignedFree(oldData);
        }
        
        capacity = newCapacity;
    }

    public void bind() {
        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, handle);
    }

    public void upload() {
        GL.InvalidateBufferData(handle);
        GL.BufferSubData(BufferTargetARB.ShaderStorageBuffer, 0, (nuint)size, data);
    }

    public void bindToPoint() {
        GL.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, bindingPoint, handle);
    }

    public void makeResident(out ulong ssboaddr) {
        // make resident
        // get address of the ssbo
        if (Settings.instance.getActualRendererMode() >= RendererMode.BindlessMDI) {
            Game.sbl.MakeNamedBufferResident(handle, (NV)GLEnum.ReadOnly);
            Game.sbl.GetNamedBufferParameter(handle, NV.BufferGpuAddressNV,
                out ssboaddr);
        }
        else {
            ssboaddr = 0;
        }
    }

    public void Dispose() {
        if (data != null) {
            NativeMemory.AlignedFree(data);
            data = null;
        }

        if (handle != 0) {
            GL.DeleteBuffer(handle);
        }

        GC.SuppressFinalize(this);
    }

    ~ShaderStorageBuffer() {
        Dispose();
    }
}