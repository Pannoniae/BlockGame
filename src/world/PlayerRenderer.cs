using System.Numerics;
using BlockGame.main;
using BlockGame.render.model;
using BlockGame.util;
using BlockGame.world.entity;
using FontStashSharp;
using Molten;
using Molten.DoublePrecision;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.world;

public class PlayerRenderer : MobRenderer<Player> {
    public new HumanModel model => (HumanModel)base.model;

    public PlayerRenderer() {
        base.model = new HumanModel();
    }

    public override void render(MatrixStack mat, Entity e, float scale, double interp) {
        render(mat, e, scale, interp, forceRender: false);
    }

    public void render(MatrixStack mat, Entity e, float scale, double interp, bool forceRender) {
        if (e is not Player player) return;

        // don't render local player in first person (unless forced)
        bool isLocalPlayer = e == Game.player;
        if (!forceRender && isLocalPlayer && Game.camera.mode == CameraMode.FirstPerson) {
            return;
        }

        bool mp = Net.mode.isMPC();

        var item = player.inventory.getSelected();
        model.armRaise = item != ItemStack.EMPTY;

        model.sneaking = player.sneaking;

        base.render(mat, player, scale, interp);

        // Render hand item in third person!
        // Position at right arm location and render item
        // BETTER IDEA, we just also steal the rotation from the arm too!
        mat.push();
        const float sc = 1f / 16f;
        mat.translate(model.rightArm.position.X * sc, model.rightArm.position.Y * sc, model.rightArm.position.Z * sc);
        mat.rotate(model.rightArm.rotation.X, 1, 0, 0);
        mat.rotate(model.rightArm.rotation.Y, 0, 1, 0);
        mat.rotate(model.rightArm.rotation.Z, 0, 0, 1);

        // adjust to arm rotation
        //mat.translate((cosSwingSqrt) / 2f, 0, (sinSwingSqrt) / -3f);

        //player.handRenderer.render(interp);
        player.handRenderer.renderThirdPerson(mat, interp);
        mat.pop();

        // render nametag in multiplayer (not for local player)
        if (mp && !isLocalPlayer && !string.IsNullOrEmpty(player.name)) {
            renderNametag(player, interp);
        }
    }

    private static void renderNametag(Player player, double interp) {
        var font = Game.fontLoader.fontSystem.GetFont(16);
        var renderer = Game.fontLoader.renderer3D;
        var textBounds = font.MeasureString(player.name);
        var pos = Vector3D.Lerp(player.prevPosition, player.position, interp);

        var mat = Game.graphics.model;
        mat.push();

        mat.loadIdentity();


        mat.translate((float)pos.X, (float)pos.Y, (float)pos.Z);

        // matrix already has player position from renderEntities
        // just translate up to above the head
        mat.translate(0, (float)Player.height + 0.3f, 0);

        // billboard matrix to face camera
        var fwd = Game.camera.forward(interp).toVec3();
        var up = Game.camera.up(interp).toVec3();
        var right = Vector3.Normalize(Vector3.Cross(up, fwd));
        up = Vector3.Normalize(Vector3.Cross(fwd, right)); // re-orthogonalize

        var bb = new Matrix4x4(
            right.X, right.Y, right.Z, 0,
            up.X, up.Y, up.Z, 0,
            -fwd.X, -fwd.Y, -fwd.Z, 0,
            0, 0, 0, 1
        );

        mat.multiply(bb);

        // scale text (font is big, world is small)
        const float scale = 1 / 96f;
        mat.scale(scale, -scale, scale); // flip text right side up

        // center text
        var textPos = new Vector2(-textBounds.X / 2, 0);

        // disable depth test so text renders on top
        var gl = Game.GL;
        gl.Disable(EnableCap.DepthTest);
        gl.Disable(EnableCap.CullFace);

        var worldMatrix = mat.top;

        // draw background rectangle
        const float padding = 2f;
        var bgColor = new FSColor(0, 0, 0, 128);
        var bgRect = new Rectangle(
            (int)(textPos.X - padding),
            (int)(textPos.Y - padding),
            (int)(textBounds.X + padding * 2),
            (int)(textBounds.Y + padding * 2)
        );
        renderer.Draw(Game.gui.colourTexture, new Vector2(bgRect.X, bgRect.Y), ref worldMatrix,
            new Rectangle(0, 0, bgRect.Width, bgRect.Height), bgColor, 0,
            new Vector2(1, 1), -0.01f);

        // draw text
        var color = new FSColor(255, 255, 255, 255);
        font.DrawText(renderer, player.name, textPos, color, ref worldMatrix);

        gl.Enable(EnableCap.DepthTest);
        gl.Enable(EnableCap.CullFace);

        mat.pop();
    }
}