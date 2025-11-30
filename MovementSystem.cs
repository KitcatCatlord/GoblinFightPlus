namespace GoblinFight_;

static class MovementSystem
{
    public static void Run(Hero hero, Dungeon dungeon)
    {
        ConsoleKey key;

        while (true)
        {
            RenderSimple(hero, dungeon);

            key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.Escape) break;

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
    }

    static void RenderSimple(Hero hero, Dungeon dungeon)
    {
        Map map = dungeon.CurrentMap();

        Console.SetCursorPosition(0, 0);

        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
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
                        Tile t = map.tiles[y, x];
                        if (t == Tile.Wall) c = '#';
                        else if (t == Tile.Floor) c = '.';
                        else if (t == Tile.StairsDown) c = '>';
                        else if (t == Tile.StairsUp) c = '<';
                        else c = ' ';
                    }
                }

                Console.Write(c);
            }
            Console.Write('\n');
        }
    }
}
