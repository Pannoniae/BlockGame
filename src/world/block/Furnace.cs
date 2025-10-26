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
     * uvs layout from furnaceUVs: [front, side, top_bottom]
     * face indices: 0=WEST, 1=EAST, 2=SOUTH, 3=NORTH, 4=DOWN, 5=UP
     * metadata: 0=WEST, 1=EAST, 2=SOUTH, 3=NORTH (facing direction)
     */
    public override UVPair getTexture(int faceIdx, int metadata) {
        var facing = (byte)(metadata & 0b11);

        // determine which texture to use for this face based on facing
        return faceIdx switch {
            0 => facing == 0 ? uvs[0] : uvs[1], // WEST: front if facing west, else side
            1 => facing == 1 ? uvs[0] : uvs[1], // EAST: front if facing east, else side
            2 => facing == 2 ? uvs[0] : uvs[1], // SOUTH: front if facing south, else side
            3 => facing == 3 ? uvs[0] : uvs[1], // NORTH: front if facing north, else side
            4 => uvs[2], // DOWN
            5 => uvs[2], // UP
            _ => uvs[0]
        };
    }

    public override bool onUse(World world, int x, int y, int z, Player player) {
        // open the crafting table UI
        var ctx = new FurnaceMenuContext(player.inventory);
        player.currentCtx = ctx;

        Screen.GAME_SCREEN.switchToMenu(new FurnaceMenu(new Vector2I(0, 32), ctx));
        ((FurnaceMenu)Screen.GAME_SCREEN.currentMenu!).setup();

        world.inMenu = true;
        Game.instance.unlockMouse();

        return true;
    }
}