using System.Diagnostics;
namespace GoblinFight_;

class Program
{
    static void Main(string[] args)
    {
      ConsoleRenderer.Init();

      var buffer = ConsoleRenderer.CreateBuffer();
      int t = 0;

      var sw = new Stopwatch();
      int frames = 0;
      double fps = 0;
      long lastTime = 0;

      while (true) {
        sw.Start();

        ConsoleRenderer.clearBuffer(buffer);

        ConsoleRenderer.DrawString(buffer, 2, 2, "Fast console testing...");
        ConsoleRenderer.DrawString(buffer, 2, 4, $"FPS: " + fps.ToString("0"));
        ConsoleRenderer.DrawString(buffer, 2, 6, $"Time: {t}");

        for (int i = 0; i < 20; i++) ConsoleRenderer.DrawChar(buffer, 10 + i, 10, '#');

        ConsoleRenderer.Render(buffer);

        // ConsoleRenderer.DrawColourString(2, 8, "Coloured test", "#FF00AA");

        frames++;
        long now = sw.ElapsedMilliseconds;
        if (now - lastTime >= 1000) {
          fps = frames * 1000.0 / (now - lastTime);
          frames = 0;
          lastTime = now;
        }
        
        t++;
      }
    }
}
