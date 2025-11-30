using System.Diagnostics;

namespace GoblinFight_;

static class MovementSystem
{
    public static void Run(Hero hero, Dungeon dungeon, char[,] buffer)
    {
        int bufferHeight = buffer.GetLength(0);
        int bufferWidth = buffer.GetLength(1);

        Stopwatch sw = new Stopwatch();
        sw.Start();

        int frames = 0;
        double fps = 0;
        long lastTime = 0;
        long lastMonsterStep = 0;
        int monsterStepMs = 200;

        while (true)
        {
            long now = sw.ElapsedMilliseconds;

            HandleInput(hero, dungeon);

            if (now - lastMonsterStep >= monsterStepMs)
            {
                UpdateMonsters(hero, dungeon);
                lastMonsterStep = now;
            }

            ConsoleRenderer.clearBuffer(buffer);
            RenderGame(hero, dungeon, buffer, fps);

            frames++;
            long now2 = sw.ElapsedMilliseconds;
            if (now2 - lastTime >= 1000)
            {
                fps = frames * 1000.0 / (now2 - lastTime);
                frames = 0;
                lastTime = now2;
            }
        }
    }

    static void HandleInput(Hero hero, Dungeon dungeon)
    {
        while (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.Escape)
            {
                Console.Clear();
                Environment.Exit(0);
            }

            int dx = 0;
            int dy = 0;

            if (key == ConsoleKey.W) dy = -1;
            else if (key == ConsoleKey.S) dy = 1;
            else if (key == ConsoleKey.A) dx = -1;
            else if (key == ConsoleKey.D) dx = 1;

            if (dx != 0 || dy != 0)
            {
                TryMoveHero(hero, dungeon, dx, dy);
            }
        }
    }

    static void TryMoveHero(Hero hero, Dungeon dungeon, int dx, int dy)
    {
        Map map = dungeon.CurrentMap();

        int nx = hero.X + dx;
        int ny = hero.Y + dy;

        if (!map.InBounds(nx, ny)) return;

        int mi = IndexOfMonsterAt(map, nx, ny);
        if (mi >= 0)
        {
            Monster m = map.monsters[mi];
            CombatSystem.HeroAttack(hero, m, map);
            return;
        }

        Tile tile = map.tiles[ny, nx];

        if (tile == Tile.Wall) return;

        hero.X = nx;
        hero.Y = ny;

        if (tile == Tile.StairsDown)
        {
            bool moved = dungeon.GoDown();
            if (moved)
            {
                Map newMap = dungeon.CurrentMap();
                if (newMap.stairsUpX >= 0 && newMap.stairsUpY >= 0)
                {
                    hero.X = newMap.stairsUpX;
                    hero.Y = newMap.stairsUpY;
                }
            }
        }
        else if (tile == Tile.StairsUp)
        {
            bool moved = dungeon.GoUp();
            if (moved)
            {
                Map newMap = dungeon.CurrentMap();
                if (newMap.stairsDownX >= 0 && newMap.stairsDownY >= 0)
                {
                    hero.X = newMap.stairsDownX;
                    hero.Y = newMap.stairsDownY;
                }
            }
        }
        else if (tile == Tile.Exit)
        {
            Console.Clear();
            Console.WriteLine("You escaped the dungeon!");
            Environment.Exit(0);
        }

        CollectLootAtPosition(hero, map);
    }

    static void CollectLootAtPosition(Hero hero, Map map)
    {
        for (int i = 0; i < map.loot.Count; i++)
        {
            if (map.loot[i].X == hero.X && map.loot[i].Y == hero.Y)
            {
                Item item = map.loot[i].item;
                bool ok = hero.AddToInventory(item);
                if (ok)
                {
                    map.loot.RemoveAt(i);
                }
                break;
            }
        }
    }

    static int IndexOfMonsterAt(Map map, int x, int y)
    {
        for (int i = 0; i < map.monsters.Count; i++)
        {
            if (map.monsters[i].X == x && map.monsters[i].Y == y) return i;
        }
        return -1;
    }

    static void UpdateMonsters(Hero hero, Dungeon dungeon)
    {
        Map map = dungeon.CurrentMap();

        for (int i = 0; i < map.monsters.Count; i++)
        {
            Monster m = map.monsters[i];

            int dist = Math.Abs(m.X - hero.X) + Math.Abs(m.Y - hero.Y);
            if (dist <= 1)
            {
                CombatSystem.MonsterAttack(hero, m);
                continue;
            }

            if (dist > 15) continue;

            int stepX;
            int stepY;
            bool hasStep = GetNextStepTowards(map, m.X, m.Y, hero.X, hero.Y, out stepX, out stepY);
            if (!hasStep) continue;

            if (stepX == hero.X && stepY == hero.Y)
            {
                CombatSystem.MonsterAttack(hero, m);
                continue;
            }

            if (map.tiles[stepY, stepX] == Tile.Wall) continue;

            int otherIndex = IndexOfMonsterAt(map, stepX, stepY);
            if (otherIndex >= 0) continue;

            m.X = stepX;
            m.Y = stepY;
        }
    }

    static bool GetNextStepTowards(Map map, int fromX, int fromY, int toX, int toY, out int stepX, out int stepY)
    {
        stepX = fromX;
        stepY = fromY;

        if (fromX == toX && fromY == toY) return false;

        int w = map.Width;
        int h = map.Height;

        bool[,] closed = new bool[h, w];
        bool[,] open = new bool[h, w];
        int[,] g = new int[h, w];
        int[,] cameX = new int[h, w];
        int[,] cameY = new int[h, w];

        int max = int.MaxValue;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                g[y, x] = max;
                cameX[y, x] = -1;
                cameY[y, x] = -1;
            }
        }

        g[fromY, fromX] = 0;
        open[fromY, fromX] = true;

        while (true)
        {
            int cx = -1;
            int cy = -1;
            int bestF = max;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (!open[y, x]) continue;
                    int hCost = Math.Abs(x - toX) + Math.Abs(y - toY);
                    int f = g[y, x] + hCost;
                    if (f < bestF)
                    {
                        bestF = f;
                        cx = x;
                        cy = y;
                    }
                }
            }

            if (cx == -1) return false;

            if (cx == toX && cy == toY)
            {
                int px = cx;
                int py = cy;

                while (true)
                {
                    int tx = cameX[py, px];
                    int ty = cameY[py, px];
                    if (tx == fromX && ty == fromY)
                    {
                        stepX = px;
                        stepY = py;
                        return true;
                    }
                    px = tx;
                    py = ty;
                }
            }

            open[cy, cx] = false;
            closed[cy, cx] = true;

            int[,] dirs = new int[4, 2] { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } };

            for (int i = 0; i < 4; i++)
            {
                int nx = cx + dirs[i, 0];
                int ny = cy + dirs[i, 1];

                if (!map.InBounds(nx, ny)) continue;
                if (closed[ny, nx]) continue;

                Tile t = map.tiles[ny, nx];
                if (t == Tile.Wall) continue;

                int newG = g[cy, cx] + 1;
                if (!open[ny, nx] || newG < g[ny, nx])
                {
                    g[ny, nx] = newG;
                    cameX[ny, nx] = cx;
                    cameY[ny, nx] = cy;
                    open[ny, nx] = true;
                }
            }
        }
    }

    static void RenderGame(Hero hero, Dungeon dungeon, char[,] buffer, double fps)
    {
        Map map = dungeon.CurrentMap();
        int h = buffer.GetLength(0);
        int w = buffer.GetLength(1);

        for (int y = 0; y < map.Height && y < h; y++)
        {
            for (int x = 0; x < map.Width && x < w; x++)
            {
                char c;

                if (hero.X == x && hero.Y == y)
                {
                    c = '@';
                }
                else
                {
                    bool monsterHere = false;
                    char mc = ' ';

                    for (int i = 0; i < map.monsters.Count; i++)
                    {
                        Monster m = map.monsters[i];
                        if (m.X == x && m.Y == y)
                        {
                            monsterHere = true;

                            if (m is Goblin) mc = 'g';
                            else if (m is Orc) mc = 'o';
                            else if (m is Dragon) mc = 'D';
                            else mc = 'm';

                            break;
                        }
                    }

                    if (monsterHere)
                    {
                        c = mc;
                    }
                    else
                    {
                        bool lootHere = false;
                        for (int i = 0; i < map.loot.Count; i++)
                        {
                            if (map.loot[i].X == x && map.loot[i].Y == y)
                            {
                                lootHere = true;
                                break;
                            }
                        }

                        if (lootHere)
                        {
                            c = '!';
                        }
                        else
                        {
                            Tile t = map.tiles[y, x];
                            if (t == Tile.Wall) c = '#';
                            else if (t == Tile.Floor) c = '.';
                            else if (t == Tile.StairsDown) c = '>';
                            else if (t == Tile.StairsUp) c = '<';
                            else if (t == Tile.Exit) c = 'E';
                            else c = ' ';
                        }
                    }
                }

                ConsoleRenderer.DrawChar(buffer, x, y, c);
            }
        }

        string fpsText = "FPS: " + fps.ToString("0");
        int fpsX = w - fpsText.Length - 1;
        if (fpsX < 0) fpsX = 0;

        string hpText = "HP: " + hero.Health.ToString();
        int hpY = h - 1;
        if (hpY < 0) hpY = 0;

        var overlays = new (int x, int y, string text, string fgHex, string bgHex)[]
        {
            (fpsX, 0, fpsText, "#00FF00", ""),
            (2, hpY, hpText, "#FF5555", "")
        };

        ConsoleRenderer.Render(buffer, overlays);
    }
}

static class CombatSystem
{
    public static void HeroAttack(Hero hero, Monster monster, Map map)
    {
        int dmg = hero.GetAttackDamage();
        bool dead = monster.Damage(dmg);
        if (dead)
        {
            Item? drop = monster.GetWeaponDrop();
            if (drop.HasValue)
            {
                MapLoot ml = new MapLoot { item = drop.Value, X = monster.X, Y = monster.Y };
                map.loot.Add(ml);
            }
            map.monsters.Remove(monster);
            hero.X = monster.X;
            hero.Y = monster.Y;
        }
        else
        {
            int md = monster.GetAttackDamage();
            bool heroDead = hero.Damage(md);
            if (heroDead)
            {
                Console.Clear();
                Console.WriteLine("You died.");
                Environment.Exit(0);
            }
        }
    }

    public static void MonsterAttack(Hero hero, Monster monster)
    {
        int md = monster.GetAttackDamage();
        bool heroDead = hero.Damage(md);
        if (heroDead)
        {
            Console.Clear();
            Console.WriteLine("You died.");
            Environment.Exit(0);
        }
    }
}
