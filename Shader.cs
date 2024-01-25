using System.Numerics;
using Silk.NET.OpenGL;


namespace BlockGame {
    public class Shader {
        private string vertexShader;
        private string fragmentShader;

        private GL GL;
        private uint programHandle;

        public Shader(GL GL, string vertexShader, string fragmentShader) {
            this.GL = GL;
            this.vertexShader = File.ReadAllText(vertexShader);
            this.fragmentShader = File.ReadAllText(fragmentShader);

            var vert = load(this.vertexShader, ShaderType.VertexShader);
            var frag = load(this.fragmentShader, ShaderType.FragmentShader);

            link(vert, frag);
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
            if (status == 0)
            {
                throw new Exception($"Program failed to link: {GL.GetProgramInfoLog(programHandle)}");
            }
            
            // yeah this one is from a tutorial, don't judge me
            GL.DetachShader(programHandle, vert);
            GL.DetachShader(programHandle, frag);
            GL.DeleteShader(vert);
            GL.DeleteShader(frag);
        }

        public void use() {
            GL.UseProgram(programHandle);
        }
        
        // uniforms
        public void setUniform(string name, int value)
        {
            int location = GL.GetUniformLocation(programHandle, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            GL.Uniform1(location, value);
        }

        public unsafe void setUniform(string name, Matrix4x4 value)
        {
            //A new overload has been created for setting a uniform so we can use the transform in our shader.
            int location = GL.GetUniformLocation(programHandle, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            GL.UniformMatrix4(location, 1, false, (float*) &value);
        }

        public void setUniform(string name, float value)
        {
            int location = GL.GetUniformLocation(programHandle, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            GL.Uniform1(location, value);
        }

        public void setUniform(string name, Vector3 value)
        {
            int location = GL.GetUniformLocation(programHandle, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            GL.Uniform3(location, value.X, value.Y, value.Z);
        }

        public void setUniform(string name, Vector4 value)
        {
            int location = GL.GetUniformLocation(programHandle, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            GL.Uniform4(location, value.X, value.Y, value.Z, value.W);
        }
    }
}