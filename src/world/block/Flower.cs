namespace BlockGame.world.block;

#pragma warning disable CS8618
public class Flower(string name) : Block(name) {
    protected override void onRegister(int id) {
        material(Material.ORGANIC);
        hardness[id] = 0;
    }

    public override void update(World world, int x, int y, int z) {
        if (world.inWorld(x, y - 1, z) && world.getBlock(x, y - 1, z) == 0) {
            world.setBlock(x, y, z, AIR.id);
        }
    }
}