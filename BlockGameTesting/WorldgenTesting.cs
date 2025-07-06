using BlockGame;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats; 

namespace BlockGameTesting;


public class WorldgenTesting {
    
    public PerlinWorldGenerator gen;
    private Random random;
    
    [SetUp]
    public void Setup() {
        gen = new PerlinWorldGenerator(null!);
        gen.setup(1337);
        random = new Random(1337);
    }

    private float GetRandomValue(int x, int y, int z) {
        // Hash function to get consistent random values for coordinates
        long hash = x * 73856093 ^ y * 19349663 ^ z * 83492791;
        hash = (hash ^ (hash >> 16)) * 0x85ebca6b;
        hash = (hash ^ (hash >> 13)) * 0xc2b2ae35;
        hash = hash ^ (hash >> 16);
        return (hash & 0x7fffffff) / (float)0x7fffffff * 2f - 1f;
    }

    private float TrilinearInterpolate(float x, float y, float z) {
        int x0 = (int)Math.Floor(x);
        int y0 = (int)Math.Floor(y);
        int z0 = (int)Math.Floor(z);
        int x1 = x0 + 1;
        int y1 = y0 + 1;
        int z1 = z0 + 1;

        float fx = x - x0;
        float fy = y - y0;
        float fz = z - z0;

        // Get values at the 8 corners of the cube
        float v000 = GetRandomValue(x0, y0, z0);
        float v001 = GetRandomValue(x0, y0, z1);
        float v010 = GetRandomValue(x0, y1, z0);
        float v011 = GetRandomValue(x0, y1, z1);
        float v100 = GetRandomValue(x1, y0, z0);
        float v101 = GetRandomValue(x1, y0, z1);
        float v110 = GetRandomValue(x1, y1, z0);
        float v111 = GetRandomValue(x1, y1, z1);

        // Linear interpolation
        float c00 = v000 * (1 - fx) + v100 * fx;
        float c01 = v001 * (1 - fx) + v101 * fx;
        float c10 = v010 * (1 - fx) + v110 * fx;
        float c11 = v011 * (1 - fx) + v111 * fx;

        float c0 = c00 * (1 - fy) + c10 * fy;
        float c1 = c01 * (1 - fy) + c11 * fy;

        return c0 * (1 - fz) + c1 * fz;
    }

    private float QuinticFade(float t) {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    private float QuinticInterpolate(float x, float y, float z) {
        int x0 = (int)Math.Floor(x);
        int y0 = (int)Math.Floor(y);
        int z0 = (int)Math.Floor(z);
        int x1 = x0 + 1;
        int y1 = y0 + 1;
        int z1 = z0 + 1;

        float fx = QuinticFade(x - x0);
        float fy = QuinticFade(y - y0);
        float fz = QuinticFade(z - z0);

        // Get values at the 8 corners of the cube
        float v000 = GetRandomValue(x0, y0, z0);
        float v001 = GetRandomValue(x0, y0, z1);
        float v010 = GetRandomValue(x0, y1, z0);
        float v011 = GetRandomValue(x0, y1, z1);
        float v100 = GetRandomValue(x1, y0, z0);
        float v101 = GetRandomValue(x1, y0, z1);
        float v110 = GetRandomValue(x1, y1, z0);
        float v111 = GetRandomValue(x1, y1, z1);

        // Quintic interpolation
        float c00 = v000 * (1 - fx) + v100 * fx;
        float c01 = v001 * (1 - fx) + v101 * fx;
        float c10 = v010 * (1 - fx) + v110 * fx;
        float c11 = v011 * (1 - fx) + v111 * fx;

        float c0 = c00 * (1 - fy) + c10 * fy;
        float c1 = c01 * (1 - fy) + c11 * fy;

        return c0 * (1 - fz) + c1 * fz;
    }

    [Test]
    public void GenHighNoise() {
        // generate an image from the trilinear value noise
        var img = new Image<Rgba32>(512, 512);
        for (int x = 0; x < img.Width; x++) {
            for (int y = 0; y < img.Height; y++) {
                var val = TrilinearInterpolate(x / 20f, 0, y / 20f);
                Console.Out.WriteLine(val);
                img[x, y] = new Rgba32(
                    (byte) ((val + 1) * 127.5f),
                    (byte) ((val + 1) * 127.5f),
                    (byte) ((val + 1) * 127.5f),
                    255);
            }
        }
        img.Save(getPath("trilinearNoise.png"));
        
        // generate an image from the quintic value noise
        var img2 = new Image<Rgba32>(512, 512);
        for (int x = 0; x < img2.Width; x++) {
            for (int y = 0; y < img2.Height; y++) {
                var val = QuinticInterpolate(x / 20f, 0, y / 20f);
                img2[x, y] = new Rgba32(
                    (byte) ((val + 1) * 127.5f),
                    (byte) ((val + 1) * 127.5f),
                    (byte) ((val + 1) * 127.5f),
                    255);
            }
        }
        img2.Save(getPath("quinticNoise.png"));
        
        Assert.Pass();
    }
    
    // use the PROJECT folder not the build folder lol
    private string getPath(string path) {
        var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var fullPath = Path.Combine(projectDir, path);
        return fullPath;
    }
}