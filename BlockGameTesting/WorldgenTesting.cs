using BlockGame;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BlockGameTesting;

public class WorldgenTesting {
    
    public PerlinWorldGenerator gen;
    
    [SetUp]
    public void Setup() {
        gen = new PerlinWorldGenerator(null!);
        gen.setup(1337);
    }

    [Test]
    public void GenHighNoise() {
        // generate an image from the highNoise
        using var img = new Image<Rgba32>(512, 512);
        for (int x = 0; x < img.Width; x++) {
            for (int y = 0; y < img.Height; y++) {
                var val = gen.getNoise3D(gen.highNoise, x / 20f, 0, y / 20f, 8, 2f);
                var orig = val;
                val = float.Tan(val);
                //Console.Out.WriteLine(val);
                img[x, y] = new Rgba32(
                    (byte) ((val + 1) * 127.5f),
                    (byte) ((val + 1) * 127.5f),
                    (byte) ((val + 1) * 127.5f),
                    255);
            }
        }
        img.Save(getPath("highNoiseT.png"));
        
        for (int x = 0; x < img.Width; x++) {
            for (int y = 0; y < img.Height; y++) {
                var val = gen.getNoise3D(gen.highNoise, x / 20f, 0, y / 20f, 8, 2f);
                val = float.Tanh(val);
                //Console.Out.WriteLine(val);
                img[x, y] = new Rgba32(
                    (byte) ((val + 1) * 127.5f),
                    (byte) ((val + 1) * 127.5f),
                    (byte) ((val + 1) * 127.5f),
                    255);
            }
        }
        img.Save(getPath("highNoiseH.png"));
        
        for (int x = 0; x < img.Width; x++) {
            for (int y = 0; y < img.Height; y++) {
                var val = gen.getNoise3D(gen.highNoise, x / 20f, 0, y / 20f, 8, 2f);
                val = float.Atanh(val);
                //Console.Out.WriteLine(val);
                img[x, y] = new Rgba32(
                    (byte) ((val + 1) * 127.5f),
                    (byte) ((val + 1) * 127.5f),
                    (byte) ((val + 1) * 127.5f),
                    255);
            }
        }
        img.Save(getPath("highNoiseH.png"));
        
        Assert.Pass();
    }
    
    // use the PROJECT folder not the build folder lol
    private string getPath(string path) {
        var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var fullPath = Path.Combine(projectDir, path);
        return fullPath;
    }
}