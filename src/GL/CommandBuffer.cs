using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.NV;
using Buffer = System.Buffer;

namespace BlockGame.GL;

//[StructLayout(LayoutKind.Sequential, Size = 16)]
public struct DrawElementsCommandNV {
    public uint header;
    public uint count;
    public uint firstIndex;
    public uint baseVertex;
}

public struct DrawElementsInstancedCommandNV {
    public uint header;
    
    public uint mode;
    public uint count;
    public uint instanceCount;
    public uint first;
    public uint baseVertex;
    public uint baseInstance;
}

//[StructLayout(LayoutKind.Sequential, Size = 16)]
public struct AttributeAddressCommandNV {
    public uint header;
    public uint index;
    public uint addressLo;
    public uint addressHi;
}


//[StructLayout(LayoutKind.Sequential, Size = 16)]
public struct ElementAddressCommandNV {
     public uint header;
     public uint addressLo;
     public uint addressHi;
     public uint typeSizeInByte;
}

// Bindless multi draw indirect structures

/// <summary>
/// Bindless pointer structure for vertex/index buffers
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct BindlessPtrNV {
    public uint index;
    public uint reserved;
    public ulong address;
    public ulong length;
}

/// <summary>
/// Standard DrawArraysIndirectCommand structure
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct DrawArraysIndirectCommand {
    public uint count;
    public uint instanceCount;
    public uint first;
    public uint baseInstance;
}

[StructLayout(LayoutKind.Sequential)]
public struct DrawElementsIndirectCommand {
    public uint count;
    public uint instanceCount;
    public uint firstIndex;
    public int  baseVertex;
    public uint baseInstance;
};

/// <summary>
/// Standard DrawElementsIndirectCommand structure  
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct DrawElementsIndirectBindlessCommandNV {
    public DrawElementsIndirectCommand cmd;
    public uint                      reserved; 
    public BindlessPtrNV               indexBuffer;
    public BindlessPtrNV               vertexBuffer; // only one here!
};

/// <summary>
/// Holds a buffer object for a NV_command_list DrawCommands buffer.
/// </summary>
public unsafe class CommandBuffer : IDisposable {
    private readonly Silk.NET.OpenGL.GL GL;
    private uint handle;

    /** size of the actually written commands */
    private int size;

    /** size of the command buffer in bytes */
    private int capacity;

    private byte* data;

    /** for debugging, a view of the data */
    private byte[] dataView => new Span<byte>(data, capacity).ToArray();


    /** offset in the command buffer where the next command will be written */
    private int offset;

    private bool triggered;

    /** actually an instanced drawelements token but shhhh */
    public static readonly uint drawelementsToken;

    public static readonly uint drawelementsInstancedToken;
    public static readonly uint attribaddressToken;
    public static readonly uint elementAddressToken;

    static CommandBuffer() {
        // initialize the static CMDL buffer if we have the extension
        if (Game.hasCMDL) {
            // get draw elements token
            //drawelementsToken = Game.cmdl.GetCommandHeader(CommandOpcodesNV.DrawElementsCommandNV, 4 * sizeof(int)) -
            //                    16;
            // change len
            //drawelementsToken &= 0xFFF0FFFF;
            //drawelementsToken |= 0x60000;
            

            //Console.Out.WriteLine($"0x{drawelementsToken:x8}");
            // print instanced elements token
            drawelementsToken =
                Game.cmdl.GetCommandHeader(CommandOpcodesNV.DrawElementsCommandNV, 4 * sizeof(int));
            Console.Out.WriteLine($"Draw Elements Token: 0x{drawelementsToken:x8}");
            //drawelementsToken = instancedElementsToken;
            
            drawelementsInstancedToken =
                Game.cmdl.GetCommandHeader(CommandOpcodesNV.DrawElementsInstancedCommandNV, 7 * sizeof(int));
            Console.Out.WriteLine($"Draw Elements Instanced Token: 0x{drawelementsInstancedToken:x8}");

            // get attribute address token
            attribaddressToken =
                Game.cmdl.GetCommandHeader(CommandOpcodesNV.AttributeAddressCommandNV, 4 * sizeof(int));
            Console.Out.WriteLine($"Attribute Address Token: 0x{attribaddressToken:x8}");
            
            // get element address token
            elementAddressToken =
                Game.cmdl.GetCommandHeader(CommandOpcodesNV.ElementAddressCommandNV, 4 * sizeof(int));
            Console.Out.WriteLine($"Element Address Token: 0x{elementAddressToken:x8}");
        }
    }

    public CommandBuffer(Silk.NET.OpenGL.GL GL, int initialCapacity) {
        this.GL = GL;
        capacity = initialCapacity;
        size = 0;

        Game.GL.DeleteBuffer(handle);
        handle = Game.GL.CreateBuffer();
        // initialize to 1024
        size = 0;

        // allocate buffer storage
        GL.NamedBufferData(handle, (uint)capacity, null, VertexBufferObjectUsage.StaticDraw);

        // allocate managed memory for staging data
        data = (byte*)NativeMemory.AlignedAlloc((nuint)capacity, 16);

        Console.Out.WriteLine($"CommandBuffer created with capacity {capacity} bytes");
    }

    public void putData<T>(T src) where T : unmanaged {
        putData(offset, src);
    }

    public void putData<T>(int offset, T src) where T : unmanaged {
        if (offset + sizeof(T) > capacity) {
            
            throw new ArgumentOutOfRangeException(nameof(src), "Not enough space in command buffer");
            /*
            Console.Out.WriteLine("HELP!!!");
            // first, upload / execute
            upload();
            drawCommands(PrimitiveType.Triangles);

            //throw new ArgumentOutOfRangeException(nameof(src), "Not enough space in command buffer");
            // resize the buffer
            int newCapacity = capacity * 2;
            Console.Out.WriteLine($"Resizing command buffer from {capacity} to {newCapacity} bytes");
            capacity = newCapacity;

            GL.DeleteBuffer(handle);
            handle = GL.CreateBuffer();
            // allocate new storage
            GL.NamedBufferStorage(handle, (nuint)capacity, null, BufferStorageMask.DynamicStorageBit);
            // reallocate managed memory
            NativeMemory.AlignedFree(data);
            data = NativeMemory.AlignedAlloc((nuint)capacity, 16);
            size = 0; // reset size to 0, we will upload the data again
            offset = 0; // reset offset to 0, we will write from the start*/
        }

        // copy data to the staging buffer
        Buffer.MemoryCopy(&src, data + offset, capacity - offset, sizeof(T));
        this.offset += sizeof(T);
        if (this.offset > size) {
            size = this.offset; // update size if we wrote more than before
        }
        //Console.Out.WriteLine($"putData: wrote {sizeof(T)} bytes at offset {offset}, new size is {size}");
    }

    /** Reset the length / offset so new commands can be recorded again. */
    public void clear() {
        
        offset = 0;
        size = 0;
        
        // clear data
        //NativeMemory.AlignedFree(data);
        //data = (byte*)NativeMemory.AlignedAlloc((nuint)capacity, 16);
        
        // clear the buffer data
        
        //Buffer.MemoryCopy(null, data, capacity, capacity);
        // clear the OpenGL buffer data
        //GL.InvalidateBufferData(handle);
        
        /* bind something to the buffer address? */
        //Game.vbum.BufferAddressRange(NV.VertexAttribArrayAddressNV, 0, Game.renderer.testidx, 90);
    }

    public void upload() {
        if (!triggered) {
            GL.InvalidateBufferData(handle);
            GL.NamedBufferSubData(handle, 0, (nuint)size, data); // Use data directly!
            //triggered = true;
        }
    }

    /** Actually draw commands with this buffer.
     * English translation of the DrawCommandsNV arguments:
     * enum mode = ogl primitive mode
     * uint buffer = buffer ogl handle
     * const intptr* indirects = offsets into buffer
     * const sizei* sizes = sizes of each segment
     * uint count = number of segments
     */
    public void drawCommands(PrimitiveType mode, uint state) {

        if (triggered) {
            return;
        }
        
        Console.Out.WriteLine("sizes: " + size);
        // if we have no commands, do nothing

        nint* offsets = stackalloc nint[1];
        offsets[0] = 0; // we always start at the beginning of the buffer

        uint* sizes = stackalloc uint[1];
        sizes[0] = (uint)size;
        
        uint* fbos = stackalloc uint[1];
        fbos[0] = 0;
        
        uint* states = stackalloc uint[1];
        states[0] = state;
        
        GL.MemoryBarrier(MemoryBarrierMask.CommandBarrierBit);

        Console.Out.WriteLine(
            $"Drawing commands with mode {mode}, handle {handle}, offsets[0] = {offsets[0]}, sizes[0] = {sizes[0]}");
        if (size > 0) {
            Game.cmdl.DrawCommands((NV)mode, handle, offsets, sizes, 1);
            triggered = true;
            //Game.cmdl.DrawCommandsStates(handle, (nint*)offsets, sizes, fbos, states, 1);
        }
        

        clear();
        //GL.Flush();
        //GL.Finish();
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

    ~CommandBuffer() {
        Dispose();
    }
}