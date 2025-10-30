﻿using BlockGame.GL;
using BlockGame.util;
using BlockGame.util.stuff;
using BlockGame.world;
using BlockGame.world.entity;

namespace BlockGame.render.model;

/** Renders an entity with its associated model. Optionally applies effects like being hit, etc. */
public interface EntityRenderer<out T> where T : Entity {

    /**
     * We originally had pos/rot here in addition to scale but I kinda realised you can just 1. use the matrix stack for that if you want to modify 2. get it from the entity itself
     */
    public virtual void render(MatrixStack mat, Entity e, float scale, double interp) {

    }
}

public static class EntityRenderers {
    private static readonly XUList<EntityRenderer<Entity>> renderers = Registry.ENTITIES.track<EntityRenderer<Entity>>();

    public static readonly InstantDrawEntity ide = new(2048);

    static EntityRenderers() {
        ide.setup();
        reloadAll();
    }

    /** Register a renderer for an entity type */
    public static void register(int entityID, EntityRenderer<Entity> renderer) {
        renderers[entityID] = renderer;
    }

    /** Get renderer for an entity type */
    public static EntityRenderer<Entity> get(int entityID) {
        return renderers[entityID];
    }

    /** hot reload all entity models by recreating them */
    public static void reloadAll() {
        register(Entities.COW, new CowRenderer());
        register(Entities.PLAYER, new PlayerRenderer());
        register(Entities.ITEM_ENTITY, new ItemEntityRenderer());
    }
}