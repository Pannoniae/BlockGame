using Mono.Cecil;

namespace BlockGame.mod;

/**
 * Interface for coremods.
 * Coremods run before game loads and can transform the game assembly using Mono.Cecil.
 */
public interface Coremod {
    /**
     * Patch the game assembly before it loads.
     * Use Mono.Cecil to modify IL code, add methods, change type definitions, etc.
     */
    void patch(ModuleDefinition gameModule);
}