using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BlockGame.GL;
using BlockGame.main;
using BlockGame.util;
using BlockGame.world.chunk;
using Molten.DoublePrecision;
using BoundingFrustum = BlockGame.util.meth.BoundingFrustum;
using Plane = System.Numerics.Plane;

namespace BlockGame.render;

public partial class WorldRenderer {
    public static bool isVisible(SubChunk subChunk, BoundingFrustum frustum) {
        return !frustum.outsideCameraUpDown(subChunk.box);
    }

    /// <summary>
    /// Batched visibility check for 8 subchunks at once. Updates the isRendered field for each subchunk based on visibility.
    /// </summary>
    public static unsafe void isVisibleEight(SubChunk[] subChunks, BoundingFrustum frustum) {
        // Get visibility mask (1 bit = outside/not visible, 0 bit = visible)
        byte outsideMask = isFrontTwoEightD(subChunks, frustum.Top, frustum.Bottom);

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
        if (subChunk.hasRenderOpaque && vao != null) {
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
        if (subChunk.hasRenderTranslucent && watervao != null) {
            watervao.bind();
            setUniformPosWater(coord, waterShader, cameraPos);
            uint renderedTransparentVerts = watervao.render();
            Game.metrics.renderedVerts += (int)renderedTransparentVerts;
        }
    }

    public void drawTransparentDummy(SubChunk subChunk, Vector3D cameraPos) {
        var coord = subChunk.coord;
        var watervao = subChunk.watervao;
        if (subChunk.hasRenderTranslucent && watervao != null) {
            watervao.bind();
            setUniformPosDummy(coord, dummyShader, cameraPos);
            uint renderedTransparentVerts = watervao.render();
            Game.metrics.renderedVerts += (int)renderedTransparentVerts;
        }
    }

    public static void drawOpaqueUBO(SubChunk subChunk, uint idx) {
        var coord = subChunk.coord;
        var vao = subChunk.vao;
        if (subChunk.hasRenderOpaque && vao != null) {
            vao.bind();

            uint renderedVerts = vao.renderBaseInstance(idx);
            Game.metrics.renderedVerts += (int)renderedVerts;
            Game.metrics.renderedSubChunks += 1;
        }
    }

    public static void drawTransparentUBO(SubChunk subChunk, uint idx) {
        var coord = subChunk.coord;
        var watervao = subChunk.watervao;
        if (subChunk.hasRenderTranslucent && watervao != null) {
            watervao.bind();

            uint renderedTransparentVerts = watervao.renderBaseInstance(idx);
            Game.metrics.renderedVerts += (int)renderedTransparentVerts;
        }
    }

    public static void drawOpaqueCMDL(SubChunk subChunk, uint idx) {
        var coord = subChunk.coord;
        var vao = subChunk.vao;
        if (subChunk.hasRenderOpaque && vao != null) {
            vao.addCMDLCommand();

            uint renderedVerts = vao.renderCMDL(idx);
            Game.metrics.renderedVerts += (int)renderedVerts;
            Game.metrics.renderedSubChunks += 1;
        }
    }

    public static void drawTransparentCMDL(SubChunk subChunk, uint idx) {
        var coord = subChunk.coord;
        var watervao = subChunk.watervao;
        if (subChunk.hasRenderTranslucent && watervao != null) {
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
            subChunk.vao.addChunkCommand(bindlessBuffer, instanceId, Game.graphics.elementAddress, Game.graphics.elementLen);
        }
    }

    /// <summary>
    /// Add transparent chunks to the bindless indirect buffer for batch rendering
    /// </summary>
    public void addTransparentToBindlessBuffer(SubChunk subChunk, uint instanceId) {
        if (subChunk.hasRenderTranslucent) {
            subChunk.watervao.addChunkCommand(bindlessBuffer, instanceId, Game.graphics.elementAddress, Game.graphics.elementLen);
        }
    }

    /// <summary>
    /// Optimized isFrontTwoEight that reads directly from SubChunk[] without copying AABBs.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte isFrontTwoEightD(SubChunk[] subChunks, Plane p1, Plane p2) {
        return Avx2.IsSupported ? isFrontTwoEightDAvx2(subChunks, p1, p2) : isFrontTwoEightDFallback(subChunks, p1, p2);
    }

    /// <summary>
    /// AVX2
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe byte isFrontTwoEightDAvx2(SubChunk[] subChunks, Plane p1, Plane p2) {
        // Load plane components once
        ref var pp1 = ref Unsafe.As<Plane, float>(ref p1);
        ref var pp2 = ref Unsafe.As<Plane, float>(ref p2);

        ref var box0 = ref subChunks[0].box;
        ref var box1 = ref subChunks[1].box;
        ref var box2 = ref subChunks[2].box;
        ref var box3 = ref subChunks[3].box;
        ref var box4 = ref subChunks[4].box;
        ref var box5 = ref subChunks[5].box;
        ref var box6 = ref subChunks[6].box;
        ref var box7 = ref subChunks[7].box;

        // Build vectors
        var vMinX = Vector256.Create(
            (float)box0.min.X, (float)box1.min.X,
            (float)box2.min.X, (float)box3.min.X,
            (float)box4.min.X, (float)box5.min.X,
            (float)box6.min.X, (float)box7.min.X);

        var vMinY = Vector256.Create(
            (float)box0.min.Y, (float)box1.min.Y,
            (float)box2.min.Y, (float)box3.min.Y,
            (float)box4.min.Y, (float)box5.min.Y,
            (float)box6.min.Y, (float)box7.min.Y);

        var vMinZ = Vector256.Create(
            (float)box0.min.Z, (float)box1.min.Z,
            (float)box2.min.Z, (float)box3.min.Z,
            (float)box4.min.Z, (float)box5.min.Z,
            (float)box6.min.Z, (float)box7.min.Z);

        var vMaxX = Vector256.Create(
            (float)box0.max.X, (float)box1.max.X,
            (float)box2.max.X, (float)box3.max.X,
            (float)box4.max.X, (float)box5.max.X,
            (float)box6.max.X, (float)box7.max.X);

        var vMaxY = Vector256.Create(
            (float)box0.max.Y, (float)box1.max.Y,
            (float)box2.max.Y, (float)box3.max.Y,
            (float)box4.max.Y, (float)box5.max.Y,
            (float)box6.max.Y, (float)box7.max.Y);

        var vMaxZ = Vector256.Create(
            (float)box0.max.Z, (float)box1.max.Z,
            (float)box2.max.Z, (float)box3.max.Z,
            (float)box4.max.Z, (float)box5.max.Z,
            (float)box6.max.Z, (float)box7.max.Z);

        // Broadcast plane components
        var vPlaneX1 = Vector256.Create(pp1);
        var vPlaneY1 = Vector256.Create(Unsafe.Add(ref pp1, 1));
        var vPlaneZ1 = Vector256.Create(Unsafe.Add(ref pp1, 2));
        var vPlaneD1 = Vector256.Create(Unsafe.Add(ref pp1, 3));

        var vPlaneX2 = Vector256.Create(pp2);
        var vPlaneY2 = Vector256.Create(Unsafe.Add(ref pp2, 1));
        var vPlaneZ2 = Vector256.Create(Unsafe.Add(ref pp2, 2));
        var vPlaneD2 = Vector256.Create(Unsafe.Add(ref pp2, 3));

        // Select min or max based on plane normal signs
        var vCoordX1 = Avx.BlendVariable(vMaxX, vMinX,
            Avx.Compare(vPlaneX1, Vector256<float>.Zero, FloatComparisonMode.OrderedGreaterThanOrEqualNonSignaling));
        var vCoordY1 = Avx.BlendVariable(vMaxY, vMinY,
            Avx.Compare(vPlaneY1, Vector256<float>.Zero, FloatComparisonMode.OrderedGreaterThanOrEqualNonSignaling));
        var vCoordZ1 = Avx.BlendVariable(vMaxZ, vMinZ,
            Avx.Compare(vPlaneZ1, Vector256<float>.Zero, FloatComparisonMode.OrderedGreaterThanOrEqualNonSignaling));

        var vCoordX2 = Avx.BlendVariable(vMaxX, vMinX,
            Avx.Compare(vPlaneX2, Vector256<float>.Zero, FloatComparisonMode.OrderedGreaterThanOrEqualNonSignaling));
        var vCoordY2 = Avx.BlendVariable(vMaxY, vMinY,
            Avx.Compare(vPlaneY2, Vector256<float>.Zero, FloatComparisonMode.OrderedGreaterThanOrEqualNonSignaling));
        var vCoordZ2 = Avx.BlendVariable(vMaxZ, vMinZ,
            Avx.Compare(vPlaneZ2, Vector256<float>.Zero, FloatComparisonMode.OrderedGreaterThanOrEqualNonSignaling));

        // Compute dot products using FMA
        var zd1 = Fma.MultiplyAdd(vPlaneZ1, vCoordZ1, vPlaneD1);
        var zd2 = Fma.MultiplyAdd(vPlaneZ2, vCoordZ2, vPlaneD2);
        var yd1 = Fma.MultiplyAdd(vPlaneY1, vCoordY1, zd1);
        var yd2 = Fma.MultiplyAdd(vPlaneY2, vCoordY2, zd2);
        var vDots1 = Fma.MultiplyAdd(vPlaneX1, vCoordX1, yd1);
        var vDots2 = Fma.MultiplyAdd(vPlaneX2, vCoordX2, yd2);

        // Check which are in front of either plane
        var positive1 = Avx.Compare(vDots1, Vector256<float>.Zero, FloatComparisonMode.OrderedGreaterThanNonSignaling);
        var positive2 = Avx.Compare(vDots2, Vector256<float>.Zero, FloatComparisonMode.OrderedGreaterThanNonSignaling);
        var anyPositive = Avx.Or(positive1, positive2);

        return (byte)Avx.MoveMask(anyPositive);
    }

    /// <summary>
    /// SSE fallback for systems without AVX2.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte isFrontTwoEightDFallback(SubChunk[] subChunks, Plane p1, Plane p2) {
        var vP1 = Vector128.LoadUnsafe(ref Unsafe.As<Plane, float>(ref p1));
        var vP2 = Vector128.LoadUnsafe(ref Unsafe.As<Plane, float>(ref p2));

        var vPNormal1 = Vector128.GreaterThanOrEqual(vP1, Vector128<float>.Zero);
        var vPNormal2 = Vector128.GreaterThanOrEqual(vP2, Vector128<float>.Zero);

        byte result = 0;

        for (int i = 0; i < 8; i++) {
            var box = subChunks[i].box;
            var vMin = Vector128.Create((float)box.min.X, (float)box.min.Y, (float)box.min.Z, 1.0f);
            var vMax = Vector128.Create((float)box.max.X, (float)box.max.Y, (float)box.max.Z, 1.0f);

            var vCoord1 = Vector128.ConditionalSelect(vPNormal1, vMin, vMax);
            var vCoord2 = Vector128.ConditionalSelect(vPNormal2, vMin, vMax);

            var vDot1 = Vector128.Dot(vP1, vCoord1);
            var vDot2 = Vector128.Dot(vP2, vCoord2);

            result |= (byte)((vDot1 > 0 | vDot2 > 0).toByte() << i);
        }

        return result;
    }

}