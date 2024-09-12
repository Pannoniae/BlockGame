using BlockGame.util;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.model;

/// <summary>
/// A cube has 6 faces and 24 verts overall.
/// It has a position, and vertices can are added to it.
/// </summary>
public class Cube {
    public readonly BlockVertexTinted[] vertices = new BlockVertexTinted[24];

    /// <summary>
    /// The position of the cube.
    /// </summary>
    public Vector3D position;


    /// <summary>
    /// The the relative position of the cube (from the base position)
    /// </summary>
    public Vector3D cubePos;

    /// <summary>
    /// The rotation of the cube (from the base position)
    /// </summary>
    public Vector3D rotation;

    /// <summary>
    /// The extents of the cube.
    /// </summary>
    public Vector3D ext;

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

    public void render(double scale) {

    }
}