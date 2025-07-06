using System.Numerics;
using BlockGame.util;

namespace BlockGame;

public class MatrixStack {
    private Stack<Matrix4x4> stack = new();

    public MatrixStack() {
        stack.Push(Matrix4x4.Identity);
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
        var _ = stack.Pop();
        stack.Push(Matrix4x4.Identity);
    }

    public void scale(float sc) {
        var current = stack.Pop();
        stack.Push(current * Matrix4x4.CreateScale(sc));
    }

    public void scale(int x, int y, int z) {
        var current = stack.Pop();
        stack.Push(current * Matrix4x4.CreateScale(x, y, z));
    }

    public void rotate(float angle, int x, int y, int z) {
        var current = stack.Pop();
        var axis = Vector3.Normalize(new Vector3(x, y, z));
        stack.Push(current * Matrix4x4.CreateFromAxisAngle(axis, Meth.deg2rad(angle)));
    }

    public void translate(int x, int y, int z) {
        var current = stack.Pop();
        stack.Push(current * Matrix4x4.CreateTranslation(x, y, z));
    }
}
