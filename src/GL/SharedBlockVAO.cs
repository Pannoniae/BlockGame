using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BlockGame.GL.vertexformats;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.NV;
using Buffer = System.Buffer;
using EnableCap = Silk.NET.OpenGL.Legacy.EnableCap;
using PrimitiveType = Silk.NET.OpenGL.PrimitiveType;

namespace BlockGame.GL;

/// <summary>
/// SharedBlockVAO but we only use one VAO / vertex format then just rebind the vertex/index buffer
/// It also uses only one buffer now instead of two
/// </summary>
public sealed class SharedBlockVAO : VAO {
    public uint VAOHandle;
    public uint buffer;
    public uint count;

    // for NV_vertex_buffer_unified_memory
    public ulong bufferAddress;
    public nuint bufferLength;

    public readonly Silk.NET.OpenGL.GL GL;

    public SharedBlockVAO(uint VAOHandle) {
        this.VAOHandle = VAOHandle;
        GL = Game.GL;
    }

    public void upload(BlockVertexPacked[] data, ushort[] indices) {
        unsafe {
            GL.DeleteBuffer(buffer);
            buffer = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffer);
            count = (uint)indices.Length;
            var vertexSize = (uint)(data.Length * sizeof(BlockVertexPacked));
            fixed (BlockVertexPacked* d = data) {
                GL.BufferStorage(BufferStorageTarget.ArrayBuffer, vertexSize, d,
                    BufferStorageMask.None);
            }
        }

        format();
    }

    public void upload(Span<BlockVertexPacked> data, uint _count) {
        unsafe {
            GL.DeleteBuffer(buffer);
            buffer = GL.CreateBuffer();
            count = (uint)(_count * 1.5);
            var vertexSize = (uint)(data.Length * sizeof(BlockVertexPacked));
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffer);
            fixed (BlockVertexPacked* d = data) {
                GL.BufferStorage(BufferStorageTarget.ArrayBuffer, vertexSize, d,
                    BufferStorageMask.None);
            }

            // name the buffer
            GL.ObjectLabel(ObjectIdentifier.Buffer, buffer, uint.MaxValue, "SharedBlockVAO Buffer");

            // check for unified memory support and get buffer address
            if (Game.hasVBUM) {
                bufferLength = (nuint)vertexSize;
                // make buffer resident first, then get its GPU address
                GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffer);
                //Game.sbl.MakeNamedBufferResident(buffer, (NV)GLEnum.ReadOnly);
                //Game.sbl.GetNamedBufferParameter(buffer, NV.BufferGpuAddressNV, out bufferAddress);
                Game.sbl.MakeNamedBufferResident(buffer, (NV)GLEnum.ReadOnly);
                bufferAddress = Game.sbl.GetNamedBufferParameter(buffer, NV.BufferGpuAddressNV);
                // validate buffer address
                if (bufferAddress == 0) {
                    throw new Exception($"Failed to get GPU address for vertex buffer {buffer}");
                }
                //Console.WriteLine($"SharedBlockVAO: buffer={buffer}, address=0x{bufferAddress:X16}, length={bufferLength}");
            }
        }

        format();
    }

    public void upload(Span<BlockVertexPacked> data, Span<ushort> indices) {
        throw new Exception("this doesn't work!");
    }

    public void format() {
        var GL = Game.GL;

        // NOTE: THE NV_vertex_buffer_unified_memory extension specs are LYING TO YOU!
        // (probably by accident, to be fair, but still...)
        // you can use normal formatting functions with NV_vertex_buffer_unified_memory (in fact one of their [only publicly available?] examples does this)
        // glVertexAttribIFormatNV and glVertexAttribFormatNV literally don't work properly lol
        // it's also lying to you because you do NOT need to set BufferAddressRangeNV for each attribute, you only need it per vertex buffer *binding*.
        // so if you use vertexAttribBinding to hook up the attributes to a binding, you only need to set the address range once for that binding.
        // so we have 3 attributes here but they come from the same buffer -> you only need to set the buffer address once.
        
        
        // cmdlist path
        if (Game.hasCMDL) {
            // regular format setup

            // bind the vertex buffer to the VAO
            GL.BindVertexBuffer(0, buffer, 0, 8 * sizeof(ushort));
            // FAKE BINDING FOR THE BUFFER (only to shut up driver validation)
            GL.BindVertexBuffer(1, Game.graphics.fatQuadIndices, 0, 4 * sizeof(float));

            // 14 bytes in total, 3*2 for pos, 2*2 for uv, 4 bytes for colour
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);
            GL.EnableVertexAttribArray(3);
            GL.EnableVertexAttribArray(4);

            GL.VertexAttribIFormat(0, 3, VertexAttribIType.UnsignedShort, 0);
            GL.VertexAttribIFormat(1, 2, VertexAttribIType.UnsignedShort, 0 + 3 * sizeof(ushort));
            GL.VertexAttribFormat(2, 4, VertexAttribType.UnsignedByte, true, 0 + 5 * sizeof(ushort));
            GL.VertexAttribIFormat(3, 1, VertexAttribIType.Byte, 0 + 7 * sizeof(ushort));
            GL.VertexAttribFormat(4, 4, VertexAttribType.Float, false, 0);

            GL.VertexAttribBinding(0, 0);
            GL.VertexAttribBinding(1, 0);
            GL.VertexAttribBinding(2, 0);
            GL.VertexAttribBinding(3, 0);
            GL.VertexAttribBinding(4, 1); // Different binding point for constant attribute!!

            GL.VertexBindingDivisor(1,
                1); // Set divisor for attribute 1 (chunk position) to 1, so it updates per instance (which we only have one of, so a constant!)


            //GL.BindVertexBuffer(1, handle, 3 * sizeof(ushort), 7 * sizeof(ushort));
            //GL.BindVertexBuffer(2, handle, 5 * sizeof(ushort), 7 * sizeof(ushort));

            // this will work?
            //Game.vbum.BufferAddressRange(NV.VertexAttribArrayAddressNV, 0, 0, 0);
            //Game.vbum.BufferAddressRange(NV.ElementArrayAddressNV, 0, 0, 0);
        }
        // normal path
        else {
            GL.BindVertexBuffer(0, buffer, 0, 8 * sizeof(ushort));

            // 14 bytes in total, 3*2 for pos, 2*2 for uv, 4 bytes for colour
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);
            GL.EnableVertexAttribArray(3);

            GL.VertexAttribIFormat(0, 3, VertexAttribIType.UnsignedShort, 0);
            GL.VertexAttribIFormat(1, 2, VertexAttribIType.UnsignedShort, 0 + 3 * sizeof(ushort));
            GL.VertexAttribFormat(2, 4, VertexAttribType.UnsignedByte, true, 0 + 5 * sizeof(ushort));
            GL.VertexAttribIFormat(3, 1, VertexAttribIType.Byte, 0 + 7 * sizeof(ushort));

            GL.VertexAttribBinding(0, 0);
            GL.VertexAttribBinding(1, 0);
            GL.VertexAttribBinding(2, 0);
            GL.VertexAttribBinding(3, 0);
        }
    }

    public void bindVAO() {
        GL.BindVertexArray(VAOHandle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void bind() {
        if (Game.hasVBUM) {
            // use unified memory path - set vertex attrib addresses directly
            //var addr0 = bufferAddress;
            //var addr1 = bufferAddress + (ulong)(3 * sizeof(ushort));
            //var addr2 = bufferAddress + (ulong)(5 * sizeof(ushort));
            //Console.WriteLine($"Setting vertex attrib addresses: 0=0x{addr0:X16}, 1=0x{addr1:X16}, 2=0x{addr2:X16}");
            Game.vbum.BufferAddressRange(NV.VertexAttribArrayAddressNV, 0, bufferAddress, bufferLength);
            //Game.vbum.BufferAddressRange(NV.VertexAttribArrayAddressNV, 1, addr1, bufferLength - (3 * sizeof(ushort)));
            //Game.vbum.BufferAddressRange(NV.VertexAttribArrayAddressNV, 2, addr2, bufferLength - (5 * sizeof(ushort)));
        }
        else {
            // fallback to regular vertex buffer binding
            GL.BindVertexBuffer(0, buffer, 0, 8 * sizeof(ushort));
        }
    }
    
    public void addCMDLCommand() {
        var cmdBuffer = Game.renderer.chunkCMD;
        // set the vertex buffer address range for the VAO
        //var addr = bufferAddress;
        //Console.Out.WriteLine($"Setting vertex buffer address: 0x{addr:X16}, length={bufferLength} bytes");
        var cmd0 = new AttributeAddressCommandNV {
            header = CommandBuffer.attribaddressToken,
            index = 0,
            addressLo = (uint)(bufferAddress & 0xFFFFFFFF),
            addressHi = (uint)(bufferAddress >> 32),
        };
        cmdBuffer.putData(cmd0);

        // Attribute 1: UV at offset 3*sizeof(ushort) = 6 bytes
        /*var addr1 = bufferAddress;
        var cmd1 = new AttributeAddressCommandNV {
            header = CommandBuffer.attribaddressToken,
            index = 1,
            addressLo = (uint)(addr1 & 0xFFFFFFFF),
            addressHi = (uint)(addr1 >> 32),
        };
        cmdBuffer.putData(cmd1);

        // Attribute 2: color at offset 5*sizeof(ushort) = 10 bytes
        var addr2 = bufferAddress;
        var cmd2 = new AttributeAddressCommandNV {
            header = CommandBuffer.attribaddressToken,
            index = 2,
            addressLo = (uint)(addr2 & 0xFFFFFFFF),
            addressHi = (uint)(addr2 >> 32),
        };
        cmdBuffer.putData(cmd2);*/

        // Element buffer address
        /*var cmdElem = new ElementAddressCommandNV {
            header = CommandBuffer.elementAddressToken,
            addressLo = (uint)(Game.renderer.elementAddress & 0xFFFFFFFF),
            addressHi = (uint)(Game.renderer.elementAddress >> 32),
            typeSizeInByte = sizeof(ushort)
        };
        cmdBuffer.putData(cmdElem);*/

        //Game.vbum.BufferAddressRange(NV.VertexAttribArrayAddressNV, 0, bufferAddress, bufferLength);
        //Console.Out.WriteLine($"Set vertex buffer address for buffer {buffer} : 0x{addr:X16}, length={bufferLength} bytes");

        /*var cmd2 = new AttributeAddressCommandNV {
            header = CommandBuffer.attribaddressToken,
            index = 1,
            addressLo = (uint)((addr + (3 * sizeof(ushort))) & 0xFFFFFFFF),
            addressHi = (uint)((addr + (3 * sizeof(ushort))) >> 32),
        };
        cmdBuffer.putData(cmd2);

        var cmd3 = new AttributeAddressCommandNV {
            header = CommandBuffer.attribaddressToken,
            index = 2,
            addressLo = (uint)((addr + (5 * sizeof(ushort))) & 0xFFFFFFFF),
            addressHi = (uint)((addr + (5 * sizeof(ushort))) >> 32),
        };
        cmdBuffer.putData(cmd3);*/

        //Game.vbum.VertexAttribIFormat(0, 3, (NV)VertexAttribIType.UnsignedShort, 7 * sizeof(ushort));
        //Game.vbum.VertexAttribIFormat(1, 2, (NV)VertexAttribIType.UnsignedShort, 7 * sizeof(ushort));
        //Game.vbum.VertexAttribFormat(2, 4, (NV)VertexAttribType.UnsignedByte, true, 7 * sizeof(ushort));

        //Game.vbum.BufferAddressRange(NV.VertexAttribArrayAddressNV, 0, 0, 69);
        //Game.vbum.BufferAddressRange(NV.ElementArrayAddressNV, 0, 0, 69);
    }

    /** Add a chunk's bindless draw command to the buffer
     * Also stop zeroing lol
     */
    [SkipLocalsInit]
    public unsafe void addChunkCommand(BindlessIndirectBuffer indirect, uint baseInstance, ulong elementAddress,
        uint elementLength) {
        // hardcode sizeof(DrawElementsIndirectBindlessCommandNV) for codegen
        const int commandSize = 72;

        // uncomment this if you want to check for enough space in the indirect buffer
        // i dont think it will ever happen, but it *does* fuck the codegen up so we don't need it
        #if DEBUG
        if (indirect.offset + commandSize > indirect.capacity) {
            throw new InvalidOperationException($"Not enough space in indirect buffer. Need {commandSize} bytes, have {indirect.capacity - indirect.offset} remaining");
        }
        #endif

        DrawElementsIndirectBindlessCommandNV* commandBuffer =
            (DrawElementsIndirectBindlessCommandNV*)((byte*)indirect.data + indirect.offset);
        // Create the complete bindless command structure and just write it in one step
        *commandBuffer = new DrawElementsIndirectBindlessCommandNV {
            cmd = new DrawElementsIndirectCommand {
                count = count,
                instanceCount = 1,
                firstIndex = 0,
                baseVertex = 0,
                baseInstance = baseInstance
            },
            reserved = 0,
            indexBuffer = new BindlessPtrNV {
                index = 0, // element buffer index
                reserved = 0,
                address = elementAddress,
                length = elementLength,
            },
            vertexBuffer = new BindlessPtrNV {
                index = 0, // vertex attribute index
                reserved = 0,
                address = bufferAddress,
                length = bufferLength
            }
        };

        indirect.offset += commandSize;
        if (indirect.offset > indirect.size) {
            indirect.size = indirect.offset;
        }

        indirect.commands++;

        // game metrics
        Game.metrics.renderedVerts += (int)count;
    }

    public uint render() {
        unsafe {
            GL.DrawElements(PrimitiveType.Triangles, count, DrawElementsType.UnsignedShort, (void*)0);
            return count;
        }
    }

    public uint renderBaseInstance(uint baseInstance) {
        unsafe {
            GL.DrawElementsInstancedBaseInstance(PrimitiveType.Triangles, count, DrawElementsType.UnsignedShort,
                (void*)0, 1u, baseInstance);

            return count;
        }
    }
    
    public uint renderCMDL(uint baseInstance) {
        var cmdBuffer = Game.renderer.chunkCMD;
        /*
         * From the NV_command_list spec:
         * Interactions with ARB_shader_draw_parameters
         *
         * The drawing operations performed through this extension will not support
         * setting of the built-in GLSL values that were added by
         * ARB_shader_draw_parameters (gl_BaseInstanceARB, gl_BaseVertexARB, gl_DrawIDARB).
         * Accessing these variables will result in undefined values.
         * i.e. this shit doesn't work
         */

        ulong baseAddress = Game.renderer.ssboaddr;
        ulong chunkPosAddress = baseAddress + (baseInstance * 16); // Each Vector4 is 16 bytes

        // Set attribute 3 to point to the chunk position in SSBO
        var cmd3 = new AttributeAddressCommandNV {
            header = CommandBuffer.attribaddressToken,
            index = 1,
            addressLo = (uint)(chunkPosAddress & 0xFFFFFFFF),
            addressHi = (uint)(chunkPosAddress >> 32),
        };
        cmdBuffer.putData(cmd3);

        // set the draw command parameters
        var cmd = new DrawElementsCommandNV {
            header = CommandBuffer.drawelementsToken,
            //mode = (uint)PrimitiveType.Triangles,
            count = count,
            //instanceCount = 1,
            firstIndex = 0,
            //firstIndex = 0, 
            baseVertex = 0,
            //baseInstance = 0
        };

        //Console.Out.WriteLine(baseInstance);

        cmdBuffer.putData(cmd);
        
        return count;

        //cmdBuffer.upload();
        //cmdBuffer.drawCommands(PrimitiveType.Triangles, 0);
    }

    public void Dispose() {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private void ReleaseUnmanagedResources() {
        if (Game.hasVBUM && Game.hasSBL && buffer != 0) {
            // make buffer non-resident before deleting
            Game.sbl.MakeNamedBufferNonResident(buffer);
        }

        GL.DeleteBuffer(buffer);
    }

    ~SharedBlockVAO() {
        ReleaseUnmanagedResources();
    }
}