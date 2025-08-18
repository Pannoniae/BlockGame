using System.Runtime.InteropServices;
using Silk.NET.OpenGL.Legacy;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.GL;

/// <summary>
/// Uniform Buffer Object for efficiently setting frequently updated uniforms (or something like that idk)
/// </summary>
public unsafe class UniformBuffer : IDisposable {
    private readonly Silk.NET.OpenGL.Legacy.GL GL;
    private readonly uint handle;
    private readonly uint bindingPoint;
    private readonly int size;
    private void* data;

    public UniformBuffer(Silk.NET.OpenGL.Legacy.GL GL, int size, uint bindingPoint) {
        this.GL = GL;
        this.size = size;
        this.bindingPoint = bindingPoint;

        // create buffer
        handle = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.UniformBuffer, handle);

        // allocate buffer storage
        GL.BufferStorage(BufferStorageTarget.UniformBuffer, (nuint)size, null, BufferStorageMask.DynamicStorageBit);

        // bind to binding point
        GL.BindBufferBase(BufferTargetARB.UniformBuffer, bindingPoint, handle);

        // allocate managed memory for staging data
        data = NativeMemory.AlignedAlloc((nuint)size, 16);
        
        // Print buffer size
        Console.Out.WriteLine($"UniformBuffer created with size {size} bytes and binding point {bindingPoint}");
        
    }

    public void updateData<T>(in T value, int offset = 0) where T : unmanaged {
        #if DEBUG
        int structSize = sizeof(T);
        if (offset + structSize > size) {
            throw new ArgumentException($"Data size {structSize} at offset {offset} exceeds buffer size {size}");
        }
        #endif

        // copy to staging memory
        *(T*)((byte*)data + offset) = value;
    }

    public void bind() {
        GL.BindBuffer(BufferTargetARB.UniformBuffer, handle);
    }

    public void upload() {
        GL.InvalidateBufferData(handle);
        GL.BufferSubData(BufferTargetARB.UniformBuffer, 0, (nuint)size, data);
    }

    public void bindToPoint() {
        GL.BindBufferBase(BufferTargetARB.UniformBuffer, bindingPoint, handle);
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

    ~UniformBuffer() {
        Dispose();
    }
}