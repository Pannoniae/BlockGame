using System.Collections.Generic;
using System.Numerics;
using Silk.NET.OpenGL;

namespace BlockGame {
    public class World {
        private VertexArrayObject<float, uint> mesh;
        private Shader shader;
        private byte[,,] blocks = new byte[50, 50, 50];
        private int triCount;

        public World() {
            shader = new Shader(Game.instance.GL, "shader.vert", "shader.frag");

            for (int x = 0; x < 50; x++) {
                for (int y = 0; y < 50; y++) {
                    for (int z = 0; z < 50; z++) {
                        blocks[x, y, z] = 1;
                    }
                }
            }
        }

        public void meshTheWorld() {
            List<uint> indices = new();
            List<float> vertices = new();

            uint count = 0;

            for (int x = 0; x < blocks.GetLength(0); x++) {
                for (int y = 0; y < blocks.GetLength(1); y++) {
                    for (int z = 0; z < blocks.GetLength(1); z++) {
                        if (blocks[x, y, z] != 0) {
                            float[] currentVertices = {
                                x, y, z,
                                x + 1, y, z,
                                x + 1, y + 1, z,
                                x + 1, y + 1, z,
                                x, y + 1, z,
                                x, y, z,

                                x, y, z + 1,
                                x + 1, y, z + 1,
                                x + 1, y + 1, z + 1,
                                x + 1, y + 1, z + 1,
                                x, y + 1, z + 1,
                                x, y , z + 1,

                                x, y + 1, z + 1,
                                x, y + 1, z,
                                x, y, z,
                                x, y, z,
                                x, y, z + 1,
                                x, y + 1, z + 1,

                                x + 1, y + 1, z + 1,
                                x + 1, y + 1, z,
                                x + 1, y, z,
                                x + 1, y, z,
                                x + 1, y, z + 1,
                                x + 1, y + 1, z + 1,

                                x, y, z,
                                x + 1, y, z,
                                x + 1, y, z + 1,
                                x + 1, y, z + 1,
                                z, y, z + 1,
                                z, y, z,

                                z, y + 1, z,
                                x + 1, y + 1, z,
                                x + 1, y + 1, z + 1,
                                x + 1, y + 1, z + 1,
                                x, y + 1, z + 1,
                                x, y + 1, z,
                            };

                            vertices.AddRange(currentVertices);
                            for (int i = 0; i < 6; i++) {
                                indices.Add(count);
                                count++;
                            }
                            // add the sides to the index buffer
                        }
                    }
                }
            }

            var ebo = new BufferObject<uint>(Game.instance.GL, indices.ToArray(),
                BufferTargetARB.ElementArrayBuffer);
            var vbo = new BufferObject<float>(Game.instance.GL, vertices.ToArray(), BufferTargetARB.ArrayBuffer);
            mesh = new VertexArrayObject<float, uint>(Game.instance.GL, vbo, ebo);
            mesh.vertexAttributePointer(0, 3, VertexAttribPointerType.Float, 3, 0);

            triCount = vertices.Count / 3;
        }

        public void draw() {
            mesh.bind();
            shader.use();
            shader.setUniform("uModel", Matrix4x4.Identity);
            shader.setUniform("uView", Game.instance.camera.getViewMatrix());
            shader.setUniform("uProjection", Game.instance.camera.getProjectionMatrix());
            Game.instance.GL.DrawArrays(PrimitiveType.Lines, 0, (uint)triCount - 1);
        }
    }
}