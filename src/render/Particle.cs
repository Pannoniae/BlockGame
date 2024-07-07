using System.Numerics;
using Silk.NET.Maths;
using TrippyGL;

namespace BlockGame;

public class Particle : Entity {
    public Color4b tint;
    public double size;

    /// <summary>
    /// The time-to-live of the particle in ticks.
    /// </summary>
    public int ttl;
    public int maxLife;

    /// <summary>
    /// Is this particle valid?
    /// </summary>
    public bool active;

    public Particle(Vector3D<double> position, Color4b tint, double size, int ttl) {
        this.position = position;
        this.tint = tint;
        this.size = size;
        this.ttl = ttl;
        maxLife = ttl;
        active = true;
    }

    public void update(double dt) {
        if (active) {
            // gravity
            accel.Y = -5;
            ttl -= 1;
        }
    }

    public void render(double interp) {
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