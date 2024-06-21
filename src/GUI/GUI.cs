using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using BlockGame.util;
using BlockGame.util.font;
using FontStashSharp;
using FontStashSharp.RichText;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using TrippyGL;
using TrippyGL.Fonts;
using TrippyGL.ImageSharp;
using PrimitiveType = TrippyGL.PrimitiveType;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame.GUI;

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
    private VertexBuffer<BlockVertex> buffer;
    private Matrix4x4 ortho;

    private List<BlockVertex> guiBlock;
    private List<ushort> guiBlockI;

    public GUI() {
        GL = Game.GL;
        GD = Game.GD;
        tb = new TextureBatcher(Game.GD);
        immediatetb = new TextureBatcher(Game.GD);
        shader = SimpleShaderProgram.Create<VertexColorTexture>(Game.GD, excludeWorldMatrix: true);
        tb.SetShaderProgram(shader);
        immediatetb.SetShaderProgram(shader);
        guiTexture = Texture2DExtensions.FromFile(Game.GD, "textures/gui.png");
        colourTexture = Texture2DExtensions.FromFile(Game.GD, "textures/debug.png");
        instance = this;
        guiBlockShader = ShaderProgram.FromFiles<BlockVertex>(
            GD, "shaders/guiBlock.vert", "shaders/guiBlock.frag", "vPos", "texCoord", "iData");
        buffer = new VertexBuffer<BlockVertex>(GD, 4 * Face.MAX_FACES, 6 * Face.MAX_FACES, ElementType.UnsignedShort, BufferUsage.StreamDraw);
        guiBlock = new List<BlockVertex>();
        guiBlockI = new List<ushort>();


    }

    public void loadFont(int size) {
        guiFont = Game.fontLoader.fontSystem.GetFont(size);
        guiFontThin = Game.fontLoader.fontSystemThin.GetFont(size);
    }



    public void resize(Vector2D<int> size) {
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
        var texCoords = Block.texCoords(block.model.faces[0].min).As<float>();
        var texCoordsMax = Block.texCoords(block.model.faces[0].max).As<float>();

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

    public void drawStringUI(ReadOnlySpan<char> text, Vector2 position, Color4b color, Vector2 scale) {
        DrawString(text, position * guiScale, color == default ? Color4b.White : color, TEXTSCALE * scale, default);
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
        GD.VertexArray = buffer;
        GD.ShaderProgram = guiBlockShader;
        WorldRenderer.meshBlock(block, ref guiBlock, ref guiBlockI);
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
        guiBlockShader.Uniforms["uMVP"].SetValueMat4(mat);
        guiBlockShader.Uniforms["blockTexture"].SetValueTexture(Game.textureManager.blockTextureGUI);
        var sp = CollectionsMarshal.AsSpan(guiBlock);
        var spI = CollectionsMarshal.AsSpan(guiBlockI);
        buffer.DataSubset.SetData(sp);
        buffer.IndexSubset!.SetData(spI);
        var sSize = size * guiScale;
        GD.Viewport = new Viewport(x, Game.height - y - sSize, (uint)sSize, (uint)sSize);
        // DON'T REMOVE OR THIS FUCKING SEGFAULTS
        //GL.BindVertexArray(buffer.VertexArray.Handle);
        GD.DrawElements(PrimitiveType.Triangles, 0, (uint)spI.Length);
        GD.Viewport = viewport;
    }

    public void drawBlockUI(Block block, int x, int y, int size) {
        drawBlock(block, x * guiScale, y * guiScale, size);
    }
}