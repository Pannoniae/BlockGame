using System.Diagnostics;
using System.Reflection.Metadata;
using BlockGame.main;
using BlockGame.render.model;
using BlockGame.util.log;

namespace BlockGame.util;

public class Spy {
    public static bool enabled;

    private static string projectDir;

    private static FileSystemWatcher[]? spies;


    public static void init() {
        enabled = Environment.GetEnvironmentVariable("DOTNET_WATCH") == "1" || MetadataUpdater.IsSupported;

        Log.info(enabled ? "Spy enabled" : "No spy!");

        // output working directory
        Log.info("Working directory: " + Directory.GetCurrentDirectory());

        if (enabled) {
            setup();
        }
    }

    public static void setup() {
        // Go up from output dir to project dir
        var outputDir = AppDomain.CurrentDomain.BaseDirectory; // D:\dev\cs\BlockGame\bin\Debug\net10.0\
        projectDir = Path.GetFullPath(Path.Combine(outputDir, "..", "..", "..")); // D:\dev\cs\BlockGame\

        var folders = new[] { "textures", "shaders", "assets", "fonts" };
        spies = folders.Select(folder => {
            var sourcePath = Path.Combine(projectDir, "src", Game.assets.getPath(folder));
            var watcher = new FileSystemWatcher(sourcePath, "*.*") {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size |
                               NotifyFilters.Attributes | NotifyFilters.FileName
            };
            watcher.Filters.Add("*.png");
            watcher.Filters.Add("*.vert");
            watcher.Filters.Add("*.frag");
            watcher.Filters.Add("*.glsl");
            watcher.Filters.Add("*.inc");
            watcher.Filters.Add("*.ogg");
            watcher.Filters.Add("*.flac");
            watcher.Changed += changed;
            watcher.Created += changed;
            watcher.Renamed += changed;
            watcher.EnableRaisingEvents = true;
            return watcher;
        }).ToArray();
    }

    private static void changed(object sender, FileSystemEventArgs e) {
        // SANITY CHECK SHIT
        // if it doesn't *actually* match the extension, ignore it (.pdnsave for example)
        var ext = Path.GetExtension(e.FullPath).ToLower();
        if (ext is not (".png" or ".vert" or ".frag" or ".glsl" or ".inc" or ".ogg" or ".flac")) {
            return;
        }

        var outputDir = AppDomain.CurrentDomain.BaseDirectory;
        var srcDir = Path.Combine(projectDir, "src"); // D:\dev\cs\BlockGame\src

        // Get relative path from src: textures\items.png
        var relativePath = Path.GetRelativePath(srcDir, e.FullPath);
        var destPath = Path.Combine(outputDir, relativePath);


        // trick! we just wait a bit lol

        Thread.Sleep(1000);

        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        File.Copy(e.FullPath, destPath, true);

        // reload assets
        Game.instance.executeOnMainThread(() => {
            Game.textures.reloadAll();
            EntityRenderers.reloadAll();
        });
        Log.info("Reloaded assets due to file change: " + e.FullPath);
    }
}