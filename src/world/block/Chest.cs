using BlockGame.main;
using BlockGame.ui;
using BlockGame.ui.menu;
using BlockGame.util;
using BlockGame.world.block.entity;
using BlockGame.world.entity;
using BlockGame.world.item.inventory;
using Molten;

namespace BlockGame.world.block;

public class Chest : EntityBlock {
    public Chest(string name) : base(name) {
    }

    protected override void onRegister(int id) {
        renderType[id] = RenderType.CUBE_DYNTEXTURE;
    }

    /**
     * metadata bits 0-1: horizontal facing (0=WEST, 1=EAST, 2=SOUTH, 3=NORTH)
     * default front is -Z (SOUTH), we want it to face the player
     */
    public override void place(World world, int x, int y, int z, byte metadata, RawDirection dir) {
        // face opposite to player (so front faces player)
        var opposite = Direction.getOpposite(dir);
        byte facing = opposite switch {
            RawDirection.WEST => 0,
            RawDirection.EAST => 1,
            RawDirection.SOUTH => 2,
            RawDirection.NORTH => 3,
            _ => 2 // default south
        };

        uint blockValue = id;
        blockValue = blockValue.setMetadata(facing);

        world.setBlockMetadata(x, y, z, blockValue);
        world.blockUpdateNeighbours(x, y, z);
    }

    public override byte maxValidMetadata() => 3;

    /**
     * uvs: [side, side, front, side, bottom, top]
     * face matching metadata direction gets front, others get side
     */
    public override UVPair getTexture(int faceIdx, int metadata) {
        if (faceIdx >= 4) return uvs[faceIdx]; // top/bottom

        return faceIdx == (metadata & 0b11) ? uvs[2] : uvs[0];
    }

    public override void onBreak(World world, int x, int y, int z, byte metadata) {

        // drop contents
        var be = world.getBlockEntity(x, y, z) as ChestBlockEntity;
        be?.dropContents(world, x, y, z);

        base.onBreak(world, x, y, z, metadata);

    }

    public override bool onUse(World world, int x, int y, int z, Player player) {
        if (world.getBlockEntity(x, y, z) is not ChestBlockEntity be) {
            return false;
        }

        var ctx = new ChestMenuContext(player.inventory, be);
        player.currentCtx = ctx;

        Screen.GAME_SCREEN.switchToMenu(new ChestMenu(new Vector2I(0, 12), ctx));
        ((ChestMenu)Screen.GAME_SCREEN.currentMenu!).setup();

        world.inMenu = true;
        Game.instance.unlockMouse();

        return true;
    }

    public override BlockEntity get() {
        return new ChestBlockEntity();
    }
}