namespace BlockGame;

public class EmptyBlockData : BlockData {

    public ushort this[int x, int y, int z] {
        get => 0;
        set { }
    }
}