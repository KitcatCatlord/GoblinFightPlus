namespace GoblinFight_;

/* Hero plan:
 * You start with base health, strength and skill.
 * You can train before you enter the 'dungeon' to increase your strength, but that also gives the enemies time to train so they get a bit stronger.
 *  I can take this mechanic from my other program and just retrofit it here.
 * Then when you enter, the damage you do is your strength * skill + weapon damage rounded to nearest int.
 * You have an inventory of weapons and can select one to use as your main.
 *  Maybe different weapons have different dps?
 */

struct item
{
    public string name;
    public int damage;
    public double cooldown; // seconds
    public int weight;
}
class itemDatabase
{
    public static item goblinsArm = new item {
        name = "Goblin's Arm",
        damage = 3,
        cooldown = 0.5,
        weight = 4
    };
    public static item Sword = new item {
        name = "Sword",
        damage = 10,
        cooldown = 0.8,
        weight = 8
    };
    public static item Axe = new item {
        name = "Axe",
        damage = 15,
        cooldown = 1.5,
        weight = 8
    };
}
class Hero
{
    private int _health = 100;
    private double _strength = 5;
    private int _skill = 1;
    private double _carryWeight = 0;

    static item Fist = new item { name = "Fist", damage = 2, cooldown = 0.5, weight = 0 };
    private item _equippedItem = Fist;
    private List<item> _inventory = new List<item>();

    public void updateCarryWeight()
    {
        _carryWeight = _strength * 10;
    }

    public bool Damage(int attackDamage)
    {
        _health = -attackDamage;
        if (_health <= 0)
        {
            _health = 0;
            return true;
        }
        return false;
    }
    public List<item> ListInventory() => _inventory;
    public item? GetItem(int index)
    {
        if (index >= 0 && index < _inventory.Count) return _inventory[index];
        return null;
    }
    public object? ListInventory(int index = -1)
    {
        if (index == -1)
            return _inventory;

        if (index >= 0 && index < _inventory.Count)
        {
            return _inventory[index];
        }
        return null;
    }
}
class Program
{

    static void Main(string[] args)
    {
        Console.WriteLine("Hello, world!");
    }
}
