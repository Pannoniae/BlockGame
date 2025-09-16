using BlockGame.main;
using Silk.NET.OpenGL.Legacy;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BlockGame.GL;

public class BTexture2D : IEquatable<BTexture2D>, IDisposable {
    public uint handle;

    public Silk.NET.OpenGL.Legacy.GL GL;

    public string path;

    public Memory<Rgba32> imageData;
    public Image<Rgba32> image;

    public BTexture2D(string path) {
        GL = Game.GL;
        this.path = path;
    }

    public virtual unsafe void reload() {
        GL.DeleteTexture(handle);
        handle = GL.CreateTexture(TextureTarget.Texture2D);
        GL.TextureParameter(handle, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        GL.TextureParameter(handle, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
        GL.TextureParameter(handle, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
        GL.TextureParameter(handle, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        GL.TextureParameter(handle, TextureParameterName.TextureBaseLevel, 0);
        GL.TextureParameter(handle, TextureParameterName.TextureMaxLevel, 0);
        image?.Dispose();
        image = Image.Load<Rgba32>(path);
        GL.TextureStorage2D(handle, 1, SizedInternalFormat.Rgba8, (uint)image.Width, (uint)image.Height);
        if (image.DangerousTryGetSinglePixelMemory(out imageData)) {
            //Console.Out.WriteLine("Loading textures the proper way!");
            fixed (Rgba32* pixels = &imageData.Span.GetPinnableReference()) {
                GL.TextureSubImage2D(handle, 0, 0, 0, (uint)image.Width, (uint)image.Height,
                    PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
            }
        }
        else {
            image.ProcessPixelRows(accessor => {
                for (int rowIndex = 0; rowIndex < accessor.Height; ++rowIndex) {
                    fixed (Rgba32* pixels = &imageData.Span.GetPinnableReference()) {
                        GL.TextureSubImage2D(handle, 0, 0, rowIndex, (uint)accessor.Width, 1U,
                            PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
                    }
                }
            });
        }
    }

    public BTexture2D(uint width, uint height) {
        unsafe {
            GL = Game.GL;

            handle = GL.CreateTexture(TextureTarget.Texture2D);
            GL.TextureParameter(handle, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
            GL.TextureParameter(handle, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
            GL.TextureParameter(handle, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
            GL.TextureParameter(handle, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
            GL.TextureParameter(handle, TextureParameterName.TextureBaseLevel, 0);
            GL.TextureParameter(handle, TextureParameterName.TextureMaxLevel, 1);
            image = new Image<Rgba32>((int)width, (int)height);
            GL.TextureStorage2D(handle, 1, SizedInternalFormat.Rgba8, width, height);
        }
    }

    public Rgba32 getPixel(int x, int y) {
        return image[x, y];
    }

    public uint width => (uint)image.Width;

    public uint height => (uint)image.Height;

    public void bind() {
        Game.graphics.tex(0, handle);
    }

    public void Dispose() {
        GL.DeleteTexture(handle);
    }
    
    public unsafe void updateTexture(int left, int top, int width, int height, int srcX, int srcY) {
        // get pixels from the image
        var pixels = new Rgba32[width * height];
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                pixels[y * width + x] = imageData.Span[(srcY + y) * image.Width + srcX + x];
            }
        }
        GL.InvalidateTexImage(handle, 0);
        fixed (Rgba32* pixelsPtr = pixels) {
            GL.TextureSubImage2D(handle, 0, left, top, (uint)width, (uint)height, PixelFormat.Rgba, PixelType.UnsignedByte, pixelsPtr);
        }
    }

    public void updateTexture<T>(T[] data, int x, int y, uint boundsWidth, uint boundsHeight) where T : unmanaged {
        unsafe {
            GL.InvalidateTexImage(handle, 0);
            fixed (T* dataPtr = data) {
                GL.TextureSubImage2D(handle, 0, x, y, boundsWidth, boundsHeight,
                    PixelFormat.Rgba, PixelType.UnsignedByte, dataPtr);
            }
        }
    }

    public bool Equals(BTexture2D? other) {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return handle == other.handle;
    }

    public override bool Equals(object? obj) {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((BTexture2D)obj);
    }

    public override int GetHashCode() {
        return (int)handle;
    }

    public static bool operator ==(BTexture2D? left, BTexture2D? right) {
        return Equals(left, right);
    }

    public static bool operator !=(BTexture2D? left, BTexture2D? right) {
        return !Equals(left, right);
    }
}