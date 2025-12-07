namespace BlockGame.mod;

/**
 * Interface for normal mods.

 */
public interface Mod {
    /**
     * Called after vanilla blocks are registered, before postLoad.
     * This is where mods should register their custom blocks, items, etc.
     */
    void onInit();
}