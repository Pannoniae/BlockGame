using System.Text;

namespace BdfNet;

public class BdfChar
{
    public int Encoding { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int XOffset { get; set; }
    public int YOffset { get; set; }
    public int XAdvance { get; set; }
    public byte[,] Bitmap { get; set; }
    
    public string DebugPrint()
    {
        var sb = new StringBuilder();
        for (int row = 0; row < Height; row++)
        {
            for (int col = 0; col < Width; col++)
            {
                sb.Append(Bitmap[row, col] == 1 ? "*" : " ");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
