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

struct Item
{
    public string name;
    public int damage;
    public double cooldown; // seconds
    public int weight;
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
class Hero
{
    private int _health = 100;
    private double _strength = 5;
    private int _skill = 1;
    private double _carryWeight = 0;
    public int X;
    public int Y;

    static Item Fist = new Item { name = "Fist", damage = 2, cooldown = 0.5, weight = 0 };
    private Item _equippedItem = Fist;
    private List<Item> _inventory = new List<Item>();

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
    public virtual int GetAttackDamage()
    {
        double raw = _strength * _skill;
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
    public int X;
    public int Y;
    public Monster(int health, int strength, int skill, Item[] possibleWeapons)
    {
        _health = health;
        _strength = strength;
        _skill = skill;
        _possibleWeapons = possibleWeapons;
        var rnd = new Random();
        _equippedItem = possibleWeapons[rnd.Next(possibleWeapons.Length)];
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
    public virtual int GetAttackDamage()
    {
        double raw = _strength * _skill + _equippedItem.damage;
        return (int)Math.Round(raw);
    }
    public Item GetWeaponDrop()
    {
        return _equippedItem;
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
        }
    )
    { }
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
        }
    )
    { }
}
class Dragon : Monster
{
    public Dragon() : base(
        200,
        12,
        3,
        new Item[] {
            ItemDatabase.FireBreath
        }
    )
    { }
}
class Program
{
    static void Main(string[] args)
    {
        Hero hero = new Hero();
        hero.UpdateCarryWeight();

        Dungeon dungeon = DungeonGenerator.CreateSimpleDungeon(5, 40, 20);

        Map startMap = dungeon.CurrentMap();
        if (startMap.stairsDownX >= 0 && startMap.stairsDownY >= 0)
        {
            hero.X = startMap.stairsDownX;
            hero.Y = startMap.stairsDownY;
        }

        MovementSystem.Run(hero, dungeon);

    }
}
