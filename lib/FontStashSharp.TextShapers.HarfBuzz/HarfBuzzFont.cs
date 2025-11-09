using HarfBuzzSharp;
using System;
using System.Runtime.InteropServices;

namespace FontStashSharp
{
	/// <summary>
	/// Wrapper for HarfBuzz font resources
	/// </summary>
	internal class HarfBuzzFont : IDisposable
	{
		private Blob _blob;
		private Face _face;
		private Font _font;
		private GCHandle _fontDataHandle;

		public HarfBuzzFont(byte[] fontData)
		{
			// Pin the byte array in memory
			_fontDataHandle = GCHandle.Alloc(fontData, GCHandleType.Pinned);
			var dataPtr = _fontDataHandle.AddrOfPinnedObject();

			// Create HarfBuzz blob from font data
			_blob = new Blob(dataPtr, fontData.Length, MemoryMode.ReadOnly);

			// Create face from blob and font from face
			_face = new Face(_blob, 0);
			_font = new Font(_face);
		}

		/// <summary>
		/// Shape text using this font
		/// </summary>
		public void Shape(HarfBuzzSharp.Buffer buffer)
		{
			_font.Shape(buffer);
		}

		public void Dispose()
		{
			_font?.Dispose();
			_face?.Dispose();
			_blob?.Dispose();

			if (_fontDataHandle.IsAllocated)
			{
				_fontDataHandle.Free();
			}
		}
	}
}
