using System;
using System.Text;

public static class ConsoleRenderer
{
    static int width;
    static int height;

    public static void Init()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.CursorVisible = false;

        width = Console.WindowWidth;
        height = Console.WindowHeight;

        if (OperatingSystem.IsWindows()) Console.SetBufferSize(width, height);
    }

    public static char[,] CreateBuffer()
    {
        return new char[height, width];
    }
    public static void clearBuffer(char[,] buffer)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                buffer[y, x] = ' ';
            }
        }
    }
    public static bool DrawChar(char[,] buffer, int x, int y, char c)
    {
        if (x >= 0 && x < width && y >= 0 && y < height) { buffer[y, x] = c; return false; }
        return true;
    }
    public static bool DrawString(char[,] buffer, int x, int y, string s)
    {
        bool failed = false;
        for (int i = 0; i < s.Length; i++) failed = DrawChar(buffer, x + i, y, s[i]);
        return failed;
    }
    public static void DrawColourString(int x, int y, string s, string hex)
    {
        if (hex.StartsWith("#")) hex = hex.Substring(1);
        if (hex.Length != 6) return; //NTS: Throws an error at 7?

        int r = Convert.ToInt32((hex.Substring(0, 2), 16));
        int g = Convert.ToInt32((hex.Substring(0, 2), 16));
        int b = Convert.ToInt32((hex.Substring(0, 2), 16));

        Console.SetCursorPosition(x, y);
        Console.Write($"\u001b[38;2;{r};{g};{b}m{s}\u001b[0m]]"); //TODO: Test this!!!!!!!
    }
    public static void Render(char[,] buffer) {
      Console.SetCursorPosition(0,0);
      var sb = new StringBuilder(width * height);

      for (int y = 0; y < height; y++) {
        for (int x = 0; x < width; x++) {
          sb.Append(buffer[y,x]);
        }
        if (y < height - 1) sb.Append('\n');
      }

      Console.Write(sb.ToString());
    }
}
