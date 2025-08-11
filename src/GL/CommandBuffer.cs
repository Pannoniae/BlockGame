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
    public uint firstIndex;
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
    public int baseVertex;
    public uint baseInstance;
};

/// <summary>
/// Standard DrawElementsIndirectCommand structure  
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct DrawElementsIndirectBindlessCommandNV {
    public DrawElementsIndirectCommand cmd;
    public uint reserved;
    public BindlessPtrNV indexBuffer;
    public BindlessPtrNV vertexBuffer; // only one here!
};

/** <summary>
 * Holds a buffer object for a NV_command_list DrawCommands buffer.
 * </summary>
 *
 * <remarks>
 * <para>
 * NOTE: this extension is *slightly* janky for OBVIOUS reasons (after all, we're just pushing tokens into the GPU's decoder)
 * What you need to know:
 * </para>
 * 
 * <para>
 * 1. Validation happens *before* any of the commands are seen, so ALWAYS have the attribute data properly formatted, and each buffer binding point bound.
 * This is obviously VAO state, so you need to do the following:
 * Even if they are completely bogus bindings, have them bound. Use glBindVertexBuffer to associate the attributes with the buffer binding points.
 * The buffers you bind in there are completely irrelevant, you only care that you have a good combination of glVertexAttribBinding(attrib, binding) and
 * glBindVertexBuffer(binding, bogusBuffer, irrelevantOffset, correctStride);
 * The stride is important for sourcing vertex buffer data.
 * </para>
 *
 * <para>
 * 2. Additionally, before executing any glDrawCommandsNV, you need to have addresses for each binding point. The NV specs are being unclear/shit about this point,
 * but every time you see a reference to attributes in any of the bindless address extensions (where you give a GPU address to an attribute,
 * stuff like the AttributeAddressCommandNV token or glBufferAddressRangeNV(VERTEX_ATTRIB_ARRAY_ADDRESS_NV). These might SAY attribute, but they in reality refer to binding points.
 * </para>
 *
 * <para>
 * If you don't do these things, shit *might* (read: will probably) still work, but the driver will keep throwing bogus validation errors, which is something you don't want. (you either mask them in the debug callback and risk masking important correctness issues,
 * or you do print them but they'll cause an enormous logspam, neither are good.)
 * </para><para>
 * Keeping the driver happy is easy and doesn't cost shit in performance, so always do that.
 * </para>
 * 
 * <para>
 * 3. ARB_shader_draw_parameters do *not* work with this extension for some godforsaken reason. It's okay, but that means that you can't use gl_BaseInstance as a makeshift constant buffer.
 * You *can* use per-instance data though, so a good way of passing very small constants (like a buffer index or something) are "constant" vertex attributes, i.e. something with a >0 divisor,
 * which means it increments per-instance. Set to a high value if you want it constant over instances, set to 1 if you want it per instance.
 * MAKE SURE TO CALL THE RIGHT FUNCTION. It's glVertexBindingDivisor, NOT glVertexAttribDivisor. Forget about the second one, it doesn't exist.
 * You can't use gl_BaseVertex either, and shit like gl_VertexIndex or gl_InstanceIndex are out of the question too.
 * </para>
 * 
 * <para>
 * 4. As said earlier, we are pushing tokens into the GPU's decoder buffer. This means the only validation you get is a great fucking segfault of your process or an assertion hardcrash inside the driver, which isn't really helpful.
 * I've included a very limited and very janky (read: written by LLM) validator for your command buffers which validates that you didn't fuck up the writing or the headers.
 * Call cmdBuffer.dumpCommands() just before you issue a drawCommands() and check the console for errors.
 * </para>
 * </remarks> 
*/
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

    private static nint[] offsets;
    private static uint[] sizes;


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

            //offsets = new nint[256000];
            //sizes = new uint[256000];

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
        //Buffer.MemoryCopy(&src, data + offset, capacity - offset, sizeof(T));


        byte* location = (data + offset);
        // write the entire structure, not just one byte
        *(T*)location = src;

        this.offset += sizeof(T);
        if (this.offset > size) {
            size = this.offset;
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
        //if (triggered) {
        //    return;
        //}

        //Console.Out.WriteLine("sizes: " + size);
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

        //Console.Out.WriteLine(
        //    $"Drawing commands with mode {mode}, handle {handle}, offsets[0] = {offsets[0]}, sizes[0] = {sizes[0]}");
        if (size > 0) {
            //fixed (nint* offsetsPtr = offsets) {
                //fixed (uint* sizesPtr = sizes) {
                    Game.cmdl.DrawCommands((NV)mode, handle, offsets, sizes, 1);
                    //triggered = true;
                //}
            //}
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

    /// <summary>
    /// Dumps the contents of the command buffer, listing all tokens and their data.
    /// Also validates the structure and reports any issues found.
    /// </summary>
    public void dumpCommands() {
        if (size == 0) {
            Console.WriteLine("CommandBuffer is empty (size = 0)");
            return;
        }

        Console.WriteLine($"CommandBuffer Dump (size: {size} bytes, capacity: {capacity} bytes)");
        Console.WriteLine("=" + new string('=', 60));

        int pos = 0;
        int cmdIndex = 0;
        List<string> validationErrors = [];

        while (pos < size) {
            if (pos + sizeof(uint) > size) {
                validationErrors.Add($"ERROR at offset {pos}: Not enough bytes remaining for command header");
                break;
            }

            // read the header token
            uint header = *(uint*)(data + pos);
            
            Console.WriteLine($"Command #{cmdIndex} at offset {pos:X4} (0x{pos:X8}):");
            Console.WriteLine($"  Header: 0x{header:X8}");

            // identify command type and parse accordingly
            if (header == drawelementsToken) {
                if (pos + sizeof(DrawElementsCommandNV) > size) {
                    validationErrors.Add($"ERROR: DrawElementsCommandNV at offset {pos} extends beyond buffer");
                    break;
                }

                var cmd = *(DrawElementsCommandNV*)(data + pos);
                Console.WriteLine($"  Type: DrawElementsCommandNV");
                Console.WriteLine($"  Count: {cmd.count}");
                Console.WriteLine($"  FirstIndex: {cmd.firstIndex}");
                Console.WriteLine($"  BaseVertex: {cmd.baseVertex}");
                
                // validation
                if (cmd.header != drawelementsToken) {
                    validationErrors.Add($"WARNING: Header mismatch in DrawElementsCommandNV at offset {pos}");
                }

                pos += sizeof(DrawElementsCommandNV);
            }
            else if (header == drawelementsInstancedToken) {
                if (pos + sizeof(DrawElementsInstancedCommandNV) > size) {
                    validationErrors.Add($"ERROR: DrawElementsInstancedCommandNV at offset {pos} extends beyond buffer");
                    break;
                }

                var cmd = *(DrawElementsInstancedCommandNV*)(data + pos);
                Console.WriteLine($"  Type: DrawElementsInstancedCommandNV");
                Console.WriteLine($"  Mode: {cmd.mode}");
                Console.WriteLine($"  Count: {cmd.count}");
                Console.WriteLine($"  InstanceCount: {cmd.instanceCount}");
                Console.WriteLine($"  First: {cmd.firstIndex}");
                Console.WriteLine($"  BaseVertex: {cmd.baseVertex}");
                Console.WriteLine($"  BaseInstance: {cmd.baseInstance}");

                // validation
                if (cmd.header != drawelementsInstancedToken) {
                    validationErrors.Add($"WARNING: Header mismatch in DrawElementsInstancedCommandNV at offset {pos}");
                }

                pos += sizeof(DrawElementsInstancedCommandNV);
            }
            else if (header == attribaddressToken) {
                if (pos + sizeof(AttributeAddressCommandNV) > size) {
                    validationErrors.Add($"ERROR: AttributeAddressCommandNV at offset {pos} extends beyond buffer");
                    break;
                }

                var cmd = *(AttributeAddressCommandNV*)(data + pos);
                Console.WriteLine($"  Type: AttributeAddressCommandNV");
                Console.WriteLine($"  Index: {cmd.index}");
                Console.WriteLine($"  Address: 0x{((ulong)cmd.addressHi << 32 | cmd.addressLo):X16}");

                // validation
                if (cmd.header != attribaddressToken) {
                    validationErrors.Add($"WARNING: Header mismatch in AttributeAddressCommandNV at offset {pos}");
                }

                pos += sizeof(AttributeAddressCommandNV);
            }
            else if (header == elementAddressToken) {
                if (pos + sizeof(ElementAddressCommandNV) > size) {
                    validationErrors.Add($"ERROR: ElementAddressCommandNV at offset {pos} extends beyond buffer");
                    break;
                }

                var cmd = *(ElementAddressCommandNV*)(data + pos);
                Console.WriteLine($"  Type: ElementAddressCommandNV");
                Console.WriteLine($"  Address: 0x{((ulong)cmd.addressHi << 32 | cmd.addressLo):X16}");
                Console.WriteLine($"  TypeSizeInByte: {cmd.typeSizeInByte}");

                // validation
                if (cmd.header != elementAddressToken) {
                    validationErrors.Add($"WARNING: Header mismatch in ElementAddressCommandNV at offset {pos}");
                }

                pos += sizeof(ElementAddressCommandNV);
            }
            else {
                // unknown command
                validationErrors.Add($"ERROR: Unknown command header 0x{header:X8} at offset {pos}");
                Console.WriteLine($"  Type: UNKNOWN (0x{header:X8})");
                
                // try to advance by 4 bytes to potentially recover
                pos += sizeof(uint);
            }

            Console.WriteLine();
            cmdIndex++;

            // safety check to prevent infinite loops
            if (cmdIndex > 10000) {
                validationErrors.Add("ERROR: Too many commands parsed, possible corruption or infinite loop");
                break;
            }
        }

        Console.WriteLine($"Total commands parsed: {cmdIndex}");
        Console.WriteLine($"Final position: {pos} / {size} bytes");

        // print validation results
        if (validationErrors.Count > 0) {
            Console.WriteLine("\nValidation Issues:");
            Console.WriteLine("-" + new string('-', 30));
            foreach (var error in validationErrors) {
                Console.WriteLine($"  {error}");
            }
        } else {
            Console.WriteLine("\nValidation: All commands appear to be valid ✓");
        }

        Console.WriteLine("=" + new string('=', 60));
    }
}