using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using BlockGame;

[assembly: IgnoresAccessChecksTo("BlockGame")]

namespace BlockGameTesting;

public class InterpBenchmark {
    public PerlinWorldGenerator gen;

    /*[SetUp]
    public void setup() {
        gen = new PerlinWorldGenerator(null!);
        // initialise buffer with random data
        gen.setup(1337);
        
        gen.highBuffer = new float[16 * 128 * 16];
        var a = 3;
        a = 3;
        // fill it with random values
        for (int i = 0; i < gen.highBuffer.Length; i++) {
            gen.highBuffer[i] = (float)Random.Shared.NextDouble();
        }
    }

    [Test]
    public void runScalar() {
        // run it 200 times first to warm up
        for (int i = 0; i < 200; i++) {
            gen.interpolatePure(gen.highBuffer, new ChunkCoord(0, 0));
        }
        // run the scalar version of the function
        var sw = Stopwatch.StartNew();
        float a = gen.interpolatePure(gen.highBuffer, new ChunkCoord(0, 0));
        sw.Stop();
        Console.WriteLine($"Scalar: {sw.Elapsed.TotalMilliseconds} ms");
    }

    [Test]
    public void runSIMD() {
        // run it 200 times first to warm up
        for (int i = 0; i < 200; i++) {
            gen.interpolateSIMDPure(gen.highBuffer, new ChunkCoord(0, 0));
        }
        // run the SIMD version of the function
        var sw = Stopwatch.StartNew();
        float a = gen.interpolateSIMDPure(gen.highBuffer, new ChunkCoord(0, 0));
        sw.Stop();
        Console.WriteLine($"SIMD: {sw.Elapsed.TotalMilliseconds} ms");
    }
    
    [Test]
    public void runBatch() {
        // run it 200 times first to warm up
        for (int i = 0; i < 200; i++) {
            gen.interpolateSIMDBatchPure(gen.highBuffer, new ChunkCoord(0, 0));
        }
        // run the SIMD version of the function
        var sw = Stopwatch.StartNew();
        var a = gen.interpolateSIMDBatchPure(gen.highBuffer, new ChunkCoord(0, 0));
        sw.Stop();
        Console.WriteLine($"SIMD2: {sw.Elapsed.TotalMilliseconds} ms");
    }*/
}