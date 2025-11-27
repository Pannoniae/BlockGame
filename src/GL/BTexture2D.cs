using System.Diagnostics.CodeAnalysis;
using BlockGame.main;
using BlockGame.util;
using Silk.NET.OpenGL.Legacy;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BlockGame.GL;

public class BTexture2D : IEquatable<BTexture2D>, IDisposable {
    public uint handle;

    public string? path;

    public int width;
    public int height;
    public double iwidth;
    public double iheight;

    public Memory<Rgba32> imageData;
    public Image<Rgba32> image = null!;

    public BTexture2D(string path) {
        this.path = path;
    }

    public virtual unsafe void reload() {
        var GL = Game.GL;
        GL.DeleteTexture(handle);
        handle = GL.CreateTexture(TextureTarget.Texture2D);
        GL.TextureParameter(handle, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        GL.TextureParameter(handle, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
        GL.TextureParameter(handle, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
        GL.TextureParameter(handle, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        GL.TextureParameter(handle, TextureParameterName.TextureBaseLevel, 0);
        GL.TextureParameter(handle, TextureParameterName.TextureMaxLevel, 0);
        image?.Dispose();
        using var s = Assets.open(path!);
        image = Image.Load<Rgba32>(s);
        uploadImage(GL, image);
    }

    /** load from absolute path (not assets/) */
    public unsafe void loadFromFile(string absolutePath) {
        var GL = Game.GL;
        GL.DeleteTexture(handle);
        handle = GL.CreateTexture(TextureTarget.Texture2D);
        GL.TextureParameter(handle, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        GL.TextureParameter(handle, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
        GL.TextureParameter(handle, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
        GL.TextureParameter(handle, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        GL.TextureParameter(handle, TextureParameterName.TextureBaseLevel, 0);
        GL.TextureParameter(handle, TextureParameterName.TextureMaxLevel, 0);
        image?.Dispose();
        image = Image.Load<Rgba32>(absolutePath);
        uploadImage(GL, image);
    }

    /** load from PNG byte array (for network-received skins) */
    public unsafe void loadFromBytes(byte[] pngData) {
        var GL = Game.GL;
        GL.DeleteTexture(handle);
        handle = GL.CreateTexture(TextureTarget.Texture2D);
        GL.TextureParameter(handle, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        GL.TextureParameter(handle, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
        GL.TextureParameter(handle, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
        GL.TextureParameter(handle, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        GL.TextureParameter(handle, TextureParameterName.TextureBaseLevel, 0);
        GL.TextureParameter(handle, TextureParameterName.TextureMaxLevel, 0);
        image?.Dispose();
        using var ms = new MemoryStream(pngData);
        image = Image.Load<Rgba32>(ms);
        uploadImage(GL, image);
    }

    /** load from Image<Rgba32> (for texture pack icons) */
    public unsafe void loadFromImage(Image<Rgba32> img) {
        var GL = Game.GL;
        GL.DeleteTexture(handle);
        handle = GL.CreateTexture(TextureTarget.Texture2D);
        GL.TextureParameter(handle, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        GL.TextureParameter(handle, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
        GL.TextureParameter(handle, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
        GL.TextureParameter(handle, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        GL.TextureParameter(handle, TextureParameterName.TextureBaseLevel, 0);
        GL.TextureParameter(handle, TextureParameterName.TextureMaxLevel, 0);
        image?.Dispose();
        image = img; // take ownership
        uploadImage(GL, image);
    }

    /** check if image has enough opaque pixels (not invisible) */
    public static bool validateTransparency(Image<Rgba32> img) {
        int totalPixels = img.Width * img.Height;
        int opaquePixels = 0;

        img.ProcessPixelRows(accessor => {
            for (int y = 0; y < accessor.Height; y++) {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++) {
                    if (row[x].A >= 128) {
                        opaquePixels++;
                    }
                }
            }
        });

        // require at least 30% opaque pixels
        return opaquePixels >= totalPixels * 0.3f;
    }

    private unsafe void uploadImage(Silk.NET.OpenGL.Legacy.GL GL, Image<Rgba32> img) {
        GL.TextureStorage2D(handle, 1, SizedInternalFormat.Rgba8, (uint)img.Width, (uint)img.Height);

        if (Game.isAMDCard) {
            // old AMD drivers have R/B swap bug, convert to BGRA and upload
            using var bgra = img.CloneAs<Bgra32>();
            if (bgra.DangerousTryGetSinglePixelMemory(out var bgraData)) {
                fixed (Bgra32* pixels = &bgraData.Span.GetPinnableReference()) {
                    GL.TextureSubImage2D(handle, 0, 0, 0, (uint)bgra.Width, (uint)bgra.Height,
                        PixelFormat.Bgra, PixelType.UnsignedByte, pixels);
                }
            }
            else {
                bgra.ProcessPixelRows(accessor => {
                    for (int rowIndex = 0; rowIndex < accessor.Height; ++rowIndex) {
                        var row = accessor.GetRowSpan(rowIndex);
                        fixed (Bgra32* pixels = &row.GetPinnableReference()) {
                            GL.TextureSubImage2D(handle, 0, 0, rowIndex, (uint)accessor.Width, 1U,
                                PixelFormat.Bgra, PixelType.UnsignedByte, pixels);
                        }
                    }
                });
            }
        }
        else {
            if (img.DangerousTryGetSinglePixelMemory(out imageData)) {
                fixed (Rgba32* pixels = &imageData.Span.GetPinnableReference()) {
                    GL.TextureSubImage2D(handle, 0, 0, 0, (uint)img.Width, (uint)img.Height,
                        PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
                }
            }
            else {
                img.ProcessPixelRows(accessor => {
                    for (int rowIndex = 0; rowIndex < accessor.Height; ++rowIndex) {
                        fixed (Rgba32* pixels = &imageData.Span.GetPinnableReference()) {
                            GL.TextureSubImage2D(handle, 0, 0, rowIndex, (uint)accessor.Width, 1U,
                                PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
                        }
                    }
                });
            }
        }

        width = img.Width;
        height = img.Height;
        iwidth = 1.0 / width;
        iheight = 1.0 / height;
    }

    public BTexture2D(uint width, uint height, bool linear = false) {
        unsafe {
            var GL = Game.GL;

            handle = GL.CreateTexture(TextureTarget.Texture2D);
            GL.TextureParameter(handle, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
            GL.TextureParameter(handle, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
            var filter = linear ? GLEnum.Linear : GLEnum.Nearest;
            GL.TextureParameter(handle, TextureParameterName.TextureMinFilter, (int)filter);
            GL.TextureParameter(handle, TextureParameterName.TextureMagFilter, (int)filter);
            GL.TextureParameter(handle, TextureParameterName.TextureBaseLevel, 0);
            GL.TextureParameter(handle, TextureParameterName.TextureMaxLevel, 1);
            image = new Image<Rgba32>((int)width, (int)height);
            GL.TextureStorage2D(handle, 1, SizedInternalFormat.Rgba8, width, height);
        }

        this.width = (int)width;
        this.height = (int)height;
        iwidth = 1.0 / this.width;
        iheight = 1.0 / this.height;
    }

    public Rgba32 getPixel(int x, int y) {
        return image[x, y];
    }

    public void bind() {
        Game.graphics.tex(0, handle);
    }

    public virtual void Dispose() {
        Game.GL.DeleteTexture(handle);
        GC.SuppressFinalize(this);
    }
    
    public unsafe void updateTexture(int left, int top, int width, int height, int srcX, int srcY) {
        if (Game.isAMDCard) {
            // old AMD drivers, need BGRA
            var pixels = new Bgra32[width * height];
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    var p = imageData.Span[(srcY + y) * image.Width + srcX + x];
                    pixels[y * width + x] = new Bgra32(p.R, p.G, p.B, p.A);
                }
            }
            Game.GL.InvalidateTexImage(handle, 0);
            fixed (Bgra32* pixelsPtr = pixels) {
                Game.GL.TextureSubImage2D(handle, 0, left, top, (uint)width, (uint)height, PixelFormat.Bgra, PixelType.UnsignedByte, pixelsPtr);
            }
        }
        else {
            var pixels = new Rgba32[width * height];
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    pixels[y * width + x] = imageData.Span[(srcY + y) * image.Width + srcX + x];
                }
            }
            Game.GL.InvalidateTexImage(handle, 0);
            fixed (Rgba32* pixelsPtr = pixels) {
                Game.GL.TextureSubImage2D(handle, 0, left, top, (uint)width, (uint)height, PixelFormat.Rgba, PixelType.UnsignedByte, pixelsPtr);
            }
        }
    }

    public void updateTexture(Rgba32[] data, int x, int y, uint boundsWidth, uint boundsHeight, bool inv = true) {
        unsafe {
            if (Game.isAMDCard) {
                // old AMD drivers, convert to BGRA
                var bgra = new Bgra32[data.Length];
                for (int i = 0; i < data.Length; i++) {
                    var p = data[i];
                    bgra[i] = new Bgra32(p.R, p.G, p.B, p.A);
                }
                fixed (Bgra32* dataPtr = bgra) {
                    Game.GL.InvalidateTexImage(handle, 0);
                    Game.GL.TextureSubImage2D(handle, 0, x, y, boundsWidth, boundsHeight,
                        PixelFormat.Bgra, PixelType.UnsignedByte, dataPtr);
                }
            }
            else {
                fixed (Rgba32* dataPtr = data) {
                    Game.GL.InvalidateTexImage(handle, 0);
                    Game.GL.TextureSubImage2D(handle, 0, x, y, boundsWidth, boundsHeight,
                        PixelFormat.Rgba, PixelType.UnsignedByte, dataPtr);
                }
            }
        }
    }

    // overload for raw RGBA byte arrays
    public void updateTexture(byte[] data, int x, int y, uint boundsWidth, uint boundsHeight, bool inv = true) {
        unsafe {
            if (Game.isAMDCard) {
                // convert RGBA bytes to BGRA
                int pixelCount = data.Length / 4;
                var bgra = new Bgra32[pixelCount];
                fixed (byte* src = data) {
                    var rgba = (Rgba32*)src;
                    for (int i = 0; i < pixelCount; i++) {
                        var p = rgba[i];
                        bgra[i] = new Bgra32(p.R, p.G, p.B, p.A);
                    }
                }
                fixed (Bgra32* dataPtr = bgra) {
                    Game.GL.InvalidateTexImage(handle, 0);
                    Game.GL.TextureSubImage2D(handle, 0, x, y, boundsWidth, boundsHeight,
                        PixelFormat.Bgra, PixelType.UnsignedByte, dataPtr);
                }
            }
            else {
                fixed (byte* dataPtr = data) {
                    Game.GL.InvalidateTexImage(handle, 0);
                    Game.GL.TextureSubImage2D(handle, 0, x, y, boundsWidth, boundsHeight,
                        PixelFormat.Rgba, PixelType.UnsignedByte, dataPtr);
                }
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

    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
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