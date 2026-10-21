using BlockGame.main;
using BlockGame.net.srv;
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
    public override void place(World world, int x, int y, int z, byte metadata, Placement info) {
        // face opposite to player (so front faces player)
        var facing = Direction.getOpposite(info.hfacing);

        uint blockValue = id;
        blockValue = blockValue.setMetadata((byte)facing);

        world.setBlockMetadata(x, y, z, blockValue);
        world.blockUpdateNeighbours(x, y, z);
    }

    public override byte maxValidMetadata() => 3;

    /**
     * uvs: [side, side, front, back, bottom, top]
     * face matching metadata direction gets front, opposite gets back, others get side
     */
    public override UVPair getTexture(int faceIdx, int metadata) {
        if (faceIdx >= 4) {
            return uvs[faceIdx]; // top/bottom
        }

        int facing = metadata & 0b11;
        if (faceIdx == facing) {
            return uvs[2]; // front
        }

        if (faceIdx == (facing ^ 1)) {
            return uvs[3]; // back
        }

        return uvs[0]; // side
    }

    public override void onBreak(World world, int x, int y, int z, byte metadata) {
        // drop contents
        var be = world.getBlockEntity(x, y, z) as ChestBlockEntity;
        // todo move this to somewhere shared?
        if (be != null && world.isServer) {
            GameServer.instance.closeViewers(be);
        }

        be?.dropContents(world, x, y, z);

        base.onBreak(world, x, y, z, metadata);
    }

    public override bool onUse(World world, int x, int y, int z, Player player) {
        if (world.getBlockEntity(x, y, z) is not ChestBlockEntity be) {
            return false;
        }

        // MP client: server will handle opening (client sent PlaceBlockPacket, will receive InventoryOpenPacket)
        if (!world.isServer) {
            return true; // return true to prevent block placement
        }

        var ctx = new ChestMenuContext(player.inventory, be);
        return GameServer.openInventory(
            (ServerPlayer)player,
            ctx,
            invType: 0, // chest
            title: "Chest",
            position: new Vector3I(x, y, z),
            slots: be.slots
        );
    }

    public override BlockEntity get() {
        return new ChestBlockEntity();
    }
}