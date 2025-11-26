namespace BlockGame.world.entity;

/**
 * Regeneration effect - heals entity over time.
 * value = total HP to heal, distributed over duration.
 */
public class RegenEffect : Effect {
    public double value;  // total HP remaining to heal

    public RegenEffect(int duration, double totalHealing, int amplifier = 0) : base(duration, amplifier) {
        this.value = totalHealing;
    }

    public override void tick(Entity entity) {
        if (duration <= 0 || value <= 0) {
            return;
        }

        // distribute healing evenly over remaining duration
        double healPerTick = value / duration;

        entity.heal(healPerTick);
        value -= healPerTick;
    }

    public override int getID() {
        return EffectRegistry.REGEN;
    }
}
