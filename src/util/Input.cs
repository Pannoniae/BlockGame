namespace BlockGame.util;

public class Input {
    public string name;

    public int key;
    public int defaultKey;
    
    public Input(string name, int defaultKey) {
        this.name = name;
        this.defaultKey = defaultKey;
        this.key = defaultKey;
    }
    
    public void bind(int key) {
        this.key = key;
    }
    
    public void reset() {
        key = defaultKey;
    }
    
    public bool bound() {
        return key != defaultKey;
    }
    
    public bool pressed() {
        return InputTracker.pressed(key);
    }
    
    public bool down() {
        return InputTracker.down(key);
    }
    
    public bool released() {
        return InputTracker.released(key);
    }
    
    public override string ToString() {
        return name + " (" + (key == -1 ? "unbound" : InputTracker.getKeyName(key)) + ")";
    }
    
}