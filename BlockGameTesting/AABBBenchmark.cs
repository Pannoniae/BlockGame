using System.Diagnostics;
using System.Numerics;
using BlockGame;
using BlockGame.util;

namespace BlockGameTesting;

/** Enjoy AIslop code
 *
 * This benchmark tests the performance of the AABB.isFrontTwoEight method and its AVX2/AVX512 variants.
 * It generates a set of random AABBs and two test planes, then benchmarks the unified method and its specific implementations.
 */
[TestFixture]
public class AABBBenchmark {
    private readonly AABB[] testAABBs = new AABB[8];
    private readonly Plane testPlane1;
    private readonly Plane testPlane2;
    
    public AABBBenchmark() {
        // generate *diverse* test AABBs
        var rnd = new XRandom(42);
        for (int i = 0; i < 8; i++) {
            var minX = rnd.Next(-100, 100);
            var minY = rnd.Next(-100, 100);
            var minZ = rnd.Next(-100, 100);
            var sizeX = rnd.Next(1, 20);
            var sizeY = rnd.Next(1, 20);
            var sizeZ = rnd.Next(1, 20);
            
            testAABBs[i] = new AABB(
                new Molten.DoublePrecision.Vector3D(minX, minY, minZ),
                new Molten.DoublePrecision.Vector3D(minX + sizeX, minY + sizeY, minZ + sizeZ)
            );
        }
        
        // generate test planes with realistic frustum plane normals
        testPlane1 = new Plane(0.7071f, 0.0f, 0.7071f, -50.0f);
        testPlane2 = new Plane(0.0f, 0.7071f, 0.7071f, -30.0f);
    }

    [Test]
    public void BenchmarkIsFrontTwoEightVariants() {
        const int warmupIterations = 100000;
        const int benchmarkIterations = 100000;

        // warmup all variants first
        for (int i = 0; i < warmupIterations; i++) {
            AABB.isFrontTwoEight(testAABBs, testPlane1, testPlane2);
            CallAvx2Method(testAABBs, testPlane1, testPlane2);
            CallAvx512Method(testAABBs, testPlane1, testPlane2);
            CallFallbackMethod(testAABBs, testPlane1, testPlane2);
        }
        
        // benchmark the unified method (which delegates to specific implementations)
        var sw = Stopwatch.StartNew();
        var result = 0;
        for (int i = 0; i < benchmarkIterations; i++) {
            result ^= AABB.isFrontTwoEight(testAABBs, testPlane1, testPlane2);
        }
        sw.Stop();
        
        Console.WriteLine($"isFrontTwoEight (unified): {sw.Elapsed.TotalMicroseconds:F1} μs total, {sw.Elapsed.TotalMicroseconds / benchmarkIterations:F3} μs/call, result: {result:X2}");
        
        // benchmark AVX2 variant directly
        sw.Restart();
        result = 0;
        for (int i = 0; i < benchmarkIterations; i++) {
            result ^= CallAvx2Method(testAABBs, testPlane1, testPlane2);
        }
        sw.Stop();
        
        Console.WriteLine($"isFrontTwoEightAvx2: {sw.Elapsed.TotalMicroseconds:F1} μs total, {sw.Elapsed.TotalMicroseconds / benchmarkIterations:F3} μs/call, result: {result:X2}");
        
        // benchmark AVX512 variant directly
        sw.Restart();
        result = 0;
        for (int i = 0; i < benchmarkIterations; i++) {
            result ^= CallAvx512Method(testAABBs, testPlane1, testPlane2);
        }
        sw.Stop();
        
        Console.WriteLine($"isFrontTwoEightAvx512: {sw.Elapsed.TotalMicroseconds:F1} μs total, {sw.Elapsed.TotalMicroseconds / benchmarkIterations:F3} μs/call, result: {result:X2}");

        // benchmark fallback variant directly
        sw.Restart();
        result = 0;
        for (int i = 0; i < benchmarkIterations; i++) {
            result ^= CallFallbackMethod(testAABBs, testPlane1, testPlane2);
        }
        sw.Stop();
        
        Console.WriteLine($"isFrontTwoEightFallback: {sw.Elapsed.TotalMicroseconds:F1} μs total, {sw.Elapsed.TotalMicroseconds / benchmarkIterations:F3} μs/call, result: {result:X2}");

        // benchmark individual AABB checks for comparison
        sw.Restart();
        byte individualResult = 0;
        for (int i = 0; i < benchmarkIterations; i++) {
            byte mask = 0;
            for (int j = 0; j < 8; j++) {
                if (testAABBs[j].isFrontTwo(testPlane1, testPlane2)) {
                    mask |= (byte)(1 << j);
                }
            }
            individualResult ^= mask;
        }
        sw.Stop();
        
        Console.WriteLine($"Individual isFrontTwo calls: {sw.Elapsed.TotalMicroseconds:F1} μs total, {sw.Elapsed.TotalMicroseconds / benchmarkIterations:F3} μs/call, result: {individualResult:X2}");
    }

    [Test]
    public void VerifyAllVariantsProduceSameResult() {
        var unified = AABB.isFrontTwoEight(testAABBs, testPlane1, testPlane2);
        var avx2 = CallAvx2Method(testAABBs, testPlane1, testPlane2);
        var avx512 = CallAvx512Method(testAABBs, testPlane1, testPlane2);
        var fallback = CallFallbackMethod(testAABBs, testPlane1, testPlane2);
        
        // manual calculation for verification
        byte expected = 0;
        for (int i = 0; i < 8; i++) {
            if (testAABBs[i].isFrontTwo(testPlane1, testPlane2)) {
                expected |= (byte)(1 << i);
            }
        }
        
        Assert.That(unified, Is.EqualTo(expected), "Unified method result mismatch");
        Assert.That(avx2, Is.EqualTo(expected), "AVX2 method result mismatch");
        Assert.That(avx512, Is.EqualTo(expected), "AVX512 method result mismatch");
        Assert.That(fallback, Is.EqualTo(expected), "Fallback method result mismatch");
        
        Console.WriteLine($"All variants produce correct result: {expected:X2}");
    }

    //[MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallAvx2Method(Span<AABB> aabbs, Plane p1, Plane p2) {
        return AABB.isFrontTwoEightAvx2(aabbs, p1, p2);
    }

    //[MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallAvx512Method(Span<AABB> aabbs, Plane p1, Plane p2) {
        return AABB.isFrontTwoEightAvx512(aabbs, p1, p2);
    }

    //[MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallFallbackMethod(Span<AABB> aabbs, Plane p1, Plane p2) {
        return AABB.isFrontTwoEightFallback(aabbs, p1, p2);
    }
}