using System.Runtime.CompilerServices;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.NV;
using EnableCap = Silk.NET.OpenGL.Legacy.EnableCap;
using PrimitiveType = Silk.NET.OpenGL.PrimitiveType;

namespace BlockGame.GL;

/// <summary>
/// SharedBlockVAO but we only use one VAO / vertex format then just rebind the vertex/index buffer
/// It also uses only one buffer now instead of two
/// </summary>
public sealed class SharedBlockVAO : VAO
{
    public uint VAOHandle;
    public uint buffer;
    public uint count;
    
    // for NV_vertex_buffer_unified_memory
    private ulong bufferAddress;
    private nuint bufferLength;
    private bool useUnifiedMemory;

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
            buffer = GL.GenBuffer();
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
            useUnifiedMemory = Game.hasVBUM;
            if (useUnifiedMemory) {
                bufferLength = (nuint)vertexSize;
                // make buffer resident first, then get its GPU address
                GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffer);
                Game.sbl.MakeBufferResident((NV)BufferTargetARB.ArrayBuffer, (NV)GLEnum.ReadOnly);
                Game.sbl.GetBufferParameter(BufferTargetARB.ArrayBuffer, NV.BufferGpuAddressNV, out bufferAddress);
                //Console.WriteLine($"SharedBlockVAO: buffer={buffer}, address=0x{bufferAddress:X16}, length={bufferLength}");
            }
        }

        format();
    }

    public void upload(Span<BlockVertexPacked> data, Span<ushort> indices) {
        throw new Exception("this doesn't work!");
    }

    public void format() {
        // 14 bytes in total, 3*2 for pos, 2*2 for uv, 4 bytes for colour
        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);
        
        // NOTE: THE NV_vertex_buffer_unified_memory extension specs are LYING TO YOU!
        // (probably by accident, to be fair, but still...)
        // you can use normal formatting functions with NV_vertex_buffer_unified_memory (in fact one of their [only publicly available?] examples does this)
        // glVertexAttribIFormatNV and glVertexAttribFormatNV literally don't work properly lol
        // it's also lying to you because you do NOT need to set BufferAddressRangeNV for each attribute, you only need it per vertex buffer *binding*.
        // so if you use vertexAttribBinding to hook up the attributes to a binding, you only need to set the address range once for that binding.
        // so we have 3 attributes here but they come from the same buffer -> you only need to set the buffer address once.

        if (false && useUnifiedMemory) {
            // use unified memory format functions
            Game.vbum.VertexAttribIFormat(0, 3, (NV)VertexAttribIType.UnsignedShort, 7 * sizeof(ushort));
            Game.vbum.VertexAttribIFormat(1, 2, (NV)VertexAttribIType.UnsignedShort, 7 * sizeof(ushort));
            Game.vbum.VertexAttribFormat(2, 4, (NV)VertexAttribType.UnsignedByte, true, 7 * sizeof(ushort));
        } else {
            // regular format setup
            GL.VertexAttribIFormat(0, 3, VertexAttribIType.UnsignedShort, 0);
            GL.VertexAttribIFormat(1, 2, VertexAttribIType.UnsignedShort, 0 + 3 * sizeof(ushort));
            GL.VertexAttribFormat(2, 4, VertexAttribType.UnsignedByte, true, 0 + 5 * sizeof(ushort));

            GL.VertexAttribBinding(0, 0);
            GL.VertexAttribBinding(1, 0);
            GL.VertexAttribBinding(2, 0);
            
            // bind the vertex buffer to the VAO
            GL.BindVertexBuffer(0, buffer, 0, 7 * sizeof(ushort));
        }
    }

    public void bindVAO() {
        GL.BindVertexArray(VAOHandle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void bind() {
        if (useUnifiedMemory) {
            // use unified memory path - set vertex attrib addresses directly
            //var addr0 = bufferAddress;
            //var addr1 = bufferAddress + (ulong)(3 * sizeof(ushort));
            //var addr2 = bufferAddress + (ulong)(5 * sizeof(ushort));
            //Console.WriteLine($"Setting vertex attrib addresses: 0=0x{addr0:X16}, 1=0x{addr1:X16}, 2=0x{addr2:X16}");
            Game.vbum.BufferAddressRange(NV.VertexAttribArrayAddressNV, 0, bufferAddress, bufferLength);
            //Game.vbum.BufferAddressRange(NV.VertexAttribArrayAddressNV, 1, addr1, bufferLength - (3 * sizeof(ushort)));
            //Game.vbum.BufferAddressRange(NV.VertexAttribArrayAddressNV, 2, addr2, bufferLength - (5 * sizeof(ushort)));
        } else {
            // fallback to regular vertex buffer binding
            GL.BindVertexBuffer(0, buffer, 0, 7 * sizeof(ushort));
        }
    }

    public uint render() {
        unsafe {
            GL.DrawElements(PrimitiveType.Triangles, count, DrawElementsType.UnsignedShort, (void*)0);
            return count;
        }
    }

    public uint renderBaseInstance(uint baseInstance) {
        unsafe {
            GL.DrawElementsInstancedBaseInstance(PrimitiveType.Triangles, count, DrawElementsType.UnsignedShort, (void*)0, 1, baseInstance);
            return count;
        }
    }

    public void Dispose() {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private void ReleaseUnmanagedResources() {
        if (useUnifiedMemory && Game.hasSBL && buffer != 0) {
            // make buffer non-resident before deleting
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffer);
            Game.sbl.MakeBufferNonResident((NV)BufferTargetARB.ArrayBuffer);
        }
        GL.DeleteBuffer(buffer);
    }

    ~SharedBlockVAO() {
        ReleaseUnmanagedResources();
    }
}