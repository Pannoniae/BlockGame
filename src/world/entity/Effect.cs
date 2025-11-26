namespace BlockGame.world.entity;

/**
 * Base class for status effects applied to entities.
 * Subclass to create new effect types with custom tick behaviour.
 */
public abstract class Effect {
    public int duration;      // ticks remaining
    public int amplifier;     // effect strength (0 = level 1)

    public Effect(int duration, int amplifier = 0) {
        this.duration = duration;
        this.amplifier = amplifier;
    }

    /**
     * Called each game tick while effect is active.
     * Override to implement effect behaviour.
     */
    public abstract void tick(Entity entity);

    /**
     * Runtime int ID for this effect type.
     */
    public abstract int getID();

    /**
     * Check if effect has expired.
     */
    public bool isExpired() {
        return duration <= 0;
    }

    /**
     * Decrement duration by one tick.
     */
    public void age() {
        duration--;
    }
}
