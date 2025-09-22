using BlockGame.GL;
using BlockGame.main;
using BlockGame.util;
using BlockGame.world.chunk;
using Molten.DoublePrecision;
using BoundingFrustum = BlockGame.util.meth.BoundingFrustum;

namespace BlockGame.render;

public partial class WorldRenderer {
    public static bool isVisible(SubChunk subChunk, BoundingFrustum frustum) {
        return !frustum.outsideCameraUpDown(subChunk.box);
    }

    /// <summary>
    /// Batched visibility check for 8 subchunks at once. Updates the isRendered field for each subchunk based on visibility.
    /// </summary>
    public static unsafe void isVisibleEight(SubChunk[] subChunks, BoundingFrustum frustum) {
        // Extract AABBs from subchunks
        Span<AABB> aabbs = stackalloc AABB[8];
        for (int i = 0; i < 8; i++) {
            aabbs[i] = subChunks[i].box;
        }

        // Get visibility mask (1 bit = outside/not visible, 0 bit = visible)
        byte outsideMask = frustum.outsideCameraUpDownEight(aabbs);

        // Update isRendered for each subchunk
        for (int i = 0; i < 8; i++) {
            subChunks[i].isRendered = (outsideMask & (1 << i)) == 0;
        }
    }

    private void setUniformPos(SubChunkCoord coord, Shader s, Vector3D cameraPos) {
        s.setUniformBound(uChunkPos, (float)(coord.x * 16 - cameraPos.X), (float)(coord.y * 16 - cameraPos.Y),
            (float)(coord.z * 16 - cameraPos.Z));
    }

    private void setUniformPosWater(SubChunkCoord coord, Shader s, Vector3D cameraPos) {
        s.setUniformBound(wateruChunkPos, (float)(coord.x * 16 - cameraPos.X), (float)(coord.y * 16 - cameraPos.Y),
            (float)(coord.z * 16 - cameraPos.Z));
    }

    private void setUniformPosDummy(SubChunkCoord coord, Shader s, Vector3D cameraPos) {
        s.setUniformBound(dummyuChunkPos, (float)(coord.x * 16 - cameraPos.X), (float)(coord.y * 16 - cameraPos.Y),
            (float)(coord.z * 16 - cameraPos.Z));
    }

    public void drawOpaque(SubChunk subChunk, Vector3D cameraPos) {
        var coord = subChunk.coord;
        var vao = subChunk.vao;
        if (subChunk.hasRenderOpaque) {
            vao.bind();
            //GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
            setUniformPos(coord, worldShader, cameraPos);
            uint renderedVerts = vao.render();
            Game.metrics.renderedVerts += (int)renderedVerts;
            Game.metrics.renderedSubChunks += 1;
        }
    }

    public void drawTransparent(SubChunk subChunk, Vector3D cameraPos) {
        var coord = subChunk.coord;
        var watervao = subChunk.watervao;
        if (subChunk.hasRenderTranslucent) {
            watervao.bind();
            setUniformPosWater(coord, waterShader, cameraPos);
            uint renderedTransparentVerts = watervao.render();
            Game.metrics.renderedVerts += (int)renderedTransparentVerts;
        }
    }

    public void drawTransparentDummy(SubChunk subChunk, Vector3D cameraPos) {
        var coord = subChunk.coord;
        var watervao = subChunk.watervao;
        if (subChunk.hasRenderTranslucent) {
            watervao.bind();
            setUniformPosDummy(coord, dummyShader, cameraPos);
            uint renderedTransparentVerts = watervao.render();
            Game.metrics.renderedVerts += (int)renderedTransparentVerts;
        }
    }

    public static void drawOpaqueUBO(SubChunk subChunk, uint idx) {
        var coord = subChunk.coord;
        var vao = subChunk.vao;
        if (subChunk.hasRenderOpaque) {
            vao.bind();

            uint renderedVerts = vao.renderBaseInstance(idx);
            Game.metrics.renderedVerts += (int)renderedVerts;
            Game.metrics.renderedSubChunks += 1;
        }
    }

    public static void drawTransparentUBO(SubChunk subChunk, uint idx) {
        var coord = subChunk.coord;
        var watervao = subChunk.watervao;
        if (subChunk.hasRenderTranslucent) {
            watervao.bind();

            uint renderedTransparentVerts = watervao.renderBaseInstance(idx);
            Game.metrics.renderedVerts += (int)renderedTransparentVerts;
        }
    }

    public static void drawOpaqueCMDL(SubChunk subChunk, uint idx) {
        var coord = subChunk.coord;
        var vao = subChunk.vao;
        if (subChunk.hasRenderOpaque) {
            vao.addCMDLCommand();

            uint renderedVerts = vao.renderCMDL(idx);
            Game.metrics.renderedVerts += (int)renderedVerts;
            Game.metrics.renderedSubChunks += 1;
        }
    }

    public static void drawTransparentCMDL(SubChunk subChunk, uint idx) {
        var coord = subChunk.coord;
        var watervao = subChunk.watervao;
        if (subChunk.hasRenderTranslucent) {
            watervao.addCMDLCommand();

            uint renderedTransparentVerts = watervao.renderCMDL(idx);
            Game.metrics.renderedVerts += (int)renderedTransparentVerts;
        }
    }

    /// <summary>
    /// Add opaque chunks to the bindless indirect buffer for batch rendering
    /// </summary>
    public void addOpaqueToBindlessBuffer(SubChunk subChunk, uint instanceId) {
        if (subChunk.hasRenderOpaque) {
            subChunk.vao.addChunkCommand(bindlessBuffer, instanceId, elementAddress, elementLen);
        }
    }

    /// <summary>
    /// Add transparent chunks to the bindless indirect buffer for batch rendering
    /// </summary>
    public void addTransparentToBindlessBuffer(SubChunk subChunk, uint instanceId) {
        if (subChunk.hasRenderTranslucent) {
            subChunk.watervao.addChunkCommand(bindlessBuffer, instanceId, elementAddress, elementLen);
        }
    }

}