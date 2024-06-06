namespace BlockGame.util;

public class BlockModel {
    public Face[] faces;

    public static BlockModel makeCube(UVPair[] uvs) {
        var model = new BlockModel();
        model.faces = new Face[6];
        // west
        model.faces[0] = new(0, 1, 1, 0, 0, 1, 0, 0, 0, 0, 1, 0, uvs[0], uvs[0] + 1, RawDirection.WEST);
        // east
        model.faces[1] = new(1, 1, 0, 1, 0, 0, 1, 0, 1, 1, 1, 1, uvs[1], uvs[1] + 1, RawDirection.EAST);
        // south
        model.faces[2] = new(0, 1, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, uvs[2], uvs[2] + 1, RawDirection.SOUTH);
        // north
        model.faces[3] = new(1, 1, 1, 1, 0, 1, 0, 0, 1, 0, 1, 1, uvs[3], uvs[3] + 1, RawDirection.NORTH);
        // down
        model.faces[4] = new(1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, uvs[4], uvs[4] + 1, RawDirection.DOWN);
        // up
        model.faces[5] = new(0, 1, 1, 0, 1, 0, 1, 1, 0, 1, 1, 1, uvs[5], uvs[5] + 1, RawDirection.UP);
        return model;
    }

    /// <summary>
    /// Liquids are cubes but only the bottom 7/8th of the block is drawn
    /// </summary>
    public static BlockModel makeLiquid(UVPair[] uvs) {
        // make it slightly smaller to avoid zfighting
        var model = new BlockModel();
        model.faces = new Face[6];

        const float offset = 0.0005f;

        const float height = 15 / 16f;

        // west
        model.faces[0] = new(0 + offset, 1 - offset, 1 - offset, 0 + offset, 0 + offset, 1 - offset, 0 + offset, 0 + offset, 0 + offset, 0 + offset, 1 - offset, 0 + offset,
            uvs[0], uvs[0] + 1, RawDirection.WEST, true);
        // east
        model.faces[1] = new(1 - offset, 1 - offset, 0 + offset, 1 - offset, 0 + offset, 0 + offset, 1 - offset, 0 + offset, 1 - offset, 1 - offset, 1 - offset, 1 - offset,
            uvs[1], uvs[1] + 1, RawDirection.EAST, true);
        // south
        model.faces[2] = new(0 + offset, 1 - offset, 0 + offset, 0 + offset, 0 + offset, 0 + offset, 1 - offset, 0 + offset, 0 + offset, 1 - offset, 1 - offset, 0 + offset,
            uvs[2], uvs[2] + 1, RawDirection.SOUTH, true);
        // north
        model.faces[3] = new(1 - offset, 1 - offset, 1 - offset, 1 - offset, 0 + offset, 1 - offset, 0 + offset, 0 + offset, 1 - offset, 0 + offset, 1 - offset, 1 - offset,
            uvs[3], uvs[3] + 1, RawDirection.NORTH, true);
        // down
        model.faces[4] = new(1 - offset, 0 + offset, 1 - offset, 1 - offset, 0 + offset, 0 + offset, 0 + offset, 0 + offset, 0 + offset, 0 + offset, 0 + offset, 1 - offset,
            uvs[4], uvs[4] + 1, RawDirection.DOWN, true);
        // up
        model.faces[5] = new(0, height, 1, 0, height, 0, 1, height, 0, 1, height, 1,
            uvs[5], uvs[5] + 1, RawDirection.UP, true, true);
        return model;
    }

    // make a nice X
    public static BlockModel makeGrass(UVPair[] uvs) {
        var model = new BlockModel();
        model.faces = new Face[4];

        // offset from edge
        var offset = 1 / (8 * MathF.Sqrt(2));

        // x1
        model.faces[0] = new(0 + offset, 1, 1 - offset, 0 + offset, 0, 1 - offset, 1 - offset, 0, 0 + offset, 1 - offset, 1, 0 + offset,
            uvs[0], uvs[0] + 1, RawDirection.WEST, true, true);
        // x2
        model.faces[1] = new(0 + offset, 1, 0 + offset, 0 + offset, 0, 0 + offset, 1 - offset, 0, 1 - offset, 1 - offset, 1, 1 - offset,
            uvs[1], uvs[1] + 1, RawDirection.SOUTH, true, true);
        // x1 rear
        model.faces[2] = new(1 - offset, 1, 0 + offset, 1 - offset, 0, 0 + offset, 0 + offset, 0, 1 - offset, 0 + offset, 1, 1 - offset,
            uvs[0], uvs[0] + 1, RawDirection.EAST, true, true);
        // x2 rear
        model.faces[3] = new(1 - offset, 1, 1 - offset, 1 - offset, 0, 1 - offset, 0 + offset, 0, 0 + offset, 0 + offset, 1, 0 + offset,
            uvs[1], uvs[1] + 1, RawDirection.NORTH, true, true);
        return model;
    }

    public static BlockModel makeStairs(UVPair[] uvs) {
        var model = new BlockModel();
        model.faces = new Face[10];

        // bottom
        model.faces[0] = new(1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, uvs[4], uvs[4] + 1, RawDirection.DOWN);
        // top top
        model.faces[1] = new(0, 1, 1, 0, 1, 0.5f, 1, 1, 0.5f, 1, 1, 1, uvs[5], uvs[5] + new UVPair(1, 0.5f), RawDirection.UP, true, true);
        // top bottom
        model.faces[2] = new(0, 0.5f, 0.5f, 0, 0.5f, 0, 1, 0.5f, 0, 1, 0.5f, 0.5f, uvs[5] + new UVPair(0.5f, 0), uvs[5] + new UVPair(1, 0.5f), RawDirection.UP, true, true);
        // left bottom
        model.faces[3] = new(0, 0.5f, 1, 0, 0, 1, 0, 0, 0, 0, 0.5f, 0, uvs[0] + new UVPair(0, 0.5f), uvs[0] + new UVPair(0.5f, 1), RawDirection.WEST, true, true);
        // left top
        model.faces[4] = new(0, 1, 1, 0, 0.5f, 1, 0, 0.5f, 0.5f, 0, 1, 0.5f, uvs[0], uvs[0] + new UVPair(0.5f, 0.5f), RawDirection.WEST, true, true);
        // right bottom
        model.faces[5] = new(1, 0.5f, 0, 1, 0, 0, 1, 0, 1, 1, 0.5f, 1, uvs[1] + new UVPair(0, 0.5f), uvs[0] + 1, RawDirection.EAST, true, true);
        // right top
        model.faces[6] = new(1, 1, 0.5f, 1, 0.5f, 0.5f, 1, 0.5f, 1, 1, 1, 1, uvs[1] + new UVPair(0.5f, 0), uvs[1] + new UVPair(1, 0.5f), RawDirection.EAST, true, true);
        // back
        model.faces[7] = new(1, 1, 1, 1, 0, 1, 0, 0, 1, 0, 1, 1, uvs[3], uvs[3] + 1, RawDirection.NORTH);
        // front top
        model.faces[8] = new(0, 1, 0.5f, 0, 0.5f, 0.5f, 1, 0.5f, 0.5f, 1, 1, 0.5f, uvs[2], uvs[2] + new UVPair(1, 0.5f), RawDirection.SOUTH, true, true);
        // front bottom
        model.faces[9] = new(0, 0.5f, 0, 0, 0, 0, 1, 0, 0, 1, 0.5f, 0, uvs[2] + new UVPair(0, 0.5f), uvs[2] + 1, RawDirection.SOUTH, true, true);

        return model;
    }

    public static BlockModel emptyBlock() {
        var model = new BlockModel();
        model.faces = new Face[0];
        return model;
    }
}