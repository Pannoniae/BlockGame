using System;
using BlockGame.util.log;

namespace BlockGame.world.worldgen;

public class WorldgenUtil {
    /**
     * Print noise resolution analysis to console.
     *
     * freq: base frequency (1/scale)
     * octaves: number of octaves
     * lacunarity: frequency multiplier per octave (typically 2.0)
     * gain: amplitude multiplier per octave (typically 0.5)
     */
    public static void printNoiseResolution(float freq, int octaves, float lacunarity = 2f, float gain = 0.5f) {
        // base wavelength (feature size of first octave)
        float baseWavelength = 1f / freq;

        // highest frequency (last octave)
        float highestFreq = freq * MathF.Pow(lacunarity, octaves - 1);
        float smallestWavelength = 1f / highestFreq;

        // calculate approximate gradient (change per block)
        // sum all octave contributions
        float totalGradient = 0f;
        for (int i = 0; i < octaves; i++) {
            float octaveFreq = freq * MathF.Pow(lacunarity, i);
            float octaveAmp = MathF.Pow(gain, i);
            totalGradient += octaveFreq * octaveAmp;
        }

        // highest octave gradient (most detail)
        float highestOctaveAmp = MathF.Pow(gain, octaves - 1);
        float highestGradient = highestFreq * highestOctaveAmp;

        // sufficiency check
        bool sufficient = smallestWavelength >= 1f && smallestWavelength <= 4f;
        string sufficiencyMsg;
        if (smallestWavelength < 1f) {
            sufficiencyMsg = "TOO MANY octaves - aliasing/waste";
        } else if (smallestWavelength > 4f) {
            sufficiencyMsg = "TOO FEW octaves - missing detail";
        } else {
            sufficiencyMsg = "GOOD";
        }

        Log.info("=== Noise Resolution Analysis ===");
        Log.info($"Frequency: {freq:F6} (scale: {1f/freq:F2})");
        Log.info($"Octaves: {octaves}, Lacunarity: {lacunarity}, Gain: {gain}");
        Log.info("\n");
        Log.info($"1. Largest feature size: {baseWavelength:F2} blocks");
        Log.info($"2. Smallest feature size: {smallestWavelength:F2} blocks");
        Log.info($"3. Max gradient (change/block): {totalGradient:F4}");
        Log.info($"   Highest octave gradient: {highestGradient:F4}");
        Log.info("\n");
        Log.info($"Octave sufficiency: {sufficiencyMsg}");
        Log.info("  (ideal: smallest feature = 1-4 blocks)");
        Log.info("\n");
    }
}