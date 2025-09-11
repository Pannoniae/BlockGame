namespace BlockGame.util;

public class BlockModel {
    public Face[] faces;

    public static BlockModel makeCube(Block bl) {
        var uvs = bl.uvs;
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
    public static BlockModel makeLiquid(Block bl) {
        var uvs = bl.uvs;
        // make it slightly smaller to avoid zfighting
        var model = new BlockModel();
        model.faces = new Face[6];

        const float offset = 0;

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
    public static BlockModel makeGrass(Block bl) {
        var uvs = bl.uvs;
        var model = new BlockModel();
        model.faces = new Face[4];

        // offset from edge
        var offset = 1 / 16 * mathsqrt(2);

        // x1
        model.faces[0] = new(0 + offset, 1, 1 - offset, 0 + offset, 0, 1 - offset, 1 - offset, 0, 0 + offset, 1 - offset, 1, 0 + offset,
            uvs[0], uvs[0] + 1, RawDirection.NONE, true, true);
        // x2
        model.faces[1] = new(0 + offset, 1, 0 + offset, 0 + offset, 0, 0 + offset, 1 - offset, 0, 1 - offset, 1 - offset, 1, 1 - offset,
            uvs[1], uvs[1] + 1, RawDirection.NONE, true, true);
        // x1 rear
        model.faces[2] = new(1 - offset, 1, 0 + offset, 1 - offset, 0, 0 + offset, 0 + offset, 0, 1 - offset, 0 + offset, 1, 1 - offset,
            uvs[0], uvs[0] + 1, RawDirection.NONE, true, true);
        // x2 rear
        model.faces[3] = new(1 - offset, 1, 1 - offset, 1 - offset, 0, 1 - offset, 0 + offset, 0, 0 + offset, 0 + offset, 1, 0 + offset,
            uvs[1], uvs[1] + 1, RawDirection.NONE, true, true);
        return model;
    }

    private static int mathsqrt(int i) {
        return 0;
    }

    public static BlockModel makeStairs(Block bl) {
        var uvs = bl.uvs;
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

    //make a 8x8 pixel half cube
    public static BlockModel makeHalfCube(Block bl) {
        var uvs = bl.uvs;
        var model = new BlockModel();
        model.faces = new Face[6];
        // west
        model.faces[0] = new(0.25f, 0.5f, 0.75f, 0.25f, 0, 0.75f, 0.25f, 0, 0.25f, 0.25f, 0.5f, 0.25f, uvs[0], uvs[0] + 0.5f, RawDirection.WEST, true, true);
        // east
        model.faces[1] = new(0.75f, 0.5f, 0.25f, 0.75f, 0, 0.25f, 0.75f, 0, 0.75f, 0.75f, 0.5f, 0.75f, uvs[0] + new UVPair(0.5f, 0), uvs[0] + new UVPair(1, 0.5f), RawDirection.EAST, true, true);
        // south
        model.faces[2] = new(0.25f, 0.5f, 0.25f, 0.25f, 0, 0.25f, 0.75f, 0, 0.25f, 0.75f, 0.5f, 0.25f, uvs[0] + new UVPair(0, 0.5f), uvs[0] + new UVPair(0.5f, 1), RawDirection.SOUTH, true, true);
        // north
        model.faces[3] = new(0.75f, 0.5f, 0.75f, 0.75f, 0, 0.75f, 0.25f, 0, 0.75f, 0.25f, 0.5f, 0.75f, uvs[0] + new UVPair(0.5f, 0.5f), uvs[0] + new UVPair(1, 1), RawDirection.NORTH, true, true);
        // down
        model.faces[4] = new(0.75f, 0, 0.75f, 0.75f, 0, 0.25f, 0.25f, 0, 0.25f, 0.25f, 0, 0.75f, uvs[1], uvs[1] + 0.5f, RawDirection.DOWN, true, true);
        // up
        model.faces[5] = new(0.25f, 0.5f, 0.75f, 0.25f, 0.5f, 0.25f, 0.75f, 0.5f, 0.25f, 0.75f, 0.5f, 0.75f, uvs[1] + new UVPair(0, 0.5f), uvs[1] + new UVPair(0.5f, 1), RawDirection.UP, true, true);
        return model;

    }
    
    //make slab
    public static BlockModel makeSlab(Block bl) {
        var uvs = bl.uvs;
        var model = new BlockModel();
        model.faces = new Face[6];
        // west
        model.faces[0] = new(0, 0.5f, 1, 0, 0, 1, 0, 0, 0, 0, 0.5f, 0, uvs[0], uvs[0] + 0.5f, RawDirection.WEST, true, true);
        // east
        model.faces[1] = new(1, 0.5f, 0, 1, 0, 0, 1, 0, 1, 1, 0.5f, 1, uvs[0], uvs[0] + 0.5f, RawDirection.EAST, true, true);
        // south
        model.faces[2] = new(0, 0.5f, 0, 0, 0, 0, 1 ,0, 0, 1, 0.5f, 0, uvs[0], uvs[0] + 0.5f, RawDirection.SOUTH, true, true);
        // north
        model.faces[3] = new(1, 0.5f, 1, 1, 0, 1, 0, 0, 1, 0, 0.5f, 1, uvs[0], uvs[0] + 0.5f, RawDirection.NORTH, true, true);
        // down
        model.faces[4] = new(1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, uvs[0], uvs[0] + 0.5f, RawDirection.DOWN, true, true);
        // up
        model.faces[5] = new(0, 0.5f, 1, 0, 0.5f, 0, 1, 0.5f, 0, 1, 0.5f, 1, uvs[0], uvs[0] + 0.5f, RawDirection.UP, true, true);
        return model;

    }

    //make a 12x14 pixel partial cube
    public static BlockModel makeLantern(Block bl) {
        var uvs = bl.uvs;
        var model = new BlockModel(); 
        const float offsetH = 1 / 16f;
        const float offset = 2 / 16f;
        const float offset1 = 7 / 16f;

        model.faces = new Face[11];
        // west
        model.faces[0] = new(0 + offset, 1 - offsetH, 1 - offset, 0 + offset, 0, 1 - offset, 0 + offset, 0, 0 + offset, 0 + offset, 1 - offsetH, 0 + offset, uvs[0] + new UVPair(offset,offsetH), uvs[0] + new UVPair(1 - offset, 1), RawDirection.WEST, true, true);
        // east
        model.faces[1] = new(1 - offset, 1 - offsetH, 0 + offset, 1 - offset, 0, 0 + offset, 1 - offset, 0, 1 - offset, 1 - offset, 1 - offsetH, 1 - offset, uvs[0] + new UVPair(offset,offsetH), uvs[0] + new UVPair(1 - offset, 1), RawDirection.EAST, true, true);
        // south
        model.faces[2] = new(0 + offset, 1 - offsetH, 0 + offset, 0 + offset, 0, 0 + offset, 1 - offset, 0, 0 + offset, 1 - offset, 1 - offsetH, 0 + offset, uvs[0] + new UVPair(offset,offsetH), uvs[0] + new UVPair(1 - offset, 1), RawDirection.SOUTH, true, true);
        // north
        model.faces[3] = new(1 - offset, 1 - offsetH, 1 - offset, 1 - offset, 0, 1 - offset, 0 + offset, 0, 1 - offset, 0 + offset, 1 - offsetH, 1 - offset, uvs[0] + new UVPair(offset,offsetH), uvs[0] + new UVPair(1 - offset, 1), RawDirection.NORTH, true, true);
        // down
        model.faces[4] = new(1 - offset, 0, 1 - offset, 1 - offset, 0, 0 + offset, 0 + offset, 0, 0 + offset, 0 + offset, 0, 1 - offset, uvs[1] + offset, uvs[1] + (1 - offset), RawDirection.DOWN, true, true);
        // up
        model.faces[5] = new(0 + offset, 1 - offsetH, 1 - offset, 0 + offset, 1 - offsetH, 0 + offset, 1 - offset, 1 - offsetH, 0 + offset, 1 - offset, 1 - offsetH, 1 - offset, uvs[2] + offset, uvs[2] + (1 - offset), RawDirection.UP, true, true);

        model.faces[6] = new(0 + offset1, 1, 1 - offset1, 0 + offset1, 1 - offsetH, 1 - offset1, 0 + offset1, 1 - offsetH, 0 + offset1, 0 + offset1, 1, 0 + offset1, uvs[1] + new UVPair(offset, offset), uvs[1] + new UVPair(1-2*offset, 1-2*offset), RawDirection.WEST, true, true);
        model.faces[7] = new(1 - offset1, 1, 0 + offset1, 1 - offset1, 1 - offsetH, 0 + offset1, 1 - offset1, 1 - offsetH, 1 - offset1, 1 - offset1, 1, 1 - offset1, uvs[1] + new UVPair(offset, offset), uvs[1] + new UVPair(1-2*offset, 1-2*offset), RawDirection.EAST, true, true);
        model.faces[8] = new(0 + offset1, 1, 0 + offset1, 0 + offset1, 1 - offsetH, 0 + offset1, 1 - offset1, 1 - offsetH, 0 + offset1, 1 - offset1, 1, 0 + offset1, uvs[1] + new UVPair(offset, offset), uvs[1] + new UVPair(1-2*offset, 1-2*offset), RawDirection.SOUTH, true, true);
        model.faces[9] = new(1 - offset1, 1, 1 - offset1, 1 - offset1, 1 - offsetH, 1 - offset1, 0 + offset1, 1 - offsetH, 1 - offset1, 0 + offset1, 1, 1 - offset1, uvs[1] + new UVPair(offset, offset), uvs[1] + new UVPair(1-2*offset, 1-2*offset), RawDirection.NORTH, true, true);
        model.faces[10] = new(0 + offset1, 1, 1 - offset1, 0 + offset1, 1, 0 + offset1, 1 - offset1, 1, 0 + offset1, 1 - offset1, 1, 1 - offset1, uvs[1] + new UVPair(offset, offset), uvs[1] + new UVPair(1-2*offset, 1-2*offset), RawDirection.UP, true, true);
        return model;
    }

    //makeTorch
    public static BlockModel makeTorch(Block bl) {
        var uvs = bl.uvs;
        var model = new BlockModel();
        model.faces = new Face[10];
        //bottom
        //west
        model.faces[0] = new(7 / 16f, 0.5f, 9 / 16f, 7 / 16f, 0, 9 / 16f, 7 / 16f, 0, 7 / 16f, 7 / 16f, 0.5f, 7 / 16f, uvs[0] + new UVPair(1 / 16f, 0.5f), uvs[0] + new UVPair(3 / 16f, 1), RawDirection.WEST, true, true);
        // east
        model.faces[1] = new(9 / 16f, 0.5f, 7 / 16f, 9 / 16f, 0, 7 / 16f, 9 / 16f, 0, 9 / 16f, 9 / 16f, 0.5f, 9 / 16f, uvs[0] + new UVPair(1 / 16f, 0.5f), uvs[0] + new UVPair(3 / 16f, 1), RawDirection.EAST, true, true);
        // south
        model.faces[2] = new(7 / 16f, 0.5f, 7 / 16f, 7 / 16f, 0, 7 / 16f, 9 / 16f, 0, 7 / 16f, 9 / 16f, 0.5f, 7 / 16f, uvs[0] + new UVPair(1 / 16f, 0.5f), uvs[0] + new UVPair(3 / 16f, 1), RawDirection.SOUTH, true, true);
        // north
        model.faces[3] = new(9 / 16f, 0.5f, 9 / 16f, 9 / 16f, 0, 9 / 16f, 7 / 16f, 0, 9 / 16f, 7 / 16f, 0.5f, 9 / 16f, uvs[0] + new UVPair(1 / 16f, 0.5f), uvs[0] + new UVPair(3 / 16f, 1), RawDirection.NORTH, true, true);
        // down
        model.faces[4] = new(9 / 16f, 0, 9 / 16f, 9 / 16f, 0, 7 / 16f, 7 / 16f, 0, 7 / 16f, 7 / 16f, 0, 9 / 16f, uvs[4] + new UVPair(0, 4 / 16f), uvs[4] + 4 / 16f, RawDirection.DOWN, true, true);

        //top
        //west
        model.faces[5] = new(6 / 16f, 15 / 16f, 10 / 16f, 6 / 16f, 0.5f, 10 / 16f, 6 / 16f, 0.5f, 6 / 16f, 6 / 16f, 15 / 16f, 6 / 16f, uvs[0] + new UVPair(4 / 16f, 1 / 16f), uvs[0] + new UVPair(0, 0.5f), RawDirection.WEST, true, true);
        // east
        model.faces[6] = new(10 / 16f, 15 / 16f, 6 / 16f, 10 / 16f, 0.5f, 6 / 16f, 10 / 16f, 0.5f, 10 / 16f, 10 / 16f, 15 / 16f, 10 / 16f, uvs[0] + new UVPair(4 / 16f, 1 / 16f), uvs[0] + new UVPair(0, 0.5f), RawDirection.EAST, true, true);
        // south
        model.faces[7] = new(6 / 16f, 15 / 16f, 6 / 16f, 6 / 16f, 0.5f, 6 / 16f, 10 / 16f, 0.5f, 6 / 16f, 10 / 16f, 15 / 16f, 6 / 16f, uvs[0] + new UVPair(0, 1 / 16f), uvs[0] + new UVPair(4 / 16f, 0.5f), RawDirection.SOUTH, true, true);
        // north
        model.faces[8] = new(10 / 16f, 15 / 16f, 10 / 16f, 10 / 16f, 0.5f, 10 / 16f, 6 / 16f, 0.5f, 10 / 16f, 6 / 16f, 15 / 16f, 10 / 16f, uvs[0] + new UVPair(0, 1 / 16f), uvs[0] + new UVPair(4 / 16f, 0.5f), RawDirection.NORTH, true, true);

        // up
        model.faces[9] = new(6 / 16f, 15 / 16f, 10 / 16f, 6 / 16f, 15 / 16f, 6 / 16f, 10 / 16f, 15 / 16f, 6 / 16f, 10 / 16f, 15 / 16f, 10 / 16f, uvs[5], uvs[5] + new UVPair(4 / 16f, 4 / 16f), RawDirection.UP, true, true);

        return model;
    }

    public static BlockModel emptyBlock() {
        var model = new BlockModel();
        model.faces = [];
        return model;
    }
}
