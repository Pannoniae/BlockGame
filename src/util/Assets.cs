namespace BlockGame.util;

public partial class Assets {


    public Assets() {
    }

    public void init() {

    }

    public static string getPath(string path) {
        return "assets/" + path;
    }

    public static string load(string path) {
        var fullPath = getPath(path);
        if (!File.Exists(fullPath)) {
            throw new FileNotFoundException("Asset not found: " + fullPath);
        }

        return File.ReadAllText(fullPath);
    }

    public static FileStream open(string path) {
        var fullPath = getPath(path);

        //Console.Out.WriteLine(fullPath);
        if (!File.Exists(fullPath)) {
            throw new FileNotFoundException("Asset not found: " + fullPath);
        }

        return File.OpenRead(fullPath);
    }
}