using Molten.DoublePrecision;
using System.Numerics;
using BlockGame.GL;
using BlockGame.GL.vertexformats;


namespace BlockGame.util;

public class Debug {
    public readonly InstantDrawColour idc;
    
    public Debug() {
        idc = new InstantDrawColour(8192);
        idc.setup();
    }
    
    public void renderTick(double interp) {
        // Set projection and view uniforms for the instant draw system
        Matrix4x4 projMatrix = main.Game.camera.getProjectionMatrix();
        Matrix4x4 viewMatrix = main.Game.camera.getViewMatrix(interp);
        Matrix4x4 mvpMatrix = viewMatrix * projMatrix;
        
        idc.setMVP(mvpMatrix);
    }

    public void drawLine(Vector3D from, Vector3D to, Color4b colour = default) {
        if (colour == default) {
            colour = Color4b.Red;
        }
        
        idc.addVertex(new VertexTinted(from.toVec3(), colour));
        idc.addVertex(new VertexTinted(to.toVec3(), colour));
        
    }

    public void drawAABB(AABB aabb, Color4b colour = default) {
        // corners
        var lsw = aabb.min;
        var lse = new Vector3D(aabb.x1, aabb.y0, aabb.z0);
        var lnw = new Vector3D(aabb.x0, aabb.y0, aabb.z1);
        var lne = new Vector3D(aabb.x1, aabb.y0, aabb.z1);

        var usw = new Vector3D(aabb.x0, aabb.y1, aabb.z0);
        var use = new Vector3D(aabb.x1, aabb.y1, aabb.z0);
        var unw = new Vector3D(aabb.x0, aabb.y1, aabb.z1);
        var une = new Vector3D(aabb.x1, aabb.y1, aabb.z1);

        // join them with lines
        drawLine(lsw, lse);
        drawLine(lse, lne);
        drawLine(lne, lnw);
        drawLine(lnw, lsw);

        drawLine(usw, use);
        drawLine(use, une);
        drawLine(une, unw);
        drawLine(unw, usw);

        // draw columns
        drawLine(lsw, usw);
        drawLine(lse, use);
        drawLine(lne, une);
        drawLine(lnw, unw);
    }

    public void drawTranslucentPlane(Vector3D p1, Vector3D p2, Vector3D p3, Vector3D p4, Color4b colour) {
        idc.addVertex(new VertexTinted(p1.toVec3(), colour));
        idc.addVertex(new VertexTinted(p2.toVec3(), colour));
        idc.addVertex(new VertexTinted(p3.toVec3(), colour));
        idc.addVertex(new VertexTinted(p4.toVec3(), colour));
    }
}