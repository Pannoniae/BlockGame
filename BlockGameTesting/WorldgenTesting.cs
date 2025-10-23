using BlockGame;
using BlockGame.util;
using BlockGame.world.worldgen;
using BlockGame.world.worldgen.generator;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BlockGameTesting;

public class WorldgenTesting {
    public NewWorldGenerator gen;

    [SetUp]
    public void Setup() {
        gen = new NewWorldGenerator(null!, 3);
        var rand = new XRandom(1338);
        gen.setup(rand, 1338);
    }

    [Test]
    public void GenHighNoise() {


        using var img = new Image<Rgba32>(512, 512);
        for (int x = 0; x < img.Width; x++) {
            for (int y = 0; y < img.Height; y++) {
                var val = WorldgenUtil.getNoise3D(gen.t2n, x / 12f, 0, y / 12f, 4, 2f);
                // "random" noise
                var val2 = WorldgenUtil.getNoise3D(gen.t2n, x + 1444, 0, y + 1444, 1, 2f);
                var orig = val;
                //val = float.Tan(val);
                //Console.Out.WriteLine(val);
                var v = (byte)((val + 1) * 127.5f);
                v += (byte)((val2 + 0.4f) * 127.5f / 4f);
                // quantise to yes/no
                v = (byte)(v > 144 ? 255 : 0);
                img[x, y] = new Rgba32(
                    v,
                    v,
                    v,
                    v > 0 ? 255 : 0);
            }
        }

        img.Save(getPath("highNoiseC.png"));


        for (int x = 0; x < img.Width; x++) {
            for (int y = 0; y < img.Height; y++) {
                var val = WorldgenUtil.getNoise3D(gen.t2n, x / 20f, 0, y / 20f, 2, 2f);
                var orig = val;
                //val = float.Tan(val);
                //Console.Out.WriteLine(val);
                img[x, y] = new Rgba32(
                    (byte)((val + 1) * 127.5f),
                    (byte)((val + 1) * 127.5f),
                    (byte)((val + 1) * 127.5f),
                    255);
            }
        }

        img.Save(getPath("highNoiseT.png"));

        for (int x = 0; x < img.Width; x++) {
            for (int y = 0; y < img.Height; y++) {
                var val = WorldgenUtil.getNoise3D(gen.t2n, x / 20f, 0, y / 20f, 16, 2f);
                var orig = val;
                val = float.Tan(val);
                //Console.Out.WriteLine(val);
                img[x, y] = new Rgba32(
                    (byte)((val + 1) * 127.5f),
                    (byte)((val + 1) * 127.5f),
                    (byte)((val + 1) * 127.5f),
                    255);
            }
        }

        img.Save(getPath("highNoiseT2.png"));

        var e = new ExpNoise(1337);
        //e.setExp(1337, float.Pow(float.E, 10), 0f);
        e.setExp(1337, 9, 0f);
        for (int x = 0; x < img.Width; x++) {
            for (int y = 0; y < img.Height; y++) {
                var val = WorldgenUtil.getNoise3D(e, x / 20f, 0, y / 20f, 4, 2f) * 2f;
                //val = float.Tanh(val);
                //Console.Out.WriteLine(val);
                img[x, y] = new Rgba32(
                    (byte)((val + 1) * 127.5f),
                    (byte)((val + 1) * 127.5f),
                    (byte)((val + 1) * 127.5f),
                    255);
            }
        }

        img.Save(getPath("highNoiseH.png"));

        e.setExp(1337, 109, 0f);
        for (int x = 0; x < img.Width; x++) {
            for (int y = 0; y < img.Height; y++) {
                var val = WorldgenUtil.getNoise3D(e, x / 20f, 0, y / 20f, 4, 2f) * 2f;
                //val = float.Tanh(val);
                //Console.Out.WriteLine(val);
                img[x, y] = new Rgba32(
                    (byte)((val + 1) * 127.5f),
                    (byte)((val + 1) * 127.5f),
                    (byte)((val + 1) * 127.5f),
                    255);
            }
        }

        img.Save(getPath("highNoiseHA.png"));

        for (int x = 0; x < img.Width; x++) {
            for (int y = 0; y < img.Height; y++) {
                var val = WorldgenUtil.getNoise3D(gen.t2n, x / 20f, 0, y / 20f, 8, 2f);
                val = float.Atanh(val);
                //Console.Out.WriteLine(val);
                img[x, y] = new Rgba32(
                    (byte)((val + 1) * 127.5f),
                    (byte)((val + 1) * 127.5f),
                    (byte)((val + 1) * 127.5f),
                    255);
            }
        }

        img.Save(getPath("highNoiseH2.png"));

        var xs = 1000;

        for (int x = 0; x < img.Width; x++) {
            for (int y = 0; y < img.Height; y++) {
                var val = WorldgenUtil.getNoise2D(gen.on, x / (4f) * xs, y / (4f) * xs, 4, 2f);
                var val2 = WorldgenUtil.getNoise2D(gen.mn, x / (226200f) * xs, y / (226200f) * xs, 4, 2f);

                //val = val is > -0.1f and < 0.15f ? (float.Abs(val - 0.025f) - 0.125f) : 1f;
                //val = val2 < -0.2f ? float.Abs(val2 + 0.6f) : 1f;
                val = val2 < -0.4f ? float.Abs(val2 + 0.8f) : 1f;

                //Console.Out.WriteLine(val);
                img[x, y] = new Rgba32(
                    (byte)((val + 1) * 127.5f),
                    (byte)((val + 1) * 127.5f),
                    (byte)((val + 1) * 127.5f),
                    255);
            }
        }

        img.Save(getPath("highNoiseO.png"));

        Assert.Pass();
    }

    // use the PROJECT folder not the build folder lol
    private string getPath(string path) {
        var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var fullPath = Path.Combine(projectDir, path);
        return fullPath;
    }
}