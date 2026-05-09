using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GAME;

public static class PixelTextRenderer
{
    // Harfler 3x5 piksel desenleriyle tanımlanır.
    // Bu küçük font, dışarıdan font dosyası eklemeden HUD yazısı çizmemizi sağlar.
    private static readonly Dictionary<char, string[]> Glyphs = new()
    {
        ['0'] = new[] { "111", "101", "101", "101", "111" },
        ['1'] = new[] { "010", "110", "010", "010", "111" },
        ['2'] = new[] { "111", "001", "111", "100", "111" },
        ['3'] = new[] { "111", "001", "111", "001", "111" },
        ['4'] = new[] { "101", "101", "111", "001", "001" },
        ['5'] = new[] { "111", "100", "111", "001", "111" },
        ['6'] = new[] { "111", "100", "111", "101", "111" },
        ['7'] = new[] { "111", "001", "010", "010", "010" },
        ['8'] = new[] { "111", "101", "111", "101", "111" },
        ['9'] = new[] { "111", "101", "111", "001", "111" },
        ['-'] = new[] { "000", "000", "111", "000", "000" },
        ['/'] = new[] { "001", "001", "010", "100", "100" },
        ['.'] = new[] { "000", "000", "000", "000", "010" },
        ['A'] = new[] { "010", "101", "111", "101", "101" },
        ['B'] = new[] { "110", "101", "110", "101", "110" },
        ['C'] = new[] { "111", "100", "100", "100", "111" },
        ['D'] = new[] { "110", "101", "101", "101", "110" },
        ['E'] = new[] { "111", "100", "110", "100", "111" },
        ['F'] = new[] { "111", "100", "110", "100", "100" },
        ['G'] = new[] { "111", "100", "101", "101", "111" },
        ['H'] = new[] { "101", "101", "111", "101", "101" },
        ['I'] = new[] { "111", "010", "010", "010", "111" },
        ['K'] = new[] { "101", "101", "110", "101", "101" },
        ['L'] = new[] { "100", "100", "100", "100", "111" },
        ['M'] = new[] { "101", "111", "111", "101", "101" },
        ['N'] = new[] { "101", "111", "111", "111", "101" },
        ['O'] = new[] { "111", "101", "101", "101", "111" },
        ['P'] = new[] { "110", "101", "110", "100", "100" },
        ['R'] = new[] { "110", "101", "110", "101", "101" },
        ['S'] = new[] { "111", "100", "111", "001", "111" },
        ['T'] = new[] { "111", "010", "010", "010", "010" },
        ['U'] = new[] { "101", "101", "101", "101", "111" },
        ['V'] = new[] { "101", "101", "101", "101", "010" },
        ['X'] = new[] { "101", "101", "010", "101", "101" },
        ['Y'] = new[] { "101", "101", "010", "010", "010" },
        ['Z'] = new[] { "111", "001", "010", "100", "111" },
        [' '] = new[] { "000", "000", "000", "000", "000" },
    };

    public static void Draw(SpriteBatch spriteBatch, Texture2D pixel, string text, Vector2 position, Color color, int scale)
    {
        var cursorX = (int)position.X;
        var cursorY = (int)position.Y;
        var spacing = scale;

        foreach (var character in text.ToUpperInvariant())
        {
            if (!Glyphs.TryGetValue(character, out var glyph))
                throw new ArgumentException($"'{character}' karakteri icin piksel yazı deseni yok.", nameof(text));

            // Desendeki her '1' değeri ekrana bir küçük kare olarak çizilir.
            for (var row = 0; row < glyph.Length; row++)
            {
                for (var column = 0; column < glyph[row].Length; column++)
                {
                    if (glyph[row][column] == '1')
                    {
                        spriteBatch.Draw(
                            pixel,
                            new Rectangle(cursorX + column * scale, cursorY + row * scale, scale, scale),
                            color);
                    }
                }
            }

            cursorX += glyph[0].Length * scale + spacing;
        }
    }
}
