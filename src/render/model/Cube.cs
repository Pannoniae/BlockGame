using System.Numerics;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.util;
using BlockGame.world;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.render.model;

/**
 * A cube has 6 faces and 24 verts overall.
 * It has a position, and vertices can be added to it.
 *
 * GLaDOS: this enrichment centre does not guarantee the precision of the edges of all provided cubes.
 *
 * TODO do we *really* need doubles here on the CPU side? I'm not SURE, but I don't wanna be fucked by z-fighting and texture bleeding again
 * status update: nevermind
 **/
public class Cube {
    public EntityVertex[] vertices = new EntityVertex[24];

    private static int v;

    /** The position of the cube. */
    public Vector3 position;

    /** The the relative position of the cube (from the base position) */
    public Vector3 offset;

    /** The rotation of the cube (from the base position) IN DEGREES */
    public Vector3 rotation;

    /** The extents of the cube. */
    public Vector3 extents;

    /** The integer texcoords of the cube. */
    public Vector2 texCoords;

    /** is the cube rendered? */
    public bool rendered = true;

    /** Whether to mirror the whole thing horizontally. (i'm lazy and don't wanna define the arms twice) */
    public bool hmirror;

    public bool dside;


    private Matrix4x4 cmat;

    public Cube() {

    }

    public Cube pos(float x, float y, float z) {
        position = new Vector3(x, y, z);
        return this;
    }

    public Cube off(float x, float y, float z) {
        offset = new Vector3(x, y, z);
        return this;
    }


    public Cube tex(int x, int y) {
        texCoords = new Vector2(x, y);
        return this;
    }

    public Cube ext(float x, float y, float z) {
        extents = new Vector3(x, y, z);
        return this;
    }

    public Cube rot(float x, float y, float z) {
        rotation = new Vector3(x, y, z);
        return this;
    }

    public Cube exp(float e) {
        extents += new Vector3(e * 2);
        offset -= new Vector3(e);
        return this;
    }

    public Cube mirror() {
        hmirror = !hmirror;
        return this;
    }

    public Cube dsided() {
        dside = true;
        return this;
    }


    /*
     * TODO we could do some fuckery with two identical classes, one building stage and one final and the methods above would return the builder type
     * then we just bitcast the builder to the final type in gen() and return that
     * BUT I'M LAZY AGAIN
     *
     * alternatively we could define all the props in some giant
     */
    public Cube gen(int xs, int ys) {
        float x0 = offset.X;
        float y0 = offset.Y;
        float z0 = offset.Z;
        float x1 = offset.X + extents.X;
        float y1 = offset.Y + extents.Y;
        float z1 = offset.Z + extents.Z;

        float u0 = texCoords.X;
        float v0 = texCoords.Y;

        float xe = extents.X;
        float ye = extents.Y;
        float ze = extents.Z;

        if (hmirror) {
            // swap x0 and x1
            (x0, x1) = (x1, x0);
        }

        var nnn = new Vector3(x0, y0, z0);
        var nnx = new Vector3(x0, y0, z1);
        var nxn = new Vector3(x0, y1, z0);
        var nxx = new Vector3(x0, y1, z1);
        var xnn = new Vector3(x1, y0, z0);
        var xnx = new Vector3(x1, y0, z1);
        var xxn = new Vector3(x1, y1, z0);
        var xxx = new Vector3(x1, y1, z1);

        // build vertices for real!

        v = 0;

        face(nxx, nnx, nnn, nxn, u0 + ze + xe, u0 + ze + xe + ze, v0 + ze, v0 + ze + ye, hmirror); // west
        face(xxn, xnn, xnx, xxx, u0, u0 + ze, v0 + ze, v0 + ze + ye, hmirror); // east
        face(nxn, nnn, xnn, xxn, u0 + ze + xe + ze, u0 + ze + xe + ze + xe, v0 + ze, v0 + ze + ye, hmirror); // south
        face(xxx, xnx, nnx, nxx, u0 + ze, u0 + ze + xe, v0 + ze, v0 + ze + ye, hmirror); // north
        face(xnx, xnn, nnn, nnx, u0 + ze + xe, u0 + ze + xe + xe, v0, v0 + ze, hmirror); // down
        face(nxx, nxn, xxn, xxx, u0 + ze, u0 + ze + xe, v0, v0 + ze, hmirror); // up

        // divide uvs down
        for (int i = 0; i < vertices.Length; i++) {
            vertices[i].u /= xs;
            vertices[i].v /= ys;
        }


        if (hmirror) {
            // mirror the vertices horizontally
            for (int i = 0; i < vertices.Length; i += 4) {
                // swap 1 <-> 4 and 2 <-> 3 in each 4-vert face
                (vertices[i + 0], vertices[i + 3]) = (vertices[i + 3], vertices[i + 0]);
                (vertices[i + 1], vertices[i + 2]) = (vertices[i + 2], vertices[i + 1]);
            }
        }

        // if dside is set, we also render the backfaces. we need to invert the normals
        if (dside) {
            var originalVerts = (EntityVertex[])vertices.Clone();
            Array.Resize(ref vertices, vertices.Length * 2);

            for (int i = 0; i < originalVerts.Length; i += 4) {
                var v0f = originalVerts[i + 3];
                var v1f = originalVerts[i + 2];
                var v2f = originalVerts[i + 1];
                var v3f = originalVerts[i + 0];

                var n = v0f.unpackNormal();
                n = -n;
                vertices[v++] = new EntityVertex(v0f.x, v0f.y, v0f.z, v0f.u, v0f.v, v0f.r, v0f.g, v0f.b, v0f.a, n.X, n.Y, n.Z);
                vertices[v++] = new EntityVertex(v1f.x, v1f.y, v1f.z, v1f.u, v1f.v, v1f.r, v1f.g, v1f.b, v1f.a, n.X, n.Y, n.Z);
                vertices[v++] = new EntityVertex(v2f.x, v2f.y, v2f.z, v2f.u, v2f.v, v2f.r, v2f.g, v2f.b, v2f.a, n.X, n.Y, n.Z);
                vertices[v++] = new EntityVertex(v3f.x, v3f.y, v3f.z, v3f.u, v3f.v, v3f.r, v3f.g, v3f.b, v3f.a, n.X, n.Y, n.Z);
            }


        }

        return this;
    }

    private void face(Vector3 xx, Vector3 xn, Vector3 nn, Vector3 nx, float u0, float u1, float v0, float v1, bool mirrored) {

        // normal is just the cross product of two edges:tm:
        var e0 = xn - xx; // bottom left to top left
        var e1 = nx - xx; // top right to top left
        var n = mirrored ? Vector3.Cross(e0, e1) : Vector3.Cross(e1, e0);
        n.normi();

        vertices[v++] = new EntityVertex(xx.X, xx.Y, xx.Z, u0, v0, 255, 255, 255, 255, n.X, n.Y, n.Z); // top left
        vertices[v++] = new EntityVertex(xn.X, xn.Y, xn.Z, u0, v1, 255, 255, 255, 255, n.X, n.Y, n.Z); // bottom left
        vertices[v++] = new EntityVertex(nn.X, nn.Y, nn.Z, u1, v1, 255, 255, 255, 255, n.X, n.Y, n.Z); // bottom right
        vertices[v++] = new EntityVertex(nx.X, nx.Y, nx.Z, u1, v0, 255, 255, 255, 255, n.X, n.Y, n.Z); // top right
    }


    public void xfrender(FastInstantDrawEntity ide, MatrixStack mat, float scale) {
        if (!rendered) return;

        ref var c = ref cmat;
        c = Matrix4x4.Identity;

        if (rotation != Vector3.Zero) {
            c *= Matrix4x4.CreateRotationX(Meth.deg2rad(rotation.X));
            c *= Matrix4x4.CreateRotationY(Meth.deg2rad(rotation.Y));
            c *= Matrix4x4.CreateRotationZ(Meth.deg2rad(rotation.Z));
        }
        c *= Matrix4x4.CreateTranslation(position.X * scale, position.Y * scale, position.Z * scale);

        foreach (ref EntityVertex vert in vertices.AsSpan()) {
            var v = vert;
            EntityVertex.mul(ref v, c, scale);
            ide.addVertex(v);
        }
    }

    // todo if there is no rotation, we could just batch the cubes together and draw them all at once!
    // like, to vert.scale(scale).offset(position * scale) and just use the same instantdrawentity call
    // we'd need to track this though, and I'm not in the mood to fuck this up rn...
    public void render(MatrixStack mat, float scale) {
        var ide = EntityRenderers.ide;
        if (rendered) {
            mat.push();
            mat.translate(position.X * scale, position.Y * scale, position.Z * scale);
            mat.rotate(rotation.X, 1, 0, 0);
            mat.rotate(rotation.Y, 0, 1, 0);
            mat.rotate(rotation.Z, 0, 0, 1);

            ide.begin(PrimitiveType.Quads);

            // actually draw!
            foreach (EntityVertex vert in vertices) {
                var v = vert.scale(scale);
                ide.addVertex(v);
            }
            ide.end();

            mat.pop();
        }
    }
}