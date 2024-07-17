using System.Numerics;
using Silk.NET.Maths;
using TrippyGL;

namespace BlockGame;

public class Particle : Entity {

    /// <summary>
    /// The texture coordinates of the particle.
    /// </summary>
    public float u;

    /// <summary>
    /// The texture coordinates of the particle.
    /// </summary>
    public float v;

    /// <summary>
    /// The texture the particle uses.
    /// </summary>
    public string texture;

    /// <summary>
    /// The size of the particle. (world coords)
    /// </summary>
    public double size;

    /// <summary>
    /// The time-to-live of the particle in ticks.
    /// </summary>
    public int ttl;

    /// <summary>
    /// Is this particle valid?
    /// </summary>
    public bool active;

    public Particle(Vector3D<double> position, string texture, float u, float v, double size, int ttl) {
        this.position = position;
        this.texture = texture;
        this.u = u;
        this.v = v;
        this.size = size;
        this.ttl = ttl;
        active = true;
    }

    public void update(double dt) {
        if (active) {
            // gravity
            accel.Y = -5;
            ttl -= 1;
        }
    }

    public void render(double dt, double interp) {
        if (active) {
            //var pos = Vector3D<int>.Lerp(prevPosition, position, (float)interp);
            //var col = new Vector4(tint, ttl / maxLife);
            //Renderer.instance.drawBillboard(pos, size, col);
        }
    }

    public void reset() {
        active = false;
    }
}