using FontStashSharp.Base;

namespace FontStashSharp.Rasterizers.StbTrueTypeSharp
{
	public class StbTrueTypeSharpLoader : IFontLoader
	{
		private readonly StbTrueTypeSharpSettings _settings;

		public StbTrueTypeSharpLoader(StbTrueTypeSharpSettings settings)
		{
			_settings = settings;
		}

		public IFontSource Load(byte[] data)
		{
			return new StbTrueTypeSharpSource(data, _settings);
		}
	}
}
