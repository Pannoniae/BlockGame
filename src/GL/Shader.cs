using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ARB;

namespace BlockGame.GL;

public class Definition(string name, string value = "") {
    public string name { get; set; } = name;
    public string value { get; set; } = value;
}

public enum ShaderVariant {
    /* Normal shader, no special features */
    Normal,
    /* Used for "fake" instanced chunk rendering, when ARB_shader_draw_parameters is available */
    Instanced,
    /* Used for command list rendering, when NV_command_list is available */
    CommandList
}

public partial class Shader : IDisposable {
    private string name;

    private string vertexShader;
    private string fragmentShader;

    private readonly Silk.NET.OpenGL.GL GL;
    public uint programHandle;
    private Dictionary<string, Definition> defs = new();
    private readonly HashSet<string> includes = [];
    
    // Named strings for ARB_shading_language_include
    private static readonly Dictionary<string, string> namedStrings = new();
    private static readonly Lock namedStringsLock = new();

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

    public static Shader createVariant(Silk.NET.OpenGL.GL GL, string name, string vertexShaderPath,
        string? fragmentShaderPath = null, ShaderVariant? variant = null, IEnumerable<Definition>? defs = null) {
        var definitions = new List<Definition>();
        
        // append additional definitions
        if (defs != null) {
            definitions.AddRange(defs);
        }

        // autoselect if null
        var selectedVariant = variant ?? (Game.hasCMDL ? ShaderVariant.CommandList :
            Game.hasInstancedUBO ? ShaderVariant.Instanced :
            ShaderVariant.Normal);
        
        if (Game.isNVCard) {
            definitions.Add(new Definition("NV_EXTENSIONS"));
        }

        // Add variant-specific defines
        switch (selectedVariant) {
            case ShaderVariant.Instanced:
                definitions.Add(new Definition("INSTANCED_RENDERING"));
                break;
            case ShaderVariant.CommandList:
                definitions.Add(new Definition("INSTANCED_RENDERING"));
                definitions.Add(new Definition("NV_COMMAND_LIST"));
                break;
        }

        return fragmentShaderPath == null
            ? new Shader(GL, name, vertexShaderPath, definitions)
            : new Shader(GL, name, vertexShaderPath, fragmentShaderPath, definitions);
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
        vertexShader = preprocess(vertexShaderPath, File.ReadAllText(vertexShaderPath));
        var vert = load(vertexShader, ShaderType.VertexShader);
        link(vert);
    }

    [GeneratedRegex(@"#include\s+[""<](.+)["">]")]
    private static partial Regex includeRegex();

    [GeneratedRegex(@"#define\s+(\w+)(?:\s+(.*))?")]
    private static partial Regex defineRegex();
    
    // ARB_shading_language_include support
    public static void namedStringARB(string name, string content) {
        if (!Game.hasShadingLanguageInclude) return;
        
        if (!name.StartsWith('/')) {
            throw new ArgumentException("Named string path must start with '/'", nameof(name));
        }
        
        lock (namedStringsLock) {
            namedStrings[name] = content;
        }
        
        unsafe {
            var nameBytes = Encoding.UTF8.GetBytes(name);
            var contentBytes = Encoding.UTF8.GetBytes(content);
            
            fixed (byte* namePtr = nameBytes)
            fixed (byte* contentPtr = contentBytes) {
                Game.arbInclude.NamedString(ARB.ShaderIncludeArb, nameBytes.Length, namePtr, contentBytes.Length, contentPtr);
            }
        }
    }
    
    public static void deleteNamedStringARB(string name) {
        if (!Game.hasShadingLanguageInclude) return;
        
        if (!name.StartsWith('/')) {
            throw new ArgumentException("Named string path must start with '/'", nameof(name));
        }
        
        lock (namedStringsLock) {
            namedStrings.Remove(name);
        }
        
        unsafe {
            var nameBytes = Encoding.UTF8.GetBytes(name);
            fixed (byte* namePtr = nameBytes) {
                Game.arbInclude.DeleteNamedString(nameBytes.Length, namePtr);
            }
        }
    }
    
    public static void initializeIncludeFiles() {
        if (!Game.hasShadingLanguageInclude) return;
        
        // Scan common shader directories for include files
        var searchDirectories = new[] { "shaders"};
        
        foreach (var dir in searchDirectories) {
            if (!Directory.Exists(dir)) continue;
            
            // Find all .inc and .inc.glsl files
            var incFiles = Directory.GetFiles(dir, "*.inc*", SearchOption.AllDirectories);
            
            foreach (var filePath in incFiles) {
                var content = File.ReadAllText(filePath);
                
                // Convert filesystem path to named string path
                // e.g., "shaders/common.inc" -> "/shaders/common.inc"
                var namedPath = "/" + filePath.Replace('\\', '/');
                
                namedStringARB(namedPath, content);
                Console.WriteLine($"Registered shader include: {namedPath}");
            }
        }
    }

    private string preprocess(string filePath, string source) {
        // Reset included files for each new preprocessing
        if (Path.GetFullPath(filePath) == filePath) {
            includes.Clear();
        }

        // Add this file to included files to prevent circular includes
        includes.Add(Path.GetFullPath(filePath));

        var lines = source.Split(["\r\n", "\n"], StringSplitOptions.None);
        var result = new StringBuilder();
        bool definitionsInjected = false;

        foreach (string line in lines) {
            // Inject definitions after #version directive
            if (!definitionsInjected && line.TrimStart().StartsWith("#version")) {
                result.AppendLine(line);
                
                // append the include extension
                if (Game.hasShadingLanguageInclude) {
                    result.AppendLine("#extension GL_ARB_shading_language_include : enable");
                }
                
                // Inject constructor-provided definitions after #version
                foreach (var def in defs) {
                    if (string.IsNullOrEmpty(def.Value.value)) {
                        result.AppendLine($"#define {def.Key}");
                    } else {
                        result.AppendLine($"#define {def.Key} {def.Value.value}");
                    }
                }
                definitionsInjected = true;
                continue;
            }
            
            
            // Handle #include directives
            if (line.TrimStart().StartsWith("#include")) {
                if (!Game.hasShadingLanguageInclude) {
                    // Manual preprocessing when ARB_shading_language_include is not available
                    var includeRegex = Shader.includeRegex();
                    var match = includeRegex.Match(line);

                    if (match.Success) {
                        var includePath = match.Groups[1].Value;
                        
                        string fullPath;
                        if (includePath.StartsWith('/')) {
                            // Path relative to project root - remove leading '/' and resolve from current directory
                            fullPath = Path.GetFullPath(includePath[1..]);
                        } else {
                            // Relative path - combine with current file's directory
                            // this should never happen tho? not sure, needs testing
                            var baseDir = Path.GetDirectoryName(filePath);
                            fullPath = Path.GetFullPath(Path.Combine(baseDir ?? "", includePath));
                        }

                        // Prevent circular includes
                        if (!includes.Contains(fullPath)) {
                            if (File.Exists(fullPath)) {
                                var includeContent = File.ReadAllText(fullPath);
                                // Recursively preprocess the included file
                                var processedInclude = preprocess(fullPath, includeContent);
                                result.Append(processedInclude);
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
                else {
                    // When ARB_shading_language_include is available, pass #include directives through and don't do shit
                    result.AppendLine(line);
                }
            }
            // Handle #define directives
            else if (line.TrimStart().StartsWith("#define")) {
                result.AppendLine(line);
                
                // this is disabled because we emit defines into the header of the shader instead and let glslc do its job lol
                // Extract the definition name and value
                /*var defineRegex = Shader.defineRegex();
                var match = defineRegex.Match(line);

                if (match.Success) {
                    var name = match.Groups[1].Value;
                    var value = match.Groups[2].Success ? match.Groups[2].Value.Trim() : "";

                    // Only store the definition if it doesn't already exist (constructor definitions take precedence)
                    if (!defs.ContainsKey(name)) {
                        defs[name] = new Definition(name, value);
                    }
                }*/
            }
            
            // Process normal code (OpenGL compiler handles macro expansion)
            else {
                result.AppendLine(line);
            }
        }

        return result.ToString();
    }

    public uint load(string shader, ShaderType type) {
        var shaderHandle = GL.CreateShader(type);
        GL.ShaderSource(shaderHandle, shader);
        
        // Use CompileShaderIncludeARB if available, otherwise standard CompileShader
        if (Game.hasShadingLanguageInclude) {
            compileShaderWithInclude(shaderHandle);
        } else {
            GL.CompileShader(shaderHandle);
        }
        
        string infoLog = GL.GetShaderInfoLog(shaderHandle);
        if (!string.IsNullOrWhiteSpace(infoLog)) {
            throw new Exception($"Error compiling shader of type {type}: {infoLog}");
        }

        return shaderHandle;
    }
    
    private static unsafe void compileShaderWithInclude(uint shaderHandle) {
        // Get all registered named strings as search paths
        List<string> searchPaths;
        lock (namedStringsLock) {
            searchPaths = namedStrings.Keys.ToList();
        }
        
        if (searchPaths.Count == 0) {
            // fallback to standard CompileShader if no includes are registered
            Game.GL.CompileShader(shaderHandle);
            return;
        }
        
        // Use stackalloc for the byte arrays and pointers
        var pathPtrs = stackalloc byte*[searchPaths.Count];
        
        // Convert each path to UTF8 bytes directly on the stack
        for (int i = 0; i < searchPaths.Count; i++) {
            var path = searchPaths[i];
            var pathBytes = stackalloc byte[Encoding.UTF8.GetByteCount(path) + 1]; // +1 for null terminator
            Encoding.UTF8.GetBytes(path, new Span<byte>(pathBytes, Encoding.UTF8.GetByteCount(path)));
            pathBytes[Encoding.UTF8.GetByteCount(path)] = 0; // null terminator
            pathPtrs[i] = pathBytes;
        }
        
        Game.arbInclude.CompileShaderInclude(shaderHandle, (uint)searchPaths.Count, pathPtrs, null);
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

    public void setUniform(int loc, ulong value) {
        Game.sbl.ProgramUniform(programHandle, loc, value);
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

    public InstantShader(Silk.NET.OpenGL.GL GL, string name, string vertexShader, string fragmentShader) : base(GL,
        name, vertexShader, fragmentShader) {
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