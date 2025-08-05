using System.Numerics;
using BlockGame.util;

namespace BlockGame;

public class MatrixStack {
    private Stack<Matrix4x4> stack = new();
    
    private bool reverse;

    public MatrixStack() {
        stack.Push(Matrix4x4.Identity);
    }

    /** Reverses the order of operations in the stack.
     * This means that when you apply transformations, they will be applied in reverse order.
     * For example, if you call `scale`, then `translate`, the translate will be applied first,
     * followed by the scale, instead of the usual order.
     */
    public MatrixStack reversed() {
        reverse = !reverse; 
        return this;
    }

    public Matrix4x4 top => stack.Peek();

    public void push() {
        stack.Push(stack.Peek());
    }

    public void pop() {
        if (stack.Count > 1) {
            stack.Pop();
        }
        else {
            throw new SkillIssueException("Cannot pop the last matrix from the stack.");
        }
    }
    
    public void loadIdentity() {
        stack.Pop();
        stack.Push(Matrix4x4.Identity);
    }

    public void scale(float sc) {
        var current = stack.Pop();
        var a = current;
        var b = Matrix4x4.CreateScale(sc);
        swapIf(ref a, ref b);
    }

    private void swapIf(ref Matrix4x4 a, ref Matrix4x4 b) {
        if (reverse) {
            (a, b) = (b, a);
        }
        stack.Push(a * b);
    }

    public void scale(int x, int y, int z) {
        var current = stack.Pop();
        var a = current;
        var b = Matrix4x4.CreateScale(x, y, z);
        swapIf(ref a, ref b);
    }
    
    public void scale(float x, float y, float z) {
        var current = stack.Pop();
        var a = current;
        var b = Matrix4x4.CreateScale(x, y, z);
        swapIf(ref a, ref b);
        
    }

    public void rotate(float angle, int x, int y, int z) {
        var current = stack.Pop();
        var axis = Vector3.Normalize(new Vector3(x, y, z));
        var a = current;
        var b = Matrix4x4.CreateFromAxisAngle(axis, Meth.deg2rad(angle));
        swapIf(ref a, ref b);
    }
    
    public void rotate(float angle, float x, float y, float z) {
        var current = stack.Pop();
        var axis = Vector3.Normalize(new Vector3(x, y, z));
        var a = current;
        var b = Matrix4x4.CreateFromAxisAngle(axis, Meth.deg2rad(angle));
        swapIf(ref a, ref b);
    }

    public void translate(int x, int y, int z) {
        var current = stack.Pop();
        var a = current;
        var b = Matrix4x4.CreateTranslation(x, y, z);
        swapIf(ref a, ref b);
    }
    
    public void translate(float x, float y, float z) {
        var current = stack.Pop();
        var a = current;
        var b = Matrix4x4.CreateTranslation(x, y, z);
        swapIf(ref a, ref b);
    }
    
    public void multiply(Matrix4x4 matrix) {
        var current = stack.Pop();
        var a = current;
        var b = matrix;
        swapIf(ref a, ref b);
    }
}
