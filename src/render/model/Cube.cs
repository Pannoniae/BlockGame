using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.util;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame;

/**
 * A cube has 6 faces and 24 verts overall.
 * It has a position, and vertices can be added to it.
 **/
public class Cube {
    public readonly BlockVertexTinted[] vertices = new BlockVertexTinted[24];

    /** The position of the cube. */
    public Vector3D position;

    /** The the relative position of the cube (from the base position) */
    public Vector3D cubePos;

    /** The rotation of the cube (from the base position) */
    public Vector3D rotation;

    /** The extents of the cube. */
    public Vector3D ext;

    /** The integer texcoords of the cube. */
    public Vector2D texCoords;

    public Cube(Vector3D cubePos, Vector3D ext, double expand, UVPair[] uvs) {
        this.cubePos = cubePos - expand;
        this.ext = ext + expand * 2;

        if (uvs == null || uvs.Length != 6) {
            throw new ArgumentException("UVs must be of length 6");
        }

        var colour = Color.White;
        // generate vertices
        // west

    }

    public Cube pos(Vector3D pos) {
        position = pos;
        return this;
    }

    public Cube rot(Vector3D rot) {
        rotation = rot;
        return this;
    }

    public void render(InstantDrawTexture idt, double scale) {

    }
}