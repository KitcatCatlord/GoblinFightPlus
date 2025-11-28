using System.Diagnostics;
namespace GoblinFight_;

class Program
{
    static (int r, int g, int b) HueToRgb(double hue)
    {
        hue = hue % 360;
        double c = 1;
        double x = 1 - Math.Abs((hue / 60) % 2 - 1);

        double r = 0, g = 0, b = 0;

        if (hue < 60) { r = c; g = x; b = 0; }
        else if (hue < 120) { r = x; g = c; b = 0; }
        else if (hue < 180) { r = 0; g = c; b = x; }
        else if (hue < 240) { r = 0; g = x; b = c; }
        else if (hue < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        return ((int)(r * 255), (int)(g * 255), (int)(b * 255));
    }

    static void Main(string[] args)
    {
        double hue = 0;
        ConsoleRenderer.Init();

        var buffer = ConsoleRenderer.CreateBuffer();
        int t = 0;

        var sw = new Stopwatch();
        int frames = 0;
        double fps = 0;
        long lastTime = 0;

        while (true)
        {
            sw.Start();

            hue += 0.01;
            if (hue >= 360) hue = 0;
            var (r, g, b) = HueToRgb(hue);
            string hex = $"{r:X2}{g:X2}{b:X2}";

            ConsoleRenderer.clearBuffer(buffer);

            ConsoleRenderer.DrawString(buffer, 2, 2, "Fast console testing...");
            ConsoleRenderer.DrawString(buffer, 2, 4, $"FPS: " + fps.ToString("0"));
            ConsoleRenderer.DrawString(buffer, 2, 6, $"Time: {t}");

            for (int i = 0; i < 20; i++)
            {
                ConsoleRenderer.DrawChar(buffer, 10 + i, 10, '#');
            }

            var overlays = new (int x, int y, string text, string fgHex, string bgHex)[]
            {
                (2, 8, "Coloured test", hex, ""),
                (2, 9, "BG test", "#FFFFFF", hex)
            };

            ConsoleRenderer.Render(buffer, overlays);

            frames++;
            long now = sw.ElapsedMilliseconds;
            if (now - lastTime >= 1000)
            {
                fps = frames * 1000.0 / (now - lastTime);
                frames = 0;
                lastTime = now;
            }

            t++;
        }
    }
}
