using System.Numerics;
using System.Runtime.InteropServices;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.main;
using BlockGame.ui.menu;
using BlockGame.util;
using BlockGame.util.font;
using BlockGame.world.block;
using BlockGame.world.item;
using FontStashSharp;
using FontStashSharp.RichText;
using Molten;
using Silk.NET.OpenGL.Legacy;
using Rectangle = System.Drawing.Rectangle;
using RectangleF = System.Drawing.RectangleF;
using Shader = BlockGame.GL.Shader;
using VertexColorTexture = BlockGame.GL.VertexColorTexture;

namespace BlockGame.ui;

/// <summary>
/// GUI class which can draw onto the menu.
/// Supports scaling with guiScale.
/// Drawing methods ending with "UI" draw on the virtual GUI coordinate system, so they are positioned in the right place when the GUI scale is changed.
/// </summary>
public class GUI {

    public static int guiScale = 4;

    public SpriteBatch tb;
    public SpriteBatch immediatetb;
    public BTexture2D guiTexture;
    public BTexture2D colourTexture;

    public int uiWidth;
    public int uiHeight;

    public int uiCentreX;
    public int uiCentreY;

    /// <summary>
    /// Like the normal shader but it's simple (doesn't need fog/lighting/anything)
    /// </summary>
    public Shader guiBlockShader;

    public DynamicSpriteFont guiFont = null!;
    public DynamicSpriteFont guiFontThin = null!;
    public Rectangle buttonRect = new(96, 0, 96, 16);
    public Rectangle grayButtonRect = new(0, 16 * 2, 96, 16);

    public static GUI instance;
    private StreamingVAO<BlockVertexTinted> buffer;
    private Matrix4x4 ortho;

    private List<BlockVertexTinted> guiBlock;
    private int uMVP;
    private int blockTexture = 0;

    private Vector2 backgroundScrollOffset = Vector2.Zero;
    private static readonly Color4b bgGray = Color4b.DarkGray;
    private static readonly Color4b skyc = Color4b.CornflowerBlue;

    public static bool WIREFRAME = false;
    
    /** pixels per second */
    private const float SCROLL_SPEED = 32.0f; 

    public GUI() {
        //shader = new InstantShader(GL, "shaders/batch.vert", "shaders/batch.frag");
        tb = Game.graphics.mainBatch;
        immediatetb = Game.graphics.immediateBatch;
        guiTexture = new BTexture2D("textures/gui.png");
        colourTexture = new BTexture2D("textures/debug.png");
        guiTexture.reload();
        colourTexture.reload();
        instance = this;
        guiBlockShader = new Shader(Game.GL, nameof(guiBlockShader), "shaders/ui/simpleBlock.vert", "shaders/ui/simpleBlock.frag");
        buffer = new StreamingVAO<BlockVertexTinted>();
        buffer.bind();
        buffer.setSize(Face.MAX_FACES * 4);
        // GD, 4 * Face.MAX_FACES, 6 * Face.MAX_FACES, ElementType.UnsignedShort, BufferUsage.StreamDraw
        guiBlock = new List<BlockVertexTinted>();

        uMVP = guiBlockShader.getUniformLocation("uMVP");
        blockTexture = guiBlockShader.getUniformLocation("blockTexture");
    }

    public void loadFont(int size) {
        guiFont = Game.fontLoader.fontSystem.GetFont(size);
        guiFontThin = Game.fontLoader.fontSystemThin.GetFont(size);
    }


    public void resize(Vector2I size) {
        ortho = Matrix4x4.CreateOrthographicOffCenter(0, size.X, size.Y, 0, -1f, 1f);
        //shader.Projection = ortho;
        uiCentreX = size.X / 2 / guiScale;
        uiCentreY = size.Y / 2 / guiScale;

        uiWidth = size.X / guiScale;
        uiHeight = size.Y / guiScale;
        //worldShader.Projection = Game.instance.world.player.camera.getProjectionMatrix();
        //worldShader.View = Game.instance.world.player.camera.getViewMatrix(1);

        // handle guiscale
        var blockSize = 16d;
        blockSize *= guiScale * 2;

        // if one block is a given size, how many blocks can we fit on the screen?
        var xCount = (int)Math.Ceiling(Game.width / blockSize);
        var yCount = (int)Math.Ceiling(Game.height / blockSize);
    }

    public void update(double dt) {
        updateBackgroundScroll(dt);
    }

    // Call this in your update method
    public void updateBackgroundScroll(double dt) {
        backgroundScrollOffset.Y += (float)(SCROLL_SPEED * dt); // Slower vertical scroll
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

    public void drawItem(ItemSlot slot, Vector2 pos) {
        var stack = slot.getStack();
        
        if (stack == null || stack.id == Items.AIR) {
            return;
        }
        
        var itemPos = slot.itemPos;
        
        // if block
        var item = Item.get(stack.id);
        if (item.isBlock()) {
            var blockID = item.getBlockID();
            if (Block.renderItemLike[blockID]) {
                drawItemSprite(item, stack, pos.X + itemPos.X, pos.Y + itemPos.Y);
            } else {
                Game.gui.drawBlockUI(item.getBlock(), (int)(pos.X + itemPos.X), (int)(pos.Y + itemPos.Y),
                    ItemSlot.ITEMSIZE, (byte)stack.metadata);
                // draw amount text
                if (stack.quantity > 1) {
                    var s = stack.quantity.ToString();
                    Game.gui.drawStringUIThin(s,
                        new Vector2(
                            pos.X + itemPos.X + ItemSlot.ITEMSIZE - ItemSlot.PADDING -
                            s.Length * 6f / guiScale,
                            pos.Y + itemPos.Y + ItemSlot.ITEMSIZE - 13f / guiScale - ItemSlot.PADDING));
                }
            }
        }
        else if (item.isItem()) {
            drawItemSprite(item, stack, pos.X + itemPos.X, pos.Y + itemPos.Y);
        }
    }

    public void drawItemWithoutInv(ItemSlot slot) {
        var stack = slot.getStack();
        var itemPos = slot.itemPos;

        var item = Item.get(stack.id);
        if (item.isBlock()) {
            var blockID = item.getBlockID();
            if (Block.renderItemLike[blockID]) {
                drawItemSprite(item, stack, itemPos.X, itemPos.Y);
            } else {
                Game.gui.drawBlockUI(item.getBlock(), itemPos.X, itemPos.Y, ItemSlot.ITEMSIZE, (byte)stack.metadata);
                drawQuantityText(stack, itemPos.X, itemPos.Y);
            }
        }
        else if (item.isItem()) {
            drawItemSprite(item, stack, itemPos.X, itemPos.Y);
        }
    }

    public void drawCursorItem(ItemStack? cursorItem, Vector2 mousePos) {
        if (cursorItem == null || cursorItem.id == Items.AIR) {
            return;
        }

        var item = Item.get(cursorItem.id);
        var pos = s2u(mousePos);
        // offset by half item size so it's centered on cursor
        var drawX = pos.X - ItemSlot.ITEMSIZE / 2f;
        var drawY = pos.Y - ItemSlot.ITEMSIZE / 2f;

        if (item.isBlock()) {
            var blockID = item.getBlockID();
            if (Block.renderItemLike[blockID]) {
                drawItemSprite(item, cursorItem, drawX, drawY);
            } else {
                Game.gui.drawBlockUI(item.getBlock(), (int)drawX, (int)drawY, ItemSlot.ITEMSIZE, (byte)cursorItem.metadata);

                // draw quantity if > 1
                if (cursorItem.quantity > 1) {
                    var s = cursorItem.quantity.ToString();
                    Game.gui.drawStringUIThin(s,
                        new Vector2(drawX + ItemSlot.ITEMSIZE - ItemSlot.PADDING - s.Length * 6f / guiScale,
                            drawY + ItemSlot.ITEMSIZE - 13f / guiScale - ItemSlot.PADDING));
                }
            }
        }
        else if (item.isItem()) {
            drawItemSprite(item, cursorItem, drawX, drawY);
        }
    }

    /// <summary>
    /// Draw a full-screen background with a block texture and the specified block size in pixels.
    /// </summary>
    public void drawBG(Block block, float size) {
        var texCoords = UVPair.texCoords(block.model.faces[0].min);
        var texCoordsMax = UVPair.texCoords(block.model.faces[0].max);

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
                tb.DrawRaw(Game.textures.blockTexture,
                    new VertexColorTexture(new Vector3(left, top, 0), bgGray,
                        new Vector2(texCoords.X, texCoords.Y)),
                    new VertexColorTexture(new Vector3(right, top, 0), bgGray,
                        new Vector2(texCoordsMax.X, texCoords.Y)),
                    new VertexColorTexture(new Vector3(right, bottom, 0), bgGray,
                        new Vector2(texCoordsMax.X, texCoordsMax.Y)),
                    new VertexColorTexture(new Vector3(left, bottom, 0), bgGray,
                        new Vector2(texCoords.X, texCoordsMax.Y)));
            }
        }
    }

    public void drawBG(float size) {
        var left = 0;
        var right = Game.width;
        var top = 0;
        var bottom = Game.height;

        // handle guiscale
        size *= guiScale * 2;

        // if one block is a given size, how many blocks can we fit on the screen?
        var xCount = Game.width / size;
        var yCount = Game.height / size;

        var texCoords = new Vector2(0, 0);
        var texCoordsMax = new Vector2(xCount, yCount);
        tb.DrawRaw(Game.textures.background,
            new VertexColorTexture(new Vector3(left, top, 0), bgGray, new Vector2(texCoords.X, texCoords.Y)),
            new VertexColorTexture(new Vector3(right, top, 0), bgGray, new Vector2(texCoordsMax.X, texCoords.Y)),
            new VertexColorTexture(new Vector3(right, bottom, 0), bgGray,
                new Vector2(texCoordsMax.X, texCoordsMax.Y)),
            new VertexColorTexture(new Vector3(left, bottom, 0), bgGray,
                new Vector2(texCoords.X, texCoordsMax.Y)));

    }

    public void drawScrollingBG(float size) {
        var left = 0;
        var right = Game.width;
        var top = 0;
        var bottom = Game.height;

        // Handle guiscale
        float blockSize = size * guiScale * 2;

        // Calculate visible area in blocks
        var xCount = (int)Math.Ceiling(Game.width / blockSize) + 2; // +2 for smooth scrolling
        var yCount = (int)Math.Ceiling(Game.height / blockSize) + 2;

        // Get starting world coordinates
        // Start 3 blocks above the grass layer so grass is visible at top of screen
        int startX = (int)Math.Floor(backgroundScrollOffset.X / blockSize);
        int startY = (int)Math.Floor(backgroundScrollOffset.Y / blockSize) - 3;

        // Calculate fractional offset for smooth scrolling
        float offsetX = backgroundScrollOffset.X % blockSize;
        float offsetY = backgroundScrollOffset.Y % blockSize;

        Span<ushort> ores = [Blocks.AMBER_ORE, Blocks.RED_ORE, Blocks.EMERALD_ORE, Blocks.DIAMOND_ORE, Blocks.TITANIUM_ORE, Blocks.AMETHYST_ORE];

        // Draw layered background
        for (int x = 0; x < xCount; x++) {
            for (int y = 0; y < yCount; y++) {
                // screen position
                float tileLeft = x * blockSize - offsetX;
                float tileTop = y * blockSize - offsetY;

                // absolute world position
                int worldX = startX + x;
                int worldY = startY + y;
                
                
                ushort blockId = getBlockTypeForDepth(worldX, worldY);
                
                // Skip drawing air blocks
                if (blockId == Block.AIR.id) {
                    
                    // draw gray rectangle for air blocks
                    tb.DrawRaw(colourTexture,
                        new VertexColorTexture(new Vector3(tileLeft, tileTop, 0), bgGray * skyc, new Vector2(0, 0)),
                        new VertexColorTexture(new Vector3(tileLeft + blockSize, tileTop, 0), bgGray * skyc, new Vector2(1, 0)),
                        new VertexColorTexture(new Vector3(tileLeft + blockSize, tileTop + blockSize, 0), bgGray * skyc, new Vector2(1, 1)),
                        new VertexColorTexture(new Vector3(tileLeft, tileTop + blockSize, 0), bgGray * skyc, new Vector2(0, 1)));
                    
                    continue;
                }
                
                // If it's in the stone layer, check for ores
                if (blockId == Block.STONE.id && shouldPlaceOre(worldX, worldY)) {
                    int oreIndex = XHash.hashRange(worldX, worldY, ores.Length);
                    blockId = ores[oreIndex];
                }
                
                var block = Block.get(blockId);
                var texCoords_ = UVPair.texCoords(block.model!.faces[0].min);
                var texCoordsMax_ = UVPair.texCoords(block.model!.faces[0].max);
                var texCoords = new Vector2(texCoords_.X, texCoords_.Y);
                var texCoordsMax = new Vector2(texCoordsMax_.X, texCoordsMax_.Y);

                tb.DrawRaw(Game.textures.blockTexture,
                    new VertexColorTexture(new Vector3(tileLeft, tileTop, 0), bgGray, texCoords),
                    new VertexColorTexture(new Vector3(tileLeft + blockSize, tileTop, 0), bgGray, new Vector2(texCoordsMax.X, texCoords.Y)),
                    new VertexColorTexture(new Vector3(tileLeft + blockSize, tileTop + blockSize, 0), bgGray, texCoordsMax),
                    new VertexColorTexture(new Vector3(tileLeft, tileTop + blockSize, 0), bgGray, new Vector2(texCoords.X, texCoordsMax.Y)));
            }
        }
    }

    private bool shouldPlaceOre(int worldX, int worldZ) {
        return XHash.hashRange(worldX, worldZ, 20) == 0; // 5% chance
    }

    // determine block type based on depth (y coordinate)
    private ushort getBlockTypeForDepth(int worldX, int worldY) {
        // air above surface (y < 0)
        if (worldY < 0) {
            return Blocks.AIR;
        }
        
        // surface layer (y = 0) is grass
        if (worldY == 0) {
            return Blocks.GRASS;
        }
        
        // dirt percentage increases linearly from depth 1 to 7
        if (worldY >= 1 && worldY <= 7) {
            // calculate dirt percentage: 100% at depth 1, decreasing to ~15% at depth 7
            float dirtPercentage = 1.0f - ((worldY - 1) / 6.0f * 0.85f);
            float randomValue = XHash.hashFloat(worldX, worldY);
            
            if (randomValue < dirtPercentage) {
                return Blocks.DIRT;
            }
        }
        
        // what isn't dirt is stone
        return Blocks.STONE;
    }


    public void draw(BTexture2D texture, Vector2 position, float scale = 1f, Rectangle? source = null,
        Color4b color = default, Vector2 origin = default, float depth = 0f) {
        tb.Draw(texture, position, source, color == default ? Color4b.White : color, scale != 1f ? guiScale * scale : guiScale, 0f, origin, depth);
    }

    public void draw(BTexture2D texture, RectangleF dest, Rectangle? source = null, Color4b color = default) {
        tb.Draw(texture, dest, source, color == default ? Color4b.White : color);
    }

    public void drawGradientVertical(BTexture2D texture, RectangleF dest, Color4b topColor, Color4b bottomColor, Rectangle? source = null) {
        var left = dest.X;
        var right = dest.X + dest.Width;
        var top = dest.Y;
        var bottom = dest.Y + dest.Height;
        
        var texCoords = source.HasValue ? 
            new RectangleF(source.Value.X, source.Value.Y, source.Value.Width, source.Value.Height) :
            new RectangleF(0, 0, 1, 1);
        
        tb.DrawRaw(texture,
            new VertexColorTexture(new Vector3(left, top, 0), topColor, new Vector2(texCoords.X, texCoords.Y)),
            new VertexColorTexture(new Vector3(right, top, 0), topColor, new Vector2(texCoords.Right, texCoords.Y)),
            new VertexColorTexture(new Vector3(right, bottom, 0), bottomColor, new Vector2(texCoords.Right, texCoords.Bottom)),
            new VertexColorTexture(new Vector3(left, bottom, 0), bottomColor, new Vector2(texCoords.X, texCoords.Bottom)));
    }

    public void drawImmediate(BTexture2D texture, Vector2 position, Rectangle? source = null,
        Color4b color = default, Vector2 origin = default, float depth = 0f) {
        immediatetb.Draw(texture, position, source, color == default ? Color4b.White : color, guiScale, 0f, origin,
            depth);
    }

    public void drawUI(BTexture2D texture, Vector2 position, Rectangle? source = null,
        Color4b color = default, Vector2 origin = default, float depth = 0f) {
        tb.Draw(texture, position * guiScale, source, color == default ? Color4b.White : color, guiScale, 0f, origin,
            depth);
    }

    public void drawUI(BTexture2D texture, RectangleF dest, Rectangle? source = null, Color4b color = default,
        float depth = 0f) {
        tb.Draw(texture, new RectangleF(
            (int)(dest.X * guiScale),
            (int)(dest.Y * guiScale),
            (int)(dest.Width * guiScale),
            (int)(dest.Height * guiScale)), source, color == default ? Color4b.White : color, depth);
    }

    public void drawUIImmediate(BTexture2D texture, Vector2 position, Rectangle? source = null,
        Color4b color = default, Vector2 origin = default, float depth = 0f) {
        immediatetb.Draw(texture, position * guiScale, source, color == default ? Color4b.White : color, guiScale, 0f,
            origin, depth);
    }

    
    /** Text rendering is at half gui scale, which is normally 2x (compared to the guiscale 4x) */
    public static int TEXTSCALE => guiScale / 2;
    public static Vector2 TEXTSCALEV => new(TEXTSCALE, TEXTSCALE);

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

    public void drawRString(RichTextLayout layout, Vector2 position, TextHorizontalAlignment alignment,
        Color4b color = default) {
        DrawRString(layout, position, color == default ? Color4b.White : color, new Vector2(TEXTSCALE), alignment);
    }

    public void drawStringUI(ReadOnlySpan<char> text, Vector2 position, Color4b color = default) {
        DrawString(text, position * guiScale, color == default ? Color4b.White : color, new Vector2(TEXTSCALE),
            default);
    }

    public void drawStringUI(ReadOnlySpan<char> text, Vector2 position, Color4b colour, Vector2 scale) {
        DrawString(text, position * guiScale, colour == default ? Color4b.White : colour, TEXTSCALE * scale, default);
    }

    public void drawStringUIThin(ReadOnlySpan<char> text, Vector2 position, Color4b colour = default) {
        DrawStringThin(text, position * guiScale, colour == default ? Color4b.White : colour, new Vector2(TEXTSCALE),
            default);
    }


    /// <summary>
    /// Rotation Y = yaw (horizontal rotation)
    /// Rotation X = pitch (banking down or up)
    /// Rotation Z = roll (tilt sideways)
    /// </summary>
    public void drawString3D(ReadOnlySpan<char> text, Vector3 position, Vector3 rotation, float scale = 1f,
        Color4b colour = default) {
        // flip the text - 2D rendering goes +y=down, we want +y=up
        // 1 pt should be 1/16th pixel
        var flip = Matrix4x4.Identity;
        flip.M22 = -1;
        var rot = Matrix4x4.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z);
        var mat = flip * rot * Matrix4x4.CreateScale(1 / 256f * scale) * Matrix4x4.CreateTranslation(position);
        guiFontThin.DrawText(Game.fontLoader.renderer3D, text, new Vector2(0, 0),
            colour == default ? FSColor.White : colour.toFS(), ref mat);
    }

    public void drawStringOnBlock(ReadOnlySpan<char> text, Vector3I pos, RawDirection face, float scale,
        Color4b colour = default) {
        // draw slightly out so the block won't z-fight with the text
        const float offset = 0.001f;
        Vector3 rotation = Vector3.Zero;
        var deg90ToRad = Meth.deg2rad(90);
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
        DrawString(text, new Vector2(position.X - offsetX, position.Y - offsetY),
            color == default ? Color4b.White : color, new Vector2(TEXTSCALE), default);
    }

    public void drawStringCentredUI(ReadOnlySpan<char> text, Vector2 position, Color4b color = default) {
        var offsetX = guiFont.MeasureString(text, TEXTSCALEV).X / 2;
        var offsetY = guiFont.MeasureString(text, TEXTSCALEV).Y / 2;
        DrawString(text, new Vector2(position.X * guiScale - offsetX, position.Y * guiScale - offsetY),
            color == default ? Color4b.White : color, new Vector2(TEXTSCALE), default);
    }

    // some day we'll have a better API, but not this day
    public void drawStringShadowed(ReadOnlySpan<char> text, Vector2 position, Color4b color = default) {
        DrawString(text, position + new Vector2(1, 1), Color4b.DimGray, new Vector2(TEXTSCALE), default);
        DrawString(text, position, color == default ? Color4b.White : color, new Vector2(TEXTSCALE), default);
    }

    public void drawStringShadowedUI(ReadOnlySpan<char> text, Vector2 position, Color4b color = default) {
        DrawString(text, position * guiScale + new Vector2(1, 1), Color4b.DimGray, new Vector2(TEXTSCALE), default);
        DrawString(text, position * guiScale, color == default ? Color4b.White : color, new Vector2(TEXTSCALE),
            default);
    }

    public void drawStringCentredShadowed(ReadOnlySpan<char> text, Vector2 position, Color4b color = default) {
        var offsetX = guiFont.MeasureString(text, TEXTSCALEV).X / 2;
        var offsetY = guiFont.MeasureString(text, TEXTSCALEV).Y / 2;
        DrawString(text, new Vector2(position.X - offsetX, position.Y - offsetY) + new Vector2(1, 1), Color4b.DimGray,
            new Vector2(TEXTSCALE), default);
        DrawString(text, new Vector2(position.X - offsetX, position.Y - offsetY),
            color == default ? Color4b.White : color, new Vector2(TEXTSCALE), default);
    }

    public void drawStringCentredShadowedUI(ReadOnlySpan<char> text, Vector2 position, Color4b color = default) {
        var offsetX = guiFont.MeasureString(text, TEXTSCALEV).X / 2;
        var offsetY = guiFont.MeasureString(text, TEXTSCALEV).Y / 2;
        DrawString(text,
            new Vector2(position.X * guiScale - offsetX, position.Y * guiScale - offsetY) + new Vector2(1, 1),
            Color4b.DimGray, new Vector2(TEXTSCALE), default);
        DrawString(text, new Vector2(position.X * guiScale - offsetX, position.Y * guiScale - offsetY),
            color == default ? Color4b.White : color, new Vector2(TEXTSCALE), default);
    }

    protected void DrawString(ReadOnlySpan<char> text, Vector2 position, Color4b colour) {
        guiFont.DrawText(Game.fontLoader.renderer, text, position, colour.toFS());
    }

    protected void DrawString(ReadOnlySpan<char> text, Vector2 position, Color4b colour, Vector2 scale,
        Vector2 offset) {
        guiFont.DrawText(Game.fontLoader.renderer, text, position, colour.toFS(), 0, offset, scale);
    }

    protected void DrawString(ReadOnlySpan<char> text, Vector2 position, Color4b colour, Vector2 scale, float rotation,
        Vector2 offset) {
        guiFont.DrawText(Game.fontLoader.renderer, text, position, colour.toFS(), rotation, offset, scale);
    }

    protected void DrawStringThin(ReadOnlySpan<char> text, Vector2 position, Color4b colour, Vector2 scale, Vector2 offset) {
        var aspectScale = new Vector2(scale.X * Game.fontLoader.thinFontAspectRatio, scale.Y);
        guiFontThin.DrawText(Game.fontLoader.renderer, text, position, colour.toFS(), 0, offset, aspectScale);
    }

    protected void DrawRString(RichTextLayout layout, Vector2 position, Color4b colour, Vector2 scale, TextHorizontalAlignment alignment) {
        if (layout.Font == guiFontThin) {
            // If the layout uses the thin font, we need to adjust the scale for the aspect ratio
            scale = new Vector2(scale.X * Game.fontLoader.thinFontAspectRatio, scale.Y);
        }
        
        layout.Draw(Game.fontLoader.renderer, position, colour.toFS(), 0, new Vector2(0), scale, 0f, alignment);
    }
    
    // selector versions
    
    public void drawStringShadowed(string text, Vector2 position, bool thin) {
        if (thin) {
            DrawStringThin(text, position + new Vector2(1, 1), Color4b.DimGray, new Vector2(TEXTSCALE), default);
            DrawStringThin(text, position, Color4b.White, new Vector2(TEXTSCALE), default);
        }
        else {
            DrawString(text, position + new Vector2(1, 1), Color4b.DimGray, new Vector2(TEXTSCALE), default);
            DrawString(text, position, Color4b.White, new Vector2(TEXTSCALE), default);
        }
    }
    
    public void drawString(string text, Vector2 position, bool thin) {
        if (thin) {
            DrawStringThin(text, position, Color4b.White, new Vector2(TEXTSCALE), default);
        }
        else {
            DrawString(text, position, Color4b.White, new Vector2(TEXTSCALE), default);
        }
    }
    
    public Vector2 measureString(ReadOnlySpan<char> text) {
        return guiFont.MeasureString(text, TEXTSCALEV);
    }

    public Vector2 measureStringThin(ReadOnlySpan<char> text) {
        var measurement = guiFontThin.MeasureString(text, TEXTSCALEV);
        return new Vector2(measurement.X * Game.fontLoader.thinFontAspectRatio, measurement.Y);
    }
    
    public Vector2 measureString(ReadOnlySpan<char> text, bool thin) {
        if (thin) {
            var measurement = guiFontThin.MeasureString(text, TEXTSCALEV);
            return new Vector2(measurement.X * Game.fontLoader.thinFontAspectRatio, measurement.Y);
        }
        return guiFont.MeasureString(text, TEXTSCALEV);
    }

    public Vector2 measureStringSmall(ReadOnlySpan<char> text) {
        var measurement = guiFontThin.MeasureString(text, TEXTSCALEV / 2f);
        return new Vector2(measurement.X * Game.fontLoader.thinFontAspectRatio, measurement.Y);
    }
    
    public Vector2 measureStringUI(ReadOnlySpan<char> text) {
        // 
        return guiFont.MeasureString(text);
    }
    
    public Vector2 measureStringUIThin(ReadOnlySpan<char> text) {
        var measurement = guiFontThin.MeasureString(text);
        return new Vector2(measurement.X * Game.fontLoader.thinFontAspectRatio, measurement.Y);
    }
    
    public Vector2 measureStringUICentred(ReadOnlySpan<char> text) {
        return guiFont.MeasureString(text) / 2f;
    }
    
    public Vector2 measureStringUI(ReadOnlySpan<char> text, bool thin) {
        if (thin) {
            var measurement = guiFontThin.MeasureString(text);
            return new Vector2(measurement.X * Game.fontLoader.thinFontAspectRatio, measurement.Y);
        }
        return guiFont.MeasureString(text);
    }
    
    public void drawWireframe(Rectangle bounds, Color4b col) {
        // draw an empty rectangle with the given color
        
        tb.DrawRaw(colourTexture,
            new VertexColorTexture(new Vector3(bounds.X, bounds.Y, 0), col, new Vector2(0, 0)),
            new VertexColorTexture(new Vector3(bounds.X + bounds.Width, bounds.Y, 0), col, new Vector2(1, 0)),
            new VertexColorTexture(new Vector3(bounds.X + bounds.Width, bounds.Y + bounds.Height, 0), col,
                new Vector2(1, 1)),
            new VertexColorTexture(new Vector3(bounds.X, bounds.Y + bounds.Height, 0), col, new Vector2(0, 1)));
    }

    public void drawBlock(Block block, int x, int y, int size, byte metadata = 0) {
        //GD.Clear(ClearBuffers.Color);
        Game.graphics.saveViewport();
        //var dt = GD.DepthTestingEnabled;
        //GD.DepthTestingEnabled = true;


        buffer.bind();
        Game.renderer.bindQuad();
        guiBlockShader.use();

        // bind block texture
        Game.textures.blockTexture.bind();
        
        // calculate light level at player

        /*var pos = Game.player?.position.toBlockPos();

        byte light;

        if (pos.HasValue) {
            var posObj = pos.Value;
            light = Game.world?.getLight(posObj.X, posObj.Y, posObj.Z) ?? 0;
        }
        else {
            light = 15;
        }*/

        Game.blockRenderer.setupStandalone();
        Game.blockRenderer.renderBlock(block, metadata, Vector3I.Zero, guiBlock, 
            lightOverride: 255, cullFaces: false);
        
        // assemble the matrix using existing matrix stacks from Graphics
        var projMatrix = Matrix4x4.CreateOrthographicOffCenterLeftHanded(-0.75f, 0.75f, 0.75f, -0.75f, -10, 10);
        
        Game.graphics.model.push();
        Game.graphics.model.loadIdentity();
        
        
        // view transformation - camera position
        var camPos = new Vector3(1, 2 / 3f, 1);
        var viewMatrix = Matrix4x4.CreateLookAt(camPos, new Vector3(0, 0, 0), new Vector3(0, 1, 0));
        Game.graphics.model.multiply(viewMatrix);
        
        // model transformations
        Game.graphics.model.translate(0, 5 / 6f, 0);
        Game.graphics.model.scale(1, -1, 1);
        
        // combine matrices
        var mat = Game.graphics.model.top * projMatrix;
        
        // restore matrix stacks
        Game.graphics.model.pop();
        //Matrix4x4.CreateTranslation(0, 0, 0);
        //var unit = GD.BindTextureSetActive(Game.instance.blockTexture);
        guiBlockShader.setUniform(uMVP, mat);
        guiBlockShader.setUniform(blockTexture, 0);
        var sp = CollectionsMarshal.AsSpan(guiBlock);
        buffer.upload(sp);
        var sSize = size * guiScale;
        Game.graphics.setViewport(x, Game.height - y - sSize, sSize, sSize);
        // DON'T REMOVE OR THIS FUCKING SEGFAULTS
        // status update: it doesn't segfault anymore because we hacked the trippygl layer to reset their expectations!
        // it no longer thinks we have vertex arrays bound when we actually trashed it in our renderer
        //GL.BindVertexArray(buffer.VertexArray.Handle);
        unsafe {
            Game.GL.DrawElements(PrimitiveType.Triangles, (uint)(sp.Length * 1.5), DrawElementsType.UnsignedShort, (void*)0);
        }

        // restore
        //GD.DepthTestingEnabled = dt;
        Game.graphics.restoreViewport();
    }

    public void drawBlockUI(Block block, int x, int y, int size, byte metadata = 0) {
        drawBlock(block, x * guiScale, y * guiScale, size, metadata);
    }

    private void drawItemSprite(Item item, ItemStack stack, float x, float y) {
        var destRect = new RectangleF(x, y, ItemSlot.ITEMSIZE, ItemSlot.ITEMSIZE);

        if (item.isBlock() && Block.renderItemLike[item.getBlockID()]) {
            // get texture directly from block
            var block = item.getBlock();
            var texUV = block.getTexture(0, (byte)stack.metadata);
            var uv = UVPair.texCoordsI(texUV);
            var sourceRect = new Rectangle((int)uv.X, (int)uv.Y, UVPair.ATLASSIZE, UVPair.ATLASSIZE);
            drawUI(Game.textures.blockTexture, destRect, sourceRect);
        } else {
            // normal item rendering
            var texUV = item.getTexture(stack);
            var uv = UVPair.texCoordsI(texUV);
            var sourceRect = new Rectangle((int)uv.X, (int)uv.Y, UVPair.ATLASSIZE, UVPair.ATLASSIZE);
            drawUI(Game.textures.itemTexture, destRect, sourceRect);
        }

        drawQuantityText(stack, x, y);
    }

    private void drawQuantityText(ItemStack stack, float x, float y) {
        if (stack.quantity > 1) {
            var s = stack.quantity.ToString();
            Game.gui.drawStringUIThin(s,
                new Vector2(x + ItemSlot.ITEMSIZE - ItemSlot.PADDING - s.Length * 6f / guiScale,
                    y + ItemSlot.ITEMSIZE - 13f / guiScale - ItemSlot.PADDING));
        }
    }
}
