namespace BdfNet;

public class BdfFont
{
    public string FontName { get; set; }
    public int PointSize { get; set; }
    public int ResolutionX { get; set; }
    public int ResolutionY { get; set; }
    public int[] FontBoundingBox { get; set; } // Width, Height, X-Offset, Y-Offset    
    public int Ascent { get; set; }
    public int Descent { get; set; }
    public Dictionary<int, BdfChar> Characters { get; set; } = [];

    public static BdfFont Load(string path)
    {
        using var stream = File.OpenRead(path);
        return Load(stream);
    }

    public static BdfFont Load(Stream stream)
    {
        var font = new BdfFont();
        BdfChar? currentChar = null;

        using var reader = new StreamReader(stream);
        var lines = new Queue<string>(reader.ReadToEnd().Split('\n'));

        while (true)
        {
            if (lines.Count == 0)
                break;

            var line = lines.Dequeue();

            if (line.StartsWith("FONT "))
            {
                font.FontName = line.Substring(5).Trim();
            }
            else if (line.StartsWith("SIZE "))
            {
                var parts = line.Substring(5).Trim().Split(' ');
                font.PointSize = int.Parse(parts[0]);
                font.ResolutionX = int.Parse(parts[1]);
                font.ResolutionY = int.Parse(parts[2]);
            }
            else if (line.StartsWith("FONTBOUNDINGBOX "))
            {
                var parts = line.Substring(16).Trim().Split(' ');
                font.FontBoundingBox = new int[4];
                for (var i = 0; i < 4; i++)
                {
                    font.FontBoundingBox[i] = int.Parse(parts[i]);
                }
            }
            else if (line.StartsWith("FONT_ASCENT"))
            {
                font.Ascent = int.Parse(line.Substring(12).Trim());
            }
            else if (line.StartsWith("FONT_DESCENT"))
            {
                font.Descent = int.Parse(line.Substring(13).Trim());
            }
            else if (line.StartsWith("STARTPROPERTIES"))
            {
                // Skip properties
                while ((line = reader.ReadLine()) != null && !line.StartsWith("ENDPROPERTIES"))
                {
                }
            }
            else if (line.StartsWith("CHARS"))
            {
                // Skip character count
            }
            else if (line.StartsWith("STARTCHAR"))
            {
                currentChar = new BdfChar();
            }
            else if (line.StartsWith("ENCODING"))
            {
                if (currentChar is null)
                    throw new Exception("ENCODING found before STARTCHAR");

                currentChar.Encoding = int.Parse(line.Substring(9).Trim());
            }
            else if (line.StartsWith("DWIDTH"))
            {
                if (currentChar is null)
                    throw new Exception("DWIDTH found before STARTCHAR");

                var parts = line.Substring(7).Trim().Split(' ');
                currentChar.XAdvance = int.Parse(parts[0]);
            }
            else if (line.StartsWith("BBX"))
            {
                if (currentChar is null)
                    throw new Exception("BBX found before STARTCHAR");

                var parts = line.Substring(4).Trim().Split(' ');
                currentChar.Width = int.Parse(parts[0]);
                currentChar.Height = int.Parse(parts[1]);
                currentChar.XOffset = int.Parse(parts[2]);
                currentChar.YOffset = int.Parse(parts[3]);
                currentChar.Bitmap = new byte[currentChar.Height, currentChar.Width];
            }
            else if (line.StartsWith("BITMAP"))
            {
                if (currentChar is null)
                    throw new Exception("BITMAP found before STARTCHAR");

                var bitmapLines = new List<string>();
                while ((line = lines.Dequeue()) != null && !line.StartsWith("ENDCHAR"))
                {
                    bitmapLines.Add(line.Trim());
                }

                currentChar.Bitmap = new byte[currentChar.Height, currentChar.Width];

                for (var row = 0; row < currentChar.Height; row++)
                {
                    var hexLine = bitmapLines[row];
                    for (var col = 0; col < currentChar.Width; col += 8)
                    {
                        var byteValue = Convert.ToByte(hexLine.Substring(col / 4, 2), 16);

                        for (var bit = 0; bit < 8; bit++)
                        {
                            if (col + bit < currentChar.Width)
                            {
                                currentChar.Bitmap[row, col + bit] = (byte)((byteValue >> (7 - bit)) & 1);
                            }
                        }
                    }
                }

                font.Characters.TryAdd(currentChar.Encoding, currentChar);
                currentChar = null;
            }
        }

        return font;
    }
}
