namespace BlockGame.main;

public class Net {
    public static NetMode mode;
}

public enum NetMode {
    NONE = 0,
    SP = 1,
    MPC = 2,
    MPS = 4,

    BOTH = SP | MPC | MPS,
    CL = SP | MPC,
    SRV = SP | MPS
}