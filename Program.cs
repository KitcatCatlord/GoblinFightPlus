namespace GoblinFight_;

/* Hero plan:
 * You start with base health, strength and skill.
 * You can train before you enter the 'dungeon' to increase your strength, but that also gives the enemies time to train so they get a bit stronger.
 *  I can take this mechanic from my other program and just retrofit it here.
 * Then when you enter, the damage you do is your strength * skill + weapon damage rounded to nearest int.
 * You have an inventory of weapons and can select one to use as your main.
 *  Maybe different weapons have different dps?
 *
 * I'll add training later, it's too much work for one night...
 *  
 * Later, maybe add weapon qualities? So good quality would do more damage or attack faster for example.
 */
enum GameState
{
    Playing,
    Paused,
    Inventory
}

enum ItemQuality
{
    Common,
    Uncommon,
    Rare,
    Epic
}

struct Item
{
    public string name;
    public int damage;
    public double cooldown; // seconds
    public int weight;
    public int quality;
}

class ItemDatabase
{
    public static Item goblinsArm = new Item
    {
        name = "Goblin's Arm",
        damage = 3,
        cooldown = 0.5,
        weight = 4
    };
    public static Item Sword = new Item
    {
        name = "Sword",
        damage = 10,
        cooldown = 0.8,
        weight = 8
    };
    public static Item Axe = new Item
    {
        name = "Axe",
        damage = 15,
        cooldown = 1.5,
        weight = 8
    };
    public static Item Bow = new Item
    {
        name = "Bow",
        damage = 7,
        cooldown = 1.2,
        weight = 3
    };
    public static Item FireBreath = new Item
    {
        name = "Fire Breath",
        damage = 20,
        cooldown = 1.8,
        weight = 0
    };
}

static class ItemFactory
{
    public static Item MakeWithQuality(Item template, int quality)
    {
        if (quality < 0) quality = 0;
        if (quality > 3) quality = 3;

        Item item = template;
        item.quality = quality;

        double mult = 1.0 + 0.25 * quality;
        item.damage = (int)Math.Round(template.damage * mult);

        double speedMult = 1.0 - 0.1 * quality;
        if (speedMult < 0.5) speedMult = 0.5;
        item.cooldown = template.cooldown * speedMult;

        // string qName = QualityName(quality);
        // if (qName != "") item.name = qName + " " + template.name;
        // else item.name = template.name;
        item.name = template.name;

        return item;
    }
    public static string ShortQualityName(int q)
    {
        if (q == 3) return "Legendary";
        if (q == 2) return "Rare";
        if (q == 1) return "Fine";
        return "Common";
    }

    public static Item MakeRandomQuality(Item template)
    {
        int roll = RNG.Next(0, 100);
        int q;
        if (roll < 5) q = 3;
        else if (roll < 20) q = 2;
        else if (roll < 50) q = 1;
        else q = 0;
        return MakeWithQuality(template, q);
    }

    static string QualityName(int quality)
    {
        if (quality == 1) return "Fine";
        if (quality == 2) return "Rare";
        if (quality == 3) return "Legendary";
        return "";
    }
}

class Hero
{
    private int _health = 100;
    private double _strength = 5;
    private int _skill = 1;
    private double _carryWeight = 0;

    static Item Fist = new Item { name = "Fist", damage = 2, cooldown = 0.5, weight = 0 };
    private Item _equippedItem = Fist;
    private List<Item> _inventory = new List<Item>();

    public int X;
    public int Y;

    public void UpdateCarryWeight()
    {
        _carryWeight = _strength * 10;
    }

    public bool Damage(int attackDamage) // true for no health
    {
        _health -= attackDamage;
        if (_health <= 0)
        {
            _health = 0;
            return true;
        }
        return false;
    }
    public List<Item> ListInventory() => _inventory;
    public Item? GetItem(int index)
    {
        if (index >= 0 && index < _inventory.Count)
            return _inventory[index];
        return null;
    }
    public double GetInventoryWeight()
    {
        double total = 0;
        for (int i = 0; i < _inventory.Count; i++)
        {
            total += _inventory[i].weight;
        }
        return total;
    }
    public double GetCarryWeight() => _carryWeight;
    public bool AddToInventory(Item collectedItem) // false for failed
    {
        double inventoryWeight = GetInventoryWeight();
        double carryWeight = GetCarryWeight();
        if ((inventoryWeight + collectedItem.weight) > carryWeight) return false;
        _inventory.Add(collectedItem);
        return true;
    }
    public bool RemoveFromInventory(int index) // false for failed
    {
        if (index >= 0 && index < _inventory.Count)
        {
            _inventory.RemoveAt(index);
            return true;
        }
        return false;
    }
    public bool EquipItem(int index)
    {
        if (index >= 0 && index < _inventory.Count)
        {
            Item newItem = _inventory[index];
            double newTotalWeight = GetInventoryWeight() - newItem.weight + _equippedItem.weight;

            if (newTotalWeight > _carryWeight)
                return false;

            Item temp = _equippedItem;
            _equippedItem = newItem;
            _inventory[index] = temp;

            return true;
        }
        return false;
    }
    public bool DropItem(int index, Map map)
    { // false for failed
        if (index < 0 || index >= _inventory.Count) return false;
        
        Item drop = _inventory[index];
        _inventory.RemoveAt(index);

        MapLoot ml = new MapLoot { item = drop, X = X, Y = Y};
        map.loot.Add(ml);

        return true; // If you drop two items what happens... eh not my problem
    }
    public virtual int GetAttackDamage()
    {
        double raw = _strength * _skill + _equippedItem.damage;
        return (int)Math.Round(raw);
    }
    public int Health => _health;
    public double Strength => _strength;
    public int Skill => _skill;
    public Item EquippedItem => _equippedItem;
}

abstract class Monster
{
    protected int _health;
    protected int _strength;
    protected int _skill;
    protected Item _equippedItem;
    protected Item[] _possibleWeapons;
    protected int _moveIntervalMs;
    protected long _lastMoveTimeMs;
    protected long _lastHitTimeMs;
    public int X;
    public int Y;
    private double _lastAttackTime = 0;

    public Monster(int health, int strength, int skill, Item[] possibleWeapons, int moveIntervalMs)
    {
        _health = health;
        _strength = strength;
        _skill = skill;
        _possibleWeapons = possibleWeapons;
        _moveIntervalMs = moveIntervalMs;
        _lastMoveTimeMs = RNG.Next(0, moveIntervalMs);
        _lastHitTimeMs = -1000000;
        int idx = RNG.Next(0, _possibleWeapons.Length);
        Item template = _possibleWeapons[idx];
        _equippedItem = ItemFactory.MakeRandomQuality(template);
    }
    public bool Damage(int attackDamage)
    {
        _health -= attackDamage;
        if (_health <= 0)
        {
            _health = 0;
            return true;
        }
        return false;
    }
    public int Health => _health;
    public int Strength => _strength;
    public int Skill => _skill;
    public Item EquippedWeapon => _equippedItem;
    public int MoveIntervalMs => _moveIntervalMs;
    public long LastMoveTimeMs
    {
        get => _lastMoveTimeMs;
        set => _lastMoveTimeMs = value;
    }
    public long LastHitTimeMs
    {
        get => _lastHitTimeMs;
        set => _lastHitTimeMs = value;
    }
    public virtual int GetAttackDamage(double now)
    {
        if (now - _lastAttackTime < _equippedItem.cooldown)
            return 0;

        _lastAttackTime = now;

        double raw = _strength * _skill + _equippedItem.damage;
        return (int)Math.Round(raw);
    }
    public Item? GetWeaponDrop()
    {
        int roll = RNG.Next(0, 100);
        if (roll < 25) return _equippedItem;
        return null;
    }
}

class Goblin : Monster
{
    public Goblin() : base(
        30,
        2,
        1,
        new Item[] {
            ItemDatabase.goblinsArm,
            ItemDatabase.Bow
        },
        600
    )
    {
    }
}

class Orc : Monster
{
    public Orc() : base(
        60,
        5,
        2,
        new Item[] {
            ItemDatabase.Sword,
            ItemDatabase.Axe,
            ItemDatabase.Bow
        },
        800
    )
    {
    }
}

class Dragon : Monster
{
    public Dragon() : base(
        200,
        12,
        3,
        new Item[] {
            ItemDatabase.FireBreath
        },
        1300
    )
    {
    }
}

class Program
{
    static void Main(string[] args)
    {
        ConsoleRenderer.Init();
        var buffer = ConsoleRenderer.CreateBuffer();

        Console.WriteLine("Welcome to Goblin Fight Plus!");
        Console.WriteLine("I should really call it Dungeon Fighters or something but anywho.");
        Console.WriteLine("The controls are WASD, I to open inventory, ESC to pause, and SPACE to attack");
        Console.Write("\nAnyway, enter how many layers you'd like in the dungeon: ");
        int layers = int.TryParse(Console.ReadLine(), out int value) ? value : 5;
        Console.WriteLine("Alright! Press ENTER when you're ready!");
        Console.ReadLine();
        Console.Clear();

        Hero hero = new Hero();
        hero.UpdateCarryWeight();

        Dungeon dungeon = DungeonGenerator.CreateSimpleDungeon(layers, 40, 20);

        Map startMap = dungeon.CurrentMap();

        int spawnX = startMap.stairsDownX;
        int spawnY = startMap.stairsDownY;

        if (spawnX < 0 || spawnY < 0 || startMap.tiles[spawnY, spawnX] != Tile.Floor || startMap.isDoor[spawnY, spawnX])
        {
            while (true)
            {
                int x = RNG.Next(1, startMap.Width - 1);
                int y = RNG.Next(1, startMap.Height - 1);
                if (startMap.tiles[y, x] != Tile.Floor) continue;
                if (startMap.isDoor[y, x]) continue;
                if ((x == startMap.stairsDownX && y == startMap.stairsDownY) || (x == startMap.stairsUpX && y == startMap.stairsUpY) || (x == startMap.exitX && y == startMap.exitY)) continue;
                spawnX = x;
                spawnY = y;
                break;
            }
        }

        hero.X = spawnX;
        hero.Y = spawnY;

        for (int i = 0; i < startMap.monsters.Count; i++)
        {
            Monster m = startMap.monsters[i];
            if (m is Goblin)
            {
                int dist = Math.Abs(m.X - hero.X) + Math.Abs(m.Y - hero.Y);
                if (dist < 5)
                {
                    while (true)
                    {
                        int x = RNG.Next(1, startMap.Width - 1);
                        int y = RNG.Next(1, startMap.Height - 1);
                        if (startMap.tiles[y, x] != Tile.Floor) continue;
                        if (startMap.isDoor[y, x]) continue;
                        int d2 = Math.Abs(x - hero.X) + Math.Abs(y - hero.Y);
                        if (d2 < 5) continue;

                        bool monsterHere = false;
                        for (int j = 0; j < startMap.monsters.Count; j++)
                        {
                            if (j == i) continue;
                            if (startMap.monsters[j].X == x && startMap.monsters[j].Y == y)
                            {
                                monsterHere = true;
                                break;
                            }
                        }
                        if (monsterHere) continue;

                        if ((x == startMap.stairsDownX && y == startMap.stairsDownY) || (x == startMap.stairsUpX && y == startMap.stairsUpY) || (x == startMap.exitX && y == startMap.exitY)) continue;

                        m.X = x;
                        m.Y = y;
                        break;
                    }
                }
            }
        }

        MovementSystem.Run(hero, dungeon, buffer);
    }
}
