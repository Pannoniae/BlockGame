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
        register(0x00, typeof(HugPacket));
        register(0x01, typeof(LoginSuccessPacket));
        register(0x02, typeof(LoginFailedPacket));
        register(0x03, typeof(DisconnectPacket));
        register(0x04, typeof(RespawnPacket));
        register(0x05, typeof(GamemodePacket));
        register(0x06, typeof(PlayerListAddPacket));
        register(0x07, typeof(PlayerListRemovePacket));
        register(0x08, typeof(PlayerListUpdatePingPacket));

        
        register(0x0A, typeof(AuthRequiredPacket));
        register(0x0B, typeof(AuthPacket));

        // world state (0x10-0x1F)
        register(0x10, typeof(ChunkDataPacket));
        register(0x11, typeof(UnloadChunkPacket));
        register(0x12, typeof(BlockChangePacket));
        register(0x13, typeof(MultiBlockChangePacket));
        register(0x15, typeof(TimeUpdatePacket));

        // entity sync (0x20-0x2F)
        register(0x20, typeof(SpawnEntityPacket));
        register(0x21, typeof(SpawnPlayerPacket));
        register(0x22, typeof(DespawnEntityPacket));
        register(0x23, typeof(EntityPositionPacket));
        register(0x24, typeof(EntityVelocityPacket));
        register(0x25, typeof(EntityRotationPacket));
        register(0x26, typeof(EntityPositionRotationPacket));
        register(0x27, typeof(EntityStatePacket));
        register(0x28, typeof(PlayerHealthPacket));
        register(0x29, typeof(EntityActionPacket));
        register(0x2A, typeof(TeleportPacket));
        register(0x2B, typeof(EntityPositionDeltaPacket));

        // player actions (0x30-0x3F)
        register(0x30, typeof(PlayerPositionPacket));
        register(0x31, typeof(PlayerRotationPacket));
        register(0x32, typeof(PlayerPositionRotationPacket));
        register(0x33, typeof(RespawnRequestPacket));
        register(0x34, typeof(StartBlockBreakPacket));
        register(0x35, typeof(CancelBlockBreakPacket));
        register(0x36, typeof(FinishBlockBreakPacket));
        register(0x37, typeof(BlockBreakProgressPacket));
        register(0x38, typeof(PlaceBlockPacket));
        register(0x39, typeof(DropItemPacket));
        register(0x3A, typeof(AttackEntityPacket));
        register(0x3C, typeof(UseItemPacket));

        // inventory (0x40-0x4F)
        register(0x40, typeof(SetSlotPacket));
        register(0x41, typeof(InventorySyncPacket));
        register(0x42, typeof(InventorySlotClickPacket));
        register(0x43, typeof(InventoryAckPacket));
        register(0x44, typeof(InventoryOpenPacket));
        register(0x45, typeof(InventoryClosePacket));
        register(0x46, typeof(HeldItemChangePacket));
        register(0x47, typeof(FurnaceSyncPacket));
        register(0x48, typeof(PlayerHeldItemChangePacket));
        register(0x49, typeof(ResyncCompletePacket));
        register(0x4A, typeof(ResyncAckPacket));

        // block entities (0x50-0x5F)
        register(0x50, typeof(UpdateBlockEntityPacket));
        register(0x51, typeof(BlockEntityDataPacket));

        // chat & commands (0x70-0x7F)
        register(0x70, typeof(ChatMessagePacket));
        register(0x71, typeof(CommandPacket));
    }


    public static void register(int id, Type type) {
        if (map.containsKey(id)) {
            InputException.throwNew($"Packet ID '{id}' is already registered to '{map[id].Name}'!");
        }
        if (map.containsValue(type)) {
            InputException.throwNew($"Packet type '{type.Name}' is already registered with ID '{map.getKey(type)}'!");
        }

        map[id] = type;
    }

    public static Type getType(int id) {
        return map[id];
    }

    public static int getID(Type type) {
        return map.getKey(type);
    }

    public static int getID<T>() where T : Packet {
        return getID(typeof(T));
    }
}