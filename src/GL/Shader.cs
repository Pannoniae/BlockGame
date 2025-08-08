using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Silk.NET.OpenGL;

namespace BlockGame.GL;

public class Definition(string name, string value = "") {
    public string name { get; set; } = name;
    public string value { get; set; } = value;

}

public partial class Shader : IDisposable {

    private string name;
    
    private string vertexShader;
    private string fragmentShader;

    private readonly Silk.NET.OpenGL.GL GL;
    public uint programHandle;
    private Dictionary<string, Definition> defs = new();
    private readonly HashSet<string> includes = [];

    public Shader(Silk.NET.OpenGL.GL GL, string name, string vertexShaderPath, string fragmentShaderPath,
        IEnumerable<Definition>? defs = null) {
        this.GL = GL;
        this.name = name;

        // Initialize preprocessor definitions
        if (defs != null) {
            foreach (var def in defs) {
                this.defs[def.name] = def;
            }
        }

        // Load and preprocess the shaders
        vertexShader = preprocess(vertexShaderPath, File.ReadAllText(vertexShaderPath));
        fragmentShader = preprocess(fragmentShaderPath, File.ReadAllText(fragmentShaderPath));

        var vert = load(vertexShader, ShaderType.VertexShader);
        var frag = load(fragmentShader, ShaderType.FragmentShader);
        link(vert, frag);
    }

    /// <summary>
    /// Used for depth pass shaders.
    /// </summary>
    public Shader(Silk.NET.OpenGL.GL GL, string name, string vertexShaderPath,
        IEnumerable<Definition>? customDefinitions = null) {
        this.GL = GL;
        this.name = name;

        // Initialize preprocessor definitions
        if (customDefinitions != null) {
            foreach (var def in customDefinitions) {
                defs[def.name] = def;
            }
        }

        // Load and preprocess the shader
        this.vertexShader = preprocess(vertexShaderPath, File.ReadAllText(vertexShaderPath));
        var vert = load(this.vertexShader, ShaderType.VertexShader);
        link(vert);
    }
    
    [GeneratedRegex(@"#include\s+[""<](.+)["">]")]
    private static partial Regex includeRegex();
    
    [GeneratedRegex(@"#define\s+(\w+)(?:\s+(.*))?")]
    private static partial Regex defineRegex();

    private string preprocess(string filePath, string source) {
        // Reset included files for each new preprocessing
        if (Path.GetFullPath(filePath) == filePath) {
            includes.Clear();
        }

        // Add this file to included files to prevent circular includes
        includes.Add(Path.GetFullPath(filePath));

        var lines = source.Split('\n');
        var result = new StringBuilder();

        foreach (string line in lines) {
            // Handle #include directives
            if (line.TrimStart().StartsWith("#include")) {
                // Extract the file path from the include directive
                var includeRegex = Shader.includeRegex();
                var match = includeRegex.Match(line);

                if (match.Success) {
                    var includePath = match.Groups[1].Value;
                    var baseDir = Path.GetDirectoryName(filePath);
                    var fullPath = Path.GetFullPath(Path.Combine(baseDir ?? "", includePath));

                    // Prevent circular includes
                    if (!includes.Contains(fullPath)) {
                        if (File.Exists(fullPath)) {
                            var includeContent = File.ReadAllText(fullPath);
                            // Recursively preprocess the included file
                            var processedInclude = preprocess(fullPath, includeContent);
                            result.AppendLine(processedInclude);
                        }
                        else {
                            // Add comment showing the include failed
                            result.AppendLine($"// Failed to include '{includePath}': File not found");
                            Console.WriteLine($"Failed to include '{includePath}': File not found");
                            result.AppendLine(line);
                        }
                    }
                    else {
                        // Add comment showing circular include was prevented
                        result.AppendLine($"// Skipped circular include of '{includePath}'");
                        Console.WriteLine($"Skipped circular include of '{includePath}'");
                    }
                }
                else {
                    // Include directive with invalid format
                    result.AppendLine(line);
                }
            }
            // Handle #define directives
            else if (line.TrimStart().StartsWith("#define")) {
                result.AppendLine(line);

                // Extract the definition name and value
                var defineRegex = Shader.defineRegex();
                var match = defineRegex.Match(line);

                if (match.Success) {
                    var name = match.Groups[1].Value;
                    var value = match.Groups[2].Success ? match.Groups[2].Value.Trim() : "";

                    // Store the definition
                    defs[name] = new Definition(name, value);
                }
            }
            // Process normal code with replacements for defined macros
            else {
                var processedLine = line;

                // Replace defined macros
                foreach (var def in defs) {
                    if (!string.IsNullOrEmpty(def.Value.value)) {
                        var regex = new Regex($@"\b{def.Key}\b");
                        processedLine = regex.Replace(processedLine, def.Value.value);
                    }
                }

                result.AppendLine(processedLine);
            }
        }

        return result.ToString();
    }

    public uint load(string shader, ShaderType type) {
        var shaderHandle = GL.CreateShader(type);
        GL.ShaderSource(shaderHandle, shader);
        GL.CompileShader(shaderHandle);
        string infoLog = GL.GetShaderInfoLog(shaderHandle);
        if (!string.IsNullOrWhiteSpace(infoLog)) {
            throw new Exception($"Error compiling shader of type {type}: {infoLog}");
        }

        return shaderHandle;
    }

    public void link(uint vert, uint frag) {
        programHandle = GL.CreateProgram();
        GL.AttachShader(programHandle, vert);
        GL.AttachShader(programHandle, frag);
        GL.LinkProgram(programHandle);
        GL.GetProgram(programHandle, GLEnum.LinkStatus, out var status);
        if (status == 0) {
            throw new Exception($"Program failed to link: {GL.GetProgramInfoLog(programHandle)}");
        }

        // yeah this one is from a tutorial, don't judge me
        GL.DetachShader(programHandle, vert);
        GL.DetachShader(programHandle, frag);
        GL.DeleteShader(vert);
        GL.DeleteShader(frag);
    }

    private void link(uint vert) {
        programHandle = GL.CreateProgram();
        GL.AttachShader(programHandle, vert);
        GL.LinkProgram(programHandle);
        GL.GetProgram(programHandle, GLEnum.LinkStatus, out var status);
        if (status == 0) {
            throw new Exception($"Program failed to link: {GL.GetProgramInfoLog(programHandle)}");
        }

        // yeah this one is from a tutorial, don't judge me
        GL.DetachShader(programHandle, vert);
        GL.DeleteShader(vert);
    }

    public void use() {
        GL.UseProgram(programHandle);
    }
    // uniforms

    public int getUniformLocation(string name) {
        int location = GL.GetUniformLocation(programHandle, name);
        if (location == -1) {
            throw new Exception($"{name} uniform not found on shader {this.name}.");
        }

        return location;
    }

    public void setUniform(int loc, int value) {
        GL.ProgramUniform1(programHandle, loc, value);
    }

    public unsafe void setUniform(int loc, Matrix4x4 value) {
        GL.ProgramUniformMatrix4(programHandle, loc, 1, false, (float*)&value);
    }

    public void setUniform(int loc, float value) {
        GL.ProgramUniform1(programHandle, loc, value);
    }
    
    public unsafe void setUniform(int loc, Vector2 value) {
        GL.ProgramUniform2(programHandle, loc, 1, (float*)&value);
    }

    public unsafe void setUniform(int loc, Vector3 value) {
        GL.ProgramUniform3(programHandle, loc, 1, (float*)&value);
    }


    // If you change this to ProgramUniform3, it's twice as slow... why???
    public void setUniformBound(int loc, float x, float y, float z) {
        GL.Uniform3(loc, x, y, z);
    }

    public unsafe void setUniform(int loc, Vector4 value) {
        GL.ProgramUniform4(programHandle, loc, 1, (float*)&value);
    }

    public void setUniform(int loc, bool value) {
        GL.ProgramUniform1(programHandle, loc, value ? 1 : 0);
    }

    private void ReleaseUnmanagedResources() {
        if (programHandle != 0) {
            //GL.DeleteProgram(programHandle);
            programHandle = 0;
        }
        
        defs.Clear();
        includes.Clear();
    }

    public void Dispose() {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~Shader() {
        ReleaseUnmanagedResources();
    }
}

public class InstantShader : Shader {
    public int uMVP;

    private Matrix4x4 world = Matrix4x4.Identity;
    private Matrix4x4 view = Matrix4x4.Identity;
    private Matrix4x4 projection = Matrix4x4.Identity;

    public Matrix4x4 Projection {
        get => projection;
        set => setProjection(value);
    }

    public Matrix4x4 View {
        get => view;
        set => setView(value);
    }

    public Matrix4x4 World {
        get => world;
        set => setWorld(value);
    }

    private void setMVP(Matrix4x4 value) {
        setUniform(uMVP, value);
    }

    public InstantShader(Silk.NET.OpenGL.GL GL, string name, string vertexShader, string fragmentShader) : base(GL, name, vertexShader, fragmentShader) {
        uMVP = getUniformLocation("uMVP");
    }

    public InstantShader(Silk.NET.OpenGL.GL GL, string name, string vertexShader) : base(GL, name, vertexShader) {
        uMVP = getUniformLocation("uMVP");
    }

    public void setWorld(Matrix4x4 mat) {
        world = mat;
        setUniform(uMVP, world * view * projection);
    }

    public void setView(Matrix4x4 mat) {
        view = mat;
        setUniform(uMVP, world * view * projection);
    }

    public void setProjection(Matrix4x4 mat) {
        projection = mat;
        setUniform(uMVP, world * view * projection);
    }

    public void setMVP(Matrix4x4 world, Matrix4x4 view, Matrix4x4 projection) {
        this.world = world;
        this.view = view;
        this.projection = projection;
        setUniform(uMVP, world * view * projection);
    }


}
