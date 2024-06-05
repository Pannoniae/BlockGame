using BlockGame.data.font;
using FontStashSharp.Interfaces;

namespace BlockGame.font;

public class BDFLoader : IFontLoader {
    public IFontSource Load(byte[] data) {
        return new BDFSource(data);
    }
}