using System.Buffers.Binary;
using BlockGame.util;

namespace BlockGame.net.packet;

/**
 * This isn't a normal registry because we need stable IDs. F in the chat
 */
public class PacketRegistry {
    private static readonly XBiMap<int, Type> map = [];

    /**
     * Yes we have some holes, don't worry about it, I have it all specced out (mostly) (hopefully) (fingers crossed)
     * Hopefully, plans won't change! (insert narrator saying "they always do")
     */
    static PacketRegistry() {
        // connection & auth (0x00-0x0F)
        register<HugPacket>(0x00);
        register<LoginSuccessPacket>(0x01);
        register<LoginFailedPacket>(0x02);
        register<DisconnectPacket>(0x03);
        register<RespawnPacket>(0x04);
        register<GamemodePacket>(0x05);
        register<PlayerListAddPacket>(0x06);
        register<PlayerListRemovePacket>(0x07);
        register<PlayerListUpdatePingPacket>(0x08);
        register<PlayerSkinPacket>(0x09);


        register<AuthRequiredPacket>(0x0A);
        register<AuthPacket>(0x0B);

        // world state (0x10-0x1F)
        register<ChunkDataPacket>(0x10);
        register<UnloadChunkPacket>(0x11);
        register<BlockChangePacket>(0x12);
        register<MultiBlockChangePacket>(0x13);
        register<LightUpdatePacket>(0x14);
        register<TimeUpdatePacket>(0x15);

        // entity sync (0x20-0x2F)
        register<SpawnEntityPacket>(0x20);
        register<SpawnPlayerPacket>(0x21);
        register<DespawnEntityPacket>(0x22);
        register<EntityImpulsePacket>(0x24);
        register<EntityPositionRotationPacket>(0x26);
        register<EntityStatePacket>(0x27);
        register<PlayerHealthPacket>(0x28);
        register<EntityActionPacket>(0x29);
        register<TeleportPacket>(0x2A);
        register<AddEffectPacket>(0x2C);
        register<RemoveEffectPacket>(0x2D);

        // player actions (0x30-0x3F)
        register<PlayerPositionRotationPacket>(0x32);
        register<RespawnRequestPacket>(0x33);
        register<StartBlockBreakPacket>(0x34);
        register<CancelBlockBreakPacket>(0x35);
        register<FinishBlockBreakPacket>(0x36);
        register<BlockBreakProgressPacket>(0x37);
        register<PlaceBlockPacket>(0x38);
        register<DropItemPacket>(0x39);
        register<AttackEntityPacket>(0x3A);
        register<EntityDamagePacket>(0x3B);
        register<UseItemPacket>(0x3C);
        register<UpdateBlockBreakProgressPacket>(0x3D);
        register<InteractEntityPacket>(0x3F);

        // inventory (0x40-0x4F)
        register<SetSlotPacket>(0x40);
        register<InventorySyncPacket>(0x41);
        register<InventorySlotClickPacket>(0x42);
        register<InventoryAckPacket>(0x43);
        register<InventoryOpenPacket>(0x44);
        register<InventoryClosePacket>(0x45);
        register<HeldItemChangePacket>(0x46);
        register<FurnaceSyncPacket>(0x47);
        register<PlayerHeldItemChangePacket>(0x48);
        register<InventoryResyncTermPacket>(0x49);
        register<InventoryResyncAckPacket>(0x4A);

        // block entities (0x50-0x5F)
        register<UpdateBlockEntityPacket>(0x50);
        register<BlockEntityDataPacket>(0x51);

        // chat & commands (0x70-0x7F)
        register<ChatMessagePacket>(0x70);
        register<CommandPacket>(0x71);
        register<RenderDistancePacket>(0x72);
        register<ServerStatsPacket>(0x73);
    }


    private delegate Packet Reader(PacketBuffer buf);

    private static readonly Reader?[] readers = new Reader?[256];

    public static void register<T>(int id) where T : Packet, new() {
        var type = typeof(T);
        if (map.containsKey(id)) {
            InputException.throwNew($"Packet ID '{id}' is already registered to '{map[id].Name}'!");
        }
        if (map.containsValue(type)) {
            InputException.throwNew($"Packet type '{type.Name}' is already registered with ID '{map.getKey(type)}'!");
        }

        map[id] = type;

        readers[id] = static buf => {
            var p = new T();
            p.read(buf);
            return p;
        };
    }

    public static Type getType(int id) {
        return map[id];
    }

    public static Packet decode(ReadOnlySpan<byte> frame, out Type type) {
        var id = BinaryPrimitives.ReadInt32LittleEndian(frame);
        type = getType(id);
        return readers[id]!(new PacketBuffer(frame[4..]));
    }

    public static int getID(Type type) {
        return map.getKey(type);
    }

    public static int getID<T>() where T : Packet {
        return getID(typeof(T));
    }
}