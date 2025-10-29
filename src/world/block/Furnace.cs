using BlockGame.main;
using BlockGame.ui;
using BlockGame.ui.menu;
using BlockGame.util;
using BlockGame.world.item.inventory;
using Molten;

namespace BlockGame.world.block;

public class Furnace : Block {
    public Furnace(string name) : base(name) {
    }

    protected override void onRegister(int id) {
        renderType[id] = RenderType.CUBE_DYNTEXTURE;
    }

    /**
     * metadata bits 0-1: horizontal facing (0=WEST, 1=EAST, 2=SOUTH, 3=NORTH)
     * default front is -Z (SOUTH), we want it to face the player
     */
    public override void place(World world, int x, int y, int z, byte metadata, RawDirection dir) {
        var opposite = Direction.getOpposite(dir);
        byte facing = opposite switch {
            RawDirection.WEST => 0,
            RawDirection.EAST => 1,
            RawDirection.SOUTH => 2,
            RawDirection.NORTH => 3,
            _ => 2
        };

        uint blockValue = id;
        blockValue = blockValue.setMetadata(facing);

        world.setBlockMetadata(x, y, z, blockValue);
        world.blockUpdateNeighbours(x, y, z);
    }

    public override byte maxValidMetadata() => 3;

    /** uvs: [front, side, top_bottom] */
    public override UVPair getTexture(int faceIdx, int metadata) {
        var facing = (byte)(metadata & 0b11);

        return faceIdx switch {
            0 or 1 or 2 or 3 => facing == faceIdx ? uvs[0] : uvs[1],
            4 or 5 => uvs[2],
            _ => uvs[0]
        };
    }

    public override bool onUse(World world, int x, int y, int z, Player player) {
        var ctx = new FurnaceMenuContext(player.inventory);
        player.currentCtx = ctx;

        Screen.GAME_SCREEN.switchToMenu(new FurnaceMenu(new Vector2I(0, 32), ctx));
        ((FurnaceMenu)Screen.GAME_SCREEN.currentMenu!).setup();

        world.inMenu = true;
        Game.instance.unlockMouse();

        return true;
    }
}