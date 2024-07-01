using BlockGame.util;

namespace BlockGame;

public class Entity {
    public const int MAX_SWING_TICKS = 8;
    public const int AIR_HIT_CD = 10;

    public int airHitCD;

    public int swingTicks;
    public bool swinging;

    /// 0 to 1
    public double prevSwingProgress;
    public double swingProgress;

    public double getSwingProgress(double dt) {
        var value = double.Lerp(prevSwingProgress, swingProgress, dt);
        // if it just finished swinging, lerp to 1
        if (prevSwingProgress != 0 && swingProgress == 0) {
            value = double.Lerp(prevSwingProgress, 1, dt);
        }
        return value;
    }

    public void updateSwing() {
        swingProgress = (double)swingTicks / MAX_SWING_TICKS;
        if (swinging) {
            swingTicks++;
            if (swingTicks >= MAX_SWING_TICKS) {
                swinging = false;
                swingTicks = 0;
            }
        }
        else {
            swingTicks = 0;
        }
        if (airHitCD > 0) {
            airHitCD--;
        }
        Console.Out.WriteLine(airHitCD);
    }

    public void setSwinging(bool hit) {
        if (!hit) {
            if (airHitCD == 0) {
                swinging = true;
                swingTicks = 0;
                airHitCD = AIR_HIT_CD;
            }
        }
        else {
            swinging = true;
            swingTicks = 0;
        }
    }
}