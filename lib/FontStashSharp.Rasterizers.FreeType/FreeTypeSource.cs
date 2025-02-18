using FontStashSharp.Interfaces;
using FreeTypeSharp;
using FreeTypeSharp;
using System;
using System.Runtime.InteropServices;

namespace FontStashSharp.Rasterizers.FreeType
{
	internal unsafe sealed class FreeTypeSource: IFontSource
	{
		private static FT_LibraryRec_* _libraryHandle;
		private GCHandle _memoryHandle;
		private FT_FaceRec_* _faceHandle;
		private readonly FT_FaceRec_ _rec;



		unsafe public FreeTypeSource(byte[] data)
		{
			FT_Error err;
			if (_libraryHandle == (FT_LibraryRec_**)0) {
				fixed (FT_LibraryRec_** a = &_libraryHandle) {
					FT_LibraryRec_** libraryRef = a;
					err = FT.FT_Init_FreeType(libraryRef!);


					if (err != FT_Error.FT_Err_Ok)
						throw new FreeTypeException(err);

					_libraryHandle = *libraryRef;
				}
			}

			_memoryHandle = GCHandle.Alloc(data, GCHandleType.Pinned);

			//FT_FaceRec_** faceRef = null;
			fixed (FT_FaceRec_** a = &_faceHandle) {
				err = FT.FT_New_Memory_Face(_libraryHandle, (byte*)_memoryHandle.AddrOfPinnedObject(), data.Length, 0, a);
			}

			if (err != FT_Error.FT_Err_Ok)
				throw new FreeTypeException(err);

			//_faceHandle = faceRef;
			_rec = *_faceHandle;
		}

		~FreeTypeSource()
		{
			Dispose(false);
		}

		private void Dispose(bool disposing)
		{
			if (_faceHandle != (void*)0)
			{
				FT.FT_Done_Face(_faceHandle);
				_faceHandle = (FT_FaceRec_*)0;
			}

			if (_memoryHandle.IsAllocated)
				_memoryHandle.Free();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public int? GetGlyphId(int codepoint)
		{
			var result = FT.FT_Get_Char_Index(_faceHandle, (uint)codepoint);
			if (result == 0)
			{
				return null;
			}

			return (int?)result;
		}

		public int GetGlyphKernAdvance(int previousGlyphId, int glyphId, float fontSize)
		{
			FT_Vector_* kerning = (FT_Vector_*)0;
			if (FT.FT_Get_Kerning(_faceHandle, (uint)previousGlyphId, (uint)glyphId, 0, kerning) != FT_Error.FT_Err_Ok)
			{
				return 0;
			}

			return (int)kerning->x >> 6;
		}

		private void SetPixelSizes(float width, float height)
		{
			var err = FT.FT_Set_Pixel_Sizes(_faceHandle, (uint)width, (uint)height);
			if (err != FT_Error.FT_Err_Ok)
				throw new FreeTypeException(err);
		}

		private void LoadGlyph(int glyphId)
		{
			var err = FT.FT_Load_Glyph(_faceHandle, (uint)glyphId, (FT_LOAD)(0 | FT.FT_LOAD_TARGET_MONO));
			if (err != FT_Error.FT_Err_Ok)
				throw new FreeTypeException(err);
		}

		private unsafe void GetCurrentGlyph(out FT_GlyphSlotRec_ glyph)
		{
			glyph = *_rec.glyph;
		}

		public void GetGlyphMetrics(int glyphId, float fontSize, out int advance, out int x0, out int y0, out int x1, out int y1)
		{
			SetPixelSizes(0, fontSize);
			LoadGlyph(glyphId);

			FT_GlyphSlotRec_ glyph;
			GetCurrentGlyph(out glyph);
			advance = (int)glyph.advance.x >> 6;
			x0 = (int)glyph.metrics.horiBearingX >> 6;
			y0 = -(int)glyph.metrics.horiBearingY >> 6;
			x1 = x0 + ((int)glyph.metrics.width >> 6);
			y1 = y0 + ((int)glyph.metrics.height >> 6);
		}

		public unsafe void GetMetricsForSize(float fontSize, out int ascent, out int descent, out int lineHeight)
		{
			SetPixelSizes(0, fontSize);
			var sizeRec = *_rec.size;

			ascent = (int)sizeRec.metrics.ascender >> 6;
			descent = (int)sizeRec.metrics.descender >> 6;
			lineHeight = (int)sizeRec.metrics.height >> 6;
		}

		public unsafe void RasterizeGlyphBitmap(int glyphId, float fontSize, byte[] buffer, int startIndex, int outWidth, int outHeight, int outStride)
		{
			SetPixelSizes(0, fontSize);
			LoadGlyph(glyphId);

			FT.FT_Render_Glyph(_rec.glyph, FT_Render_Mode_.FT_RENDER_MODE_MONO);

			FT_GlyphSlotRec_ glyph;
			GetCurrentGlyph(out glyph);
			var ftbmp = glyph.bitmap;

			/*fixed (byte* bptr = buffer)
			{
				for (var y = 0; y < outHeight; ++y)
				{
					var pos = (y * outStride) + startIndex;

					byte* dst = bptr + pos;
					byte* src = (byte*)ftbmp.buffer + y * ftbmp.pitch;
					for (var x = 0; x < outWidth; ++x)
					{
						*dst++ = (byte)(*src++ * 255);
					}
				}
			}*/
			fixed (byte* bptr = buffer)
			{
				for (var y = 0; y < outHeight; ++y)
				{
					var pos = (y * outStride) + startIndex;

					byte* dst = bptr + pos;
					byte* src = (byte*)ftbmp.buffer + y * ftbmp.pitch;
					byte bit = *src;
					for (var x = 0; x < outWidth; ++x, bit <<= 1)
					{
						if ((x & 7) == 0)
							bit = *src++;
						*dst++ = (byte)((bit & 128) == 0 ? 0 : 255);
					}
				}
			}
		}
	}
}
