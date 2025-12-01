namespace GoblinFight_;

enum Tile
{
    Empty,
    Floor,
    Wall,
    StairsDown,
    StairsUp,
    Exit
}

static class RNG
{
    static Random _r = new Random();
    public static int Next(int min, int max) => _r.Next(min, max);
}

class MapLoot
{
    public Item item;
    public int X;
    public int Y;
}

class Map
{
    public int Width;
    public int Height;
    public Tile[,] tiles;
    public bool[,] isDoor;
    public List<Monster> monsters = new List<Monster>();
    public List<MapLoot> loot = new List<MapLoot>();
    public int stairsUpX = -1;
    public int stairsUpY = -1;
    public int stairsDownX = -1;
    public int stairsDownY = -1;
    public int exitX = -1;
    public int exitY = -1;

    public Map(int w, int h)
    {
        Width = w;
        Height = h;
        tiles = new Tile[h, w];
        isDoor = new bool[h, w];
    }

    public bool InBounds(int x, int y)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height) return true;
        return false;
    }

    public void SetTile(int x, int y, Tile t)
    {
        if (InBounds(x, y)) tiles[y, x] = t;
    }

    public Tile GetTile(int x, int y)
    {
        if (InBounds(x, y)) return tiles[y, x];
        return Tile.Empty;
    }
}

class Dungeon
{
    public List<Map> layers = new List<Map>();
    public int CurrentLayer = 0;

    public Map CurrentMap()
    {
        return layers[CurrentLayer];
    }

    public bool GoDown()
    {
        if (CurrentLayer < layers.Count - 1)
        {
            CurrentLayer++;
            return true;
        }
        return false;
    }

    public bool GoUp()
    {
        if (CurrentLayer > 0)
        {
            CurrentLayer--;
            return true;
        }
        return false;
    }
}

static class DungeonGenerator
{
    public static Dungeon CreateSimpleDungeon(int layerCount, int width, int height)
    {
        if (layerCount < 1) layerCount = 1;
        if (width < 10) width = 10;
        if (height < 10) height = 10;

        Dungeon d = new Dungeon();

        int prevDownX = -1;
        int prevDownY = -1;

        for (int i = 0; i < layerCount; i++)
        {
            bool hasUp = i > 0;
            bool hasDown = i < layerCount - 1;
            bool isLast = i == layerCount - 1;

            int upX = prevDownX;
            int upY = prevDownY;

            int downX;
            int downY;

            Map m;

            while (true)
            {
                m = CreateLayer(width, height, hasUp, hasDown, isLast, upX, upY, out downX, out downY);

                if (IsFullyAccessible(m))
                    break;
            }

            d.layers.Add(m);
            prevDownX = downX;
            prevDownY = downY;
        }

        return d;
    }

    static Map CreateLayer(int w, int h, bool hasUp, bool hasDown, bool isLast, int upX, int upY, out int downX, out int downY)
    {
        Map m = new Map(w, h);

        GeneratePartitionedLayout(m);

        downX = -1;
        downY = -1;

        if (hasUp)
        {
            EnsureSafeFloor(m, out upX, out upY);
            m.stairsUpX = upX;
            m.stairsUpY = upY;
            m.tiles[upY, upX] = Tile.StairsUp;
        }

        if (hasDown)
        {
            int sx;
            int sy;
            EnsureSafeFloor(m, out sx, out sy);
            m.stairsDownX = sx;
            m.stairsDownY = sy;
            m.tiles[sy, sx] = Tile.StairsDown;
            downX = sx;
            downY = sy;
        }

        if (isLast)
        {
            int ex;
            int ey;
            EnsureSafeFloor(m, out ex, out ey);
            m.exitX = ex;
            m.exitY = ey;
            m.tiles[ey, ex] = Tile.Exit;
        }

        int monsterCount = RNG.Next(3, 7);
        for (int i = 0; i < monsterCount; i++)
        {
            int mx;
            int my;
            EnsureSafeFloor(m, out mx, out my);
            Monster monster = RandomMonster();
            monster.X = mx;
            monster.Y = my;
            m.monsters.Add(monster);
        }

        int lootCount = RNG.Next(2, 6);
        for (int i = 0; i < lootCount; i++)
        {
            int lx;
            int ly;
            EnsureSafeFloor(m, out lx, out ly);
            Item template = RandomLootTemplate();
            Item q = ItemFactory.MakeRandomQuality(template);
            m.loot.Add(new MapLoot { item = q, X = lx, Y = ly });
        }

        return m;
    }

    static bool IsFullyAccessible(Map m)
    {
        int w = m.Width;
        int h = m.Height;

        bool[,] visited = new bool[h, w];

        int startX = -1;
        int startY = -1;

        for (int y = 0; y < h && startX == -1; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (m.tiles[y, x] == Tile.Floor)
                {
                    startX = x;
                    startY = y;
                    break;
                }
            }
        }

        if (startX == -1) return false;

        Queue<(int x, int y)> q = new Queue<(int x, int y)>();
        q.Enqueue((startX, startY));
        visited[startY, startX] = true;

        int[,] dirs = new int[,] { {1,0},{-1,0},{0,1},{0,-1} };

        while (q.Count > 0)
        {
            var (cx, cy) = q.Dequeue();

            for (int i = 0; i < 4; i++)
            {
                int nx = cx + dirs[i, 0];
                int ny = cy + dirs[i, 1];

                if (!m.InBounds(nx, ny)) continue;
                if (visited[ny, nx]) continue;
                if (m.tiles[ny, nx] == Tile.Wall) continue;

                visited[ny, nx] = true;
                q.Enqueue((nx, ny));
            }
        }

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (m.tiles[y, x] == Tile.Floor || m.tiles[y, x] == Tile.StairsUp || m.tiles[y, x] == Tile.StairsDown || m.tiles[y, x] == Tile.Exit)
                {
                    if (!visited[y, x])
                        return false;
                }
            }
        }

        return true;
    }

    static void GeneratePartitionedLayout(Map m)
    {
        int w = m.Width;
        int h = m.Height;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (x == 0 || x == w - 1 || y == 0 || y == h - 1)
                    m.tiles[y, x] = Tile.Wall;
                else
                    m.tiles[y, x] = Tile.Floor;
                m.isDoor[y, x] = false;
            }
        }

        PartitionRegion(m, 1, 1, w - 2, h - 2, 0);
    }

    static void PartitionRegion(Map m, int x, int y, int w, int h, int depth)
    {
        int minSize = 6;
        int maxDepth = 6;

        if (depth >= maxDepth) return;
        if (w < minSize * 2 && h < minSize * 2) return;

        bool splitVertical;
        if (w >= h * 1.3) splitVertical = true;
        else if (h >= w * 1.3) splitVertical = false;
        else splitVertical = RNG.Next(0, 2) == 0;

        if (splitVertical)
        {
            if (w < minSize * 2) return;

            int splitX = RNG.Next(x + minSize, x + w - minSize);
            for (int yy = y; yy < y + h; yy++)
                m.tiles[yy, splitX] = Tile.Wall;

            TryMakeDoor(m, splitX, y, h, vertical: true);

            int leftW = splitX - x;
            int rightX = splitX + 1;
            int rightW = x + w - rightX;

            if (leftW > 0) PartitionRegion(m, x, y, leftW, h, depth + 1);
            if (rightW > 0) PartitionRegion(m, rightX, y, rightW, h, depth + 1);
        }
        else
        {
            if (h < minSize * 2) return;

            int splitY = RNG.Next(y + minSize, y + h - minSize);
            for (int xx = x; xx < x + w; xx++)
                m.tiles[splitY, xx] = Tile.Wall;

            TryMakeDoor(m, splitY, x, w, vertical: false);

            int topH = splitY - y;
            int bottomY = splitY + 1;
            int bottomH = y + h - bottomY;

            if (topH > 0) PartitionRegion(m, x, y, w, topH, depth + 1);
            if (bottomH > 0) PartitionRegion(m, x, bottomY, w, bottomH, depth + 1);
        }
    }

    static void TryMakeDoor(Map m, int splitLine, int start, int len, bool vertical)
    {
        int attempts = 0;

        while (attempts < 20)
        {
            attempts++;
            int px = vertical ? splitLine : RNG.Next(start + 1, start + len - 1);
            int py = vertical ? RNG.Next(start + 1, start + len - 1) : splitLine;

            if (!m.InBounds(px, py)) continue;

            if (vertical)
            {
                if (px > 0 && px < m.Width - 1)
                {
                    if (m.tiles[py, px - 1] == Tile.Floor && m.tiles[py, px + 1] == Tile.Floor)
                    {
                        m.tiles[py, px] = Tile.Floor;
                        m.isDoor[py, px] = true;
                        return;
                    }
                }
            }
            else
            {
                if (py > 0 && py < m.Height - 1)
                {
                    if (m.tiles[py - 1, px] == Tile.Floor && m.tiles[py + 1, px] == Tile.Floor)
                    {
                        m.tiles[py, px] = Tile.Floor;
                        m.isDoor[py, px] = true;
                        return;
                    }
                }
            }
        }
    }

    static void EnsureSafeFloor(Map m, out int x, out int y)
    {
        while (true)
        {
            FindRandomFloor(m, out x, out y);
            if (IsNearDoor(m, x, y)) continue;
            if (IsNearStairs(m, x, y)) continue;
            return;
        }
    }

    static bool IsNearStairs(Map m, int x, int y) {
        int sx1 = m.stairsUpX;
        int sy1 = m.stairsUpX;
        int sx2 = m.stairsUpX;
        int sy2 = m.stairsUpX;
        
        for (int dy = -5; dy <= 5; dy++) {
            for (int dx = -5; dx <= 5; dx++) {
                int nx = x + dx;
                int ny = y + dy;

                if (!m.InBounds(nx, ny)) continue;

                if ((nx == sx1 && ny == sy1) || (nx == sx2 && ny == sy2) || (nx == m.exitX && ny == m.exitY))
                    return true;
            }
        }
        return false;
    }

    static bool IsNearDoor(Map m, int x, int y)
    {
        for (int dy = -2; dy <= 2; dy++)
        {
            for (int dx = -2; dx <= 2; dx++)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (!m.InBounds(nx, ny)) continue;
                if (m.isDoor[ny, nx]) return true;
            }
        }
        return false;
    }

    static void FindRandomFloor(Map m, out int x, out int y)
    {
        while (true)
        {
            x = RNG.Next(1, m.Width - 1);
            y = RNG.Next(1, m.Height - 1);
            if (m.tiles[y, x] != Tile.Floor) continue;
            if (m.isDoor[y, x]) continue;
            return;
        }
    }

    static Monster RandomMonster()
    {
        int roll = RNG.Next(0, 100);
        if (roll < 60) return new Goblin();
        if (roll < 90) return new Orc();
        return new Dragon();
    }

    static Item RandomLootTemplate()
    {
        int roll = RNG.Next(0, 100);
        if (roll < 40) return ItemDatabase.Sword;
        if (roll < 70) return ItemDatabase.Axe;
        if (roll < 90) return ItemDatabase.Bow;
        return ItemDatabase.goblinsArm;
    }
}
