using System.Numerics;
using System.Runtime.InteropServices;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.main;
using BlockGame.util;
using BlockGame.util.font;
using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.item;
using FontStashSharp;
using FontStashSharp.RichText;
using Molten;
using Silk.NET.OpenGL.Legacy;
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
    public DynamicSpriteFont guiFontThinl = null!;

    public Rectangle scrollbarRect = new(199, 0, 6, 20);

    public static GUI instance;
    private StreamingVAO<BlockVertexTinted> buffer;
    private Matrix4x4 ortho;

    private List<BlockVertexTinted> guiBlock;
    private int uMVP;
    private int blockTexture = 0;

    private Vector2 backgroundScrollOffset = Vector2.Zero;
    private static readonly Color bgGray = new Color(192, 192, 192, 255);
    private static readonly Color skyc = Color.CornflowerBlue;

    public static bool WIREFRAME = false;
    public static bool SHOW_GUI_BOUNDS = false;

    /** pixels per second */
    private const float SCROLL_SPEED = 32.0f;

    // dragging state
    public bool draggingBG = false;
    public Vector2 dragPos = Vector2.Zero;
    public Vector2 dragVel = Vector2.Zero;
    private const float DRAG_DAMPING = 0.97f; // per tick
    private const float DRAG_EPSILON = 0.1f;

    public const int heartX = 224;
    public const int heartY = 0;
    public const int heartNoX = 213;
    public const int heartNoY = 0;
    public const int heartW = 9;
    public const int heartH = 9;

    public GUI() {
        //shader = new InstantShader(GL, "shaders/batch.vert", "shaders/batch.frag");
        tb = Game.graphics.mainBatch;
        immediatetb = Game.graphics.immediateBatch;
        guiTexture = new BTexture2D("textures/gui.png");
        colourTexture = new BTexture2D("textures/debug.png");
        guiTexture.reload();
        colourTexture.reload();
        instance = this;
        guiBlockShader = new Shader(Game.GL, nameof(guiBlockShader), "shaders/ui/simpleBlock.vert",
            "shaders/ui/simpleBlock.frag");
        buffer = new StreamingVAO<BlockVertexTinted>();
        buffer.bind();
        buffer.setSize(Face.MAX_FACES * 4);
        // GD, 4 * Face.MAX_FACES, 6 * Face.MAX_FACES, ElementType.UnsignedShort, BufferUsage.StreamDraw
        guiBlock = [];

        uMVP = guiBlockShader.getUniformLocation("uMVP");
        blockTexture = guiBlockShader.getUniformLocation("blockTexture");
    }

    public void loadFont(int size) {
        guiFont = Game.fontLoader.fontSystem.GetFont(size);
        guiFontThin = Game.fontLoader.fontSystemThin.GetFont(size);
        guiFontThinl = Game.fontLoader.fontSystemThinl.GetFont(size);
    }


    public void resize(Vector2I size) {
        refreshMatrix(size);

        uiCentreX = size.X / 2 / guiScale;
        uiCentreY = size.Y / 2 / guiScale;

        uiWidth = size.X / guiScale;
        uiHeight = size.Y / guiScale;

        // handle guiscale
        var blockSize = 16d;
        blockSize *= guiScale * 2;
    }

    public void refreshMatrix(Vector2I size) {
        var near = Settings.instance.reverseZ ? 10000f : -10000f;
        var far = Settings.instance.reverseZ ? -10000f : 10000f;
        ortho = Matrix4x4.CreateOrthographicOffCenterLeftHanded(0, size.X, size.Y, 0, near, far);
    }

    public void update(double dt) {
        updateBackgroundScroll(dt);
    }

    // Call this in your update method
    public void updateBackgroundScroll(double dt) {
        // always auto-scroll vertically
        backgroundScrollOffset.Y += (float)(SCROLL_SPEED * dt);

        if (!draggingBG) {
            // apply velocity with damping
            backgroundScrollOffset += dragVel * (float)dt;

            // decay velocity exponentially
            dragVel *= DRAG_DAMPING;

            // stop if velocity too small
            if (dragVel.LengthSquared() < DRAG_EPSILON * DRAG_EPSILON) {
                dragVel = Vector2.Zero;
            }
        }
    }

    public void startDrag(Vector2 mousePos) {
        draggingBG = true;
        dragPos = mousePos;
        dragVel = Vector2.Zero;
    }

    public void updateDrag(Vector2 mousePos, double dt) {
        if (!draggingBG) return;

        var d = mousePos - dragPos;
        backgroundScrollOffset.Y -= d.Y;

        if (dt > 0) {
            dragVel.Y = -d.Y / (float)dt;
        }

        dragPos = mousePos;
    }

    public void endDrag() {
        draggingBG = false;
        // velocity is already set from last updateDrag
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

    /** Draw a colored tint over a slot (for status indication) */
    public void drawSlotTint(Vector2 pos, Rectangle slotRect, Color tint) {
        var dest = new RectangleF(pos.X + slotRect.X + ItemSlot.PADDING, pos.Y + slotRect.Y + ItemSlot.PADDING,
            slotRect.Width - ItemSlot.PADDING * 2, slotRect.Height - ItemSlot.PADDING * 2);
        drawUI(colourTexture, dest, new Rectangle(0, 0, 1, 1), tint);
    }

    public void drawItem(ItemSlot slot, Vector2 pos) {
        var stack = slot.getStack();

        if (stack == null || stack.id == Item.AIR.id) {
            return;
        }

        var itemPos = slot.itemPos;
        var shiny = 0f;
        if (slot.inventory is PlayerInventory sh) {
            shiny = sh.shiny[slot.index];
        }

        shiny *= shiny;

        // if block
        var item = Item.get(stack.id);
        if (item.isBlock()) {
            var blockID = item.getBlockID();
            if (Block.renderItemLike[blockID]) {
                drawItemSprite(item, stack, pos.X + itemPos.X, pos.Y + itemPos.Y, shiny);
            }
            else {
                Game.gui.drawBlockUI(item.getBlock(), (int)(pos.X + itemPos.X), (int)(pos.Y + itemPos.Y),
                    ItemSlot.ITEMSIZE, (byte)stack.metadata, 0, shiny);
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
            drawItemSprite(item, stack, pos.X + itemPos.X, pos.Y + itemPos.Y, shiny);
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
            }
            else {
                Game.gui.drawBlockUI(item.getBlock(), itemPos.X, itemPos.Y, ItemSlot.ITEMSIZE, (byte)stack.metadata);
                drawQuantityText(stack, itemPos.X, itemPos.Y);
            }
        }
        else if (item.isItem()) {
            drawItemSprite(item, stack, itemPos.X, itemPos.Y);
        }
    }

    public void drawCursorItem(ItemStack? cursorItem, Vector2 mousePos) {
        if (cursorItem == null || cursorItem.id == Item.AIR.id) {
            return;
        }

        var item = Item.get(cursorItem.id);
        var pos = s2u(mousePos);
        // offset by half item size so it's centered on cursor
        var drawX = pos.X - ItemSlot.ITEMSIZE / 2f;
        var drawY = pos.Y - ItemSlot.ITEMSIZE / 2f;

        // this is the part where we flush everything so the ordering works
        tb.End();
        tb.Begin();

        if (item.isBlock()) {
            var blockID = item.getBlockID();
            if (Block.renderItemLike[blockID]) {
                drawItemSprite(item, cursorItem, drawX, drawY);
            }
            else {
                // render cursor item closer to camera to avoid z-fighting with gui blocks
                Game.gui.drawBlockUI(item.getBlock(), (int)drawX, (int)drawY, ItemSlot.ITEMSIZE,
                    (byte)cursorItem.metadata, -1000);

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
        var texCoords = UVPair.texCoords(block.uvs[0]);
        var texCoordsMax = UVPair.texCoords(block.uvs[0] + 1);

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
        var xCount = (int)Math.Ceiling(Game.width / blockSize) + 2;
        var yCount = (int)Math.Ceiling(Game.height / blockSize) + 2;

        // Get starting world coordinates
        // Start 10 blocks above the grass layer for drag headroom
        int startX = (int)Math.Floor(backgroundScrollOffset.X / blockSize);
        int startY = (int)Math.Floor(backgroundScrollOffset.Y / blockSize) - 3;

        // Calculate fractional offset for smooth scrolling
        float offsetX = ((backgroundScrollOffset.X % blockSize) + blockSize) % blockSize;
        float offsetY = ((backgroundScrollOffset.Y % blockSize) + blockSize) % blockSize;

        Span<ushort> ores = [
             Block.AMBER_ORE.id,  Block.CINNABAR_ORE.id,  Block.EMERALD_ORE.id,  Block.DIAMOND_ORE.id,  Block.TITANIUM_ORE.id,
            Block.AMETHYST_ORE.id
        ];

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
                        new VertexColorTexture(new Vector3(tileLeft + blockSize, tileTop, 0), bgGray * skyc,
                            new Vector2(1, 0)),
                        new VertexColorTexture(new Vector3(tileLeft + blockSize, tileTop + blockSize, 0), bgGray * skyc,
                            new Vector2(1, 1)),
                        new VertexColorTexture(new Vector3(tileLeft, tileTop + blockSize, 0), bgGray * skyc,
                            new Vector2(0, 1)));

                    continue;
                }

                // If it's in the stone layer, check for ores
                if (blockId == Block.STONE.id && shouldPlaceOre(worldX, worldY)) {
                    int oreIndex = XHash.hashRange(worldX, worldY, ores.Length);
                    blockId = ores[oreIndex];
                }

                var block = Block.get(blockId);
                var texCoords_ = UVPair.texCoords(block.uvs[0]);
                var texCoordsMax_ = UVPair.texCoords(block.uvs[0] + 1);
                var texCoords = new Vector2(texCoords_.X, texCoords_.Y);
                var texCoordsMax = new Vector2(texCoordsMax_.X, texCoordsMax_.Y);

                tb.DrawRaw(Game.textures.blockTexture,
                    new VertexColorTexture(new Vector3(tileLeft, tileTop, 0), bgGray, texCoords),
                    new VertexColorTexture(new Vector3(tileLeft + blockSize, tileTop, 0), bgGray,
                        new Vector2(texCoordsMax.X, texCoords.Y)),
                    new VertexColorTexture(new Vector3(tileLeft + blockSize, tileTop + blockSize, 0), bgGray,
                        texCoordsMax),
                    new VertexColorTexture(new Vector3(tileLeft, tileTop + blockSize, 0), bgGray,
                        new Vector2(texCoords.X, texCoordsMax.Y)));
            }
        }
    }

    private static bool shouldPlaceOre(int worldX, int worldZ) {
        return XHash.hashRange(worldX, worldZ, 20) == 0; // 5% chance
    }

    // determine block type based on depth (y coordinate)
    private static ushort getBlockTypeForDepth(int worldX, int worldY) {
        // air above surface (y < 0)
        if (worldY < 0) {
            return  Block.AIR.id;
        }

        // surface layer (y = 0) is grass
        if (worldY == 0) {
            return  Block.GRASS.id;
        }

        // dirt percentage increases linearly from depth 1 to 7
        if (worldY >= 1 && worldY <= 7) {
            // calculate dirt percentage: 100% at depth 1, decreasing to ~15% at depth 7
            float dirtPercentage = 1.0f - ((worldY - 1) / 6.0f * 0.85f);
            float randomValue = XHash.hashFloat(worldX, worldY);

            if (randomValue < dirtPercentage) {
                return  Block.DIRT.id;
            }
        }

        // what isn't dirt is stone
        return  Block.STONE.id;
    }


    public void draw(BTexture2D texture, Vector2 position, float scale = 1f, Rectangle? source = null,
        Color color = default, Vector2 origin = default, float depth = 0f) {
        tb.Draw(texture, position, source, color == default ? Color.White : color,
            scale != 1f ? guiScale * scale : guiScale, 0f, origin, depth);
    }

    public void draw(BTexture2D texture, RectangleF dest, Rectangle? source = null, Color color = default) {
        tb.Draw(texture, dest, source, color == default ? Color.White : color);
    }

    public void drawGradientVertical(BTexture2D texture, RectangleF dest, Color topColor, Color bottomColor,
        Rectangle? source = null) {
        var left = dest.X;
        var right = dest.X + dest.Width;
        var top = dest.Y;
        var bottom = dest.Y + dest.Height;

        var texCoords = source.HasValue
            ? new RectangleF(source.Value.X, source.Value.Y, source.Value.Width, source.Value.Height)
            : new RectangleF(0, 0, 1, 1);

        tb.DrawRaw(texture,
            new VertexColorTexture(new Vector3(left, top, 0), topColor, new Vector2(texCoords.X, texCoords.Y)),
            new VertexColorTexture(new Vector3(right, top, 0), topColor, new Vector2(texCoords.Right, texCoords.Y)),
            new VertexColorTexture(new Vector3(right, bottom, 0), bottomColor,
                new Vector2(texCoords.Right, texCoords.Bottom)),
            new VertexColorTexture(new Vector3(left, bottom, 0), bottomColor,
                new Vector2(texCoords.X, texCoords.Bottom)));
    }

    public void drawImmediate(BTexture2D texture, Vector2 position, Rectangle? source = null,
        Color color = default, Vector2 origin = default, float depth = 0f) {
        immediatetb.Draw(texture, position, source, color == default ? Color.White : color, guiScale, 0f, origin,
            depth);
    }

    public void drawUI(BTexture2D texture, Vector2 position, Rectangle? source = null,
        Color color = default, Vector2 origin = default, float depth = 0f) {
        tb.Draw(texture, position * guiScale, source, color == default ? Color.White : color, guiScale, 0f, origin,
            depth);
    }

    public void drawUI(BTexture2D texture, RectangleF dest, Rectangle? source = null, Color color = default,
        float depth = 0f) {
        tb.Draw(texture, new RectangleF(
            (int)(dest.X * guiScale),
            (int)(dest.Y * guiScale),
            (int)(dest.Width * guiScale),
            (int)(dest.Height * guiScale)), source, color == default ? Color.White : color, depth);
    }

    public void drawUIImmediate(BTexture2D texture, Vector2 position, Rectangle? source = null,
        Color color = default, Vector2 origin = default, float depth = 0f) {
        immediatetb.Draw(texture, position * guiScale, source, color == default ? Color.White : color, guiScale, 0f,
            origin, depth);
    }

    public void drawUIImmediate(BTexture2D texture, RectangleF position, Rectangle? source = null) {
        var np = new RectangleF(
            position.X * guiScale,
            position.Y * guiScale,
            position.Width * guiScale,
            position.Height * guiScale);
        immediatetb.Draw(texture, np, source, Color.White);
    }


    /** Text rendering is at half gui scale, which is normally 2x (compared to the guiscale 4x) */
    public static int TEXTSCALE => guiScale / 2;

    public static Vector2 TEXTSCALEV => new(TEXTSCALE, TEXTSCALE);

    // maybe some day we will have common logic for these functions if the number of permutations grow in size. BUT NOT TODAY

    public void drawString(ReadOnlySpan<char> text, Vector2 position, Color color = default) {
        DrawString(text, position, color == default ? Color.White : color, new Vector2(TEXTSCALE), default);
    }

    public void drawStringSmall(ReadOnlySpan<char> text, Vector2 position, Color color = default) {
        DrawStringThin(text, position, color == default ? Color.White : color, new Vector2(TEXTSCALE / 2f), default);
    }

    public void drawStringThin(ReadOnlySpan<char> text, Vector2 position, Color color = default) {
        DrawStringThin(text, position, color == default ? Color.White : color, new Vector2(TEXTSCALE), default);
    }

    public void drawRString(RichTextLayout layout, Vector2 position, TextHorizontalAlignment alignment,
        Color color = default) {
        DrawRString(layout, position, color == default ? Color.White : color, new Vector2(TEXTSCALE), alignment);
    }

    public void drawStringUI(ReadOnlySpan<char> text, Vector2 position, Color color = default) {
        DrawString(text, position * guiScale, color == default ? Color.White : color, new Vector2(TEXTSCALE),
            default);
    }

    public void drawStringUI(ReadOnlySpan<char> text, Vector2 position, Color colour, Vector2 scale) {
        DrawString(text, position * guiScale, colour == default ? Color.White : colour, TEXTSCALE * scale, default);
    }

    public void drawStringUIThin(ReadOnlySpan<char> text, Vector2 position, Color colour = default) {
        DrawStringThin(text, position * guiScale, colour == default ? Color.White : colour, new Vector2(TEXTSCALE),
            default);
    }

    public void drawStringUIThin(ReadOnlySpan<char> text, Vector2 position, Color colour, Vector2 scale) {
        DrawStringThin(text, position * guiScale, colour == default ? Color.White : colour, TEXTSCALE * scale, default);
    }


    /// <summary>
    /// Rotation Y = yaw (horizontal rotation)
    /// Rotation X = pitch (banking down or up)
    /// Rotation Z = roll (tilt sideways)
    /// </summary>
    public void drawString3D(ReadOnlySpan<char> text, Vector3 position, Vector3 rotation, float scale = 1f,
        Color colour = default) {
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
        Color colour = default) {
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

    public void drawStringCentred(ReadOnlySpan<char> text, Vector2 position, Color color = default) {
        var offsetX = guiFont.MeasureString(text, TEXTSCALEV).X / 2;
        var offsetY = guiFont.MeasureString(text, TEXTSCALEV).Y / 2;
        DrawString(text, new Vector2(position.X - offsetX, position.Y - offsetY),
            color == default ? Color.White : color, new Vector2(TEXTSCALE), default);
    }

    public void drawStringCentredUI(ReadOnlySpan<char> text, Vector2 position, Color color = default) {
        var offsetX = guiFont.MeasureString(text, TEXTSCALEV).X / 2;
        var offsetY = guiFont.MeasureString(text, TEXTSCALEV).Y / 2;
        DrawString(text, new Vector2(position.X * guiScale - offsetX, position.Y * guiScale - offsetY),
            color == default ? Color.White : color, new Vector2(TEXTSCALE), default);
    }

    // some day we'll have a better API, but not this day
    public void drawStringShadowed(ReadOnlySpan<char> text, Vector2 position, Color color = default) {
        DrawString(text, position + new Vector2(1, 1), Color.DimGray, new Vector2(TEXTSCALE), default);
        DrawString(text, position, color == default ? Color.White : color, new Vector2(TEXTSCALE), default);
    }

    public void drawStringShadowedUI(ReadOnlySpan<char> text, Vector2 position, Color color = default) {
        DrawString(text, position * guiScale + new Vector2(1, 1), Color.DimGray, new Vector2(TEXTSCALE), default);
        DrawString(text, position * guiScale, color == default ? Color.White : color, new Vector2(TEXTSCALE),
            default);
    }

    public void drawStringCentredShadowed(ReadOnlySpan<char> text, Vector2 position, Color color = default) {
        var offsetX = guiFont.MeasureString(text, TEXTSCALEV).X / 2;
        var offsetY = guiFont.MeasureString(text, TEXTSCALEV).Y / 2;
        DrawString(text, new Vector2(position.X - offsetX, position.Y - offsetY) + new Vector2(1, 1), Color.DimGray,
            new Vector2(TEXTSCALE), default);
        DrawString(text, new Vector2(position.X - offsetX, position.Y - offsetY),
            color == default ? Color.White : color, new Vector2(TEXTSCALE), default);
    }

    public void drawStringCentredShadowedUI(ReadOnlySpan<char> text, Vector2 position, Color color = default) {
        var offsetX = guiFont.MeasureString(text, TEXTSCALEV).X / 2;
        var offsetY = guiFont.MeasureString(text, TEXTSCALEV).Y / 2;
        DrawString(text,
            new Vector2(position.X * guiScale - offsetX, position.Y * guiScale - offsetY) + new Vector2(1, 1),
            Color.DimGray, new Vector2(TEXTSCALE), default);
        DrawString(text, new Vector2(position.X * guiScale - offsetX, position.Y * guiScale - offsetY),
            color == default ? Color.White : color, new Vector2(TEXTSCALE), default);
    }

    protected void DrawString(ReadOnlySpan<char> text, Vector2 position, Color colour) {
        guiFont.DrawText(Game.fontLoader.renderer, text, position, colour.toFS());
    }

    protected void DrawString(ReadOnlySpan<char> text, Vector2 position, Color colour, Vector2 scale,
        Vector2 offset) {
        guiFont.DrawText(Game.fontLoader.renderer, text, position, colour.toFS(), 0, offset, scale);
    }

    protected void DrawString(ReadOnlySpan<char> text, Vector2 position, Color colour, Vector2 scale, float rotation,
        Vector2 offset) {
        guiFont.DrawText(Game.fontLoader.renderer, text, position, colour.toFS(), rotation, offset, scale);
    }

    protected void DrawStringThin(ReadOnlySpan<char> text, Vector2 position, Color colour, Vector2 scale,
        Vector2 offset) {
        var aspectScale = new Vector2(scale.X * Game.fontLoader.thinFontAspectRatio, scale.Y);
        guiFontThin.DrawText(Game.fontLoader.renderer, text, position, colour.toFS(), 0, offset, aspectScale);
    }

    protected void DrawRString(RichTextLayout layout, Vector2 position, Color colour, Vector2 scale,
        TextHorizontalAlignment alignment) {
        if (layout.Font == guiFontThin) {
            // If the layout uses the thin font, we need to adjust the scale for the aspect ratio
            scale = new Vector2(scale.X * Game.fontLoader.thinFontAspectRatio, scale.Y);
        }

        layout.Draw(Game.fontLoader.renderer, position, colour.toFS(), 0, new Vector2(0), scale, 0f, alignment);
    }

    // selector versions

    public void drawStringShadowed(string text, Vector2 position, bool thin) {
        if (thin) {
            DrawStringThin(text, position + new Vector2(1, 1), Color.DimGray, new Vector2(TEXTSCALE), default);
            DrawStringThin(text, position, Color.White, new Vector2(TEXTSCALE), default);
        }
        else {
            DrawString(text, position + new Vector2(1, 1), Color.DimGray, new Vector2(TEXTSCALE), default);
            DrawString(text, position, Color.White, new Vector2(TEXTSCALE), default);
        }
    }

    public void drawString(string text, Vector2 position, bool thin) {
        if (thin) {
            DrawStringThin(text, position, Color.White, new Vector2(TEXTSCALE), default);
        }
        else {
            DrawString(text, position, Color.White, new Vector2(TEXTSCALE), default);
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
        return guiFont.MeasureString(text, new Vector2((float)TEXTSCALE / guiScale));
    }

    public Vector2 measureStringUIThin(ReadOnlySpan<char> text) {
        var measurement = guiFontThin.MeasureString(text, new Vector2((float)TEXTSCALE / guiScale));
        return new Vector2(measurement.X * Game.fontLoader.thinFontAspectRatio, measurement.Y);
    }

    public Vector2 measureStringUICentred(ReadOnlySpan<char> text) {
        return guiFont.MeasureString(text) / 2f;
    }

    public Vector2 measureStringUI(ReadOnlySpan<char> text, bool thin) {
        if (thin) {
            var measurement = guiFontThin.MeasureString(text, new Vector2((float)TEXTSCALE / guiScale));
            return new Vector2(measurement.X * Game.fontLoader.thinFontAspectRatio, measurement.Y);
        }

        return guiFont.MeasureString(text, new Vector2((float)TEXTSCALE / guiScale));
    }

    public void drawWireframe(Rectangle bounds, Color col) {
        // draw an empty rectangle with the given color

        tb.DrawRaw(colourTexture,
            new VertexColorTexture(new Vector3(bounds.X, bounds.Y, 0), col, new Vector2(0, 0)),
            new VertexColorTexture(new Vector3(bounds.X + bounds.Width, bounds.Y, 0), col, new Vector2(1, 0)),
            new VertexColorTexture(new Vector3(bounds.X + bounds.Width, bounds.Y + bounds.Height, 0), col,
                new Vector2(1, 1)),
            new VertexColorTexture(new Vector3(bounds.X, bounds.Y + bounds.Height, 0), col, new Vector2(0, 1)));
    }

    public void drawBlock(Block block, int x, int y, int size, byte metadata = 0, float depth = 0, float shiny = 0) {
        //Console.Out.WriteLine(Game.GL.GetBoolean(GetPName.DepthTest));
        Game.GL.Enable(EnableCap.DepthTest);

        var idt = Game.graphics.idt;

        Game.blockRenderer.setupStandalone();
        Game.blockRenderer.renderBlock(block, metadata, Vector3I.Zero, guiBlock,
            lightOverride: 255, cullFaces: false);

        var mat = Game.graphics.model;

        mat.push();
        mat.loadIdentity();


        // view transformation - camera position
        //var camPos = new Vector3(1, 2 / 3f, 1);
        // fuck this, we rotate it manually! :trolley:
        //var viewMatrix = Matrix4x4.CreateLookAt(camPos, new Vector3(0, 0, 0), new Vector3(0, 1, 0));
        var viewMatrix = Matrix4x4.Identity;
        //mat.multiply(viewMatrix);
        mat.translate(x, y, depth);
        // +1 because it doesn't touch the bottom!!
        // add the padding

        mat.translate(2.5f * guiScale, 2.5f * guiScale, 0);

        // we're now in screen space

        // scale up to proper scale
        //mat.scale(size * guiScale * (5 / 8f));
        //mat.scale(size * guiScale * (2 / 3f));
        mat.scale(size * guiScale * (32 / 48f));


        // "shrink"
        //mat.scale(2 / 3f);

        // translate into the correct spot
        mat.translate(0f / 6f, 11f / 12f, 0);

        var yScale = 1.0f + shiny;
        mat.scale(1, yScale, 1);

        mat.translate(0.5f, 0.5f, 0.5f);

        // rotate "down"
        // we cheat a bit because this one makes the block ACTUALLY fit into the gui perfectly
        mat.rotate(24, 1, 0, 0);
        //mat.rotate(30, 1, 0, 0);

        mat.rotate(270 + 45, 0, 1, 0);
        //mat.rotate(45, 0, 0, 0);

        mat.translate(-0.5f, -0.5f, -0.5f);


        // model transformations
        //mat.translate(5 / 6f, 5 / 6f, 0);
        mat.scale(1, -1, 1);

        // test point debug
        //Console.Out.WriteLine($"Test point: {Vector3.Transform(new Vector3(1, 1, 1), mat.top)}");

        buffer.bind();
        Game.renderer.bindQuad();

        Game.graphics.instantTextureShader.use();

        Game.graphics.tex(0, Game.textures.blockTexture);
        var sp = CollectionsMarshal.AsSpan(guiBlock);
        buffer.upload(sp);

        idt.model(mat);
        idt.view(viewMatrix);
        idt.proj(ortho);
        //idt.proj(Matrix4x4.CreateOrthographicOffCenterLeftHanded(-0.75f, 0.75f, 0.75f, -0.75f, -10, 10));
        idt.applyMat();
        //Game.graphics.setViewport(x, Game.height - y - sSize, sSize, sSize);

        // use shader
        Game.graphics.instantTextureShader.use();

        buffer.render();

        // restore matrix stacks
        mat.pop();

        Game.GL.Disable(EnableCap.DepthTest);
    }

    public void drawBlockUI(Block block, int x, int y, int size, byte metadata = 0, float depth = 0, float shiny = 0) {
        drawBlock(block, x * guiScale, y * guiScale, size, metadata, depth, shiny);
    }

    private void drawItemSprite(Item item, ItemStack stack, float x, float y, float shiny = 0) {
        // apply Y-stretch for shiny items
        var yScale = 1.0f + shiny;
        var yOffset = (ItemSlot.ITEMSIZE * (yScale - 1.0f)) / 2f; // centre the stretched item

        var destRect = new RectangleF(x, y - yOffset, ItemSlot.ITEMSIZE, ItemSlot.ITEMSIZE * yScale);

        if (item.isBlock() && Block.renderItemLike[item.getBlockID()]) {
            // get texture directly from block
            var block = item.getBlock();
            var texUV = block.getTexture(0, (byte)stack.metadata);
            var uv = UVPair.texCoordsI(texUV);
            var sourceRect = new Rectangle((int)uv.X, (int)uv.Y, UVPair.ATLASSIZE, UVPair.ATLASSIZE);
            drawUI(Game.textures.blockTexture, destRect, sourceRect);
        }
        else {
            // normal item rendering
            var texUV = item.getTexture(stack);
            var uv = UVPair.texCoordsiI(texUV);
            var sourceRect = new Rectangle((int)uv.X, (int)uv.Y, UVPair.ATLASSIZE, UVPair.ATLASSIZE);
            drawUI(Game.textures.itemTexture, destRect, sourceRect);
        }

        var max = Item.durability[stack.id];
        if (max > 0 && stack.metadata > 0) {
            var dmg = (float)stack.metadata / max;
            var rem = 1f - dmg;

            // HSV
            var hue = rem * 120f;
            const float c = 255f;
            var xx = c * (1 - float.Abs(hue / 60f % 2 - 1));
            var bc = hue < 60f
                ? new Color((byte)c, (byte)xx, (byte)0)
                : new Color((byte)xx, (byte)c, (byte)0);

            var bg = new RectangleF(x, y + ItemSlot.ITEMSIZE - 1f, ItemSlot.ITEMSIZE, 1f);
            drawUI(colourTexture, bg, new Rectangle(0, 0, 1, 1), new Color(64, 64, 64, 255));

            // draw durability bar
            var width = ItemSlot.ITEMSIZE * rem;
            var bar = new RectangleF(x, y + ItemSlot.ITEMSIZE - 1f, width, 1f);
            drawUI(colourTexture, bar, new Rectangle(0, 0, 1, 1), bc);
        }

        drawQuantityText(stack, x, y);
    }

    private static void drawQuantityText(ItemStack stack, float x, float y) {
        if (stack.quantity > 1) {
            var s = stack.quantity.ToString();
            Game.gui.drawStringUIThin(s,
                new Vector2(x + ItemSlot.ITEMSIZE - ItemSlot.PADDING - s.Length * 6f / guiScale,
                    y + ItemSlot.ITEMSIZE - 13f / guiScale - ItemSlot.PADDING));
        }
    }

    public void drawGUIBounds() {
        if (!SHOW_GUI_BOUNDS) return;

        // show the virtual GUI coordinate space (minimum 360x270 starting from 0,0)
        const float minVirtualWidth = 360f;
        const float minVirtualHeight = 240f;

        var w = minVirtualWidth * guiScale;
        var h = minVirtualHeight * guiScale;

        const float lineWidth = 2f;
        var col = Color.Cyan;

        // top
        tb.DrawRaw(colourTexture,
            new VertexColorTexture(new Vector3(0, 0, 0), col, new Vector2(0, 0)),
            new VertexColorTexture(new Vector3(w, 0, 0), col, new Vector2(1, 0)),
            new VertexColorTexture(new Vector3(w, lineWidth, 0), col, new Vector2(1, 1)),
            new VertexColorTexture(new Vector3(0, lineWidth, 0), col, new Vector2(0, 1)));

        // bottom
        tb.DrawRaw(colourTexture,
            new VertexColorTexture(new Vector3(0, h - lineWidth, 0), col, new Vector2(0, 0)),
            new VertexColorTexture(new Vector3(w, h - lineWidth, 0), col, new Vector2(1, 0)),
            new VertexColorTexture(new Vector3(w, h, 0), col, new Vector2(1, 1)),
            new VertexColorTexture(new Vector3(0, h, 0), col, new Vector2(0, 1)));

        // left
        tb.DrawRaw(colourTexture,
            new VertexColorTexture(new Vector3(0, 0, 0), col, new Vector2(0, 0)),
            new VertexColorTexture(new Vector3(lineWidth, 0, 0), col, new Vector2(1, 0)),
            new VertexColorTexture(new Vector3(lineWidth, h, 0), col, new Vector2(1, 1)),
            new VertexColorTexture(new Vector3(0, h, 0), col, new Vector2(0, 1)));

        // right
        tb.DrawRaw(colourTexture,
            new VertexColorTexture(new Vector3(w - lineWidth, 0, 0), col, new Vector2(0, 0)),
            new VertexColorTexture(new Vector3(w, 0, 0), col, new Vector2(1, 0)),
            new VertexColorTexture(new Vector3(w, h, 0), col, new Vector2(1, 1)),
            new VertexColorTexture(new Vector3(w - lineWidth, h, 0), col, new Vector2(0, 1)));
    }

    /**
     * Draw a vertical scrollbar using 3-patch rendering (top, middle, bottom).
     * x, y, height in GUI coords. scrollProgress 0-1.
     */
    public void drawScrollbarUI(int x, int y, int height, float scrollProgress, float viewportRatio) {
        const int WIDTH = 6;

        // draw track (dimmed)
        draw3PatchVerticalUI(x, y, WIDTH, height, scrollbarRect, Color.DimGray);

        // calculate thumb size and position
        var thumbHeight = Math.Max(10, (int)(height * viewportRatio));
        var thumbY = y + (int)((height - thumbHeight) * scrollProgress);

        // draw thumb (normal)
        draw3PatchVerticalUI(x, thumbY, WIDTH, thumbHeight, scrollbarRect);
    }

    /**
     * Draw a vertical 3-patch sprite (top 3px, stretched middle 14px, bottom 3px).
     * x, y, width, height in GUI coords. This function is hilariously hardcoded but good enough for now.
     */
    private void draw3PatchVerticalUI(int x, int y, int width, int height, Rectangle sourceRect, Color? tint = null) {
        var color = tint ?? Color.White;

        // top cap (first 3 pixels)
        drawUI(guiTexture,
            new Rectangle(x, y, width, 3),
            new Rectangle(sourceRect.X, sourceRect.Y, width, 3), color);

        // middle section (stretched)
        if (height > 6) {
            drawUI(guiTexture,
                new Rectangle(x, y + 3, width, height - 6),
                new Rectangle(sourceRect.X, sourceRect.Y + 3, width, 14), color);
        }

        // bottom cap (last 3 pixels)
        drawUI(guiTexture,
            new Rectangle(x, y + height - 3, width, 3),
            new Rectangle(sourceRect.X, sourceRect.Y + 17, width, 3), color);
    }

    /** 9-patch with separate border sizes per side
     * Less hilariously hardcoded:tm:
     */
    public void draw9PatchUI(int x, int y, int width, int height, Rectangle src, int borderL, int borderR, int borderT,
        int borderB, Color? tint = null) {
        var c = tint ?? Color.White;

        // corners
        drawUI(guiTexture, new Rectangle(x, y, borderL, borderT),
            new Rectangle(src.X, src.Y, borderL, borderT), c);
        drawUI(guiTexture, new Rectangle(x + width - borderR, y, borderR, borderT),
            new Rectangle(src.X + src.Width - borderR, src.Y, borderR, borderT), c);
        drawUI(guiTexture, new Rectangle(x, y + height - borderB, borderL, borderB),
            new Rectangle(src.X, src.Y + src.Height - borderB, borderL, borderB), c);
        drawUI(guiTexture, new Rectangle(x + width - borderR, y + height - borderB, borderR, borderB),
            new Rectangle(src.X + src.Width - borderR, src.Y + src.Height - borderB, borderR, borderB), c);

        // edges
        var midW = width - borderL - borderR;
        var midH = height - borderT - borderB;
        var srcMidW = src.Width - borderL - borderR;
        var srcMidH = src.Height - borderT - borderB;

        if (midW > 0) {
            // top
            drawUI(guiTexture, new Rectangle(x + borderL, y, midW, borderT),
                new Rectangle(src.X + borderL, src.Y, srcMidW, borderT), c);
            // bottom
            drawUI(guiTexture, new Rectangle(x + borderL, y + height - borderB, midW, borderB),
                new Rectangle(src.X + borderL, src.Y + src.Height - borderB, srcMidW, borderB), c);
        }

        if (midH > 0) {
            // left
            drawUI(guiTexture, new Rectangle(x, y + borderT, borderL, midH),
                new Rectangle(src.X, src.Y + borderT, borderL, srcMidH), c);
            // right
            drawUI(guiTexture, new Rectangle(x + width - borderR, y + borderT, borderR, midH),
                new Rectangle(src.X + src.Width - borderR, src.Y + borderT, borderR, srcMidH), c);
        }

        // centre
        if (midW > 0 && midH > 0) {
            drawUI(guiTexture, new Rectangle(x + borderL, y + borderT, midW, midH),
                new Rectangle(src.X + borderL, src.Y + borderT, srcMidW, srcMidH), c);
        }
    }

    /** Draw a hollow border (screen coords) */
    public void drawBorder(int x, int y, int width, int height, int borderWidth, Color color) {
        // top
        draw(colourTexture, new RectangleF(x, y, width, borderWidth), null, color);
        // bottom
        draw(colourTexture, new RectangleF(x, y + height - borderWidth, width, borderWidth), null, color);
        // left
        draw(colourTexture, new RectangleF(x, y, borderWidth, height), null, color);
        // right
        draw(colourTexture, new RectangleF(x + width - borderWidth, y, borderWidth, height), null, color);
    }

    /** Draw a hollow border (GUI coords) */
    public void drawBorderUI(int x, int y, int width, int height, int borderWidth, Color color) {
        var bw = borderWidth * guiScale;
        // top
        drawUI(colourTexture, new Rectangle(x, y, width, borderWidth), null, color);
        // bottom
        drawUI(colourTexture, new Rectangle(x, y + height - borderWidth, width, borderWidth), null, color);
        // left
        drawUI(colourTexture, new Rectangle(x, y, borderWidth, height), null, color);
        // right
        drawUI(colourTexture, new Rectangle(x + width - borderWidth, y, borderWidth, height), null, color);
    }
}