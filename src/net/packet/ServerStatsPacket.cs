namespace BlockGame.net.packet;

/**
 * S→C: 0x73 - Server stats for the F3 overlay.
 */
public struct ServerStatsPacket : Packet {
    /** mean ms/tick over the sample window */
    public float mspt;

    /** worst tick in the sample window */
    public float peak;

    public float tps;

    public int chunks;

    /** outstanding chunkload tickets */
    public int queue;

    public int entities;

    /**
     * ms/tick for every tick since the last stats packet, oldest first.
     * This needs to be sent because the client appends these to its own history so the graph doesn't have gaps, sending only the aggregate would probably skip
     * todo is this *really* needed?
     */
    public float[] samples;

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeFloat(mspt);
        buf.writeFloat(peak);
        buf.writeFloat(tps);
        buf.writeInt(chunks);
        buf.writeInt(queue);
        buf.writeInt(entities);

        buf.writeInt(samples.Length);
        foreach (var s in samples) {
            buf.writeFloat(s);
        }
    }

    public void read(PacketBuffer buf) {
        mspt = buf.readFloat();
        peak = buf.readFloat();
        tps = buf.readFloat();
        chunks = buf.readInt();
        queue = buf.readInt();
        entities = buf.readInt();

        var n = buf.readInt();
        samples = new float[n];
        for (var i = 0; i < n; i++) {
            samples[i] = buf.readFloat();
        }
    }
}
