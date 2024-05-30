using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using SixLabors.Fonts;
using TrippyGL;
using TrippyGL.Fonts;
using TrippyGL.Fonts.Building;
using TrippyGL.Fonts.Extensions;
using TrippyGL.ImageSharp;
using PrimitiveType = TrippyGL.PrimitiveType;
using Rectangle = System.Drawing.Rectangle;
using VertexArray = TrippyGL.VertexArray;

namespace BlockGame;

/// <summary>
/// GUI class which can draw onto the screen.
/// Supports scaling with guiScale.
/// Drawing methods ending with "UI" draw on the virtual GUI coordinate system, so they are positioned in the right place when the GUI scale is changed.
/// </summary>
public class GUI {

    public GL GL;
    public GraphicsDevice GD;

    public SimpleShaderProgram shader;

    public static int guiScale = 4;

    public TextureBatcher tb;
    public Texture2D guiTexture;
    public Texture2D colourTexture;
    public TextureFont guiFont;
    public TextureFont guiFontUnicode;

    public int uiWidth;
    public int uiHeight;

    public int uiCentreX;
    public int uiCentreY;

    /// <summary>
    /// Like the normal shader but it's simple (doesn't need fog/lighting/anything)
    /// </summary>
    public ShaderProgram guiBlockShader;

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
        shader = SimpleShaderProgram.Create<VertexColorTexture>(Game.GD, excludeWorldMatrix: true);
        tb.SetShaderProgram(shader);
        guiTexture = Texture2DExtensions.FromFile(Game.GD, "textures/gui.png");
        colourTexture = Texture2DExtensions.FromFile(Game.GD, "textures/debug.png");
        instance = this;
        guiBlockShader = ShaderProgram.FromFiles<BlockVertex>(
            GD, "shaders/guiBlock.vert", "shaders/guiBlock.frag", "vPos", "texCoord", "iData");
        buffer = new VertexBuffer<BlockVertex>(GD, 240, 360, ElementType.UnsignedShort, BufferUsage.StreamDraw);
        guiBlock = new List<BlockVertex>();
        guiBlockI = new List<ushort>();
    }

    public void loadFonts() {
        if (!File.Exists(Constants.fontFile)) {
            var collection = new FontCollection();
            var family = collection.Add("fonts/unifont-15.1.04.ttf");
            var font = family.CreateFont(12, FontStyle.Regular);
            using var ff = FontBuilderExtensions.CreateFontFile(font, (char)0, (char)127);
            guiFont = ff.CreateFont(Game.GD);
            ff.WriteToFile(Constants.fontFile);
        }
        else {
            using var ff = TrippyFontFile.FromFile(Constants.fontFile);
            guiFont = ff.CreateFont(Game.GD);
        }
    }

    /// <summary>
    /// Runs on background thread. Don't do OpenGL shit in there or try to assemble the font
    /// </summary>
    public void loadUnicodeFont() {
        if (!File.Exists(Constants.fontFileUnicode)) {
            var collection = new FontCollection();
            var family = collection.Add("fonts/unifont-15.1.04.ttf");
            var font = family.CreateFont(12, FontStyle.Regular);
            // DON'T CALL using, it would destroy the class variable
            var ffu = FontBuilderExtensions.CreateFontFile(font, (char)0, (char)0x3000);
            ff = ffu;
        }
        else {
            var ffu = TrippyFontFile.FromFile(Constants.fontFileUnicode);
            ff = ffu;
        }
    }

    /// <summary>
    /// This is needed so it loads on the main thread (memory corruption otherwise)
    /// </summary>
    public void loadUnicodeFont2() {
        if (!File.Exists(Constants.fontFileUnicode)) {
            guiFontUnicode = ff.CreateFont(Game.GD);
            ff.WriteToFile(Constants.fontFileUnicode);
        }
        else {
            guiFontUnicode = ff.CreateFont(Game.GD);
        }
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
                tb.DrawRaw(Game.instance.blockTexture,
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

    public void drawUI(Texture2D texture, Vector2 position, Rectangle? source = null,
        Color4b color = default, Vector2 origin = default, float depth = 0f) {
        tb.Draw(texture, position * guiScale, source, color == default ? Color4b.White : color, guiScale, 0f, origin, depth);
    }

    // maybe some day we will have common logic for these functions if the number of permutations grow in size. BUT NOT TODAY

    public void drawString(string text, Vector2 position, Color4b color = default) {
        tb.DrawString(guiFont, text, position, color == default ? Color4b.White : color);
    }

    public void drawStringUI(string text, Vector2 position, Color4b color = default) {
        tb.DrawString(guiFont, text, position * guiScale, color == default ? Color4b.White : color);
    }

    public void drawStringCentred(string text, Vector2 position, Color4b color = default) {
        var offsetX = guiFont.Measure(text).X / 2;
        var offsetY = guiFont.Measure(text).Y / 2;
        tb.DrawString(guiFont, text, new Vector2(position.X - offsetX, position.Y - offsetY), color == default ? Color4b.White : color);
    }

    public void drawStringCentredUI(string text, Vector2 position, Color4b color = default) {
        var offsetX = guiFont.Measure(text).X / 2;
        var offsetY = guiFont.Measure(text).Y / 2;
        tb.DrawString(guiFont, text, new Vector2(position.X * guiScale - offsetX, position.Y * guiScale - offsetY), color == default ? Color4b.White : color);
    }

    // some day we'll have a better API, but not this day
    public void drawStringShadowed(string text, Vector2 position, Color4b color = default) {
        tb.DrawString(guiFont, text, position + new Vector2(1, 1), Color4b.DimGray);
        tb.DrawString(guiFont, text, position, color == default ? Color4b.White : color);
    }

    public void drawStringShadowedUI(string text, Vector2 position, Color4b color = default) {
        tb.DrawString(guiFont, text, position * guiScale + new Vector2(1, 1), Color4b.DimGray);
        tb.DrawString(guiFont, text, position * guiScale, color == default ? Color4b.White : color);
    }

    public void drawStringCentredShadowed(string text, Vector2 position, Color4b color = default) {
        var offsetX = guiFont.Measure(text).X / 2;
        var offsetY = guiFont.Measure(text).Y / 2;
        tb.DrawString(guiFont, text, new Vector2(position.X - offsetX, position.Y - offsetY) + new Vector2(1, 1), Color4b.DimGray);
        tb.DrawString(guiFont, text, new Vector2(position.X - offsetX, position.Y - offsetY), color == default ? Color4b.White : color);
    }

    public void drawStringCentredShadowedUI(string text, Vector2 position, Color4b color = default) {
        var offsetX = guiFont.Measure(text).X / 2;
        var offsetY = guiFont.Measure(text).Y / 2;
        tb.DrawString(guiFont, text, new Vector2(position.X * guiScale - offsetX, position.Y * guiScale - offsetY) + new Vector2(1, 1), Color4b.DimGray);
        tb.DrawString(guiFont, text, new Vector2(position.X * guiScale - offsetX, position.Y * guiScale - offsetY), color == default ? Color4b.White : color);
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
        var unit = GD.BindTextureSetActive(Game.instance.blockTexture);
        guiBlockShader.Uniforms["uMVP"].SetValueMat4(mat);
        guiBlockShader.Uniforms["blockTexture"].SetValueTexture(Game.instance.blockTexture);
        //GD.ResetStates();
        buffer.DataSubset.SetData(CollectionsMarshal.AsSpan(guiBlock));
        buffer.IndexSubset!.SetData(CollectionsMarshal.AsSpan(guiBlockI));
        var sSize = size * guiScale;
        //GD.ResetStates();
        GD.Viewport = new Viewport(x, Game.height - y - sSize, (uint)sSize, (uint)sSize);
        GD.DrawElements(PrimitiveType.Triangles, 0, (uint)guiBlockI.Count);
        //GD.ResetStates();
        GD.Viewport = viewport;
    }

    public void drawBlockUI(Block block, int x, int y, int size) {
        drawBlock(block, x * guiScale, y * guiScale, size);
    }
}