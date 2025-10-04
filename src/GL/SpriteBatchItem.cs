using System.Numerics;
using Molten;

namespace BlockGame.GL
{
    /// <summary>
    /// Used internally by TextureBatcher to store the vertices for each Draw() call.
    /// </summary>
    internal sealed class SpriteBatchItem : IComparable<SpriteBatchItem>
    {
        /// <summary>The texture to draw the vertices with.</summary>
        public BTexture2D? Texture;

        /// <summary>A value used for sorting based on the BatcherBeginMode.</summary>
        public float SortValue;

        /// <summary>The top-left vertex.</summary>
        public VertexColorTexture VertexTL;
        /// <summary>The top-right vertex.</summary>
        public VertexColorTexture VertexTR;
        /// <summary>The bottom-left vertex.</summary>
        public VertexColorTexture VertexBL;
        /// <summary>The bottom-right vertex.</summary>
        public VertexColorTexture VertexBR;

        // Basic setup with position, source, and color
        public void SetValue(BTexture2D texture, Vector2 position, Rectangle source, Color color, float depth)
        {
            Texture = texture;

            VertexTL.Position = new Vector3(position, depth);
            VertexTR.Position = new Vector3(position.X + source.Width, position.Y, depth);
            VertexBL.Position = new Vector3(position.X, position.Y + source.Height, depth);
            VertexBR.Position = new Vector3(position + new Vector2(source.Width, source.Height), depth);

            VertexTL.TexCoords = new Vector2(source.X / (float)texture.width, source.Y / (float)texture.height);
            VertexBR.TexCoords = new Vector2(source.Right / (float)texture.width, source.Bottom / (float)texture.height);
            VertexTR.TexCoords = new Vector2(VertexBR.TexCoords.X, VertexTL.TexCoords.Y);
            VertexBL.TexCoords = new Vector2(VertexTL.TexCoords.X, VertexBR.TexCoords.Y);

            VertexTL.Color = color;
            VertexTR.Color = color;
            VertexBL.Color = color;
            VertexBR.Color = color;
        }

        // With scale
        public void SetValue(BTexture2D texture, Vector2 position, Rectangle source, Color color, Vector2 scale, float depth)
        {
            Texture = texture;

            Vector2 size = new Vector2(source.Width, source.Height) * scale;
            VertexTL.Position = new Vector3(position, depth);
            VertexTR.Position = new Vector3(position.X + size.X, position.Y, depth);
            VertexBL.Position = new Vector3(position.X, position.Y + size.Y, depth);
            VertexBR.Position = new Vector3(position + size, depth);

            VertexTL.TexCoords = new Vector2(source.X / (float)texture.width, source.Y / (float)texture.height);
            VertexBR.TexCoords = new Vector2(source.Right / (float)texture.width, source.Bottom / (float)texture.height);
            VertexTR.TexCoords = new Vector2(VertexBR.TexCoords.X, VertexTL.TexCoords.Y);
            VertexBL.TexCoords = new Vector2(VertexTL.TexCoords.X, VertexBR.TexCoords.Y);

            VertexTL.Color = color;
            VertexTR.Color = color;
            VertexBL.Color = color;
            VertexBR.Color = color;
        }

        // With scale, rotation, and origin
        public void SetValue(BTexture2D texture, Vector2 position, Rectangle source, Color color, Vector2 scale,
                            float rotation, Vector2 origin, float depth)
        {
            Texture = texture;

            Vector2 tl = -origin * scale;
            Vector2 tr = new Vector2(tl.X + source.Width * scale.X, tl.Y);
            Vector2 bl = new Vector2(tl.X, tl.Y + source.Height * scale.Y);
            Vector2 br = new Vector2(tr.X, bl.Y);

            float sin = MathF.Sin(rotation);
            float cos = MathF.Cos(rotation);
            VertexTL.Position = new Vector3(cos * tl.X - sin * tl.Y + position.X, sin * tl.X + cos * tl.Y + position.Y, depth);
            VertexTR.Position = new Vector3(cos * tr.X - sin * tr.Y + position.X, sin * tr.X + cos * tr.Y + position.Y, depth);
            VertexBL.Position = new Vector3(cos * bl.X - sin * bl.Y + position.X, sin * bl.X + cos * bl.Y + position.Y, depth);
            VertexBR.Position = new Vector3(cos * br.X - sin * br.Y + position.X, sin * br.X + cos * br.Y + position.Y, depth);

            VertexTL.TexCoords = new Vector2(source.X / (float)texture.width, source.Y / (float)texture.height);
            VertexBR.TexCoords = new Vector2(source.Right / (float)texture.width, source.Bottom / (float)texture.height);
            VertexTR.TexCoords = new Vector2(VertexBR.TexCoords.X, VertexTL.TexCoords.Y);
            VertexBL.TexCoords = new Vector2(VertexTL.TexCoords.X, VertexBR.TexCoords.Y);

            VertexTL.Color = color;
            VertexTR.Color = color;
            VertexBL.Color = color;
            VertexBR.Color = color;
        }

        // With worldMatrix
        public void SetValue(BTexture2D texture, Vector2 position, ref Matrix4x4 worldMatrix, Rectangle source, 
                           Color color, Vector2 scale, float rotation, Vector2 origin, float depth)
        {
            Texture = texture;

            Vector2 tl = -origin * scale;
            Vector2 tr = new Vector2(tl.X + source.Width * scale.X, tl.Y);
            Vector2 bl = new Vector2(tl.X, tl.Y + source.Height * scale.Y);
            Vector2 br = new Vector2(tr.X, bl.Y);

            float sin = MathF.Sin(rotation);
            float cos = MathF.Cos(rotation);
            Vector3 pos1 = new Vector3(cos * tl.X - sin * tl.Y + position.X, sin * tl.X + cos * tl.Y + position.Y, depth);
            Vector3 pos2 = new Vector3(cos * tr.X - sin * tr.Y + position.X, sin * tr.X + cos * tr.Y + position.Y, depth);
            Vector3 pos3 = new Vector3(cos * bl.X - sin * bl.Y + position.X, sin * bl.X + cos * bl.Y + position.Y, depth);
            Vector3 pos4 = new Vector3(cos * br.X - sin * br.Y + position.X, sin * br.X + cos * br.Y + position.Y, depth);
            
            VertexTL.Position = Vector3.Transform(pos1, worldMatrix);
            VertexTR.Position = Vector3.Transform(pos2, worldMatrix);
            VertexBL.Position = Vector3.Transform(pos3, worldMatrix);
            VertexBR.Position = Vector3.Transform(pos4, worldMatrix);

            VertexTL.TexCoords = new Vector2(source.X / (float)texture.width, source.Y / (float)texture.height);
            VertexBR.TexCoords = new Vector2(source.Right / (float)texture.width, source.Bottom / (float)texture.height);
            VertexTR.TexCoords = new Vector2(VertexBR.TexCoords.X, VertexTL.TexCoords.Y);
            VertexBL.TexCoords = new Vector2(VertexTL.TexCoords.X, VertexBR.TexCoords.Y);

            VertexTL.Color = color;
            VertexTR.Color = color;
            VertexBL.Color = color;
            VertexBR.Color = color;
        }

        // With worldMatrix without rotation
        public void SetValue(BTexture2D texture, Vector2 position, ref Matrix4x4 worldMatrix, Rectangle source, 
                           Color color, Vector2 scale, Vector2 origin, float depth)
        {
            Texture = texture;

            Vector2 tl = position - origin * scale;
            Vector2 br = tl + new Vector2(source.Width, source.Height) * scale;
            
            VertexTL.Position = Vector3.Transform(new Vector3(tl, depth), worldMatrix);
            VertexTR.Position = Vector3.Transform(new Vector3(br.X, tl.Y, depth), worldMatrix);
            VertexBL.Position = Vector3.Transform(new Vector3(tl.X, br.Y, depth), worldMatrix);
            VertexBR.Position = Vector3.Transform(new Vector3(br, depth), worldMatrix);

            VertexTL.TexCoords = new Vector2(source.X / (float)texture.width, source.Y / (float)texture.height);
            VertexBR.TexCoords = new Vector2(source.Right / (float)texture.width, source.Bottom / (float)texture.height);
            VertexTR.TexCoords = new Vector2(VertexBR.TexCoords.X, VertexTL.TexCoords.Y);
            VertexBL.TexCoords = new Vector2(VertexTL.TexCoords.X, VertexBR.TexCoords.Y);

            VertexTL.Color = color;
            VertexTR.Color = color;
            VertexBL.Color = color;
            VertexBR.Color = color;
        }

        // With scale and origin
        public void SetValue(BTexture2D texture, Vector2 position, Rectangle source, Color color, Vector2 scale,
                           Vector2 origin, float depth)
        {
            Texture = texture;

            Vector2 tl = position - origin * scale;
            Vector2 br = tl + new Vector2(source.Width, source.Height) * scale;
            VertexTL.Position = new Vector3(tl, depth);
            VertexTR.Position = new Vector3(br.X, tl.Y, depth);
            VertexBL.Position = new Vector3(tl.X, br.Y, depth);
            VertexBR.Position = new Vector3(br, depth);

            VertexTL.TexCoords = new Vector2(source.X / (float)texture.width, source.Y / (float)texture.height);
            VertexBR.TexCoords = new Vector2(source.Right / (float)texture.width, source.Bottom / (float)texture.height);
            VertexTR.TexCoords = new Vector2(VertexBR.TexCoords.X, VertexTL.TexCoords.Y);
            VertexBL.TexCoords = new Vector2(VertexTL.TexCoords.X, VertexBR.TexCoords.Y);

            VertexTL.Color = color;
            VertexTR.Color = color;
            VertexBL.Color = color;
            VertexBR.Color = color;
        }

        // With RectangleF destination 
        public void SetValue(BTexture2D texture, RectangleF destination, Rectangle source, Color color, float depth)
        {
            Texture = texture;

            VertexTL.Position = new Vector3(destination.X, destination.Y, depth);
            VertexTR.Position = new Vector3(destination.Right, destination.Y, depth);
            VertexBL.Position = new Vector3(destination.X, destination.Bottom, depth);
            VertexBR.Position = new Vector3(destination.Right, destination.Bottom, depth);

            VertexTL.TexCoords = new Vector2(source.X / (float)texture.width, source.Y / (float)texture.height);
            VertexBR.TexCoords = new Vector2(source.Right / (float)texture.width, source.Bottom / (float)texture.height);
            VertexTR.TexCoords = new Vector2(VertexBR.TexCoords.X, VertexTL.TexCoords.Y);
            VertexBL.TexCoords = new Vector2(VertexTL.TexCoords.X, VertexBR.TexCoords.Y);

            VertexTL.Color = color;
            VertexTR.Color = color;
            VertexBL.Color = color;
            VertexBR.Color = color;
        }

        // With Matrix3x2
        public void SetValue(BTexture2D texture, Matrix3x2 transform, Rectangle source, Color color, float depth)
        {
            Texture = texture;            

            transform = Matrix3x2.CreateScale(source.Width, source.Height) * transform;
            
            VertexTL.Position = new Vector3(transform.Translation, depth);
            VertexTR.Position = new Vector3(transform.M11 + transform.M31, transform.M12 + transform.M32, depth);
            VertexBL.Position = new Vector3(transform.M21 + transform.M31, transform.M22 + transform.M32, depth);
            VertexBR.Position = new Vector3(transform.M11 + transform.M21 + transform.M31, 
                                          transform.M12 + transform.M22 + transform.M32, depth);            

            VertexTL.TexCoords = new Vector2(source.X / (float)texture.width, source.Y / (float)texture.height);
            VertexBR.TexCoords = new Vector2(source.Right / (float)texture.width, source.Bottom / (float)texture.height);
            VertexTR.TexCoords = new Vector2(VertexBR.TexCoords.X, VertexTL.TexCoords.Y);
            VertexBL.TexCoords = new Vector2(VertexTL.TexCoords.X, VertexBR.TexCoords.Y);

            VertexTL.Color = color;
            VertexTR.Color = color;
            VertexBL.Color = color;
            VertexBR.Color = color;
        }

        // With Matrix3x2 and origin
        public void SetValue(BTexture2D texture, Matrix3x2 transform, Rectangle source, Color color, Vector2 origin, float depth)
        {
            // Apply origin transform
            transform.Translation -= Vector2.TransformNormal(origin, transform);
            
            // Use the regular Matrix3x2 SetValue
            SetValue(texture, transform, source, color, depth);
        }

        // For raw vertices
        public void SetVertices(BTexture2D texture, VertexColorTexture vTL, VertexColorTexture vTR, VertexColorTexture vBR, VertexColorTexture vBL)
        {
            Texture = texture;
            VertexTL = vTL;
            VertexTR = vTR;
            VertexBR = vBR;
            VertexBL = vBL;
        }

        public int CompareTo(SpriteBatchItem? other)
        {
            return SortValue.CompareTo((float)other!.SortValue);
        }
    }
}