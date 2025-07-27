using FontStashSharp.Interfaces;
using FreeTypeSharp;
using FreeTypeSharp;
using System;
using System.Runtime.InteropServices;

namespace FontStashSharp.Rasterizers.FreeType {
    internal unsafe class FreeTypeSource : IFontSource {
        private static FT_LibraryRec_* _libraryHandle;
        private GCHandle _memoryHandle;
        private FT_FaceRec_* _faceHandle;
        private readonly FT_FaceRec_ _rec;


        public unsafe FreeTypeSource(byte[] data) {
            FT_Error err;
            if (_libraryHandle == (FT_LibraryRec_**)0) {
                FT_LibraryRec_* libraryRef = null;
                err = FT.FT_Init_FreeType(&libraryRef);

                if (err != FT_Error.FT_Err_Ok) {
                    throw new FreeTypeException(err);
                }

                _libraryHandle = libraryRef;
                //Console.Out.WriteLine("_libraryHandle: " + (IntPtr)_libraryHandle);
            }

            _memoryHandle = GCHandle.Alloc(data, GCHandleType.Pinned);

            FT_FaceRec_* faceRef = null;
            err = FT.FT_New_Memory_Face(_libraryHandle, (byte*)_memoryHandle.AddrOfPinnedObject(), data.Length, 0,
                &faceRef);

            if (err != FT_Error.FT_Err_Ok) {
                throw new FreeTypeException(err);
            }

            _faceHandle = faceRef;
            _rec = *_faceHandle;
        }

        ~FreeTypeSource() {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing) {
            if (_faceHandle != (FT_FaceRec_*)0) {
                FT.FT_Done_Face(_faceHandle);
                _faceHandle = (FT_FaceRec_*)0;
            }

            if (_memoryHandle.IsAllocated)
                _memoryHandle.Free();
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public int? GetGlyphId(int codepoint) {
            if (_faceHandle == (FT_FaceRec_*)0) {
                Console.Out.WriteLine("FreeTypeSource.GetGlyphId: _faceHandle is null");
                return null;
            }

            var result = FT.FT_Get_Char_Index(_faceHandle, (uint)codepoint);
            if (result == 0) {
                Console.Out.WriteLine(
                    $"FreeTypeSource.GetGlyphId: FT_Get_Char_Index returned 0 for codepoint {codepoint}");
                return null;
            }

            return (int?)result;
        }

        public int GetGlyphKernAdvance(int previousGlyphId, int glyphId, float fontSize) {
            FT_Vector_ kerning;
            if (FT.FT_Get_Kerning(_faceHandle, (uint)previousGlyphId, (uint)glyphId, 0, &kerning) !=
                FT_Error.FT_Err_Ok) {
                Console.Out.WriteLine("FreeTypeSource.GetGlyphKernAdvance: FT_Get_Kerning failed");
                return 0;
            }

            return (int)kerning.x >> 6;
        }

        private void SetPixelSizes(float width, float height) {
            var err = FT.FT_Set_Pixel_Sizes(_faceHandle, (uint)width, (uint)height);
            if (err != FT_Error.FT_Err_Ok)
                throw new FreeTypeException(err);
        }

        private void LoadGlyph(int glyphId) {
            var err = FT.FT_Load_Glyph(_faceHandle, (uint)glyphId, 0 | FT.FT_LOAD_TARGET_NORMAL);
            if (err != FT_Error.FT_Err_Ok)
                throw new FreeTypeException(err);
        }

        private unsafe void GetCurrentGlyph(out FT_GlyphSlotRec_ glyph) {
            glyph = *_rec.glyph;
        }

        public void GetGlyphMetrics(int glyphId, float fontSize, out int advance, out int x0, out int y0, out int x1,
            out int y1) {
            if (_faceHandle == (FT_FaceRec_*)0) {
                advance = x0 = y0 = x1 = y1 = 0;
                Console.Out.WriteLine("FreeTypeSource.GetGlyphMetrics: _faceHandle is null");
                return;
            }

            SetPixelSizes(0, fontSize);
            LoadGlyph(glyphId);

            FT_GlyphSlotRec_ glyph;
            GetCurrentGlyph(out glyph);
            advance = (int)glyph.advance.x >> 6;
            x0 = (int)glyph.metrics.horiBearingX >> 6;
            y0 = -(int)glyph.metrics.horiBearingY >> 6;
            x1 = x0 + ((int)glyph.metrics.width >> 6);
            y1 = y0 + ((int)glyph.metrics.height >> 6);
            
            // NOTE: this font which we are using is 9x16, 4 below the line, 12 above the line. It kind of renders off-centre though
            // hacky solution: render it slightly lower lol
            // this used to exist in the GUI code (just render the text lower) but this is probably a better place for it, it does it globally
            y0 += 2;
            y1 += 2;
        }

        public unsafe void GetMetricsForSize(float fontSize, out int ascent, out int descent, out int lineHeight) {
            if (_faceHandle == (FT_FaceRec_*)0) {
                ascent = descent = lineHeight = 0;
                Console.Out.WriteLine("FreeTypeSource.GetMetricsForSize: _faceHandle is null");
                return;
            }

            SetPixelSizes(0, fontSize);
            var sizeRec = *_rec.size;

            //Console.Out.WriteLine("ascent: " + sizeRec.metrics.ascender);
            //Console.Out.WriteLine("descent: " + sizeRec.metrics.descender);
            //Console.Out.WriteLine("height: " + sizeRec.metrics.height);

            ascent = (int)sizeRec.metrics.ascender >> 6;
            descent = (int)sizeRec.metrics.descender >> 6;
            lineHeight = (int)sizeRec.metrics.height >> 6;
            
            //Console.Out.WriteLine($"FreeTypeSource.GetMetricsForSize: ascent={ascent}, descent={descent}, lineHeight={lineHeight}");
        }

        public unsafe void RasterizeGlyphBitmap(int glyphId, float fontSize, byte[] buffer, int startIndex,
            int outWidth, int outHeight, int outStride) {
            if (_faceHandle == (FT_FaceRec_*)0) {
                Console.Out.WriteLine("FreeTypeSource.RasterizeGlyphBitmap: _faceHandle is null");
                return;
            }

            SetPixelSizes(0, fontSize);
            LoadGlyph(glyphId);

            FT.FT_Render_Glyph(_rec.glyph, FT_Render_Mode_.FT_RENDER_MODE_NORMAL);

            FT_GlyphSlotRec_ glyph;
            GetCurrentGlyph(out glyph);
            var ftbmp = glyph.bitmap;

            if (ftbmp.buffer == (void*)0) {
                return;
            }

            fixed (byte* bptr = buffer) {
                if (ftbmp.pixel_mode == FT_Pixel_Mode_.FT_PIXEL_MODE_MONO) // FT_PIXEL_MODE_MONO - 1 bit per pixel
                {
                    for (var y = 0; y < outHeight; ++y) {
                        var pos = (y * outStride) + startIndex;
                        byte* dst = bptr + pos;
                        byte* src = ftbmp.buffer + y * ftbmp.pitch;

                        for (var x = 0; x < outWidth; ++x) {
                            int byteIndex = x >> 3; // 8 pixels per byte
                            int bitIndex = 7 - (x & 7); // bit index within the byte (0-7)
                            byte srcByte = src[byteIndex];
                            byte bit = (byte)((srcByte >> bitIndex) & 1);
                            
                            // unique (random) colour for every glyph
                            byte c = (byte)glyph.glyph_index;
                            
                            *dst++ = (byte)(bit * 255); // convert 1-bit to 8-bit (0 or 255)
                            //*dst++ = c; // convert 1-bit to 8-bit (0 or 255)
                        }
                    }
                }
                else // FT_PIXEL_MODE_GRAY or other 8-bit per pixel modes
                {
                    for (var y = 0; y < outHeight; ++y) {
                        var pos = (y * outStride) + startIndex;
                        byte* dst = bptr + pos;
                        byte* src = ftbmp.buffer + y * ftbmp.pitch;

                        for (var x = 0; x < outWidth; ++x) {
                            *dst++ = *src++;
                        }
                    }
                }
            }
        }
    }
}