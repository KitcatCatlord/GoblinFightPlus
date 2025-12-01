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

            int upX = -1;
            int upY = -1;
            if (hasUp)
            {
                upX = prevDownX;
                upY = prevDownY;
            }

            int downX;
            int downY;

            Map m = CreateLayer(width, height, hasUp, hasDown, isLast, upX, upY, out downX, out downY);
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
            if (!m.InBounds(upX, upY) || m.tiles[upY, upX] != Tile.Floor || m.isDoor[upY, upX])
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
            if (hasUp)
            {
                FindRandomFloorFar(m, upX, upY, out sx, out sy);
            }
            else
            {
                FindRandomFloor(m, out sx, out sy);
            }
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
            if (hasUp)
            {
                FindRandomFloorFar(m, upX, upY, out ex, out ey);
            }
            else
            {
                FindRandomFloor(m, out ex, out ey);
            }
            m.exitX = ex;
            m.exitY = ey;
            m.tiles[ey, ex] = Tile.Exit;
        }

        int monsterCount = RNG.Next(3, 7);
        for (int i = 0; i < monsterCount; i++)
        {
            int mx;
            int my;
            while (true)
            {
                FindRandomFloor(m, out mx, out my);
                if (m.tiles[my, mx] != Tile.Floor) continue;
                if (m.isDoor[my, mx]) continue;
                if ((mx == m.stairsUpX && my == m.stairsUpY) || (mx == m.stairsDownX && my == m.stairsDownY) || (mx == m.exitX && my == m.exitY)) continue;
                break;
            }
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
            while (true)
            {
                FindRandomFloor(m, out lx, out ly);
                if (m.tiles[ly, lx] != Tile.Floor) continue;
                if (m.isDoor[ly, lx]) continue;
                if ((lx == m.stairsUpX && ly == m.stairsUpY) || (lx == m.stairsDownX && ly == m.stairsDownY) || (lx == m.exitX && ly == m.exitY)) continue;

                bool monsterHere = false;
                for (int j = 0; j < m.monsters.Count; j++)
                {
                    if (m.monsters[j].X == lx && m.monsters[j].Y == ly)
                    {
                        monsterHere = true;
                        break;
                    }
                }
                if (monsterHere) continue;

                bool lootHere = false;
                for (int j = 0; j < m.loot.Count; j++)
                {
                    if (m.loot[j].X == lx && m.loot[j].Y == ly)
                    {
                        lootHere = true;
                        break;
                    }
                }
                if (lootHere) continue;

                break;
            }

            Item template = RandomLootTemplate();
            Item lootItem = ItemFactory.MakeRandomQuality(template);
            MapLoot ml = new MapLoot { item = lootItem, X = lx, Y = ly };
            m.loot.Add(ml);
        }

        return m;
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

        int innerX = 1;
        int innerY = 1;
        int innerW = w - 2;
        int innerH = h - 2;

        PartitionRegion(m, innerX, innerY, innerW, innerH, 0);
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
            {
                m.tiles[yy, splitX] = Tile.Wall;
            }

            int doorY = RNG.Next(y + 1, y + h - 1);
            if (doorY > y && doorY < y + h - 1 && splitX > 0 && splitX < m.Width - 1)
            {
                if (m.tiles[doorY, splitX - 1] == Tile.Floor && m.tiles[doorY, splitX + 1] == Tile.Floor)
                {
                    m.tiles[doorY, splitX] = Tile.Floor;
                    m.isDoor[doorY, splitX] = true;
                }
            }

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
            {
                m.tiles[splitY, xx] = Tile.Wall;
            }

            int doorX = RNG.Next(x + 1, x + w - 1);
            if (doorX > x && doorX < x + w - 1 && splitY > 0 && splitY < m.Height - 1)
            {
                if (m.tiles[splitY - 1, doorX] == Tile.Floor && m.tiles[splitY + 1, doorX] == Tile.Floor)
                {
                    m.tiles[splitY, doorX] = Tile.Floor;
                    m.isDoor[splitY, doorX] = true;
                }
            }

            int topH = splitY - y;
            int bottomY = splitY + 1;
            int bottomH = y + h - bottomY;

            if (topH > 0) PartitionRegion(m, x, y, w, topH, depth + 1);
            if (bottomH > 0) PartitionRegion(m, x, bottomY, w, bottomH, depth + 1);
        }
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

    static void FindRandomFloorFar(Map m, int fromX, int fromY, out int x, out int y)
    {
        int minDist = (m.Width + m.Height) / 4;
        int tries = 0;

        while (true)
        {
            x = RNG.Next(1, m.Width - 1);
            y = RNG.Next(1, m.Height - 1);
            if (m.tiles[y, x] != Tile.Floor) continue;
            if (m.isDoor[y, x]) continue;

            if (fromX >= 0 && fromY >= 0)
            {
                int dist = Math.Abs(x - fromX) + Math.Abs(y - fromY);
                if (dist < minDist && tries < 100)
                {
                    tries++;
                    continue;
                }
            }

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
