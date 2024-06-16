using System.Numerics;

namespace BlockGame;

/// <summary>
/// A MatrixStack class handling matrix operations.
/// </summary>
public class MatrixStack {
    /// <summary>
    /// The stack of matrices.
    /// </summary>
    public List<Matrix4x4> stack;

    /// <summary>
    /// The current matrix.
    /// </summary>
    public Matrix4x4 top {
        get => stack[^1];
        set => stack[^1] = value;
    }

    /// <summary>
    /// Creates a new MatrixStack.
    /// </summary>
    public MatrixStack() {
        stack = new List<Matrix4x4>();
    }

    /// <summary>
    /// Loads the identity matrix on top.
    /// </summary>
    public void loadIdentity() {
        top = Matrix4x4.Identity;
    }

    /// <summary>
    /// Pushes the current matrix to the stack.
    /// </summary>
    public void push() {
        stack.Add(top);
    }

    /// <summary>
    /// Pops the top matrix from the stack.
    /// </summary>
    public void pop() {
        stack.RemoveAt(stack.Count - 1);
    }

    /// <summary>
    /// Multiplies the current matrix by another matrix.
    /// </summary>
    public void multiply(Matrix4x4 matrix) {
        top *= matrix;
    }

    /// <summary>
    /// Sets the current matrix to another matrix.
    /// </summary>
    public void set(Matrix4x4 matrix) {
        top = matrix;
    }
}