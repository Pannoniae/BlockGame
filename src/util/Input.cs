using Silk.NET.Input;

namespace BlockGame.util;

public class Input : IEquatable<Input> {
    public readonly string name;

    public int key;
    public readonly int defaultKey;

    public Input(string name, int defaultKey) {
        this.name = name;
        this.defaultKey = defaultKey;
        this.key = defaultKey;

        InputTracker.all.Add(this);
    }

    public void bind(int key) {
        this.key = key;
    }

    public void bind(Key key) {
        bind((int)key);
    }


    public void bind(MouseButton button) {
        bind(-(int)button);
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
        return name + " (" + (key == InputTracker.DUMMY ? "unbound" : InputTracker.getKeyName(key)) + ")";
    }

    public bool Equals(Input? other) {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return name == other.name;
    }

    public override bool Equals(object? obj) {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Input)obj);
    }

    public override int GetHashCode() {
        return name.GetHashCode();
    }

    public static bool operator ==(Input left, Input right) {
        return left.Equals(right);
    }

    public static bool operator !=(Input left, Input right) {
        return !left.Equals(right);
    }
}