using FontStashSharp.Interfaces;

namespace BlockGame.util.font;

public class BDFLoader : IFontLoader {
    public IFontSource Load(byte[] data) {
        return new BDFSource(data);
    }
}