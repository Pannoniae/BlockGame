using System.Numerics;
using BlockGame.GL.vertexformats;
using BlockGame.main;
using BlockGame.render;
using BlockGame.ui;
using BlockGame.util;
using BlockGame.world.block.entity;
using BlockGame.world.item;

namespace BlockGame.world.block;

public class SignBlock : EntityBlock {
    // todo add colouring? idk

    public SignBlock(string name) : base(name) {
    }

    protected override void onRegister(int id) {
        transparency();
        material(Material.WOOD);
        renderType[id] = RenderType.CUSTOM;
        customAABB[id] = true;
        setFlammable(30);
        noCollision();
    }

    /**
     * Metadata encoding for signs:
     * bits 0-3: rotation (0-15) for standing, or direction (2, 3, 4, 5) for wall
     * bit 4: is wall sign (0 = standing, 1 = wall)
     *
     * rotation 0 = looking +Z so sign faces south, increases clockwise
     */
    public static byte getRotation(uint metadata) => (byte)(metadata & 0x0F);

    public static bool isWall(uint metadata) => (metadata & 0x10) != 0;

    public static Vector3 rots(uint metadata) => new(0, getRotation(metadata) * 22.5f, 0);

    public static Vector3 rotw(uint metadata) => getRotation(metadata) switch {
        2 => new Vector3(0, 0, 0), // south
        3 => new Vector3(0, 90f, 0), // west
        4 => new Vector3(0, 180f, 0), // north
        5 => new Vector3(0, 270f, 0), // east
        _ => Vector3.Zero
    };

    public override void update(World world, int x, int y, int z) {
        if (!canSurvive(world, x, y, z)) {
            var metadata = world.getBlockRaw(x, y, z).getMetadata();
            var (dropItem, dropMeta, dropCount) = getDrop(world, x, y, z, metadata);
            world.spawnBlockDrop(x, y, z, dropItem, dropCount, dropMeta);
            world.setBlock(x, y, z, AIR.id);
        }
    }

    public override bool canPlace(World world, int x, int y, int z, RawDirection dir) {
        return canSurvive(world, x, y, z);
    }

    private static bool canSurvive(World world, int x, int y, int z) {
        var metadata = world.getBlockRaw(x, y, z).getMetadata();
        if (isWall(metadata)) {
            // wall sign - check block behind
            var rot = getRotation(metadata);
            return rot switch {
                2 => fullBlock[world.getBlock(x, y, z + 1)],
                3 => fullBlock[world.getBlock(x + 1, y, z)],
                4 => fullBlock[world.getBlock(x, y, z - 1)],
                5 => fullBlock[world.getBlock(x - 1, y, z)],
                _ => false
            };
        }

        return fullBlock[world.getBlock(x, y - 1, z)];
    }

    public override void getAABBs(World world, int x, int y, int z, byte metadata, List<AABB> aabbs) {
        aabbs.Clear();

        if (isWall(metadata)) {
            // box attached to wall
            var rot = getRotation(metadata);
            aabbs.Add(rot switch {
                2 => new AABB(x, y + 3 / 16f, z + 1f - 1 / 16f, x + 1f, y + 13 / 16f, z + 1f), // south
                3 => new AABB(x + 1f - 1 / 16f, y + 3 / 16f, z, x + 1f, y + 13 / 16f, z + 1f), // west
                4 => new AABB(x, y + 3 / 16f, z, x + 1f, y + 13 / 16f, z + 1 / 16f), // north
                5 => new AABB(x, y + 3 / 16f, z, x + 1 / 16f, y + 13 / 16f, z + 1f), // east
                _ => new AABB(x, y, z, x + 1f, y + 1f, z + 1f)
            });
        }
        else {
            // standing sign - post in centre
            aabbs.Add(new AABB(x + 5 / 16f, y, z + 5 / 16f, x + 11 / 16f, y + 1f, z + 11 / 16f));
        }
    }

    public override (Item item, byte metadata, int count) getDrop(World world, int x, int y, int z, byte metadata) {
        return (Item.SIGN_ITEM, 0, 1);
    }

    public override bool onUse(World world, int x, int y, int z, Player player) {
        if (world.getBlockEntity(x, y, z) is not SignBlockEntity be) {
            return false;
        }

        Screen.GAME_SCREEN.switchToMenu(new ui.menu.SignMenu(be));
        //((ui.menu.SignMenu)Screen.GAME_SCREEN.currentMenu!).setup();

        world.inMenu = true;
        Game.instance.unlockMouse();

        return true;
    }

    public override void render(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices) {
        base.render(br, x, y, z, vertices);
        x &= 15;
        y &= 15;
        z &= 15;

        var metadata = br.getBlock().getMetadata();
        var tex = uvs[0];
        if (br.forceTex.u >= 0 && br.forceTex.v >= 0) {
            tex = br.forceTex;
        }

        var post = uvs[1];
        if (br.forceTex.u >= 0 && br.forceTex.v >= 0) {
            post = br.forceTex;
        }


        var uv0 = UVPair.texCoords(tex);
        var uv1 = UVPair.texCoords(tex + 1);
        var u0 = uv0.X;
        var v0 = uv0.Y;
        var u1 = uv1.X;
        var v1 = uv1.Y;

        var puv0 = UVPair.texCoords(post);
        var pu0 = puv0.X;
        var pv0 = puv0.Y;
        var puv1 = UVPair.texCoords(post + 1);
        var pu1 = puv1.X;
        var pv1 = puv1.Y;

        if (isWall(metadata)) {
            const float offset = 0.05f / 16f;
            const float h = 10f / 16f;
            const float yy0 = 3 / 16f;
            const float yy1 = yy0 + h;
            var rot = getRotation(metadata);
            var (x0, z0, x1, z1, y0, y1) = rot switch {
                2 => (0f, 1f - 1 / 16f - offset, 1f, 1f - offset, yy0, yy1), // south
                3 => (1f - 1 / 16f - offset, 0f, 1f - offset, 1f, yy0, yy1), // west
                4 => (0f, offset, 1f, 1 / 16f + offset, yy0, yy1), // north
                5 => (offset, 0f, 1 / 16f + offset, 1f, yy0, yy1), // east
                _ => (0f, 0f, 1f, 1f, yy0, yy1)
            };

            br.renderSimpleCube(x, y, z, vertices, x0, y0, z0, x1, y1, z1, u0, v0, u1, v1);
        }
        else {
            // post 2px thick
            br.renderCube(x, y, z, vertices, 7 / 16f, 0f, 7 / 16f, 9 / 16f, 7 / 16f, 9 / 16f, pu0, pv0, pu1, pv1);
            // sign board
            br.renderSign(x, y, z, vertices, u0, v0, u1, v1, getRotation(metadata));
        }
    }


    public override ItemStack getActualItem(byte metadata) {
        return new ItemStack(Item.SIGN_ITEM, 1);
    }

    public override BlockEntity get() {
        return new SignBlockEntity();
    }
}