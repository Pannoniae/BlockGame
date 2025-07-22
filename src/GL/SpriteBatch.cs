using System;
using System.Drawing;
using System.Numerics;
using Silk.NET.OpenGL;
using System.Runtime.InteropServices;
using BlockGame.GL.vertexformats;
using PrimitiveType = Silk.NET.OpenGL.PrimitiveType;

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
public struct VertexColorTexture {
    public Vector3 Position;
    public Color4b Color;
    public Vector2 TexCoords;

    public VertexColorTexture(Vector3 position, Color4b color, Vector2 texCoords) {
        Position = position;
        Color = color;
        TexCoords = texCoords;
    }
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
    private readonly Silk.NET.OpenGL.GL GL;
    public readonly uint vao;
    private uint vbo;
    private uint ibo;

    // Shader
    internal readonly Shader shader;
    private readonly int textureUniform;

    // Batch data
    private SpriteBatchItem[] batchItems;
    private uint batchItemCount;
    private VertexColorTexture[] vertices;
    private ushort[] indices;

    // State
    public bool IsActive { get; private set; }
    public BatcherBeginMode BeginMode { get; private set; }
    public bool IsDisposed { get; private set; }

    public SpriteBatch(Silk.NET.OpenGL.GL gl, uint initialBatchCapacity = InitialBatchItemsCapacity)
        : this(gl, new Shader(gl, nameof(SpriteBatch), "shaders/batch.vert", "shaders/batch.frag"), initialBatchCapacity) {
    }

    public SpriteBatch(Silk.NET.OpenGL.GL gl, Shader shader, uint initialBatchCapacity = InitialBatchItemsCapacity) {
        this.shader = shader ?? throw new ArgumentNullException(nameof(shader));

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

        // Get uniform locations
        textureUniform = shader.getUniformLocation("tex");

        // Set texture to 0
        shader.setUniform(textureUniform, 0);

        IsActive = false;
        IsDisposed = false;
    }

    private void CreateIndices(ushort[] indices, uint quadCount) {
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
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(SpriteBatch));

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
        if (!IsActive)
            throw new InvalidOperationException("Draw() must be called in between Begin() and End().");
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

    private uint NextPowerOfTwo(uint value) {
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

        if (texture == null)
            throw new ArgumentNullException(nameof(texture));

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

        if (texture == null)
            throw new ArgumentNullException(nameof(texture));

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
    public void Draw(BTexture2D texture, Vector2 position, Rectangle? source, Color4b color, float depth = 0) {
        ValidateBeginCalled();

        if (texture == null)
            throw new ArgumentNullException(nameof(texture));

        // Check if we need to flush before adding this item
        if (BeginMode == BatcherBeginMode.OnTheFly && batchItemCount > 0 && batchItems[0].Texture != texture)
            Flush(true);

        SpriteBatchItem item = GetNextBatchItem();
        item.SetValue(texture, position, source ?? new Rectangle(0, 0, (int)texture.width, (int)texture.height), color,
            depth);

        // Set sort key if needed
        SetItemSortKey(item);

        // Flush immediately if in Immediate mode
        if (BeginMode == BatcherBeginMode.Immediate)
            Flush(true);
    }

    public void Draw(BTexture2D texture, Vector2 position, Color4b color, float depth = 0) {
        Draw(texture, position, new Rectangle(0, 0, (int)texture.width, (int)texture.height), color, depth);
    }

    public void Draw(BTexture2D texture, Vector2 position, Rectangle? source = null, float depth = 0) {
        Draw(texture, position, source, Color4b.White, depth);
    }

    public void Draw(BTexture2D texture, Vector2 position, Rectangle? source, Color4b color, Vector2 scale,
        float rotation, Vector2 origin, float depth = 0) {
        ValidateBeginCalled();

        if (texture == null)
            throw new ArgumentNullException(nameof(texture));

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

    public void Draw(BTexture2D texture, Vector2 position, Rectangle? source, Color4b color, float scale,
        float rotation, Vector2 origin = default, float depth = 0) {
        Draw(texture, position, source, color, new Vector2(scale, scale), rotation, origin, depth);
    }

    public void Draw(BTexture2D texture, Vector2 position, ref Matrix4x4 worldMatrix, Rectangle? source, Color4b color,
        Vector2 scale, float rotation, Vector2 origin = default, float depth = 0) {
        ValidateBeginCalled();

        if (texture == null)
            throw new ArgumentNullException(nameof(texture));

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

    public void Draw(BTexture2D texture, RectangleF destination, Rectangle? source, Color4b color, float depth = 0) {
        ValidateBeginCalled();

        if (texture == null)
            throw new ArgumentNullException(nameof(texture));

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

    public void Draw(BTexture2D texture, RectangleF destination, Color4b color, float depth = 0) {
        Draw(texture, destination, null, color, depth);
    }

    public void Draw(BTexture2D texture, RectangleF destination, float depth = 0) {
        Draw(texture, destination, null, Color4b.White, depth);
    }

    public void Draw(BTexture2D texture, Matrix3x2 transform, Rectangle? source, Color4b color, float depth = 0) {
        ValidateBeginCalled();

        if (texture == null)
            throw new ArgumentNullException(nameof(texture));

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

    public void Draw(BTexture2D texture, Matrix3x2 transform, Rectangle? source, Color4b color, Vector2 origin,
        float depth = 0) {
        ValidateBeginCalled();

        if (texture == null)
            throw new ArgumentNullException(nameof(texture));

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

    public void Dispose() {
        if (IsDisposed)
            return;

        GL.DeleteBuffer(vbo);
        GL.DeleteBuffer(ibo);
        GL.DeleteVertexArray(vao);

        IsDisposed = true;
    }
}