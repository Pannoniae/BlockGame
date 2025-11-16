namespace BlockGame.main;

public static class Net {
    public static NetMode mode;
}

[Flags]
public enum NetMode {
    NONE = 0,
    SP = 1,
    MPC = 2,
    DED = 4,

    BOTH = SP | MPC | DED,
    CL = SP | MPC,
    SRV = SP | DED
}

public static class NetModeExt {
    extension(NetMode mode) {
        public bool isSP() {
            return (mode & NetMode.SP) != 0;
        }

        public bool isMPC() {
            return (mode & NetMode.MPC) != 0;
        }

        public bool isDed() {
            return (mode & NetMode.DED) != 0;
        }

        public bool isCL() {
            return (mode & NetMode.CL) != 0;
        }

        public bool isSRV() {
            return (mode & NetMode.SRV) != 0;
        }

        public bool isBoth() {
            return (mode & NetMode.BOTH) == NetMode.BOTH;
        }

        public bool isNone() {
            return mode == NetMode.NONE;
        }
    }
}