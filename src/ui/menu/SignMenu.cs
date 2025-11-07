using System.Numerics;
using BlockGame.main;
using BlockGame.ui.element;
using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.block.entity;
using Molten;
using Silk.NET.Input;
using Button = BlockGame.ui.element.Button;

namespace BlockGame.ui.menu;

public class SignMenu : Menu {
    private readonly SignBlockEntity signEntity;

    private int currentLine = 0;
    private int cursorPos = 0;
    private int cursorBlink = 0;

    // these constants are kind of brittle af but you can trial&error them in 5 minutes if we change the sign size so whatevs
    private const int WORLD_TO_GUI = 8;
    private const int SIGN_HEIGHT_WORLD = 10;

    // derived constants in GUI pixels
    private const int SIGN_WIDTH = SignBlockEntity.SIGN_WIDTH_WORLD * WORLD_TO_GUI;
    private const int SIGN_HEIGHT = SIGN_HEIGHT_WORLD * WORLD_TO_GUI;
    private const int MAX_LINE_WIDTH_PX = SignBlockEntity.MAX_TEXT_WIDTH_WORLD * WORLD_TO_GUI;

    public override bool isModal() => true;

    public SignMenu(SignBlockEntity entity) {
        signEntity = entity;

        currentLine = 0;
        cursorPos = signEntity.lines[0].Length;

        // done button
        var doneButton = new Button(this, "done", false, "Done");
        doneButton.setPosition(new Vector2I(0, 60));
        doneButton.centreContents();
        doneButton.clicked += _ => {
            // changes are already in signEntity.lines, just close
            Game.world.inMenu = false;
            Screen.GAME_SCREEN.backToGame();
        };
        addElement(doneButton);
    }

    public override void update(double dt) {

        Screen.GAME_SCREEN.INGAME_MENU.update(dt);

        base.update(dt);
        cursorBlink += 1;
    }

    public override void draw() {

        Screen.GAME_SCREEN.INGAME_MENU.draw();

        // draw background (just a fullscreen grey overlay)
        Game.gui.draw(Game.gui.colourTexture, new RectangleF(0, 0, Game.width, Game.height), null, new Color(0, 0, 0, 150));

        base.draw();

        const string title = "Edit Sign";
        var titleWidth = Game.gui.measureStringUI(title).X; // convert to UI coords
        Game.gui.drawStringUI(title, new Vector2((float)centre.X / GUI.guiScale - (titleWidth / 2f), (float)centre.Y / GUI.guiScale - SIGN_HEIGHT / 2f - 32f));

        var signX = centre.X / GUI.guiScale - SIGN_WIDTH / 2;
        var signY = centre.Y / GUI.guiScale - SIGN_HEIGHT / 2;

        // draw sign wood texture from atlas
        var signTex = Block.SIGN.uvs[0];
        var uv = UVPair.texCoords(signTex);
        var uvMax = UVPair.texCoords(signTex + 1);

        var t = Game.textures.blockTexture;

        // the source rectangle in the texture atlas takes fucking integer coords. oh well we can multiply
        var sourceRect = new Rectangle(
            (int)(uv.X * t.width),
            (int)(uv.Y * t.height),
            (int)((uvMax.X - uv.X) * t.width),
            (int)((uvMax.Y - uv.Y) * t.height)
        );

        Game.gui.drawUI(
            t,
            new RectangleF(signX, signY, SIGN_WIDTH, SIGN_HEIGHT),
            sourceRect
        );

        // draw text on sign at 2x scale
        // font is 16px tall, we want TEXT_HEIGHT_WORLD (2 world pixels) = 2/16 = 1/8 scale
        // in GUI: 2 world pixels * 8 = 16 GUI pixels tall (at 2x scale)
        var font = Game.fontLoader.fontSystemThin.GetFont(16);
        const float TEXT_SCALE = 2f;

        for (int i = 0; i < 4; i++) {
            var line = signEntity.lines[i];
            if (string.IsNullOrEmpty(line)) {
                continue;
            }

            // measure at rendered scale (TEXTSCALE * 2) and convert to UI coords
            var textsizepx = font.MeasureString(line, GUI.TEXTSCALEV * TEXT_SCALE);
            textsizepx.X *= Game.fontLoader.thinFontAspectRatio;
            var textSize = textsizepx / GUI.guiScale; // convert screen pixels to UI coords

            var lineX = signX + SIGN_WIDTH / 2 - textSize.X / 2; // centre horizontally
            var lineY = signY + (SignBlockEntity.TEXT_PADDING_TOP_WORLD * WORLD_TO_GUI) + i * (SignBlockEntity.LINE_SPACING_WORLD * WORLD_TO_GUI);

            // draw at 2x scale!!
            Game.gui.drawStringUIThin(line, new Vector2(lineX, lineY), Color.Black, new Vector2(TEXT_SCALE, TEXT_SCALE));
        }

        // draw cursor on current line
        // 60 TPS, blink every 1/3s
        if ((cursorBlink / 20) % 2 == 0) {
            var line = signEntity.lines[currentLine];
            var before = line[..Math.Min(cursorPos, line.Length)];

            // measure at rendered scale and convert to UI coords (must match text rendering scale)
            var beforepx = font.MeasureString(before, GUI.TEXTSCALEV * TEXT_SCALE);
            beforepx.X *= Game.fontLoader.thinFontAspectRatio;
            var beforepxui = beforepx / GUI.guiScale;

            var textpx = font.MeasureString(line.Length > 0 ? line : " ", GUI.TEXTSCALEV * TEXT_SCALE);
            textpx.X *= Game.fontLoader.thinFontAspectRatio;
            var textpxui = textpx / GUI.guiScale;

            var lineX = signX + SIGN_WIDTH / 2 - textpxui.X / 2;
            var lineY = signY + (SignBlockEntity.TEXT_PADDING_TOP_WORLD * WORLD_TO_GUI) + currentLine * (SignBlockEntity.LINE_SPACING_WORLD * WORLD_TO_GUI);

            var cx = lineX + beforepxui.X;
            var cy = lineY;

            var th = 16 * GUI.TEXTSCALE * TEXT_SCALE / GUI.guiScale;

            // draw cursor as thin vertical line
            Game.gui.drawUI(
                Game.gui.colourTexture,
                new RectangleF(cx, cy, 1, th),
                new Rectangle(0, 0, 1, 1),
                Color.Black
            );
        }
    }

    public override void postDraw() {
        Screen.GAME_SCREEN.INGAME_MENU.postDraw();
        base.postDraw();
    }

    public override void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        switch (key) {
            case Key.Up:
                currentLine = Math.Max(0, currentLine - 1);
                cursorPos = Math.Min(cursorPos, signEntity.lines[currentLine].Length);
                break;

            case Key.Down:
                currentLine = Math.Min(3, currentLine + 1);
                cursorPos = Math.Min(cursorPos, signEntity.lines[currentLine].Length);
                break;

            case Key.Left:
                cursorPos = Math.Max(0, cursorPos - 1);
                break;

            case Key.Right:
                cursorPos = Math.Min(signEntity.lines[currentLine].Length, cursorPos + 1);
                break;

            case Key.Enter:
            case Key.Tab:
                // move to next line
                currentLine = (currentLine + 1) % 4;
                cursorPos = 0;
                break;

            case Key.Backspace:
                if (cursorPos > 0) {
                    var line = signEntity.lines[currentLine];
                    signEntity.lines[currentLine] = line.Remove(cursorPos - 1, 1);
                    cursorPos--;
                }
                break;

            case Key.Delete:
                var currentLineText = signEntity.lines[currentLine];
                if (cursorPos < currentLineText.Length) {
                    signEntity.lines[currentLine] = currentLineText.Remove(cursorPos, 1);
                }
                break;

            case Key.Home:
                cursorPos = 0;
                break;

            case Key.End:
                cursorPos = signEntity.lines[currentLine].Length;
                break;

            case Key.V:
                // ctrl+v for paste
                if (keyboard.IsKeyPressed(Key.ControlLeft) || keyboard.IsKeyPressed(Key.ControlRight)) {
                    var clipboardText = keyboard.ClipboardText;
                    if (!string.IsNullOrEmpty(clipboardText)) {
                        // filter to printable characters only
                        var filtered = new string(clipboardText.Where(c => !char.IsControl(c)).ToArray());
                        if (!string.IsNullOrEmpty(filtered)) {
                            pasteText(filtered);
                        }
                    }
                }
                break;

            case Key.Escape:
                Screen.GAME_SCREEN.backToGame();
                break;
            default:
                // other keys handled in onKeyChar
                break;
        }
    }

    public override void onKeyRepeat(IKeyboard keyboard, Key key, int scancode) {
        onKeyDown(keyboard, key, scancode);
    }

    public override void onKeyChar(IKeyboard keyboard, char c) {
        // only allow printable
        if (char.IsControl(c)) {
            return;
        }

        // check if adding this character would make the line too wide
        var line = signEntity.lines[currentLine];
        var teststr = line.Insert(cursorPos, c.ToString());

        const float TEXT_SCALE = 2f;
        var font = Game.fontLoader.fontSystemThin.GetFont(16);
        var testpx = font.MeasureString(teststr, GUI.TEXTSCALEV * TEXT_SCALE);
        testpx.X *= Game.fontLoader.thinFontAspectRatio;
        var testwidth = testpx.X / GUI.guiScale;

        if (testwidth <= MAX_LINE_WIDTH_PX) {
            signEntity.lines[currentLine] = teststr;
            cursorPos++;
        }
    }

    private void pasteText(string text) {
        const float TEXT_SCALE = 2f;
        var font = Game.fontLoader.fontSystemThin.GetFont(16);

        foreach (var c in text) {
            var line = signEntity.lines[currentLine];
            var testString = line.Insert(cursorPos, c.ToString());

            var testSizePx = font.MeasureString(testString, GUI.TEXTSCALEV * TEXT_SCALE);
            testSizePx.X *= Game.fontLoader.thinFontAspectRatio;
            var testWidth = testSizePx.X / GUI.guiScale;

            if (testWidth <= MAX_LINE_WIDTH_PX) {
                signEntity.lines[currentLine] = testString;
                cursorPos++;
            }
            else {
                // can't fit more text on this line, stop pasting
                break;
            }
        }
    }
}