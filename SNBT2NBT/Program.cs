using BlockGame.util;
using BlockGame.util.xNBT;

namespace SNBT2NBT;

public class Program {
    public static void Main(string[] args) {
        if (args.Length == 0) {
            Console.WriteLine("Usage: NBTTool <input.nbt>");
            return;
        }

        var inputFile = args[0];
        if (!File.Exists(inputFile)) {
            Console.WriteLine($"File not found: {inputFile}");
            return;
        }
        
        // first try to load the file as a compressed NBT
        NBTTag? data = null;
        try {
            data = SNBT.readFromFile(inputFile);
        }
        catch (Exception e) {
            throw new InputException($"Failed to read NBT file: {e.Message}", e);
        }
        finally {
            // actually write it out
            if (data is NBTCompound compound) {
                var outputFile = Path.ChangeExtension(inputFile, ".xnbt");
                NBT.writeFile(compound, outputFile);
                Console.WriteLine($"Successfully converted {inputFile} to {outputFile}");
            } else {
                Console.WriteLine("No valid NBT data found. Make sure it's an NBT file with a compound root tag.");
            }
        }

    }
}