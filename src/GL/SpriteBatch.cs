using System.Numerics;
using Silk.NET.OpenGL.Legacy;
using System.Runtime.InteropServices;
using BlockGame.main;
using Molten;
using PrimitiveType = Silk.NET.OpenGL.Legacy.PrimitiveType;

namespace BlockGame.GL;

public enum BatcherBeginMode {
    Deferred, // Draw calls batched until End() is called
    Immediate, // Draw calls executed immediately
    OnTheFly, // Draw calls batched by texture and executed when texture changes
    SortByTexture, // Draw calls sorted by texture at End()
    SortFrontToBack, // Draw calls sorted front to back at End()
    SortBackToFront // Draw calls sorted back to front at End()
}

[StructLayout(LayoutKind.Sequential)]
public struct VertexColorTexture(Vector3 position, Color color, Vector2 texCoords) {
    public Vector3 Position = position;
    public Color Color = color;
    public Vector2 TexCoords = texCoords;
}

/// <summary>
/// A simple and efficient batch renderer for 2D textures
/// </summary>
public sealed class SpriteBatch : IDisposable {
    // Constants
    public const uint InitialBatchItemsCapacity = 256;
    public const uint MaxBatchItemCapacity = int.MaxValue;
    private const uint InitialBufferCapacity = InitialBatchItemsCapacity * 4;
    private const uint MaxBufferCapacity = 32768 * 16;

    // OpenGL resources
    private readonly Silk.NET.OpenGL.Legacy.GL GL;
    public readonly uint vao;
    private uint vbo;
    private uint ibo;

    // Shader
    internal Shader shader;
    private int textureUniform;

    // Batch data
    private SpriteBatchItem[] batchItems;
    private uint batchItemCount;
    private VertexColorTexture[] vertices;
    private ushort[] indices;

    // State
    /**
     * Disable the NV_draw_texture path if true, when the rendering happens with a world matrix instead of screen space.
     */
    public bool NoScreenSpace { get; set; } = false;

    public bool IsActive { get; private set; }
    public BatcherBeginMode BeginMode { get; private set; }
    public bool IsDisposed { get; private set; }

    public SpriteBatch(Silk.NET.OpenGL.Legacy.GL gl, uint initialBatchCapacity = InitialBatchItemsCapacity) {
        GL = gl;

        // Initialize OpenGL resources
        vao = GL.CreateVertexArray();
        GL.BindVertexArray(vao);

        vbo = GL.CreateBuffer();
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        GL.ObjectLabel(ObjectIdentifier.Buffer, vbo, uint.MaxValue, "SpriteBatch Vertex Buffer");

        ibo = GL.CreateBuffer();
        GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, ibo);
        GL.ObjectLabel(ObjectIdentifier.Buffer, ibo, uint.MaxValue, "SpriteBatch Index Buffer");

        // Set up vertex attributes
        unsafe {
            // Set up vertex attributes using the separate format API
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);

            // Define formats
            GL.VertexAttribFormat(0, 3, VertexAttribType.Float, false, 0);
            GL.VertexAttribFormat(1, 4, VertexAttribType.UnsignedByte, true, 3 * sizeof(float));
            GL.VertexAttribFormat(2, 2, VertexAttribType.Float, false, 3 * sizeof(float) + 4);

            // Bind attributes to binding point 0
            GL.VertexAttribBinding(0, 0);
            GL.VertexAttribBinding(1, 0);
            GL.VertexAttribBinding(2, 0);

            // Bind buffer to binding point with stride
            GL.BindVertexBuffer(0, vbo, 0, (uint)sizeof(VertexColorTexture));
        }

        // Initialize batch data
        batchItems = new SpriteBatchItem[initialBatchCapacity];
        for (int i = 0; i < batchItems.Length; i++)
            batchItems[i] = new SpriteBatchItem();

        batchItemCount = 0;

        // Initialize vertex and index arrays
        vertices = new VertexColorTexture[InitialBufferCapacity];
        indices = new ushort[InitialBufferCapacity * 6 / 4]; // Each quad uses 6 indices for 4 vertices

        // Set up indices for a quad (two triangles)
        CreateIndices(indices, InitialBufferCapacity / 4);

        // Create vertices
        unsafe {
            GL.BufferStorage(BufferStorageTarget.ArrayBuffer,
                (nuint)(vertices.Length * sizeof(VertexColorTexture)),
                null, BufferStorageMask.DynamicStorageBit);
        }

        // Upload the index data
        unsafe {
            fixed (ushort* ptr = indices) {
                GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, ibo);
                GL.BufferStorage(BufferStorageTarget.ElementArrayBuffer,
                    (nuint)(indices.Length * sizeof(ushort)),
                    ptr, BufferStorageMask.DynamicStorageBit);
            }
        }

        IsActive = false;
        IsDisposed = false;
    }

    public void setShader(Shader newShader) {
        ObjectDisposedException.ThrowIf(IsDisposed, nameof(SpriteBatch));
        ArgumentNullException.ThrowIfNull(newShader);

        // Set the new shader
        shader = newShader;

        // Rebind the VAO and set the texture uniform
        GL.BindVertexArray(vao);

        // Get uniform locations
        textureUniform = shader.getUniformLocation("tex");
        shader.use();
        // Set texture to 0
        shader.setUniform(textureUniform, 0);
    }

    private static void CreateIndices(ushort[] indices, uint quadCount) {
        for (uint i = 0, vertex = 0; i < quadCount; i++) {
            uint idx = i * 6;
            indices[idx] = (ushort)vertex;
            indices[idx + 1] = (ushort)(vertex + 1);
            indices[idx + 2] = (ushort)(vertex + 2);
            indices[idx + 3] = (ushort)vertex;
            indices[idx + 4] = (ushort)(vertex + 2);
            indices[idx + 5] = (ushort)(vertex + 3);
            vertex += 4;
        }
    }

    public void Begin(BatcherBeginMode beginMode = BatcherBeginMode.Deferred) {
        ObjectDisposedException.ThrowIf(IsDisposed, nameof(SpriteBatch));

        if (IsActive)
            throw new InvalidOperationException("This TextureBatcher has already begun.");

        batchItemCount = 0;
        BeginMode = beginMode;
        IsActive = true;

        // Set up the shader
        shader.use();
    }

    public void End() {
        if (!IsActive)
            throw new InvalidOperationException("Begin() must be called before End().");

        // Flush any remaining items
        Flush(BeginMode == BatcherBeginMode.Immediate || BeginMode == BatcherBeginMode.OnTheFly);

        IsActive = false;
    }

    private void ValidateBeginCalled() {
        if (!IsActive) {
            throw new InvalidOperationException("Draw() must be called in between Begin() and End().");
        }
    }

    private bool EnsureBatchListCapacity(uint requiredCapacity) {
        uint currentCapacity = (uint)batchItems.Length;
        if (currentCapacity == MaxBatchItemCapacity)
            return requiredCapacity <= currentCapacity;

        if (currentCapacity < requiredCapacity) {
            // Resize the batchItems array
            uint newCapacity = Math.Min(NextPowerOfTwo(requiredCapacity), (int)MaxBatchItemCapacity);
            Array.Resize(ref batchItems, (int)newCapacity);

            // Fill new elements with TextureBatchItem instances
            for (uint i = currentCapacity; i < batchItems.Length; i++)
                batchItems[i] = new SpriteBatchItem();
        }

        return requiredCapacity <= batchItems.Length;
    }

    private static uint NextPowerOfTwo(uint value) {
        uint result = 1;
        while (result < value)
            result <<= 1;
        return result;
    }

    private SpriteBatchItem GetNextBatchItem() {
        // Check that we have enough capacity for one more batch item
        if (!EnsureBatchListCapacity(batchItemCount + 1)) {
            // If the array can't be expanded further, try to flush
            if (BeginMode == BatcherBeginMode.OnTheFly || BeginMode == BatcherBeginMode.Immediate)
                Flush(true);
            else
                throw new InvalidOperationException(
                    "Too many TextureBatcher items. Try drawing less per Begin()-End() cycle or use OnTheFly or Immediate begin modes.");
        }

        // Return the next batch item
        return batchItems[batchItemCount++];
    }

    // DrawRaw methods
    public void DrawRaw(BTexture2D texture, VertexColorTexture vertexTL, VertexColorTexture vertexTR,
        VertexColorTexture vertexBR, VertexColorTexture vertexBL) {
        ValidateBeginCalled();

        ArgumentNullException.ThrowIfNull(texture);

        // Check if we need to flush before adding this item
        if (BeginMode == BatcherBeginMode.OnTheFly && batchItemCount > 0 && batchItems[0].Texture != texture)
            Flush(true);

        SpriteBatchItem item = GetNextBatchItem();
        item.SetVertices(texture, vertexTL, vertexTR, vertexBR, vertexBL);

        // Set sort key if needed
        SetItemSortKey(item);

        // Flush immediately if in Immediate mode
        if (BeginMode == BatcherBeginMode.Immediate)
            Flush(true);
    }

    public void DrawRaw(BTexture2D texture, VertexColorTexture vertexTL, VertexColorTexture vertexTR,
        VertexColorTexture vertexBR, VertexColorTexture vertexBL, Matrix4x4 matrix) {
        ValidateBeginCalled();

        ArgumentNullException.ThrowIfNull(texture);

        // Check if we need to flush before adding this item
        if (BeginMode == BatcherBeginMode.OnTheFly && batchItemCount > 0 && batchItems[0].Texture != texture)
            Flush(true);

        SpriteBatchItem item = GetNextBatchItem();

        // Transform vertices
        vertexTL.Position = Vector3.Transform(vertexTL.Position, matrix);
        vertexTR.Position = Vector3.Transform(vertexTR.Position, matrix);
        vertexBR.Position = Vector3.Transform(vertexBR.Position, matrix);
        vertexBL.Position = Vector3.Transform(vertexBL.Position, matrix);

        item.SetVertices(texture, vertexTL, vertexTR, vertexBR, vertexBL);

        // Set sort key if needed
        SetItemSortKey(item);

        // Flush immediately if in Immediate mode
        if (BeginMode == BatcherBeginMode.Immediate)
            Flush(true);
    }

    // Draw methods for different parameter combinations
    public void Draw(BTexture2D texture, Vector2 position, Rectangle? source, Color color, float depth = 0) {
        ValidateBeginCalled();

        ArgumentNullException.ThrowIfNull(texture);

        // Check if we need to flush before adding this item
        if (BeginMode == BatcherBeginMode.OnTheFly && batchItemCount > 0 && batchItems[0].Texture != texture)
            Flush(true);

        SpriteBatchItem item = GetNextBatchItem();
        item.SetValue(texture, position, source ?? new System.Drawing.Rectangle(0, 0, (int)texture.width, (int)texture.height), color,
            depth);

        // Set sort key if needed
        SetItemSortKey(item);

        // Flush immediately if in Immediate mode
        if (BeginMode == BatcherBeginMode.Immediate)
            Flush(true);
    }

    public void Draw(BTexture2D texture, Vector2 position, Color color, float depth = 0) {
        Draw(texture, position, new Rectangle(0, 0, (int)texture.width, (int)texture.height), color, depth);
    }

    public void Draw(BTexture2D texture, Vector2 position, Rectangle? source = null, float depth = 0) {
        Draw(texture, position, source, Color.White, depth);
    }

    public void Draw(BTexture2D texture, Vector2 position, Rectangle? source, Color color, Vector2 scale,
        float rotation, Vector2 origin, float depth = 0) {
        ValidateBeginCalled();

        ArgumentNullException.ThrowIfNull(texture);

        // Check if we need to flush before adding this item
        if (BeginMode == BatcherBeginMode.OnTheFly && batchItemCount > 0 && batchItems[0].Texture != texture)
            Flush(true);

        SpriteBatchItem item = GetNextBatchItem();
        item.SetValue(texture, position,
            source ?? new Rectangle(0, 0, (int)texture.width, (int)texture.height),
            color, scale, rotation, origin, depth);

        // Set sort key if needed
        SetItemSortKey(item);

        // Flush immediately if in Immediate mode
        if (BeginMode == BatcherBeginMode.Immediate)
            Flush(true);
    }

    public void Draw(BTexture2D texture, Vector2 position, Rectangle? source, Color color, float scale,
        float rotation, Vector2 origin = default, float depth = 0) {
        Draw(texture, position, source, color, new Vector2(scale, scale), rotation, origin, depth);
    }

    public void Draw(BTexture2D texture, Vector2 position, ref Matrix4x4 worldMatrix, Rectangle? source, Color color,
        Vector2 scale, float rotation, Vector2 origin = default, float depth = 0) {
        ValidateBeginCalled();

        ArgumentNullException.ThrowIfNull(texture);

        // Check if we need to flush before adding this item
        if (BeginMode == BatcherBeginMode.OnTheFly && batchItemCount > 0 && batchItems[0].Texture != texture)
            Flush(true);

        SpriteBatchItem item = GetNextBatchItem();
        item.SetValue(texture, position, ref worldMatrix,
            source ?? new Rectangle(0, 0, (int)texture.width, (int)texture.height),
            color, scale, rotation, origin, depth);

        // Set sort key if needed
        SetItemSortKey(item);

        // Flush immediately if in Immediate mode
        if (BeginMode == BatcherBeginMode.Immediate)
            Flush(true);
    }

    public void Draw(BTexture2D texture, RectangleF destination, Rectangle? source, Color color, float depth = 0) {
        ValidateBeginCalled();

        ArgumentNullException.ThrowIfNull(texture);

        // Check if we need to flush before adding this item
        if (BeginMode == BatcherBeginMode.OnTheFly && batchItemCount > 0 && batchItems[0].Texture != texture)
            Flush(true);

        SpriteBatchItem item = GetNextBatchItem();
        item.SetValue(texture, destination,
            source ?? new Rectangle(0, 0, (int)texture.width, (int)texture.height),
            color, depth);

        // Set sort key if needed
        SetItemSortKey(item);

        // Flush immediately if in Immediate mode
        if (BeginMode == BatcherBeginMode.Immediate)
            Flush(true);
    }

    public void Draw(BTexture2D texture, RectangleF destination, Color color, float depth = 0) {
        Draw(texture, destination, null, color, depth);
    }

    public void Draw(BTexture2D texture, RectangleF destination, float depth = 0) {
        Draw(texture, destination, null, Color.White, depth);
    }

    public void Draw(BTexture2D texture, Matrix3x2 transform, Rectangle? source, Color color, float depth = 0) {
        ValidateBeginCalled();

        ArgumentNullException.ThrowIfNull(texture);

        // Check if we need to flush before adding this item
        if (BeginMode == BatcherBeginMode.OnTheFly && batchItemCount > 0 && batchItems[0].Texture != texture)
            Flush(true);

        SpriteBatchItem item = GetNextBatchItem();
        item.SetValue(texture, transform,
            source ?? new Rectangle(0, 0, (int)texture.width, (int)texture.height),
            color, depth);

        // Set sort key if needed
        SetItemSortKey(item);

        // Flush immediately if in Immediate mode
        if (BeginMode == BatcherBeginMode.Immediate)
            Flush(true);
    }

    public void Draw(BTexture2D texture, Matrix3x2 transform, Rectangle? source, Color color, Vector2 origin,
        float depth = 0) {
        ValidateBeginCalled();

        ArgumentNullException.ThrowIfNull(texture);

        // Check if we need to flush before adding this item
        if (BeginMode == BatcherBeginMode.OnTheFly && batchItemCount > 0 && batchItems[0].Texture != texture)
            Flush(true);

        SpriteBatchItem item = GetNextBatchItem();
        item.SetValue(texture, transform,
            source ?? new Rectangle(0, 0, (int)texture.width, (int)texture.height),
            color, origin, depth);

        // Set sort key if needed
        SetItemSortKey(item);

        // Flush immediately if in Immediate mode
        if (BeginMode == BatcherBeginMode.Immediate)
            Flush(true);
    }

    private void SetItemSortKey(SpriteBatchItem item) {
        // Set sort key based on the current begin mode
        item.SortValue = BeginMode switch {
            BatcherBeginMode.SortByTexture => item.Texture!.handle,
            BatcherBeginMode.SortFrontToBack => item.VertexTL.Position.Z,
            BatcherBeginMode.SortBackToFront => -item.VertexTL.Position.Z,
            _ => 0
        };
    }

    private bool FlushIfNeeded() {
        // Check if we need to flush
        if (BeginMode == BatcherBeginMode.Immediate) {
            Flush(true);
            return true;
        }

        return false;
    }

    private void Flush(bool sameTextureEnsured) {
        if (batchItemCount == 0)
            return;

        // Sort items if needed based on the BeginMode
        if ((BeginMode & (BatcherBeginMode)1) == (BatcherBeginMode)1)
            Array.Sort(batchItems, 0, (int)batchItemCount);

        if (Game.hasNVDT && !NoScreenSpace) {
            nvFlush(sameTextureEnsured);
            return;
        }

        // Bind the VAO and shader
        GL.BindVertexArray(vao);
        shader.use();

        // First, calculate total vertices required to avoid buffer resize issues
        uint totalVertices = batchItemCount;
        EnsureBufferCapacity(totalVertices);

        // Process items in batches by texture
        uint itemStartIndex = 0;
        while (itemStartIndex < batchItemCount) {
            // Get the texture for this batch
            BTexture2D currentTexture = batchItems[itemStartIndex].Texture!;

            // Find the end of this batch (items with the same texture)
            uint itemEndIndex = sameTextureEnsured
                ? batchItemCount
                : FindDifferentTexture(currentTexture, itemStartIndex + 1);
            uint itemCount = itemEndIndex - itemStartIndex;

            // Fill the vertex buffer for this texture batch
            uint vertexIndex = 0;
            for (uint i = itemStartIndex; i < itemEndIndex; i++) {
                SpriteBatchItem item = batchItems[i];
                vertices[vertexIndex++] = item.VertexTL;
                vertices[vertexIndex++] = item.VertexBL;
                vertices[vertexIndex++] = item.VertexBR;
                vertices[vertexIndex++] = item.VertexTR;
            }

            // Upload vertex data
            unsafe {
                fixed (VertexColorTexture* ptr = vertices) {
                    GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
                    GL.InvalidateBufferData(vbo);
                    GL.BufferSubData(BufferTargetARB.ArrayBuffer,
                        0,
                        (uint)(vertexIndex * sizeof(VertexColorTexture)),
                        ptr);
                }
            }

            // Bind the texture
            currentTexture.bind();

            // Bind indices
            GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, ibo);

            /*Console.Out.WriteLine(
                $"itemStarIndex: {itemStartIndex}, itemEndIndex: {itemEndIndex}, itemCount: {itemCount}, vertexIndex: {vertexIndex}," +
                $" indices.Length: {indices.Length}, vertices.Length: {vertices.Length}");*/

            // Draw the batch
            unsafe {
                GL.DrawElements(PrimitiveType.Triangles, itemCount * 6,
                    DrawElementsType.UnsignedShort, (void*)0);
            }

            // Move to the next batch
            itemStartIndex = itemEndIndex;
        }

        // Reset batch item count
        batchItemCount = 0;
    }

    private void nvFlush(bool sameTextureEnsured) {
        // only SortByTexture can reorder freely - all other modes need submission order preserved - is this right?
        bool canReorder = BeginMode == BatcherBeginMode.SortByTexture || BeginMode == BatcherBeginMode.Immediate || BeginMode == BatcherBeginMode.OnTheFly || sameTextureEnsured || BeginMode == BatcherBeginMode.Deferred;

        // todo this might break shit so if the texture order is fucked, adjust canReorder
        if (canReorder) {
            nvFlushSeparated();
        } else {
            nvFlushOrdered();
        }

        batchItemCount = 0;
    }

    private void nvFlushSeparated() {
        // draw all non-tinted with NV path
        for (uint i = 0; i < batchItemCount; i++) {
            SpriteBatchItem item = batchItems[i];
            if (item.VertexTL.Color != Color.White) continue;

            BTexture2D tex = item.Texture!;
            var sh = Game.height;
            var x0 = item.VertexTL.Position.X;
            var y0 = sh - item.VertexTL.Position.Y;
            var x1 = item.VertexBR.Position.X;
            var y1 = sh - item.VertexBR.Position.Y;
            var z = item.VertexTL.Position.Z;

            Game.nvdt.DrawTexture(tex.handle, Game.graphics.noMipmapSampler,
                x0, y0, x1, y1, z,
                item.VertexTL.TexCoords.X, item.VertexTL.TexCoords.Y,
                item.VertexBR.TexCoords.X, item.VertexBR.TexCoords.Y);
        }

        // count tinted items
        uint tintedCount = 0;
        for (uint i = 0; i < batchItemCount; i++) {
            if (batchItems[i].VertexTL.Color != Color.White) tintedCount++;
        }

        // draw all tinted with shader path, batched by texture
        if (tintedCount > 0) {
            GL.BindVertexArray(vao);
            shader.use();
            EnsureBufferCapacity(tintedCount);

            // collect all tinted vertices
            uint vertexIndex = 0;
            for (uint i = 0; i < batchItemCount; i++) {
                SpriteBatchItem item = batchItems[i];
                if (item.VertexTL.Color == Color.White) continue;

                vertices[vertexIndex++] = item.VertexTL;
                vertices[vertexIndex++] = item.VertexBL;
                vertices[vertexIndex++] = item.VertexBR;
                vertices[vertexIndex++] = item.VertexTR;
            }

            // upload vertices
            unsafe {
                fixed (VertexColorTexture* ptr = vertices) {
                    GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
                    GL.InvalidateBufferData(vbo);
                    GL.BufferSubData(BufferTargetARB.ArrayBuffer, 0,
                        (uint)(vertexIndex * sizeof(VertexColorTexture)), ptr);
                }
            }

            GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, ibo);

            // draw batched by texture
            uint itemIdx = 0;
            while (itemIdx < batchItemCount) {
                // skip non-tinted
                while (itemIdx < batchItemCount && batchItems[itemIdx].VertexTL.Color == Color.White) {
                    itemIdx++;
                }
                if (itemIdx >= batchItemCount) break;

                // found a tinted item - batch all consecutive tinted items with same texture
                BTexture2D tex = batchItems[itemIdx].Texture!;
                uint batchStart = itemIdx;

                while (itemIdx < batchItemCount && batchItems[itemIdx].VertexTL.Color != Color.White
                       && batchItems[itemIdx].Texture == tex) {
                    itemIdx++;
                }

                uint batchCount = itemIdx - batchStart;
                tex.bind();

                unsafe {
                    GL.DrawElements(PrimitiveType.Triangles, batchCount * 6,
                        DrawElementsType.UnsignedShort, (void*)((batchStart - countWhiteItemsBefore(batchStart)) * 6 * sizeof(ushort)));
                }
            }
        }
    }

    private uint countWhiteItemsBefore(uint index) {
        uint count = 0;
        for (uint i = 0; i < index; i++) {
            if (batchItems[i].VertexTL.Color == Color.White) count++;
        }
        return count;
    }

    private void nvFlushOrdered() {
        uint itemIdx = 0;

        while (itemIdx < batchItemCount) {
            // find run of non-tinted
            uint runStart = itemIdx;
            while (itemIdx < batchItemCount && batchItems[itemIdx].VertexTL.Color == Color.White) {
                itemIdx++;
            }

            // draw non-tinted run with NV path
            if (itemIdx > runStart) {
                for (uint i = runStart; i < itemIdx; i++) {
                    SpriteBatchItem item = batchItems[i];
                    BTexture2D tex = item.Texture!;
                    var sh = Game.height;

                    Game.nvdt.DrawTexture(tex.handle, Game.graphics.noMipmapSampler,
                        item.VertexTL.Position.X, sh - item.VertexTL.Position.Y,
                        item.VertexBR.Position.X, sh - item.VertexBR.Position.Y,
                        item.VertexTL.Position.Z,
                        item.VertexTL.TexCoords.X, item.VertexTL.TexCoords.Y,
                        item.VertexBR.TexCoords.X, item.VertexBR.TexCoords.Y);
                }
            }

            // find run of tinted
            runStart = itemIdx;
            while (itemIdx < batchItemCount && batchItems[itemIdx].VertexTL.Color != Color.White) {
                itemIdx++;
            }

            // draw tinted run with shader path, batched by texture
            if (itemIdx > runStart) {
                GL.BindVertexArray(vao);
                shader.use();
                EnsureBufferCapacity(itemIdx - runStart);

                // collect vertices
                uint vertexIndex = 0;
                for (uint i = runStart; i < itemIdx; i++) {
                    SpriteBatchItem item = batchItems[i];
                    vertices[vertexIndex++] = item.VertexTL;
                    vertices[vertexIndex++] = item.VertexBL;
                    vertices[vertexIndex++] = item.VertexBR;
                    vertices[vertexIndex++] = item.VertexTR;
                }

                // upload vertices
                unsafe {
                    fixed (VertexColorTexture* ptr = vertices) {
                        GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
                        GL.InvalidateBufferData(vbo);
                        GL.BufferSubData(BufferTargetARB.ArrayBuffer, 0,
                            (uint)(vertexIndex * sizeof(VertexColorTexture)), ptr);
                    }
                }

                GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, ibo);

                // batch by texture
                uint texBatchStart = runStart;
                uint vertOffset = 0;
                while (texBatchStart < itemIdx) {
                    BTexture2D tex = batchItems[texBatchStart].Texture!;
                    uint texBatchEnd = texBatchStart + 1;
                    while (texBatchEnd < itemIdx && batchItems[texBatchEnd].Texture == tex) {
                        texBatchEnd++;
                    }

                    tex.bind();
                    unsafe {
                        GL.DrawElements(PrimitiveType.Triangles, (texBatchEnd - texBatchStart) * 6,
                            DrawElementsType.UnsignedShort, (void*)(vertOffset * sizeof(ushort)));
                    }

                    vertOffset += (texBatchEnd - texBatchStart) * 6;
                    texBatchStart = texBatchEnd;
                }
            }
        }
    }

    private uint FindDifferentTexture(BTexture2D currentTexture, uint startIndex) {
        while (startIndex < batchItemCount && batchItems[startIndex].Texture == currentTexture)
            startIndex++;
        return startIndex;
    }

    private void EnsureBufferCapacity(uint batchCount) {
        var requiredVertexCount = batchCount * 4;
        var requiredIndexCount = batchCount * 6;
        if (vertices.Length < requiredVertexCount) {
            uint newCapacity = Math.Min(NextPowerOfTwo(requiredVertexCount), (int)MaxBufferCapacity);
            Array.Resize(ref vertices, (int)newCapacity);

            // Resize vertex buffer
            unsafe {
                fixed (VertexColorTexture* ptr = vertices) {
                    GL.DeleteBuffer(vbo);
                    vbo = GL.CreateBuffer();
                    GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
                    GL.BufferStorage(BufferStorageTarget.ArrayBuffer,
                        (nuint)(newCapacity * sizeof(VertexColorTexture)),
                        ptr, BufferStorageMask.DynamicStorageBit);
                    GL.ObjectLabel(ObjectIdentifier.Buffer, vbo, uint.MaxValue, "SpriteBatch Vertex Buffer");

                    // Update binding with new buffer
                    GL.BindVertexBuffer(0, vbo, 0, (uint)sizeof(VertexColorTexture));
                }
            }

            // Resize indices if needed (each quad uses 6 indices for 4 vertices)
            if (indices.Length < requiredIndexCount) {
                var newIndexCapacity = Math.Min(NextPowerOfTwo(requiredIndexCount), MaxBufferCapacity * 6 / 4);
                Array.Resize(ref indices, (int)newIndexCapacity);

                // Set up new indices
                var newQuadCount = newIndexCapacity / 6;

                CreateIndices(indices, newQuadCount);

                // Upload the index data
                unsafe {
                    fixed (ushort* ptr = indices) {
                        GL.DeleteBuffer(ibo);
                        ibo = GL.CreateBuffer();
                        GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, ibo);
                        GL.BufferStorage(BufferStorageTarget.ElementArrayBuffer,
                            newIndexCapacity * sizeof(ushort),
                            ptr, BufferStorageMask.DynamicStorageBit);
                        GL.ObjectLabel(ObjectIdentifier.Buffer, ibo, uint.MaxValue, "SpriteBatch Index Buffer");
                    }
                }
            }
        }
    }

    private void nvEnsureBufferCapacity(uint batchCount) {
        var requiredVertexCount = batchCount * 4;
        if (vertices.Length < requiredVertexCount) {
            uint newCapacity = Math.Min(NextPowerOfTwo(requiredVertexCount), (int)MaxBufferCapacity);
            Array.Resize(ref vertices, (int)newCapacity);
        }
    }

    public void Dispose() {
        if (IsDisposed)
            return;

        GL.DeleteBuffer(vbo);
        GL.DeleteBuffer(ibo);
        GL.DeleteVertexArray(vao);

        IsDisposed = true;
    }
}