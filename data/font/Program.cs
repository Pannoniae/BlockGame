using System.Reflection;

namespace BdfNet.Example;
/*
class Program
{
    public static void Main()
    {
        const int screenWidth = 800;
        const int screenHeight = 450;

        Raylib.InitWindow(screenWidth, screenHeight, "BdfNet Example");

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "BdfNet.Example.Fonts.courR14.bdf";
        using var stream = assembly.GetManifestResourceStream(resourceName) ?? throw new Exception($"Could not find resource '{resourceName}'");

        var font = BdfFont.Load(stream);
        var aChar = font.Characters[0x61];

        var glyphImage = Raylib.GenImageColor(font.FontBoundingBox[0], font.FontBoundingBox[1], Color.White);
        for (var row = 0; row < aChar.Height; row++)
        {
            for (var col = 0; col < aChar.Width; col++)
            {
                if (aChar.Bitmap[row, col] == 1)
                {
                    Raylib.ImageDrawPixel(ref glyphImage, col, row, Color.Black);
                }
            }
        }

        var glyphTexture = Raylib.LoadTextureFromImage(glyphImage);

        Raylib.UnloadImage(glyphImage);

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.White);

            Raylib.DrawTexture(glyphTexture, screenWidth / 2 - glyphTexture.Width / 2, screenHeight / 2 - glyphTexture.Height / 2, Color.White);
            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}*/