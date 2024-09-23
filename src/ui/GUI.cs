using System.Numerics;
using System.Runtime.InteropServices;
using BlockGame.util;
using BlockGame.util.font;
using FontStashSharp;
using FontStashSharp.RichText;
using Molten;
using Silk.NET.OpenGL;
using TrippyGL;
using TrippyGL.Fonts;
using TrippyGL.ImageSharp;
using PrimitiveType = TrippyGL.PrimitiveType;
using Rectangle = System.Drawing.Rectangle;
using RectangleF = System.Drawing.RectangleF;
using Viewport = TrippyGL.Viewport;

namespace BlockGame.ui;

/// <summary>
/// GUI class which can draw onto the menu.
/// Supports scaling with guiScale.
/// Drawing methods ending with "UI" draw on the virtual GUI coordinate system, so they are positioned in the right place when the GUI scale is changed.
/// </summary>
public class GUI {

    public GL GL;
    public GraphicsDevice GD;

    public SimpleShaderProgram shader;

    public static int guiScale = 4;

    public TextureBatcher tb;
    public TextureBatcher immediatetb;
    public Texture2D guiTexture;
    public Texture2D colourTexture;

    public int uiWidth;
    public int uiHeight;

    public int uiCentreX;
    public int uiCentreY;

    /// <summary>
    /// Like the normal shader but it's simple (doesn't need fog/lighting/anything)
    /// </summary>
    public ShaderProgram guiBlockShader;

    public DynamicSpriteFont guiFont;
    public DynamicSpriteFont guiFontThin;

    public Rectangle buttonRect = new(96, 0, 96, 16);
    public Rectangle grayButtonRect = new(0, 16 * 2, 96, 16);

    public static GUI instance;
    private TrippyFontFile ff;
    private VertexBuffer<BlockVertexTinted> buffer;
    private Matrix4x4 ortho;

    private List<BlockVertexTinted> guiBlock;
    private List<ushort> guiBlockI;

    public GUI() {
        GL = Game.GL;
        GD = Game.GD;
        shader = SimpleShaderProgram.Create<VertexColorTexture>(Game.GD);
        Game.graphics.mainBatch.SetShaderProgram(shader);
        Game.graphics.immediateBatch.SetShaderProgram(shader);
        tb = Game.graphics.mainBatch;
        immediatetb = Game.graphics.immediateBatch;
        guiTexture = Texture2DExtensions.FromFile(Game.GD, "textures/gui.png");
        colourTexture = Texture2DExtensions.FromFile(Game.GD, "textures/debug.png");
        instance = this;
        guiBlockShader = ShaderProgram.FromFiles<BlockVertexTinted>(
            GD, "shaders/simpleBlock.vert", "shaders/simpleBlock.frag", "vPos", "texCoord", "tintValue");
        buffer = new VertexBuffer<BlockVertexTinted>(GD, 4 * Face.MAX_FACES, 6 * Face.MAX_FACES, ElementType.UnsignedShort, BufferUsage.StreamDraw);
        guiBlock = new List<BlockVertexTinted>();
        guiBlockI = new List<ushort>();


    }

    public void loadFont(int size) {
        guiFont = Game.fontLoader.fontSystem.GetFont(size);
        guiFontThin = Game.fontLoader.fontSystemThin.GetFont(size);
    }



    public void resize(Vector2I size) {
        ortho = Matrix4x4.CreateOrthographicOffCenter(0, size.X, size.Y, 0, -1f, 1f);
        shader.Projection = ortho;
        uiCentreX = size.X / 2 / guiScale;
        uiCentreY = size.Y / 2 / guiScale;

        uiWidth = size.X / guiScale;
        uiHeight = size.Y / guiScale;
        //worldShader.Projection = Game.instance.world.player.camera.getProjectionMatrix();
        //worldShader.View = Game.instance.world.player.camera.getViewMatrix(1);
    }


    // Conversion functions

    /// <summary>
    /// Convert a screen position to a UI position.
    /// </summary>
    public static int s2u(float pos) {
        return (int)(pos / guiScale);
    }

    /// <summary>
    /// Convert a screen position to a UI position.
    /// </summary>
    public static Vector2 s2u(Vector2 pos) {
        return new Vector2(pos.X / guiScale, pos.Y / guiScale);
    }

    /// <summary>
    /// Convert a UI position to a screen position.
    /// </summary>
    public static int u2s(int pos) {
        return pos * guiScale;
    }

    /// <summary>
    /// Convert a UI position to a screen position.
    /// </summary>
    public static Vector2 u2s(Vector2 pos) {
        return new Vector2(pos.X * guiScale, pos.Y * guiScale);
    }

    /// <summary>
    /// Draw a full-screen background with a block texture and the specified block size in pixels.
    /// </summary>
    public void drawBG(Block block, float size) {
        var texCoords = Block.texCoords(block.model.faces[0].min);
        var texCoordsMax = Block.texCoords(block.model.faces[0].max);

        // handle guiscale
        size *= guiScale * 2;

        // if one block is a given size, how many blocks can we fit on the screen?
        var xCount = (int)Math.Ceiling(Game.width / size);
        var yCount = (int)Math.Ceiling(Game.height / size);

        for (int x = 0; x < xCount; x++) {
            for (int y = 0; y < yCount; y++) {
                var left = x * size;
                var right = x * size + size;
                var top = y * size;
                var bottom = y * size + size;
                tb.DrawRaw(Game.textureManager.blockTextureGUI,
                    new VertexColorTexture(new Vector3(left, top, 0), Color4b.Gray, new Vector2(texCoords.X, texCoords.Y)),
                    new VertexColorTexture(new Vector3(right, top, 0), Color4b.Gray, new Vector2(texCoordsMax.X, texCoords.Y)),
                    new VertexColorTexture(new Vector3(right, bottom, 0), Color4b.Gray, new Vector2(texCoordsMax.X, texCoordsMax.Y)),
                    new VertexColorTexture(new Vector3(left, bottom, 0), Color4b.Gray, new Vector2(texCoords.X, texCoordsMax.Y)));
            }
        }
    }


    public void draw(Texture2D texture, Vector2 position, Rectangle? source = null,
        Color4b color = default, Vector2 origin = default, float depth = 0f) {
        tb.Draw(texture, position, source, color == default ? Color4b.White : color, guiScale, 0f, origin, depth);
    }

    public void draw(Texture2D texture, RectangleF dest, Rectangle? source = null, Color4b color = default) {
        tb.Draw(texture, dest, source, color == default ? Color4b.White : color);
    }

    public void drawImmediate(Texture2D texture, Vector2 position, Rectangle? source = null,
        Color4b color = default, Vector2 origin = default, float depth = 0f) {
        immediatetb.Draw(texture, position, source, color == default ? Color4b.White : color, guiScale, 0f, origin, depth);
    }

    public void drawUI(Texture2D texture, Vector2 position, Rectangle? source = null,
        Color4b color = default, Vector2 origin = default, float depth = 0f) {
        tb.Draw(texture, position * guiScale, source, color == default ? Color4b.White : color, guiScale, 0f, origin, depth);
    }

    public void drawUI(Texture2D texture, RectangleF dest, Rectangle? source = null, Color4b color = default, float depth = 0f) {
        tb.Draw(texture, new RectangleF(
            (int)(dest.X * guiScale),
            (int)(dest.Y * guiScale),
            (int)(dest.Width * guiScale),
            (int)(dest.Height * guiScale)), source, color == default ? Color4b.White : color, depth);
    }

    public void drawUIImmediate(Texture2D texture, Vector2 position, Rectangle? source = null,
        Color4b color = default, Vector2 origin = default, float depth = 0f) {
        immediatetb.Draw(texture, position * guiScale, source, color == default ? Color4b.White : color, guiScale, 0f, origin, depth);
    }

    internal static int TEXTSCALE => guiScale / 2;
    internal static Vector2 TEXTSCALEV => new(TEXTSCALE, TEXTSCALE);

    // maybe some day we will have common logic for these functions if the number of permutations grow in size. BUT NOT TODAY

    public void drawString(ReadOnlySpan<char> text, Vector2 position, Color4b color = default) {
        DrawString(text, position, color == default ? Color4b.White : color, new Vector2(TEXTSCALE), default);
    }

    public void drawStringSmall(ReadOnlySpan<char> text, Vector2 position, Color4b color = default) {
        DrawStringThin(text, position, color == default ? Color4b.White : color, new Vector2(TEXTSCALE / 2f), default);
    }

    public void drawStringThin(ReadOnlySpan<char> text, Vector2 position, Color4b color = default) {
        DrawStringThin(text, position, color == default ? Color4b.White : color, new Vector2(TEXTSCALE), default);
    }

    public void drawRString(RichTextLayout layout, Vector2 position, TextHorizontalAlignment alignment, Color4b color = default) {
        DrawRString(layout, position, color == default ? Color4b.White : color, new Vector2(TEXTSCALE), alignment);
    }

    public void drawStringUI(ReadOnlySpan<char> text, Vector2 position, Color4b color = default) {
        DrawString(text, position * guiScale, color == default ? Color4b.White : color, new Vector2(TEXTSCALE), default);
    }

    public void drawStringUI(ReadOnlySpan<char> text, Vector2 position, Color4b colour, Vector2 scale) {
        DrawString(text, position * guiScale, colour == default ? Color4b.White : colour, TEXTSCALE * scale, default);
    }

    public void drawStringUIThin(ReadOnlySpan<char> text, Vector2 position, Color4b colour = default) {
        DrawStringThin(text, position * guiScale, colour == default ? Color4b.White : colour, new Vector2(TEXTSCALE), default);
    }


    /// <summary>
    /// Rotation Y = yaw (horizontal rotation)
    /// Rotation X = pitch (banking down or up)
    /// Rotation Z = roll (tilt sideways)
    /// </summary>
    public void drawString3D(ReadOnlySpan<char> text, Vector3 position, Vector3 rotation, float scale = 1f, Color4b colour = default) {
        // flip the text - 2D rendering goes +y=down, we want +y=up
        // 1 pt should be 1/16th pixel
        var flip = Matrix4x4.Identity;
        flip.M22 = -1;
        var rot = Matrix4x4.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z);
        var mat = flip * rot * Matrix4x4.CreateScale(1 / 256f * scale) * Matrix4x4.CreateTranslation(position);
        guiFontThin.DrawText(Game.fontLoader.renderer3D, text, new Vector2(0, 0), colour == default ? FSColor.White : colour.toFS(), ref mat);
    }

    public void drawStringOnBlock(ReadOnlySpan<char> text, Vector3I pos, RawDirection face, float scale, Color4b colour = default) {
        // draw slightly out so the block won't z-fight with the text
        const float offset = 0.001f;
        Vector3 rotation = Vector3.Zero;
        var deg90ToRad = Utils.deg2rad(90);
        var offsetVec = Vector3.Zero;
        switch (face) {
            case RawDirection.WEST:
                pos.Z += 1;
                rotation = new Vector3(0, deg90ToRad, 0);
                offsetVec = new Vector3(-offset, 0, 0);
                break;
            case RawDirection.EAST:
                pos.X += 1;
                rotation = new Vector3(0, -deg90ToRad, 0);
                offsetVec = new Vector3(offset, 0, 0);
                break;
            case RawDirection.SOUTH:
                rotation = new Vector3(0, 0, 0);
                offsetVec = new Vector3(0, 0, -offset);
                break;
            case RawDirection.NORTH:
                pos.Z += 1;
                pos.X += 1;
                rotation = new Vector3(0, deg90ToRad * 2, 0);
                offsetVec = new Vector3(0, 0, offset);
                break;
            case RawDirection.DOWN:
                pos.Z += 1;
                pos.X += 1;
                pos.Y -= 1;
                rotation = new Vector3(-deg90ToRad, deg90ToRad * 2, 0);
                offsetVec = new Vector3(0, -offset, 0);
                break;
            case RawDirection.UP:
                pos.Z += 1;
                rotation = new Vector3(deg90ToRad, 0, 0);
                offsetVec = new Vector3(0, offset, 0);
                break;
        }
        drawString3D(text, pos.toVec3() + offsetVec, rotation, scale, colour);
    }

    public void drawStringCentred(ReadOnlySpan<char> text, Vector2 position, Color4b color = default) {
        var offsetX = guiFont.MeasureString(text, TEXTSCALEV).X / 2;
        var offsetY = guiFont.MeasureString(text, TEXTSCALEV).Y / 2;
        DrawString(text, new Vector2(position.X - offsetX, position.Y - offsetY), color == default ? Color4b.White : color, new Vector2(TEXTSCALE), default);
    }

    public void drawStringCentredUI(ReadOnlySpan<char> text, Vector2 position, Color4b color = default) {
        var offsetX = guiFont.MeasureString(text, TEXTSCALEV).X / 2;
        var offsetY = guiFont.MeasureString(text, TEXTSCALEV).Y / 2;
        DrawString(text, new Vector2(position.X * guiScale - offsetX, position.Y * guiScale - offsetY), color == default ? Color4b.White : color, new Vector2(TEXTSCALE), default);
    }

    // some day we'll have a better API, but not this day
    public void drawStringShadowed(ReadOnlySpan<char> text, Vector2 position, Color4b color = default) {
        DrawString(text, position + new Vector2(1, 1), Color4b.DimGray, new Vector2(TEXTSCALE), default);
        DrawString(text, position, color == default ? Color4b.White : color, new Vector2(TEXTSCALE), default);
    }

    public void drawStringShadowedUI(ReadOnlySpan<char> text, Vector2 position, Color4b color = default) {
        DrawString(text, position * guiScale + new Vector2(1, 1), Color4b.DimGray, new Vector2(TEXTSCALE), default);
        DrawString(text, position * guiScale, color == default ? Color4b.White : color, new Vector2(TEXTSCALE), default);
    }

    public void drawStringCentredShadowed(ReadOnlySpan<char> text, Vector2 position, Color4b color = default) {
        var offsetX = guiFont.MeasureString(text, TEXTSCALEV).X / 2;
        var offsetY = guiFont.MeasureString(text, TEXTSCALEV).Y / 2;
        DrawString(text, new Vector2(position.X - offsetX, position.Y - offsetY) + new Vector2(1, 1), Color4b.DimGray, new Vector2(TEXTSCALE), default);
        DrawString(text, new Vector2(position.X - offsetX, position.Y - offsetY), color == default ? Color4b.White : color, new Vector2(TEXTSCALE), default);
    }

    public void drawStringCentredShadowedUI(ReadOnlySpan<char> text, Vector2 position, Color4b color = default) {
        var offsetX = guiFont.MeasureString(text, TEXTSCALEV).X / 2;
        var offsetY = guiFont.MeasureString(text, TEXTSCALEV).Y / 2;
        DrawString(text, new Vector2(position.X * guiScale - offsetX, position.Y * guiScale - offsetY) + new Vector2(1, 1), Color4b.DimGray, new Vector2(TEXTSCALE), default);
        DrawString(text, new Vector2(position.X * guiScale - offsetX, position.Y * guiScale - offsetY), color == default ? Color4b.White : color, new Vector2(TEXTSCALE), default);
    }

    protected void DrawString(ReadOnlySpan<char> text, Vector2 position, Color4b colour) {
        guiFont.DrawText(Game.fontLoader.renderer, text, position, colour.toFS());
    }

    protected void DrawString(ReadOnlySpan<char> text, Vector2 position, Color4b colour, Vector2 scale, Vector2 offset) {
        guiFont.DrawText(Game.fontLoader.renderer, text, position, colour.toFS(), 0, offset, scale);
    }

    protected void DrawString(ReadOnlySpan<char> text, Vector2 position, Color4b colour, Vector2 scale, float rotation, Vector2 offset) {
        guiFont.DrawText(Game.fontLoader.renderer, text, position, colour.toFS(), rotation, offset, scale);
    }
    protected void DrawStringThin(ReadOnlySpan<char> text, Vector2 position, Color4b colour, Vector2 scale, Vector2 offset) {
        guiFontThin.DrawText(Game.fontLoader.renderer, text, position, colour.toFS(), 0, offset, scale);
    }

    protected void DrawRString(RichTextLayout layout, Vector2 position, Color4b colour, Vector2 scale, TextHorizontalAlignment alignment) {
        layout.Draw(Game.fontLoader.renderer, position, colour.toFS(), 0, new Vector2(0), scale, 0f, alignment);
    }

    public Vector2 measureString(ReadOnlySpan<char> text) {
        return guiFont.MeasureString(text, TEXTSCALEV);
    }

    public Vector2 measureStringThin(ReadOnlySpan<char> text) {
        return guiFontThin.MeasureString(text, TEXTSCALEV);
    }

    public Vector2 measureStringSmall(ReadOnlySpan<char> text) {
        return guiFontThin.MeasureString(text, TEXTSCALEV / 2f);
    }

    public void drawBlock(Block block, int x, int y, int size) {
        //GD.Clear(ClearBuffers.Color);
        var viewport = GD.Viewport;
        var dt = GD.DepthTestingEnabled;
        GD.DepthTestingEnabled = true;
        GD.VertexArray = buffer;
        GD.ShaderProgram = guiBlockShader;
        WorldRenderer.meshBlockTinted(block, ref guiBlock, ref guiBlockI, 15);
        // assemble the matrix
        // this is something like 33.7 degrees if the inverse tangent is calculated....
        // still zero idea why this works but 30 deg is garbage, 45 is weirdly elongated and 60 is squished
        var camPos = new Vector3(1, 2 / 3f, 1);

        // how I got these numbers? I don't fucking know anymore, and this is probably not the right way to do it
        var mat =
            // first we translate to the coords
            // model
            Matrix4x4.CreateScale(1, -1, 1) *
            Matrix4x4.CreateTranslation(0, 5 / 6f, 0) *
            Matrix4x4.CreateLookAt(camPos, new Vector3(0, 0, 0), new Vector3(0, 1, 0)) *
            // projection
            Matrix4x4.CreateOrthographicOffCenterLeftHanded(-0.75f, 0.75f, 0.75f, -0.75f, -10, 10);
        //Matrix4x4.CreateTranslation(0, 0, 0);
        //var unit = GD.BindTextureSetActive(Game.instance.blockTexture);
        guiBlockShader.Uniforms["uMVP"].SetValueMat4(in mat);
        guiBlockShader.Uniforms["blockTexture"].SetValueTexture(Game.textureManager.blockTextureGUI);
        var sp = CollectionsMarshal.AsSpan(guiBlock);
        var spI = CollectionsMarshal.AsSpan(guiBlockI);
        buffer.DataSubset.SetData(sp);
        buffer.IndexSubset!.SetData(spI);
        var sSize = size * guiScale;
        GD.Viewport = new Viewport(x, Game.height - y - sSize, (uint)sSize, (uint)sSize);
        // DON'T REMOVE OR THIS FUCKING SEGFAULTS
        // status update: it doesn't segfault anymore because we hacked the trippygl layer to reset their expectations!
        // it no longer thinks we have vertex arrays bound when we actually trashed it in our renderer
        //GL.BindVertexArray(buffer.VertexArray.Handle);
        GD.DrawElements(PrimitiveType.Triangles, 0, (uint)spI.Length);

        // restore
        GD.DepthTestingEnabled = dt;
        GD.Viewport = viewport;
    }

    public void drawBlockUI(Block block, int x, int y, int size) {
        drawBlock(block, x * guiScale, y * guiScale, size);
    }
}