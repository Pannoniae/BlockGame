using BlockGame.main;
using BlockGame.net.packet;
using BlockGame.net.srv;
using BlockGame.ui;
using BlockGame.ui.menu;
using BlockGame.util;
using BlockGame.world.block.entity;
using BlockGame.world.entity;
using BlockGame.world.item;
using BlockGame.world.item.inventory;
using LiteNetLib;
using Molten;

namespace BlockGame.world.block;

public class Furnace : EntityBlock {
    public Furnace(string name) : base(name) {
    }

    protected override void onRegister(int id) {
        renderType[id] = RenderType.CUBE_DYNTEXTURE;
    }

    /**
     * metadata bits 0-1: horizontal facing (0=WEST, 1=EAST, 2=SOUTH, 3=NORTH)
     * bit 2: lit (1=lit, 0=unlit)
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

        // todo place unlit

        uint blockValue = id;
        blockValue = blockValue.setMetadata(facing);

        world.setBlockMetadata(x, y, z, blockValue);
        world.blockUpdateNeighbours(x, y, z);
    }

    public override byte maxValidMetadata() => 7; // 3 bits: 2 for facing, 1 for lit

    /** uvs: [front_unlit, front_lit, side, top_bottom] */
    public override UVPair getTexture(int faceIdx, int metadata) {
        var facing = (byte)(metadata & 0b11);
        var lit = (metadata & 0b100) != 0;

        var frontTex = lit ? uvs[1] : uvs[0]; // lit front vs unlit front

        return faceIdx switch {
            0 or 1 or 2 or 3 => facing == faceIdx ? frontTex : uvs[2],
            4 or 5 => uvs[3],
            _ => frontTex
        };
    }

    public override (Item item, byte metadata, int count) getDrop(World world, int x, int y, int z, byte metadata) {
        // only the finest quality furnaces!
        return (getItem(), 0, 1);
    }

    public override void onBreak(World world, int x, int y, int z, byte metadata) {
        var be = world.getBlockEntity(x, y, z) as FurnaceBlockEntity;
        be?.dropContents(world, x, y, z);

        base.onBreak(world, x, y, z, metadata);
    }

    public override bool onUse(World world, int x, int y, int z, Player player) {
        if (world.getBlockEntity(x, y, z) is not FurnaceBlockEntity be) {
            return false;
        }

        // MP client: server will handle opening (client sent PlaceBlockPacket, will receive InventoryOpenPacket)
        if (Net.mode.isMPC()) {
            return true;
        }

        // server-side: open inventory via helper
        if (Net.mode.isDed()) {
            var ctx = new FurnaceMenuContext(player.inventory, be);
            return GameServer.openInventory(
                (ServerPlayer)player,
                ctx,
                invType: 2, // furnace
                title: "Furnace",
                position: new Vector3I(x, y, z),
                slots: be.slots,
                additionalPackets: conn => {
                    // send furnace state (smelting progress, fuel)
                    conn.send(new FurnaceSyncPacket {
                        position = new Vector3I(x, y, z),
                        fuelRemaining = be.fuelRemaining,
                        fuelMax = be.fuelMax,
                        smeltProgress = be.smeltProgress
                    }, DeliveryMethod.ReliableOrdered);
                }
            );
        }
        else {
            var ctx = new FurnaceMenuContext(player.inventory, be);
            player.currentCtx = ctx;

            Screen.GAME_SCREEN.switchToMenu(new FurnaceMenu(new Vector2I(0, 32), ctx));
            ((FurnaceMenu)Screen.GAME_SCREEN.currentMenu!).setup();

            world.inMenu = true;
            Game.instance.unlockMouse();

            return true;
        }

        return false;
    }

    public override BlockEntity get() {
        return new FurnaceBlockEntity();
    }

    public override void renderUpdate(World world, int x, int y, int z) {
        // todo add some nice flames coming from the front! :)
    }
}