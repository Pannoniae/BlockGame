using System;

namespace BlockGame {
    public class Utils {
        public static string formatMB(long bytes) {
            var factor = 1 << 20;
            var mb = bytes / factor; // round off to nearest MB
            var rem = (float)(bytes - mb * factor) / factor; // find out how many % is remaining
            return $"{mb}{rem:.###}MB";
        }
    }
}