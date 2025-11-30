namespace GoblinFight_;

/* Needed:
 * InBounds check
 * Tile setting
 * Tile getting
 * Dungeon traversal:
 *  Going up 
 *  Going down 
 * Dungeon generator:
 *  Initialisation class (to generate it)
 *  Layer creation:
 *      Monster placement
 *      Wall / floor placement
 */
enum Tile
{
    Empty,
    Floor,
    Wall,
    StairsDown,
    StairsUp // Could be ladders?
}
static class RNG
{
    static Random _r = new Random();
    public static int Next(int min, int max) => _r.Next(min, max);
}

class Map
{
    public int Width;
    public int Height;
    public Tile[,] tiles;
    public List<Monster> monsters = new List<Monster>();
    public int stairsUpX = -1;
    public int stairsUpY = -1;
    public int stairsDownX = -1;
    public int stairsDownY = -1;

    public Map(int w, int h)
    {
        Width = w;
        Height = h;
        tiles = new Tile[h, w];
    }
    public bool InBounds(int x, int y)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
            return true;
        return false;
    }
    public void SetTile(int x, int y, Tile t)
    {
        if (InBounds(x, y))
            tiles[y, x] = t;
    }
    public Tile GetTile(int x, int y)
    {
        if (InBounds(x, y))
            return tiles[y, x];
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

            int upX = -1;
            int upY = -1;
            if (hasUp)
            {
                upX = prevDownX;
                upY = prevDownY;
            }

            int downX;
            int downY;

            Map m = CreateLayer(width, height, hasUp, hasDown, upX, upY, out downX, out downY);
            d.layers.Add(m);

            prevDownX = downX;
            prevDownY = downY;
        }

        return d;
    }

    static Map CreateLayer(int w, int h, bool hasUp, bool hasDown, int upX, int upY, out int downX, out int downY)
    {
        Map m = new Map(w, h);

        GenerateRoomLayout(m.tiles, w, h);

        downX = -1;
        downY = -1;

        if (hasUp)
        {
            if (!m.InBounds(upX, upY) || m.tiles[upY, upX] != Tile.Floor)
            {
                FindRandomFloor(m, out upX, out upY);
            }
            m.stairsUpX = upX;
            m.stairsUpY = upY;
            m.tiles[upY, upX] = Tile.StairsUp;
        }

        if (hasDown)
        {
            int sx;
            int sy;
            while (true)
            {
                FindRandomFloor(m, out sx, out sy);
                if (!hasUp || sx != upX || sy != upY) break;
            }
            m.stairsDownX = sx;
            m.stairsDownY = sy;
            m.tiles[sy, sx] = Tile.StairsDown;
            downX = sx;
            downY = sy;
        }

        int monsterCount = RNG.Next(1, 4);
        for (int i = 0; i < monsterCount; i++)
        {
            int mx;
            int my;
            while (true)
            {
                FindRandomFloor(m, out mx, out my);
                Tile t = m.tiles[my, mx];
                if (t == Tile.Floor) break;
            }
            Monster monster = RandomMonster();
            monster.X = mx;
            monster.Y = my;
            m.monsters.Add(monster);
        }

        return m;
    }

    static void GenerateRoomLayout(Tile[,] tiles, int w, int h)
    {
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (x == 0 || x == w - 1 || y == 0 || y == h - 1)
                    tiles[y, x] = Tile.Wall;
                else
                    tiles[y, x] = Tile.Floor;
            }
        }

        int roomCount = RNG.Next(3, 7);

        for (int r = 0; r < roomCount; r++)
        {
            int rx = RNG.Next(2, w - 6);
            int ry = RNG.Next(2, h - 6);

            int maxRoomW = Math.Min(12, w - 2 - rx);
            int maxRoomH = Math.Min(8, h - 2 - ry);
            if (maxRoomW < 4 || maxRoomH < 4) continue;

            int rw = RNG.Next(4, maxRoomW + 1);
            int rh = RNG.Next(4, maxRoomH + 1);

            for (int y = ry; y < ry + rh; y++)
            {
                for (int x = rx; x < rx + rw; x++)
                {
                    if (y <= 0 || y >= h - 1 || x <= 0 || x >= w - 1) continue;
                    if (x == rx || x == rx + rw - 1 || y == ry || y == ry + rh - 1)
                        tiles[y, x] = Tile.Wall;
                }
            }

            AddDoorsForRect(tiles, rx, ry, rw, rh, w, h);
        }
    }

    static void AddDoorsForRect(Tile[,] tiles, int x, int y, int w, int h, int mapW, int mapH)
    {
        int doorCount = RNG.Next(1, 4);

        for (int i = 0; i < doorCount; i++)
        {
            int side = RNG.Next(0, 4);

            if (side == 0)
            {
                if (w <= 2) continue;
                int dx = RNG.Next(x + 1, x + w - 1);
                int dy = y;
                if (dy > 0 && dy < mapH - 1 && dx > 0 && dx < mapW - 1)
                    tiles[dy, dx] = Tile.Floor;
            }
            else if (side == 1)
            {
                if (w <= 2) continue;
                int dx = RNG.Next(x + 1, x + w - 1);
                int dy = y + h - 1;
                if (dy > 0 && dy < mapH - 1 && dx > 0 && dx < mapW - 1)
                    tiles[dy, dx] = Tile.Floor;
            }
            else if (side == 2)
            {
                if (h <= 2) continue;
                int dx = x;
                int dy = RNG.Next(y + 1, y + h - 1);
                if (dy > 0 && dy < mapH - 1 && dx > 0 && dx < mapW - 1)
                    tiles[dy, dx] = Tile.Floor;
            }
            else
            {
                if (h <= 2) continue;
                int dx = x + w - 1;
                int dy = RNG.Next(y + 1, y + h - 1);
                if (dy > 0 && dy < mapH - 1 && dx > 0 && dx < mapW - 1)
                    tiles[dy, dx] = Tile.Floor;
            }
        }
    }

    static void FindRandomFloor(Map m, out int x, out int y)
    {
        while (true)
        {
            x = RNG.Next(1, m.Width - 1);
            y = RNG.Next(1, m.Height - 1);
            if (m.tiles[y, x] == Tile.Floor) return;
        }
    }

    static Monster RandomMonster()
    {
        int roll = RNG.Next(0, 100);
        if (roll < 60) return new Goblin();
        if (roll < 90) return new Orc();
        return new Dragon();
    }
}
