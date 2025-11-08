using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using BlockGame.main;
using BlockGame.ui;
using BlockGame.util;
using BlockGame.util.log;
using Molten;
using Silk.NET.OpenGL.Legacy;
using Silk.NET.OpenGL.Legacy.Extensions.ARB;

namespace BlockGame.GL;

public record class Definition(string name, string value = "") {
    public string name = name;
    public string value = value;
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
    public string name;
    private string vertexShader;
    private string fragmentShader;

    private readonly Silk.NET.OpenGL.Legacy.GL GL;
    public uint programHandle;
    private Dictionary<string, Definition> defs = new();
    private readonly HashSet<string> includes = [];
    
    // Named strings for ARB_shading_language_include
    private static readonly HashSet<string> registeredIncludes = [];
    private static readonly Lock includesLock = new();

    public Shader(Silk.NET.OpenGL.Legacy.GL GL, string name, string vertexShaderPath, string fragmentShaderPath,
        IEnumerable<Definition>? defs = null) {
        this.GL = GL;
        this.name = name;

        // reg
        Game.graphics.regShader(this);

        if (defs != null) {
            foreach (var def in defs) {
                this.defs[def.name] = def;
            }
        }

        vertexShader = preprocess(vertexShaderPath, Assets.load(vertexShaderPath));
        fragmentShader = preprocess(fragmentShaderPath, Assets.load(fragmentShaderPath));

        var vert = load(vertexShader, ShaderType.VertexShader);
        var frag = load(fragmentShader, ShaderType.FragmentShader);
        link(vert, frag);
    }

    public Shader(Silk.NET.OpenGL.Legacy.GL GL, string name, string vertexShaderPath,
        IEnumerable<Definition>? customDefinitions = null) {
        this.GL = GL;
        this.name = name;

        // reg
        Game.graphics.regShader(this);

        if (customDefinitions != null) {
            foreach (var def in customDefinitions) {
                defs[def.name] = def;
            }
        }

        vertexShader = preprocess(vertexShaderPath, Assets.load(vertexShaderPath));
        var vert = load(vertexShader, ShaderType.VertexShader);
        link(vert);
    }

    [GeneratedRegex(@"#include\s+[""<](.+)["">]")]
    private static partial Regex includeRegex();

    public static void namedStringARB(string name, string content) {
        if (!Game.hasShadingLanguageInclude) return;
        
        if (!name.StartsWith('/')) {
            throw new ArgumentException("Named string path must start with '/'", nameof(name));
        }
        
        lock (includesLock) {
            registeredIncludes.Add(name);
        }
        
        unsafe {
            var nameBytes = Encoding.UTF8.GetByteCount(name);
            var contentBytes = Encoding.UTF8.GetByteCount(content);
            
            var namePtr = NativeMemory.Alloc((nuint)(nameBytes + 1));
            var contentPtr = NativeMemory.Alloc((nuint)(contentBytes + 1));
            
            try {
                Encoding.UTF8.GetBytes(name, new Span<byte>(namePtr, nameBytes));
                Encoding.UTF8.GetBytes(content, new Span<byte>(contentPtr, contentBytes));
                ((byte*)namePtr)[nameBytes] = 0;
                ((byte*)contentPtr)[contentBytes] = 0;
                
                Game.arbInclude.NamedString(ARB.ShaderIncludeArb, nameBytes, (byte*)namePtr, contentBytes, (byte*)contentPtr);
            }
            finally {
                NativeMemory.Free(namePtr);
                NativeMemory.Free(contentPtr);
            }
        }
    }
    
    public static void deleteNamedStringARB(string name) {
        if (!Game.hasShadingLanguageInclude) return;
        
        if (!name.StartsWith('/')) {
            throw new ArgumentException("Named string path must start with '/'", nameof(name));
        }
        
        lock (includesLock) {
            registeredIncludes.Remove(name);
        }
        
        unsafe {
            var nameBytes = Encoding.UTF8.GetByteCount(name);
            var namePtr = NativeMemory.Alloc((nuint)(nameBytes + 1));
            
            try {
                Encoding.UTF8.GetBytes(name, new Span<byte>(namePtr, nameBytes));
                ((byte*)namePtr)[nameBytes] = 0;
                
                Game.arbInclude.DeleteNamedString(nameBytes, (byte*)namePtr);
            }
            finally {
                NativeMemory.Free(namePtr);
            }
        }
    }
    
    public static void initializeIncludeFiles() {
        if (!Game.hasShadingLanguageInclude) return;
        
        var searchDirectories = new[] { Assets.getPath("shaders") };
        
        foreach (var dir in searchDirectories) {
            if (!Directory.Exists(dir)) continue;
            
            var incFiles = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
            
            foreach (var filePath in incFiles) {
                var content = File.ReadAllText(filePath);
                var namedPath = "/" + filePath.Replace('\\', '/');
                
                namedStringARB(namedPath, content);
                Log.info($"Registered shader include: {namedPath}");
            }
        }
    }

    private string preprocess(string filePath, string source) {
        if (Path.GetFullPath(filePath) == filePath) {
            includes.Clear();
        }

        includes.Add(Path.GetFullPath(filePath));

        var lines = source.Split(["\r\n", "\n"], StringSplitOptions.None);
        var result = new StringBuilder();
        bool definitionsInjected = false;

        foreach (string line in lines) {
            if (!definitionsInjected && line.TrimStart().StartsWith("#version")) {
                result.AppendLine(line);
                
                if (Game.hasShadingLanguageInclude) {
                    result.AppendLine("#extension GL_ARB_shading_language_include : enable");
                }
                
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
            
            // Only process includes manually if ARB extension is not available
            if (!Game.hasShadingLanguageInclude && line.TrimStart().StartsWith("#include")) {
                var match = includeRegex().Match(line);

                if (match.Success) {
                    var includePath = match.Groups[1].Value;

                    // slap the assetdir on it
                    includePath = "/assets" + includePath;
                    
                    string fullPath;
                    if (includePath.StartsWith('/')) {
                        fullPath = Path.GetFullPath(includePath[1..]);
                    } else {
                        var baseDir = Path.GetDirectoryName(filePath);
                        fullPath = Path.GetFullPath(Path.Combine(baseDir ?? "", includePath));
                    }

                    if (!includes.Contains(fullPath)) {
                        if (File.Exists(fullPath)) {
                            var includeContent = File.ReadAllText(fullPath);
                            var processedInclude = preprocess(fullPath, includeContent);
                            result.Append(processedInclude);
                        }
                        else {
                            result.AppendLine($"// Failed to include '{includePath}': File not found");
                            Log.error($"Failed to include '{includePath}': File not found");
                            result.AppendLine(line);
                        }
                    }
                    else {
                        result.AppendLine($"// Skipped circular include of '{includePath}'");
                        Log.warn($"Skipped circular include of '{includePath}'");
                    }
                }
                else {
                    result.AppendLine(line);
                }
            }
            else {
                result.AppendLine(line);
            }
        }

        return result.ToString();
    }

    public uint load(string shader, ShaderType type) {
        unsafe {
            var shaderHandle = GL.CreateShader(type);
            
            // set debug name
            var debugName = $"{name}_{type}";
            GL.ObjectLabel(GLEnum.Shader, shaderHandle, (uint)debugName.Length, debugName);
            
            GL.ShaderSource(shaderHandle, shader);
        
            if (Game.hasShadingLanguageInclude) {
                compileShaderWithInclude(shaderHandle);
            } else {
                GL.CompileShader(shaderHandle);
            }

            GL.GetShader(shaderHandle, ShaderParameterName.InfoLogLength, out var infoLogLength);
            Span<byte> outb = stackalloc byte[infoLogLength];
            Span<uint> actualLength = stackalloc uint[1];
            GL.GetShaderInfoLog(shaderHandle, (uint)infoLogLength, actualLength, outb);
            var infoLog = Encoding.UTF8.GetString(outb[..(int)actualLength[0]]);
            if (!string.IsNullOrWhiteSpace(infoLog)) {
                throw new InputException($"Error compiling shader of type {type} {name}: {infoLog}");
            }

            return shaderHandle;
        }
    }
    
    private static unsafe void compileShaderWithInclude(uint shaderHandle) {
        string[] searchPaths;
        lock (includesLock) {
            searchPaths = registeredIncludes.ToArray();
        }
        
        if (searchPaths.Length == 0) {
            Game.GL.CompileShader(shaderHandle);
            return;
        }
        
        // Calculate total size needed
        var totalSize = 0;
        var sizes = new int[searchPaths.Length];
        for (int i = 0; i < searchPaths.Length; i++) {
            sizes[i] = Encoding.UTF8.GetByteCount(searchPaths[i]) + 1; // +1 for null terminator
            totalSize += sizes[i];
        }
        
        // Allocate one contiguous block for all strings
        var allStrings = NativeMemory.Alloc((nuint)totalSize);
        var pathPtrs = stackalloc byte*[searchPaths.Length];
        
        try {
            var currentPtr = (byte*)allStrings;
            
            for (int i = 0; i < searchPaths.Length; i++) {
                pathPtrs[i] = currentPtr;
                var written = Encoding.UTF8.GetBytes(searchPaths[i], new Span<byte>(currentPtr, sizes[i] - 1));
                currentPtr[written] = 0; // null terminator
                currentPtr += sizes[i];
            }
            
            Game.arbInclude.CompileShaderInclude(shaderHandle, (uint)searchPaths.Length, pathPtrs, null);
        }
        finally {
            NativeMemory.Free(allStrings);
        }
    }

    public void link(uint vert, uint frag) {
        programHandle = GL.CreateProgram();
        
        // set debug name for program
        GL.ObjectLabel(GLEnum.Program, programHandle, (uint)name.Length, name);
        
        GL.AttachShader(programHandle, vert);
        GL.AttachShader(programHandle, frag);
        GL.LinkProgram(programHandle);
        GL.GetProgram(programHandle, GLEnum.LinkStatus, out var status);
        if (status == 0) {
            throw new InputException($"Program failed to link: {GL.GetProgramInfoLog(programHandle)}");
        }

        GL.GetProgram(programHandle, ProgramPropertyARB.InfoLogLength, out var infoLogLength);
        Span<byte> outb = stackalloc byte[infoLogLength];
        Span<uint> actualLength = stackalloc uint[1];
        GL.GetProgramInfoLog(programHandle, (uint)infoLogLength, actualLength, outb);
        var infoLog = Encoding.UTF8.GetString(outb[..(int)actualLength[0]]);
        if (!string.IsNullOrWhiteSpace(infoLog)) {
            throw new InputException($"Error compiling shader {name}: {infoLog}");
        }

        GL.DetachShader(programHandle, vert);
        GL.DetachShader(programHandle, frag);
        GL.DeleteShader(vert);
        GL.DeleteShader(frag);
    }

    private void link(uint vert) {
        programHandle = GL.CreateProgram();
        
        // set debug name for program
        GL.ObjectLabel(GLEnum.Program, programHandle, (uint)name.Length, name);
        
        GL.AttachShader(programHandle, vert);
        GL.LinkProgram(programHandle);
        GL.GetProgram(programHandle, GLEnum.LinkStatus, out var status);
        if (status == 0) {
            throw new InputException($"Program failed to link: {GL.GetProgramInfoLog(programHandle)}");
        }

        GL.GetProgram(programHandle, ProgramPropertyARB.InfoLogLength, out var infoLogLength);
        Span<byte> outb = stackalloc byte[infoLogLength];
        Span<uint> actualLength = stackalloc uint[1];
        GL.GetProgramInfoLog(programHandle, (uint)infoLogLength, actualLength, outb);
        var infoLog = Encoding.UTF8.GetString(outb[..(int)actualLength[0]]);
        if (!string.IsNullOrWhiteSpace(infoLog)) {
            throw new InputException($"Error compiling shader {name}: {infoLog}");
        }

        GL.DetachShader(programHandle, vert);
        GL.DeleteShader(vert);
    }

    public static Shader createVariant(Silk.NET.OpenGL.Legacy.GL GL, string name, string vertexShaderPath,
        string? fragmentShaderPath = null, ShaderVariant? variant = null, IEnumerable<Definition>? defs = null) {
        var definitions = new List<Definition>();
        
        if (defs != null) {
            definitions.AddRange(defs);
        }

        var selectedVariant = variant ?? Settings.instance.getActualRendererMode() switch {
            RendererMode.CommandList => ShaderVariant.CommandList,
            RendererMode.BindlessMDI => ShaderVariant.Instanced, // BindlessMDI uses same shaders as instanced
            RendererMode.Instanced => ShaderVariant.Instanced,
            RendererMode.Plain => ShaderVariant.Normal,
            _ => ShaderVariant.Normal
        };
        
        if (Game.isNVCard) {
            definitions.Add(new Definition("NV_EXTENSIONS"));
        }

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

    public void use() {
        Game.graphics.shader(programHandle);
    }

    public int getUniformLocation(string name) {
        int location = GL.GetUniformLocation(programHandle, name);
        if (location == -1) {
            throw new InputException($"{name} uniform not found on shader {this.name}.");
        }

        return location;
    }

    public int getUniformLocationOpt(string name) {
        int location = GL.GetUniformLocation(programHandle, name);

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

    public unsafe void setUniform(int loc, Vector2I value) {
        GL.ProgramUniform2(programHandle, loc, 1, (int*)&value);
    }

    public unsafe void setUniform(int loc, Vector3 value) {
        GL.ProgramUniform3(programHandle, loc, 1, (float*)&value);
    }

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
            GL.DeleteProgram(programHandle);
            programHandle = 0;
        }
    }

    public void Dispose() {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~Shader() {
        // Instead of releasing the resources in the finalizer, we throw a SkillIssueException (stuff can't be deleted from a non-main thread so this wouldn't work!)
        if (programHandle != 0) {
            SkillIssueException.throwNew($"Shader {name} was not disposed properly. Please ensure to call Dispose() on all shaders when they are no longer needed.");
        }
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

    public InstantShader(Silk.NET.OpenGL.Legacy.GL GL, string name, string vertexShader, string fragmentShader) : base(GL,
        name, vertexShader, fragmentShader) {
        uMVP = getUniformLocation("uMVP");
    }

    public InstantShader(Silk.NET.OpenGL.Legacy.GL GL, string name, string vertexShader, string fragmentShader, IEnumerable<Definition> defs) : base(GL,
        name, vertexShader, fragmentShader, defs) {
        uMVP = getUniformLocation("uMVP");
    }

    public InstantShader(Silk.NET.OpenGL.Legacy.GL GL, string name, string vertexShader) : base(GL, name, vertexShader) {
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