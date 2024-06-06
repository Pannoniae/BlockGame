namespace BlockGame.util;

public class FIFOStack<T> : LinkedList<T> {
    public T pop() {
        T first = First.Value;
        RemoveFirst();
        return first;
    }

    public void push(T obj) {
        AddFirst(obj);
    }

    public T peek() {
        return First.Value;
    }

    public T peek(int index) {
        return this.ElementAt(index);
    }

    //Remove(T object) implemented in LinkedList
}