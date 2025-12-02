using System.IO.Compression;
using BlockGame.util.xNBT;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BlockGame.render.texpack;

/**
 * Loads texture pack files from a zip archive
 */
public class ZipPackSource : PackSource {
    readonly ZipArchive archive;
    public string name { get; }

    public ZipPackSource(string zipPath) {
        archive = ZipFile.OpenRead(zipPath);
        name = Path.GetFileNameWithoutExtension(zipPath);
    }

    public bool exists(string path) {
        // normalize path separators (zip uses forward slashes)
        var normalizedPath = path.Replace('\\', '/');
        return archive.GetEntry(normalizedPath) != null;
    }

    public Stream open(string path) {
        var normalizedPath = path.Replace('\\', '/');
        var entry = archive.GetEntry(normalizedPath);
        if (entry == null) {
            throw new FileNotFoundException($"File not found in zip: {path}");
        }
        return entry.Open();
    }

    public Image<Rgba32> loadImage(string path) {
        var normalizedPath = path.Replace('\\', '/');
        var entry = archive.GetEntry(normalizedPath);
        if (entry == null) {
            throw new FileNotFoundException($"Image not found in zip: {path}");
        }

        using var stream = entry.Open();
        return Image.Load<Rgba32>(stream);
    }

    public NBTCompound? loadMetadata() {
        var entry = archive.GetEntry("pack.snbt");
        if (entry == null) return null;

        try {
            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            return (NBTCompound)SNBT.parse(content);
        } catch {
            return null; // invalid/corrupted metadata
        }
    }

    public Image<Rgba32>? loadIcon() {
        var entry = archive.GetEntry("pack.png");
        if (entry == null) return null;

        try {
            using var stream = entry.Open();
            return Image.Load<Rgba32>(stream);
        } catch {
            return null; // invalid/corrupted icon
        }
    }

    public void dispose() {
        archive.Dispose();
    }
}
