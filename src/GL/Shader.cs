using System.Numerics;
using Silk.NET.OpenGL;

namespace BlockGame.GL;

public class Shader {
    private string vertexShader;
    private string fragmentShader;

    private readonly Silk.NET.OpenGL.GL GL;
    private uint programHandle;

    public Shader(Silk.NET.OpenGL.GL GL, string vertexShader, string fragmentShader) {
        this.GL = GL;
        this.vertexShader = File.ReadAllText(vertexShader);
        this.fragmentShader = File.ReadAllText(fragmentShader);

        var vert = load(this.vertexShader, ShaderType.VertexShader);
        var frag = load(this.fragmentShader, ShaderType.FragmentShader);
        link(vert, frag);
    }

    /// <summary>
    /// Used for depth pass shaders.
    /// </summary>
    public Shader(Silk.NET.OpenGL.GL GL, string vertexShader) {
        this.GL = GL;
        this.vertexShader = File.ReadAllText(vertexShader);
        var vert = load(this.vertexShader, ShaderType.VertexShader);
        link(vert);
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
            throw new Exception($"{name} uniform not found on shader.");
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

    unsafe public void setUniform(int loc, Vector3 value) {
        GL.ProgramUniform3(programHandle, loc, 1, (float*)&value);
    }


    // If you change this to ProgramUniform3, it's twice as slow... why???
    public void setUniformBound(int loc, float x, float y, float z) {
        GL.Uniform3(loc, x, y, z);
    }

    unsafe public void setUniform(int loc, Vector4 value) {
        GL.ProgramUniform4(programHandle, loc, 1, (float*)&value);
    }

    public void setUniform(int loc, bool value) {
        GL.ProgramUniform1(programHandle, loc, value ? 1 : 0);
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

    public InstantShader(Silk.NET.OpenGL.GL GL, string vertexShader, string fragmentShader) : base(GL, vertexShader, fragmentShader) {
        uMVP = getUniformLocation("uMVP");
    }
    
    public InstantShader(Silk.NET.OpenGL.GL GL, string vertexShader) : base(GL, vertexShader) {
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