using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BlockGame.GL;

public class BTexture2D : IDisposable {
    public uint handle;

    public Silk.NET.OpenGL.GL GL;

    private readonly Memory<Rgba32> imageData;
    private readonly Image<Rgba32> image;

    public unsafe BTexture2D(string path) {
        GL = Game.GL;

        handle = GL.GenTexture();
        bind();
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 0);
        image = Image.Load<Rgba32>(path);
        GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)image.Width, (uint)image.Height, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, null);
        if (image.DangerousTryGetSinglePixelMemory(out imageData)) {
            //Console.Out.WriteLine("Loading textures the proper way!");
            fixed (Rgba32* pixels = &imageData.Span.GetPinnableReference()) {
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, (uint)image.Width, (uint)image.Height,
                    PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
            }
        }
        else {
            image.ProcessPixelRows(accessor => {
                for (int rowIndex = 0; rowIndex < accessor.Height; ++rowIndex) {
                    fixed (Rgba32* pixels = &imageData.Span.GetPinnableReference()) {
                        GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, rowIndex, (uint)accessor.Width, 1U,
                            PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
                    }
                }
            });
        }
    }

    public BTexture2D(uint width, uint height) {
        unsafe {
            GL = Game.GL;

            handle = GL.GenTexture();
            bind();
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
            image = new Image<Rgba32>((int)width, (int)height);
            GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, width, height, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, null);
        }
    }

    public Rgba32 getPixel(int x, int y) {
        return image[x, y];
    }

    public uint width => (uint)image.Width;

    public uint height => (uint)image.Height;

    public void bind() {
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, handle);
    }

    public void Dispose() {
        GL.DeleteTexture(handle);
    }

    public void SetData<T>(T[] data, int x, int y, uint boundsWidth, uint boundsHeight) where T : unmanaged {
        unsafe {
            bind();
            fixed (T* dataPtr = data) {
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, x, y, boundsWidth, boundsHeight,
                    PixelFormat.Rgba, PixelType.UnsignedByte, dataPtr);
            }
        }
    }
}