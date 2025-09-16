namespace BlockGame.render.model;

/** The geometry of an entity. */
public abstract class EntityModel {

    /** Setup anim for render so we don't have to copypaste everything */
    public void anim(double t, double yaw, double pitch, double scale) {
        
    }
    
    public void render(double t, double yaw, double pitch, double scale) {
    }
}