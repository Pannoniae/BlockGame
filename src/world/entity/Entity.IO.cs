using System.Numerics;
using BlockGame.util.xNBT;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

public partial class Entity {
    public void read(NBTCompound data) {
        id = data.getInt("id");

        // load entity type from string ID
        if (data.has("type")) {
            var id = data.getString("type");
            type = id;
        }

        position = prevPosition = new Vector3D(
            data.getDouble("posX"),
            data.getDouble("posY"),
            data.getDouble("posZ")
        );
        rotation = prevRotation = new Vector3(
            data.getFloat("rotX"),
            data.getFloat("rotY"),
            data.getFloat("rotZ")
        );
        velocity = prevVelocity = new Vector3D(
            data.getDouble("velX"),
            data.getDouble("velY"),
            data.getDouble("velZ")
        );
        accel = Vector3D.Zero;

        fireTicks = data.getInt("fireTicks");

        // is dead?
        dead = data.getByte("dead", 0) != 0;

        readx(data);
    }

    protected virtual void readx(NBTCompound data) {
    }

    public void write(NBTCompound data) {
        data.addInt("id", id);

        // save string ID
        data.addString("type", type);

        data.addDouble("posX", position.X);
        data.addDouble("posY", position.Y);
        data.addDouble("posZ", position.Z);
        data.addFloat("rotX", rotation.X);
        data.addFloat("rotY", rotation.Y);
        data.addFloat("rotZ", rotation.Z);
        data.addDouble("velX", velocity.X);
        data.addDouble("velY", velocity.Y);
        data.addDouble("velZ", velocity.Z);
        data.addInt("fireTicks", fireTicks);
        data.addByte("dead", (byte)(dead ? 1 : 0));
        writex(data);
    }

    public virtual void writex(NBTCompound data) {
    }
}