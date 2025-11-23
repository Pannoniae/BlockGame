namespace BlockGame.world.block;

#pragma warning disable CS8618
/**
 * Block hardness: 0.5 to 30+ (wide range)
 * Tool speed: 1.0 to 2.0 (linear scaling)
 * Wrong tool/tier penalty: 4x slower (0.25 multiplier)
 */
public class Material {
    public static readonly Material WOOD = new Material(SoundMaterial.WOOD, ToolType.AXE, MaterialTier.NONE, 1.0);
    public static readonly Material STONE = new Material(SoundMaterial.STONE, ToolType.PICKAXE, MaterialTier.WOOD, 1.0);
    public static readonly Material METAL = new Material(SoundMaterial.METAL, ToolType.PICKAXE, MaterialTier.STONE, 2.5);
    public static readonly Material EARTH = new Material(SoundMaterial.DIRT, ToolType.SHOVEL, MaterialTier.NONE, 0.4);

    public static readonly Material ORGANIC =
        new Material(SoundMaterial.ORGANIC, ToolType.NONE, MaterialTier.NONE, 0);

    /** Yummy! (candy blocks are decorative, not actual food) */
    public static readonly Material FOOD = new Material(SoundMaterial.ORGANIC, ToolType.NONE, MaterialTier.NONE, 0.5);

    public static readonly Material GLASS = new Material(SoundMaterial.GLASS, ToolType.NONE, MaterialTier.NONE, 0.15);

    /** Mostly ores */
    public static readonly Material FANCY_STONE =
        new Material(SoundMaterial.STONE, ToolType.PICKAXE, MaterialTier.STONE, 2.0);

    /** TODO */
    public static readonly Material HELL = new Material(SoundMaterial.STONE, ToolType.PICKAXE, MaterialTier.NONE, 1.25);

    public SoundMaterial smat;
    public ToolType toolType;
    public MaterialTier tier;
    public double hardness;

    public Material(SoundMaterial smat, ToolType toolType, MaterialTier tier, double hardness) {
        this.smat = smat;
        this.toolType = toolType;
        this.tier = tier;
        this.hardness = hardness;
    }
}

public enum ToolType : byte {
    NONE,
    PICKAXE,
    AXE,
    SHOVEL,
    HOE,
    SCYTHE,
}

public record class MaterialTier(MaterialTiers tier, double level, int durability) {
    public static readonly MaterialTier NONE = new(MaterialTiers.NONE, 0, 0);
    public static readonly MaterialTier WOOD = new(MaterialTiers.WOOD, 1, 32);
    public static readonly MaterialTier STONE = new(MaterialTiers.STONE, 2, 128);
    public static readonly MaterialTier COPPER = new(MaterialTiers.COPPER, 2.5, 256);
    public static readonly MaterialTier IRON = new(MaterialTiers.IRON, 3, 384);
    public static readonly MaterialTier GOLD = new(MaterialTiers.GOLD, 3.5, 1024);

    /** The index of the tier (NO GAMEPLAY EFFECT, DON'T USE IT FOR THAT), only use for sorting or indexing */
    public readonly MaterialTiers tier = tier;

    /** The "tier value", should roughly be increasing but can be the same or less than the previous. Used for determining stats */
    public readonly double level = level;

    /** max durability for tools/weapons of this tier */
    public readonly int durability = durability;
}

public enum MaterialTiers : byte {
    NONE,
    WOOD,
    STONE,
    COPPER,
    IRON,
    GOLD,
}

public enum SoundMaterial : byte {
    WOOD,
    STONE,
    METAL,
    DIRT,
    GRASS,
    SAND,
    GLASS,
    ORGANIC
}

public static class SoundMaterialExtensions {
    extension(SoundMaterial mat) {
        public string stepCategory() => mat switch {
            SoundMaterial.GRASS => "step/grass",
            SoundMaterial.DIRT => "step/grass",
            SoundMaterial.SAND => "step",
            SoundMaterial.WOOD => "step/wood",
            SoundMaterial.STONE => "step",
            SoundMaterial.METAL => "step",
            SoundMaterial.GLASS => "step",
            SoundMaterial.ORGANIC => "step/grass",
            _ => "step"
        };

        public string breakCategory() => mat switch {
            SoundMaterial.WOOD => "break/wood",
            SoundMaterial.STONE => "break/stone",
            SoundMaterial.SAND => "break/sand",
            SoundMaterial.METAL => "break/stone",
            SoundMaterial.DIRT => "break/grass",
            SoundMaterial.GRASS => "break/grass",
            SoundMaterial.GLASS => "break/stone",
            SoundMaterial.ORGANIC => "break/grass",
            _ => "step"
        };

        public string knockCategory() => mat switch {
            SoundMaterial.WOOD => "knock/wood",
            SoundMaterial.STONE => "break/stone",
            SoundMaterial.SAND => "break/sand",
            SoundMaterial.METAL => "break/stone",
            SoundMaterial.DIRT => "knock/grass",
            SoundMaterial.GRASS => "knock/grass",
            SoundMaterial.GLASS => "break/stone",
            SoundMaterial.ORGANIC => "knock/grass",
            _ => "step"
        };
    }
}