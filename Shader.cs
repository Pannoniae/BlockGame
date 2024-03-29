﻿using System.Numerics;
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
            //Console.Out.WriteLine("Shader created!");
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
            if (status == 0) {
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

        public int getUniformLocation(string name) {
            int location = GL.GetUniformLocation(programHandle, name);
            if (location == -1) {
                throw new Exception($"{name} uniform not found on shader.");
            }

            return location;
        }

        public void setUniform(int loc, int value) {
            GL.Uniform1(loc, value);
        }

        public unsafe void setUniform(int loc, Matrix4x4 value) {
            GL.UniformMatrix4(loc, 1, false, (float*)&value);
        }

        public void setUniform(int loc, float value) {
            GL.Uniform1(loc, value);
        }

        public void setUniform(int loc, Vector3 value) {
            GL.Uniform3(loc, value.X, value.Y, value.Z);
        }

        public void setUniform(int loc, Vector4 value) {
            GL.Uniform4(loc, value.X, value.Y, value.Z, value.W);
        }
    }
}