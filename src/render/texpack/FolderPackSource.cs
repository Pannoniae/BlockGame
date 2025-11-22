using BlockGame.util.xNBT;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BlockGame.render.texpack;

/**
 * Loads texture pack files from a directory.
 */
public class FolderPackSource : PackSource {
    public readonly string basePath;

    public string name => Path.GetFileName(basePath);

    public FolderPackSource(string path) {
        this.basePath = path;
    }

    public bool exists(string path) {
        return File.Exists(Path.Combine(basePath, path));
    }

    public Image<Rgba32> loadImage(string path) {
        return Image.Load<Rgba32>(Path.Combine(basePath, path));
    }

    public NBTCompound? loadMetadata() {
        var path = Path.Combine(basePath, "pack.snbt");
        if (!File.Exists(path)) return null;

        try {
            return (NBTCompound)SNBT.readFromFile(path);
        } catch {
            return null; // invalid/corrupted metadata
        }
    }

    public Image<Rgba32>? loadIcon() {
        var path = Path.Combine(basePath, "pack.png");
        if (!File.Exists(path)) {
            return Image.Load<Rgba32>("logo.png");
        }

        try {
            return Image.Load<Rgba32>(path);
        } catch {
            // return the base game logo
            return Image.Load<Rgba32>("logo.png");
        }
    }

    public void dispose() {
        // nothing to dispose for folders
    }
}
