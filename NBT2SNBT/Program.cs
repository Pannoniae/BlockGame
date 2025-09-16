using BlockGame.util;
using BlockGame.util.xNBT;

namespace NBT2SNBT;

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
        
        var stream = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
        
        // first try to load the file as a compressed NBT
        NBTCompound? data = null;
        try {
            data = NBT.readCompressed(stream);
        }
        catch (Exception ex) {
            // try as normal
            try {
                data = NBT.read(stream) as NBTCompound ??
                       throw new InputException("Wrong NBT! Expected compound root tag.", ex);
            }
            catch (Exception e) {
                throw new InputException($"Failed to read NBT file: {e.Message}", ex);
            }
        }
        finally {
            // actually write it out
            if (data != null) {
                var outputFile = Path.ChangeExtension(inputFile, ".snbt");
                SNBT.writeToFile(data, outputFile, true);
                Console.WriteLine($"Successfully converted {inputFile} to {outputFile}");
            } else {
                Console.WriteLine("No valid NBT data found.");
            }
            
            stream.Close();
        }

    }
}