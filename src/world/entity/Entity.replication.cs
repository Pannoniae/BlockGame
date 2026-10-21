using System.Numerics;
using BlockGame.main;
using BlockGame.net;
using BlockGame.render;
using BlockGame.util;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

public partial class Entity {
    private const int SNAPSHOTS = 16;
    private const int SNAP_MASK = SNAPSHOTS - 1;

    /** how far past the newest snapshot we can extrapolate without making it drift too much */
    private const double MAX_EXTRAPOLATE = 9;

    private int[]? snapshotTick;
    private Vector3D[]? snapshotPos;
    private Vector3[]? snapshotRot;

    /** index of the newest snapshot; -1 = none yet */
    private int snapshotHead = -1;
    private int snapshotCount;

    /** set on spawn so the first real snapshot has something to interpolate from */
    public void resetSnapshots(Vector3D pos, Vector3 rot) {
        snapshotTick ??= new int[SNAPSHOTS];
        snapshotPos ??= new Vector3D[SNAPSHOTS];
        snapshotRot ??= new Vector3[SNAPSHOTS];

        snapshotHead = 0;
        snapshotCount = 1;
        snapshotTick[0] = NetClock.newest == int.MinValue ? 0 : NetClock.newest;
        snapshotPos[0] = pos;
        snapshotRot[0] = rot;

        position = prevPosition = pos;
        rotation = prevRotation = rot;
    }

    public void addSnapshot(int tick, Vector3D pos, Vector3 rot) {
        if (snapshotHead < 0) {
            resetSnapshots(pos, rot);
            snapshotTick![0] = tick;
            return;
        }

        var newestTick = snapshotTick![snapshotHead];

        // that won't do
        if (tick < newestTick) {
            return;
        }

        if (tick == newestTick) {
            snapshotPos![snapshotHead] = pos;
            snapshotRot![snapshotHead] = rot;
            return;
        }

        snapshotHead = (snapshotHead + 1) & SNAP_MASK;
        snapshotTick[snapshotHead] = tick;
        snapshotPos![snapshotHead] = pos;
        snapshotRot![snapshotHead] = rot;
        snapshotCount = int.Min(snapshotCount + 1, SNAPSHOTS);
    }

    // ============ HIT PREDICTION ============

    /** client tick of the last hit we predicted on this entity, -1 = none */
    private int predictedHurtTick = -1;

    private const int PREDICTION_WINDOW = 30;

    public void predictHurt(double damage) {
        if (dead || iframes > 0 && damage <= lastDmg) {
            return;
        }

        var shown = iframes > 0 ? damage - lastDmg : damage;
        lastDmg = damage;
        iframes = invulnerability;
        predictedHurtTick = world.worldTick;
        showHurt(shown);
    }

    public bool consumePredictedHurt() {
        if (predictedHurtTick < 0 || world.worldTick - predictedHurtTick > PREDICTION_WINDOW) {
            return false;
        }

        predictedHurtTick = -1;
        return true;
    }

    public void showHurt(double damage) {
        dmgTime = 30;
        iframes = invulnerability;

        if (!needsDamageNumbers || damage <= 0) {
            return;
        }

        var rng = Game.clientRandom;
        var h = aabb.y1 - position.Y;
        var off = new Vector3D(rng.NextSingle() * 0.14f, h + rng.NextSingle() * 0.17f, rng.NextSingle() * 0.14f);
        world.particles.add(new DamageNumber(world, position + off, damage));
    }

    protected virtual void interpolate(double dt) {
        if (snapshotHead < 0) {
            return;
        }

        var t = NetClock.renderTick;
        var n = snapshotHead;

        // newest snapshot is already in the past, extrapolate
        if (t >= snapshotTick![n]) {
            if (snapshotCount < 2) {
                position = snapshotPos![n];
                rotation = snapshotRot![n];
                return;
            }

            var p = (n - 1) & SNAP_MASK;
            var span = snapshotTick[n] - snapshotTick[p];
            var ahead = double.Min(t - snapshotTick[n], double.Min(span, MAX_EXTRAPOLATE));
            position = snapshotPos![n] + (snapshotPos[n] - snapshotPos[p]) * (ahead / span);
            rotation = snapshotRot![n];
        }
        else {
            // interpolate
            var b = n;
            var found = false;
            for (int i = 1; i < snapshotCount; i++) {
                var a = (b - 1) & SNAP_MASK;
                if (snapshotTick[a] <= t) {
                    var f = (t - snapshotTick[a]) / (snapshotTick[b] - snapshotTick[a]);
                    position = Vector3D.Lerp(snapshotPos![a], snapshotPos[b], f);
                    rotation = Meth.lerpAngle(snapshotRot![a], snapshotRot[b], (float)f);
                    found = true;
                    break;
                }

                b = a;
            }

            // older than anything we have (just spawned): the oldest is the best we've got
            if (!found) {
                position = snapshotPos![b];
                rotation = snapshotRot![b];
            }
        }

        if (dt > 0) {
            velocity = (position - prevPosition) / dt;
        }
    }
}
