using BlockGame.util;
using BlockGame.util.xNBT;

namespace BlockGame.mod;

/**
 * Metadata and runtime info for a loaded mod.
 */
public class ModInfo {
    public string internalname;
    public string name;
    public string version;
    public string? author;
    public string? description;
    public string path;  // Filesystem path to mod directory
    public XUList<string> dependencies;
    public XUList<string> conflicts;

    public Mod? instance;  // Instantiated mod (null for coremods)

    public ModInfo(NBTCompound metadata, string path) {
        this.internalname = metadata.getString("internalname", null);
        if (this.internalname == null) {
            InputException.throwNew("Mod missing 'internalname' field!");
        }
        this.name = metadata.getString("name", internalname);
        this.version = metadata.getString("version", "1.0.0");
        this.author = metadata.getString("author", "Unknown");
        this.description = metadata.getString("description");
        this.path = path;

        // parse dependencies
        this.dependencies = [];
        if (metadata.get("dependencies", out var depsTag) && depsTag is NBTList depsList) {
            foreach (var dep in depsList.list) {
                if (dep is NBTCompound depCompound) {
                    var depName = depCompound.getString("internalname");
                    if (depName != null) {
                        dependencies.Add(depName);
                    }
                }
            }
        }

        // parse conflicts
        this.conflicts = [];
        if (metadata.get("conflicts", out var conflictsTag) && conflictsTag is NBTList conflictsList) {
            foreach (var conflict in conflictsList.list) {
                if (conflict is NBTString str) {
                    conflicts.Add(str.data);
                }
            }
        }
    }
}