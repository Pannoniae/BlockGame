using BlockGame.world;
using BlockGame.world.chunk;
using BlockGame.world.region;
using NUnit.Framework;

namespace BlockGameTesting;

public class RegionFileTests {
    private string testDir = "";

    [SetUp]
    public void Setup() {
        testDir = Path.Combine(Path.GetTempPath(), "BlockGameRegionTest_" + Guid.NewGuid());
        Directory.CreateDirectory(testDir);
    }

    [TearDown]
    public void Cleanup() {
        if (Directory.Exists(testDir)) {
            Directory.Delete(testDir, true);
        }
    }

    [Test]
    public void TestBasicWriteRead() {
        using var region = new RegionFile(testDir, 0, 0);

        // write some test chunks
        byte[] chunk0 = [1, 2, 3, 4, 5];
        byte[] chunk1 = [10, 20, 30];

        region.writeChunk(0, 0, chunk0);
        region.writeChunk(1, 0, chunk1);
        region.flush();

        // read back
        byte[]? read0 = region.readChunk(0, 0);
        byte[]? read1 = region.readChunk(1, 0);

        Assert.That(read0, Is.Not.Null);
        Assert.That(read1, Is.Not.Null);
        Assert.That(read0, Is.EqualTo(chunk0));
        Assert.That(read1, Is.EqualTo(chunk1));

        // non-existent chunk
        byte[]? read2 = region.readChunk(2, 0);
        Assert.That(read2, Is.Null);
    }

    [Test]
    public void TestPersistence() {
        // write and close
        using (var region = new RegionFile(testDir, 0, 0)) {
            var chunk = "cXM"u8.ToArray();
            region.writeChunk(5, 10, chunk);
            region.flush();
        }

        // reopen and read
        using (var region = new RegionFile(testDir, 0, 0)) {
            byte[]? read = region.readChunk(5, 10);
            Assert.That(read, Is.Not.Null);
            Assert.That(read, Is.EqualTo("cXM"u8.ToArray()));
        }
    }

    [Test]
    public void TestOverwrite() {
        using var region = new RegionFile(testDir, 0, 0);

        byte[] chunk1 = [1, 2, 3, 4, 5];
        byte[] chunk2 = [10, 20]; // smaller

        region.writeChunk(0, 0, chunk1);
        region.flush();

        region.writeChunk(0, 0, chunk2);
        region.flush();

        byte[]? read = region.readChunk(0, 0);
        Assert.That(read, Is.Not.Null);
        Assert.That(read, Is.EqualTo(chunk2));
    }

    [Test]
    public void TestDefrag() {
        using var region = new RegionFile(testDir, 0, 0);

        // write large chunks
        byte[] largeChunk = new byte[10000];
        Random.Shared.NextBytes(largeChunk);

        for (int i = 0; i < 100; i++) {
            region.writeChunk(i % 32, i / 32, largeChunk);
        }
        region.flush();

        long sizeAfterFirstWrite = new FileInfo(RegionFile.getRegionPath(testDir, 0, 0)).Length;

        // overwrite with smaller chunks (creates waste)
        byte[] smallChunk = new byte[100];
        for (int i = 0; i < 100; i++) {
            region.writeChunk(i % 32, i / 32, smallChunk);
        }
        region.flush(); // should trigger defrag

        long sizeAfterDefrag = new FileInfo(RegionFile.getRegionPath(testDir, 0, 0)).Length;

        // defrag should reduce file size significantly
        Assert.That(sizeAfterDefrag, Is.LessThan(sizeAfterFirstWrite / 2),
            $"Expected defrag to reduce size from {sizeAfterFirstWrite} to less than {sizeAfterFirstWrite / 2}, got {sizeAfterDefrag}");

        // verify data integrity
        for (int i = 0; i < 100; i++) {
            byte[]? read = region.readChunk(i % 32, i / 32);
            Assert.That(read, Is.Not.Null);
            Assert.That(read!.Length, Is.EqualTo(100));
        }
    }

    [Test]
    public void TestRegionManager() {
        using var manager = new RegionManager(testDir);

        // write to multiple regions
        var r00 = manager.getRegion(new RegionCoord(0, 0));
        var r01 = manager.getRegion(new RegionCoord(0, 1));

        r00.writeChunk(0, 0, [1, 2, 3]);
        r01.writeChunk(0, 0, [4, 5, 6]);

        manager.flushAll();

        // read back
        byte[]? read00 = r00.readChunk(0, 0);
        byte[]? read01 = r01.readChunk(0, 0);

        Assert.That(read00, Is.Not.Null);
        Assert.That(read01, Is.Not.Null);
        Assert.That(read00, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(read01, Is.EqualTo(new byte[] { 4, 5, 6 }));
    }

    [Test]
    public void TestRegionCoordConversion() {
        // chunk (0, 0) → region (0, 0), local (0, 0)
        var r1 = RegionManager.getRegionCoord(new ChunkCoord(0, 0));
        var l1 = RegionManager.getLocalCoord(new ChunkCoord(0, 0));
        Assert.That(r1.x, Is.Zero);
        Assert.That(r1.z, Is.Zero);
        Assert.That(l1.x, Is.Zero);
        Assert.That(l1.z, Is.Zero);

        // chunk (32, 32) → region (1, 1), local (0, 0)
        var r2 = RegionManager.getRegionCoord(new ChunkCoord(32, 32));
        var l2 = RegionManager.getLocalCoord(new ChunkCoord(32, 32));
        Assert.That(r2.x, Is.EqualTo(1));
        Assert.That(r2.z, Is.EqualTo(1));
        Assert.That(l2.x, Is.Zero);
        Assert.That(l2.z, Is.Zero);

        // chunk (33, 35) → region (1, 1), local (1, 3)
        var r3 = RegionManager.getRegionCoord(new ChunkCoord(33, 35));
        var l3 = RegionManager.getLocalCoord(new ChunkCoord(33, 35));
        Assert.That(r3.x, Is.EqualTo(1));
        Assert.That(r3.z, Is.EqualTo(1));
        Assert.That(l3.x, Is.EqualTo(1));
        Assert.That(l3.z, Is.EqualTo(3));

        // negative coords: chunk (-1, -1) → region (-1, -1), local (31, 31)
        var r4 = RegionManager.getRegionCoord(new ChunkCoord(-1, -1));
        var l4 = RegionManager.getLocalCoord(new ChunkCoord(-1, -1));
        Assert.That(r4.x, Is.EqualTo(-1));
        Assert.That(r4.z, Is.EqualTo(-1));
        Assert.That(l4.x, Is.EqualTo(31));
        Assert.That(l4.z, Is.EqualTo(31));
    }

    [Test]
    public void TestDeleteChunk() {
        using var region = new RegionFile(testDir, 0, 0);

        byte[] chunk = [1, 2, 3];
        region.writeChunk(0, 0, chunk);
        region.flush();

        Assert.That(region.hasChunk(0, 0), Is.True);

        region.deleteChunk(0, 0);
        region.flush();

        Assert.That(region.hasChunk(0, 0), Is.False);
        Assert.That(region.readChunk(0, 0), Is.Null);
    }

    [Test]
    public void TestLargeChunk() {
        using var region = new RegionFile(testDir, 0, 0);

        // create a large chunk
        byte[] largeChunk = new byte[500_000]; // 500 KB
        Random.Shared.NextBytes(largeChunk);

        region.writeChunk(0, 0, largeChunk);
        region.flush();

        byte[]? read = region.readChunk(0, 0);
        Assert.That(read, Is.Not.Null);
        Assert.That(read, Is.EqualTo(largeChunk));
    }
}