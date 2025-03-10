﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Numerics;

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize

namespace TrippyGL
{
    /// <summary>
    /// Represent a range of drawable text characters, where all the characters are stored
    /// inside a <see cref="Texture2D"/>. Also provides methods for measuring text.
    /// </summary>
    public abstract class TextureFont : IDisposable
    {
        /// <summary>The character <see cref="TextureFont"/>-s use to indicate a new line.</summary>
        public const char NewlineIndicator = '\n';

        /// <summary>The <see cref="Texture2D"/> containing this <see cref="TextureFont"/>'s characters.</summary>
        public readonly Texture2D Texture;

        /// <summary>This <see cref="TextureFont"/>'s size, typically measured in pixels.</summary>
        public readonly float Size;

        /// <summary>This <see cref="TextureFont"/>'s name.</summary>
        public readonly string Name;

        /// <summary>The lowest character available in this <see cref="TextureFont"/>.</summary>
        public readonly char FirstChar;

        /// <summary>The highest character available in this <see cref="TextureFont"/>.</summary>
        public readonly char LastChar;

        /// <summary>The amount of characters this <see cref="TextureFont"/> contains.</summary>
        /// <remarks>This is equal to LastChar - FirstChar + 1.</remarks>
        public int CharCount => LastChar - FirstChar + 1;

        /// <summary>The distance between the baseline and the highest glyph's highest point. Typically positive.</summary>
        public readonly float Ascender;

        /// <summary>The distance between the baseline and the lowest glyph's lowest point. Typically negative.</summary>
        public readonly float Descender;

        /// <summary>The distance between the lowest point of a line and the highest point of the next line.</summary>
        public readonly float LineGap;

        /// <summary>The baseline-to-baseline distance to advance when drawing a new line with this <see cref="TextureFont"/>.</summary>
        /// <remarks>This is calculated as ascender - descender + lineGap.</remarks>
        public readonly float LineAdvance;

        /// <summary>Offsets that should be directly applied to the characters when drawing them.</summary>
        protected readonly Vector2[] renderOffsets;

        /// <summary>The areas in <see cref="Texture"/> where each character is located.</summary>
        protected readonly Rectangle[] sources;

        /// <summary>Whether the graphics resources used by this <see cref="TextureFont"/> have been disposed.</summary>
        public bool IsDisposed => Texture.IsDisposed;

        /// <summary>
        /// Creates a <see cref="TextureFont"/>.
        /// </summary>
        /// <remarks>
        /// Any array passed to this method will NOT be copied. The provided instance will be used instead.
        /// Holding on to a reference to these arrays and modifying them afterwards can have unexpected
        /// behavior.
        /// </remarks>
        protected TextureFont(Texture2D texture, float size, char firstChar, char lastChar, Vector2[] renderOffsets,
            Rectangle[] sources, float ascender, float descender, float lineGap, string name)
        {
            if (lastChar < firstChar)
                throw new ArgumentException(nameof(firstChar) + " must be lower or equal than " + nameof(lastChar) + ".");
            int charCount = lastChar - firstChar + 1;

            Texture = texture ?? throw new ArgumentNullException(nameof(texture));

            this.renderOffsets = renderOffsets ?? throw new ArgumentNullException(nameof(renderOffsets));
            if (renderOffsets.Length != charCount)
                throw new ArgumentException("The length of the " + nameof(renderOffsets) + " array must match the amount of characters.", nameof(renderOffsets));

            this.sources = sources ?? throw new ArgumentNullException(nameof(sources));
            if (sources.Length != charCount)
                throw new ArgumentException("The length of the " + nameof(sources) + " array must match the amount of characters.", nameof(sources));

            FirstChar = firstChar;
            LastChar = lastChar;
            Size = size;
            Name = name;

            LineAdvance = ascender - descender + lineGap;
            Ascender = ascender;
            Descender = descender;
            LineGap = lineGap;
        }

        /// <summary>
        /// Checks that the given character is available in this <see cref="TextureFont"/> and
        /// throws an exception otherwise.
        /// </summary>
        protected void ValidateCharAvailable(char c)
        {
            if (!HasCharacter(c))
                throw new InvalidOperationException("This " + nameof(TextureFont) + " can't resolve this character: '" + c + "' (codePoint " + (int)c + ").");
        }

        /// <summary>
        /// Returns whether this <see cref="TextureFont"/> can draw a specified character.
        /// </summary>
        public bool HasCharacter(char character)
        {
            return character >= FirstChar && character <= LastChar;
        }

        /// <summary>
        /// Returns whether this <see cref="TextureFont"/> can draw all the characters in the specified string.
        /// </summary>
        public bool HasCharacters(ReadOnlySpan<char> characters)
        {
            for (int i = 0; i < characters.Length; i++)
                if (characters[i] < FirstChar && characters[i] > LastChar)
                    return false;
            return true;
        }

        /// <summary>
        /// Gets the distance to advance by after drawing a character.
        /// </summary>
        public abstract float GetAdvance(char character);

        /// <summary>
        /// Gets an offset that should be applied between two characters when drawing.
        /// </summary>
        public abstract Vector2 GetKerning(char fromChar, char toChar);

        /// <summary>
        /// Gets the area in the <see cref="Texture"/> where a specified character is found.
        /// </summary>
        public Rectangle GetSource(char character)
        {
            ValidateCharAvailable(character);
            return sources[character - FirstChar];
        }

        /// <summary>
        /// Gets the offset that should be applied directly to a character while drawing.
        /// </summary>
        public Vector2 GetRenderOffset(char character)
        {
            ValidateCharAvailable(character);
            return renderOffsets[character - FirstChar];
        }

        /// <summary>
        /// Measures the size in pixels of a string of text.
        /// </summary>
        public abstract Vector2 Measure(ReadOnlySpan<char> text);

        /// <summary>
        /// Measures the size in pixels of a single line of text.
        /// </summary>
        /// <remarks>
        /// For monospaced fonts, this is a O(1) operation that doesn't validate characters.<para/>
        /// For non-monospaced fonts, invalid characters (including newline) will throw an exception.
        /// </remarks>
        public abstract Vector2 MeasureLine(ReadOnlySpan<char> text);

        /// <summary>
        /// Measures the height of the given text.
        /// </summary>
        public float MeasureHeight(ReadOnlySpan<char> text)
        {
            int lineCount = 1;

            for (int i = 0; i < text.Length; i++)
                if (text[i] == NewlineIndicator)
                    lineCount++;

            return lineCount * LineAdvance;
        }

        /// <summary>
        /// Disposes the <see cref="GraphicsResource"/>-s used by this <see cref="TextureFont"/>.
        /// </summary>
        /// <remarks>
        /// If you have multiple <see cref="TextureFont"/> sharing a single texture, disposing any
        /// of these will dispose the texture, thus disposing all of them.
        /// </remarks>
        public void Dispose()
        {
            Texture.Dispose();
        }

        public override string ToString()
        {
            return string.Concat(Name ?? "Unnamed " + nameof(TextureFont), " - ", Size.ToString());
        }

        /// <summary>
        /// Creates a copy of the given string, where all the characters that aren't
        /// available in this <see cref="TextureFont"/> are replaced by a "default character".
        /// </summary>
        /// <param name="text">The string of text to sanitize.</param>
        /// <param name="defaultChar">The char to replace unavailable chars with.</param>
        /// <param name="ignoreNewline">Whether to count the newline character as a valid character.</param>
        /// <returns>A newly created string, or the same string instance if no chars need replacing.</returns>
        [return: NotNullIfNotNull("text")]
        public string? SanitizeString(string? text, char defaultChar = '?', bool ignoreNewline = true)
        {
            if (text == null)
                return null;

            // We go through the string until we find an unavailable char.
            for (int i = 0; i < text.Length; i++)
                if (!(HasCharacter(text[i]) || (ignoreNewline && text[i] == NewlineIndicator)))
                    return string.Create(text.Length, i, (chars, indx) =>
                    {
                        for (int c = 0; c < indx; c++)
                            chars[c] = text[c];

                        chars[indx] = defaultChar;

                        for (int i = indx + 1; i < chars.Length; i++)
                            chars[i] = (HasCharacter(text[i]) || (ignoreNewline && text[i] == NewlineIndicator)) ? text[i] : defaultChar;
                    });

            // If no unavailable char was found, we return the same string instance.
            return text;
        }

        /// <summary>
        /// Replaces all the characters in the given string that aren't available in
        /// this <see cref="TextureFont"/> with a "default character".
        /// </summary>
        /// <remarks>The replacing is done in-place.</remarks>
        /// <param name="text">The string of text to sanitize.</param>
        /// <param name="defaultChar">The char to replace unavailable chars with.</param>
        /// <param name="ignoreNewline">Whether to count the newline character as a valid character.</param>
        public void SanitizeString(Span<char> text, char defaultChar = '?', bool ignoreNewline = true)
        {
            for (int i = 0; i < text.Length; i++)
                if (!(HasCharacter(text[i]) || (ignoreNewline && text[i] == NewlineIndicator)))
                    text[i] = defaultChar;
        }

        /// <summary>
        /// Replaces all the characters in the given string that aren't available in
        /// this <see cref="TextureFont"/> with a "default character".
        /// </summary>
        /// <remarks>The replacing is done in-place.</remarks>
        /// <param name="text">The string of text to sanitize.</param>
        /// <param name="defaultChar">The char to replace unavailable chars with.</param>
        /// <param name="ignoreNewline">Whether to count the newline character as a valid character.</param>
        public void SanitizeString(System.Text.StringBuilder? text, char defaultChar = '?', bool ignoreNewline = true)
        {
            if (text == null)
                return;

            for (int i = 0; i < text.Length; i++)
                if (!(HasCharacter(text[i]) || (ignoreNewline && text[i] == NewlineIndicator)))
                    text[i] = defaultChar;
        }
    }
}
