using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using Buffer = System.Buffer;

namespace BlockGame.GL;

/// <summary>
/// Shader Storage Buffer Object for dynamic arrays in shaders
/// </summary>
public unsafe class ShaderStorageBuffer : IDisposable {
    private readonly Silk.NET.OpenGL.GL GL;
    public readonly uint handle;
    private readonly uint bindingPoint;
    private int size;
    private int capacity;
    private void* data;

    public ShaderStorageBuffer(Silk.NET.OpenGL.GL GL, int initialCapacity, uint bindingPoint) {
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
        
        Console.Out.WriteLine($"ShaderStorageBuffer created with capacity {capacity} bytes and binding point {bindingPoint}");
    }

    public void updateData<T>(ReadOnlySpan<T> values) where T : unmanaged {
        int requiredSize = values.Length * sizeof(T);
        
        if (requiredSize > capacity) {
            throw new ArgumentException($"Data size {requiredSize} exceeds buffer capacity {capacity}");
        }

        // copy to staging memory
        fixed (T* src = values) {
            Buffer.MemoryCopy(src, data, capacity, requiredSize);
        }
        
        size = requiredSize;
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