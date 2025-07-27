﻿using FontStashSharp.Interfaces;
using FreeTypeSharp;
using FreeTypeSharp;
using System;
using System.Runtime.InteropServices;

namespace FontStashSharp.Rasterizers.FreeType
{
	internal unsafe class FreeTypeSource : IFontSource
	{
		private static FT_LibraryRec_* _libraryHandle;
		private GCHandle _memoryHandle;
		private FT_FaceRec_* _faceHandle;
		private readonly FT_FaceRec_  _rec;



		public unsafe FreeTypeSource(byte[] data)
		{
			FT_Error err;
			if (_libraryHandle == (FT_LibraryRec_**)0) {
				FT_LibraryRec_* libraryRef = null;
				err = FT.FT_Init_FreeType(&libraryRef);

				if (err != FT_Error.FT_Err_Ok) {
					throw new FreeTypeException(err);
				}

				_libraryHandle = libraryRef;
				Console.Out.WriteLine("_libraryHandle: " + (IntPtr)_libraryHandle);
			}

			_memoryHandle = GCHandle.Alloc(data, GCHandleType.Pinned);

			FT_FaceRec_* faceRef = null;
			err = FT.FT_New_Memory_Face(_libraryHandle, (byte*)_memoryHandle.AddrOfPinnedObject(), data.Length, 0, &faceRef);

			if (err != FT_Error.FT_Err_Ok) {
				throw new FreeTypeException(err);
			}

			_faceHandle = faceRef;
			_rec = *_faceHandle;
		}

		~FreeTypeSource()
		{
			Dispose(false);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_faceHandle != (FT_FaceRec_*)0)
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
			if (_faceHandle == (FT_FaceRec_*)0) {
				Console.Out.WriteLine("FreeTypeSource.GetGlyphId: _faceHandle is null");
				return null;
			}

			var result = FT.FT_Get_Char_Index(_faceHandle, (uint)codepoint);
			if (result == 0)
			{
				Console.Out.WriteLine($"FreeTypeSource.GetGlyphId: FT_Get_Char_Index returned 0 for codepoint {codepoint}");
				return null;
			}

			return (int?)result;
		}

		public int GetGlyphKernAdvance(int previousGlyphId, int glyphId, float fontSize)
		{
			FT_Vector_ kerning;
			if (FT.FT_Get_Kerning(_faceHandle, (uint)previousGlyphId, (uint)glyphId, 0, &kerning) != FT_Error.FT_Err_Ok)
			{
				Console.Out.WriteLine("FreeTypeSource.GetGlyphKernAdvance: FT_Get_Kerning failed");
				return 0;
			}

			return (int)kerning.x >> 6;
		}

		private void SetPixelSizes(float width, float height)
		{
			var err = FT.FT_Set_Pixel_Sizes(_faceHandle, (uint)width, (uint)height);
			if (err != FT_Error.FT_Err_Ok)
				throw new FreeTypeException(err);
		}

		private void LoadGlyph(int glyphId)
		{
			var err = FT.FT_Load_Glyph(_faceHandle, (uint)glyphId, 0 | FT.FT_LOAD_TARGET_NORMAL);
			if (err != FT_Error.FT_Err_Ok)
				throw new FreeTypeException(err);
		}

		private unsafe void GetCurrentGlyph(out FT_GlyphSlotRec_ glyph)
		{
			glyph = *_rec.glyph;
		}

		public void GetGlyphMetrics(int glyphId, float fontSize, out int advance, out int x0, out int y0, out int x1, out int y1)
		{
			if (_faceHandle == (FT_FaceRec_*)0)
			{
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
		}

		public unsafe void GetMetricsForSize(float fontSize, out int ascent, out int descent, out int lineHeight)
		{
			if (_faceHandle == (FT_FaceRec_*)0)
			{
				ascent = descent = lineHeight = 0;
				Console.Out.WriteLine("FreeTypeSource.GetMetricsForSize: _faceHandle is null");
				return;
			}

			SetPixelSizes(0, fontSize);
			var sizeRec = *_rec.size;

			ascent = (int)sizeRec.metrics.ascender >> 6;
			descent = (int)sizeRec.metrics.descender >> 6;
			lineHeight = (int)sizeRec.metrics.height >> 6;
		}

		public unsafe void RasterizeGlyphBitmap(int glyphId, float fontSize, byte[] buffer, int startIndex, int outWidth, int outHeight, int outStride)
		{
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
			
			// print some info about the bitmap
			Console.Out.WriteLine($"FreeTypeSource.RasterizeGlyphBitmap: Glyph {glyphId} - Width: {ftbmp.width}, Height: {ftbmp.rows}, Pitch: {ftbmp.pitch}");

			Console.Out.WriteLine($"FreeTypeSource.RasterizeGlyphBitmap: num_grays: {ftbmp.num_grays}, pixel_mode: {ftbmp.pixel_mode}, palette_mode: {ftbmp.palette_mode}");
			Console.Out.WriteLine($"FreeTypeSource.RasterizeGlyphBitmap: Buffer Length: {buffer.Length}, Start Index: {startIndex}, Out Width: {outWidth}, Out Height: {outHeight}, Out Stride: {outStride}");
			

			if (ftbmp.buffer == (void*)0)
				return;

			fixed (byte* bptr = buffer)
			{
				if (ftbmp.pixel_mode == FT_Pixel_Mode_.FT_PIXEL_MODE_MONO) // FT_PIXEL_MODE_MONO - 1 bit per pixel
				{
					for (var y = 0; y < outHeight; ++y)
					{
						var pos = (y * outStride) + startIndex;
						byte* dst = bptr + pos;
						byte* src = (byte*)ftbmp.buffer + y * ftbmp.pitch;
						
						for (var x = 0; x < outWidth; ++x)
						{
							int byteIndex = x / 8;
							int bitIndex = 7 - (x % 8);
							byte srcByte = src[byteIndex];
							byte bit = (byte)((srcByte >> bitIndex) & 1);
							*dst++ = (byte)(bit * 255); // convert 1-bit to 8-bit (0 or 255)
						}
					}
				}
				else // FT_PIXEL_MODE_GRAY or other 8-bit per pixel modes
				{
					for (var y = 0; y < outHeight; ++y)
					{
						var pos = (y * outStride) + startIndex;
						byte* dst = bptr + pos;
						byte* src = (byte*)ftbmp.buffer + y * ftbmp.pitch;
						
						for (var x = 0; x < outWidth; ++x)
						{
							*dst++ = *src++;
						}
					}
				}
			}
		}
	}
}
