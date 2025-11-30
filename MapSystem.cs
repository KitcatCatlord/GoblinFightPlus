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
    struct RoomRect
    {
        public int x;
        public int y;
        public int w;
        public int h;
        public int cluster;
    }

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

        GenerateRoomLayout(m);

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

    static void GenerateRoomLayout(Map m)
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
            }
        }

        List<RoomRect> rooms = new List<RoomRect>();

        int maxClusters = 3;
        int clusterCount = RNG.Next(1, maxClusters + 1);
        int targetRooms = 12;
        int roomsPerClusterBase = targetRooms / clusterCount;

        for (int c = 0; c < clusterCount; c++)
        {
            int roomsInCluster = roomsPerClusterBase + RNG.Next(0, 3);
            List<RoomRect> clusterRooms = new List<RoomRect>();

            for (int n = 0; n < roomsInCluster; n++)
            {
                RoomRect r;

                if (clusterRooms.Count == 0)
                {
                    r = MakeRandomRoomCandidateForCluster(w, h, c);
                }
                else
                {
                    RoomRect baseRoom = clusterRooms[RNG.Next(0, clusterRooms.Count)];
                    r = MakeRoomNear(baseRoom, w, h);
                }

                if (r.w <= 0 || r.h <= 0) continue;
                r.cluster = c;

                bool overlap = false;
                for (int j = 0; j < rooms.Count; j++)
                {
                    if (IntersectsExpanded(r, rooms[j]))
                    {
                        overlap = true;
                        break;
                    }
                }
                if (overlap) continue;

                rooms.Add(r);
                clusterRooms.Add(r);
            }
        }

        for (int i = 0; i < rooms.Count; i++)
        {
            RoomRect r = rooms[i];

            for (int y = r.y; y < r.y + r.h; y++)
            {
                for (int x = r.x; x < r.x + r.w; x++)
                {
                    if (y <= 0 || y >= h - 1 || x <= 0 || x >= w - 1) continue;
                    if (x == r.x || x == r.x + r.w - 1 || y == r.y || y == r.y + r.h - 1)
                        m.tiles[y, x] = Tile.Wall;
                }
            }

            AddDoorsForRect(m, r);
        }
    }

    static RoomRect MakeRandomRoomCandidateForCluster(int mapW, int mapH, int cluster)
    {
        RoomRect r = new RoomRect();

        int minW = 4;
        int minH = 4;
        int maxW = Math.Min(12, mapW - 4);
        int maxH = Math.Min(8, mapH - 4);

        if (maxW < minW || maxH < minH)
        {
            r.w = 0;
            r.h = 0;
            return r;
        }

        int side = cluster % 4;

        int rw = RNG.Next(minW, maxW + 1);
        int rh = RNG.Next(minH, maxH + 1);

        if (side == 0)
        {
            r.y = 1;
            r.x = RNG.Next(1, mapW - rw - 1);
        }
        else if (side == 1)
        {
            r.y = mapH - rh - 1;
            r.x = RNG.Next(1, mapW - rw - 1);
        }
        else if (side == 2)
        {
            r.x = 1;
            r.y = RNG.Next(1, mapH - rh - 1);
        }
        else
        {
            r.x = mapW - rw - 1;
            r.y = RNG.Next(1, mapH - rh - 1);
        }

        r.w = rw;
        r.h = rh;

        if (r.x < 1) r.x = 1;
        if (r.y < 1) r.y = 1;
        if (r.x + r.w > mapW - 1) r.w = mapW - 1 - r.x;
        if (r.y + r.h > mapH - 1) r.h = mapH - 1 - r.y;

        return r;
    }

    static RoomRect MakeRoomNear(RoomRect baseRoom, int mapW, int mapH)
    {
        RoomRect r = new RoomRect();

        int minW = 4;
        int minH = 4;
        int maxW = Math.Min(12, mapW - 4);
        int maxH = Math.Min(8, mapH - 4);

        if (maxW < minW || maxH < minH)
        {
            r.w = 0;
            r.h = 0;
            return r;
        }

        int rw = RNG.Next(minW, maxW + 1);
        int rh = RNG.Next(minH, maxH + 1);

        int dx = RNG.Next(-6, 7);
        int dy = RNG.Next(-4, 5);

        r.x = baseRoom.x + dx;
        r.y = baseRoom.y + dy;
        r.w = rw;
        r.h = rh;

        if (r.x < 1) r.x = 1;
        if (r.y < 1) r.y = 1;
        if (r.x + r.w > mapW - 1) r.w = mapW - 1 - r.x;
        if (r.y + r.h > mapH - 1) r.h = mapH - 1 - r.y;

        return r;
    }

    static bool IntersectsExpanded(RoomRect a, RoomRect b)
    {
        int ax1 = a.x - 1;
        int ay1 = a.y - 1;
        int ax2 = a.x + a.w;
        int ay2 = a.y + a.h;

        int bx1 = b.x - 1;
        int by1 = b.y - 1;
        int bx2 = b.x + b.w;
        int by2 = b.y + b.h;

        if (ax1 >= bx2 || bx1 >= ax2) return false;
        if (ay1 >= by2 || by1 >= ay2) return false;
        return true;
    }

    static void AddDoorsForRect(Map m, RoomRect r)
    {
        int doorCount = RNG.Next(1, 4);
        int mapW = m.Width;
        int mapH = m.Height;

        for (int i = 0; i < doorCount; i++)
        {
            int side = RNG.Next(0, 4);

            if (side == 0)
            {
                if (r.w <= 2) continue;
                int dx = RNG.Next(r.x + 1, r.x + r.w - 1);
                int dy = r.y;
                if (dy > 0 && dy < mapH - 1 && dx > 0 && dx < mapW - 1)
                {
                    m.tiles[dy, dx] = Tile.Floor;
                    m.isDoor[dy, dx] = true;
                }
            }
            else if (side == 1)
            {
                if (r.w <= 2) continue;
                int dx = RNG.Next(r.x + 1, r.x + r.w - 1);
                int dy = r.y + r.h - 1;
                if (dy > 0 && dy < mapH - 1 && dx > 0 && dx < mapW - 1)
                {
                    m.tiles[dy, dx] = Tile.Floor;
                    m.isDoor[dy, dx] = true;
                }
            }
            else if (side == 2)
            {
                if (r.h <= 2) continue;
                int dx = r.x;
                int dy = RNG.Next(r.y + 1, r.y + r.h - 1);
                if (dy > 0 && dy < mapH - 1 && dx > 0 && dx < mapW - 1)
                {
                    m.tiles[dy, dx] = Tile.Floor;
                    m.isDoor[dy, dx] = true;
                }
            }
            else
            {
                if (r.h <= 2) continue;
                int dx = r.x + r.w - 1;
                int dy = RNG.Next(r.y + 1, r.y + r.h - 1);
                if (dy > 0 && dy < mapH - 1 && dx > 0 && dx < mapW - 1)
                {
                    m.tiles[dy, dx] = Tile.Floor;
                    m.isDoor[dy, dx] = true;
                }
            }
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
