using BlockGame.util.log;
using BlockGame.util.xNBT;

namespace BlockGame.net.srv;

/** reads and writes properties.snbt file
 * this was originally called settings but renamed to make it possible to run the client and server in the same directory
 * @author Luna
 */
public class ServerProperties {
    private NBTCompound props = new();
    private readonly string filePath;

    public ServerProperties(string filePath = "properties.snbt") {
        this.filePath = filePath;
    }

    public void load() {
        if (!File.Exists(filePath)) {
            Log.info($"No {filePath} found, using defaults");
            setDefaults();
            save();
            return;
        }

        try {
            props = (NBTCompound)SNBT.readFromFile(filePath);
            Log.info($"Loaded {filePath}");
        }
        catch (Exception e) {
            Log.error($"Error loading {filePath}: {e.Message}");
            setDefaults();
        }
    }

    public void save() {
        try {
            SNBT.writeToFile(props, filePath, prettyPrint: true);
            Log.info($"Saved {filePath}");
        }
        catch (Exception e) {
            Log.error($"Error saving {filePath}: {e.Message}");
        }
    }

    private void setDefaults() {
        props = new NBTCompound();
        props.addString("ip", "0.0.0.0");
        props.addString("ip6", "::");
        props.addInt("port", 31337);
        props.addInt("maxPlayers", 20);
        props.addString("levelName", "mplevel");
        props.addInt("renderDistance", 8);
        props.addByte("devMode", 0);
        props.addString("gamemode", "survival");
    }

    public string getString(string key, string defaultValue = "") {
        return props.has(key) ? props.getString(key) : defaultValue;
    }

    public int getInt(string key, int defaultValue = 0) {
        return props.has(key) ? props.getInt(key) : defaultValue;
    }

    public bool getBool(string key, bool defaultValue = false) {
        return props.has(key) ? props.getByte(key) != 0 : defaultValue;
    }

    public void add(string key, string value) {
        props.addString(key, value);
    }

    public void add(string key, int value) {
        props.addInt(key, value);
    }

    public void add(string key, bool value) {
        props.addByte(key, value ? (byte)1 : (byte)0);
    }
}