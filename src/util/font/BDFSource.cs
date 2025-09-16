using FontStashSharp.Base;
using FontStashSharp.Interfaces;

namespace BlockGame.util.font;

public sealed class BDFSource : IFontSource {

    public BdfFont font;
    public int width;
    public int height;
    public int yOffset;

    public BDFSource(byte[] data) {
        font = BdfFont.Load(new MemoryStream(data));
        width = font.FontBoundingBox[0];
        height = font.FontBoundingBox[1];
        // WTF?
        yOffset = -height - font.FontBoundingBox[3];
    }


    public void Dispose() {
    }

    public void GetMetricsForSize(float fontSize, out int ascent, out int descent, out int lineHeight) {
        ascent = font.Ascent;
        descent = font.Descent;
        lineHeight = height;
    }

    public int? GetGlyphId(int codepoint) {
        font.Characters.TryGetValue(codepoint, out var glyph);
        return glyph != null ? codepoint : null;
    }

    public void GetGlyphMetrics(int glyphId, float fontSize, out int advance, out int x0, out int y0, out int x1, out int y1) {
        advance = width;
        x0 = 0;
        y0 = 0 + yOffset;
        x1 = width;
        y1 = height + yOffset;
    }

    unsafe public void RasterizeGlyphBitmap(int glyphId, float fontSize, byte[] buffer, int startIndex, int outWidth, int outHeight, int outStride) {
        var hasGlyph = font.Characters.TryGetValue(glyphId, out var glyph);
        // sub it with empty array if it doesn't exist
        byte[,] array = hasGlyph ? glyph.Bitmap : new byte[height, width];
        fixed (byte* bptr = buffer) {
            for (var y = 0; y < outHeight; ++y) {
                var pos = (y * outStride) + startIndex;

                byte* dst = bptr + pos;
                //byte* src = (byte*)glyph.Bitmap[0, y];
                for (var x = 0; x < outWidth; ++x) {
                    *dst++ = (byte)(array[y, x] * 255);
                }
            }
        }
    }

    public int GetGlyphKernAdvance(int previousGlyphId, int glyphId, float fontSize) {
        return 0;
    }
}