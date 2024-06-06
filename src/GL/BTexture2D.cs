using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BlockGame;

public class BTexture2D : IDisposable {
    public uint handle;

    public GL GL;

    public unsafe BTexture2D(string path) {
        GL = Game.GL;

        handle = GL.GenTexture();
        bind();
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        using (Image<Rgba32> image = Image.Load<Rgba32>(path)) {
            GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)image.Width, (uint)image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
            Memory<Rgba32> memory;
            if (image.DangerousTryGetSinglePixelMemory(out memory)) {
                Console.Out.WriteLine("Loading textures the proper way!");
                fixed (Rgba32* pixels = &memory.Span.GetPinnableReference()) {
                    GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, (uint)image.Width, (uint)image.Height,
                        PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
                }
            }
            else {
                image.ProcessPixelRows(accessor => {
                    for (int rowIndex = 0; rowIndex < accessor.Height; ++rowIndex) {
                        fixed (Rgba32* pixels = &memory.Span.GetPinnableReference()) {
                            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, rowIndex, (uint)accessor.Width, 1U,
                                PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
                        }
                    }
                });
            }
        }
    }

    public void bind() {
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, handle);
    }

    public void Dispose() {
        // TODO release managed resources here
        GL.DeleteTexture(handle);
    }
}