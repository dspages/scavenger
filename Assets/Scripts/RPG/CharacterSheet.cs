using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CharacterSheet
{
    public enum CharacterClass
    {
        // Martial characters
        CLASS_SOLDIER, // Combat specialist that learns bonus maneuvers. Skill trees can focus on dual-wielding, polearms, bows, etc.
        CLASS_GUNSLINGER, // Firearm specialist
        CLASS_PALADIN, // Divinely powered tank abilities that synergize with heavy armor and heavy weapons
        CLASS_EARTHMAGE, // Focus on buff spells that multiply your melee combat ability, or spells that manipulate the terrain in your favor
        CLASS_HELLION, // Offensive abilities that stack poison, stun, etc. on your melee attacks.

        // Utility characters
        CLASS_HERBALIST, // Crafts and uses performance enhancing drugs on allies
        CLASS_SURGEON, // Utility-focused class that can heal damage between combats and gets bonus interactions with certain environmental doodads
        CLASS_BATTLEPRIEST,
        CLASS_WATERMAGE,
        CLASS_WITCHDOCTOR,

        // Scoundrel characters
        CLASS_ROGUE, // Stealth and lockpicking, abilities tied to perception
        CLASS_TINKER, // 
        CLASS_CHARLETAN,
        CLASS_AIRMAGE,
        CLASS_SHADOWCULTIST, // Sneaky and infernal abilities

        // Support characters
        CLASS_GENERAL, // Coordinates and boosts the morale of allies
        CLASS_SCIENTIST, // Utility focused class that improves crafting
        CLASS_CELESTIAL, // Caster with divine-themed abilities
        CLASS_FIREMAGE, // Damage-focused caster
        CLASS_WARLOCK, // Caster focused on summoning demons, CC, and other 'evil' powers
    }

    private IndicatorBar healthBar;
    private IndicatorBar manaBar;
    private GameObject combatPrefab;
    public GameObject avatar;
    public Inventory inventory;

    public int strength; // Carrying capacity, melee damage, thrown range
    public int agility; // dodge chance, crit chance
    public int speed; // Action point pool
    public int intellect; // Controls how many special abilities you can learn and level up
    public int endurance; // Life points, physical resistances
    public int perception; // Fog of war clearing, ranged accuracy, bonus loot
    public int willpower; // Mental resistances, mana pool

    public int level = 1;
    public int xp = 0;

    public string firstName;

    public bool dead = false;

    public EquippableWeapon weaponEquipped;
    private Equipment equipment;

    public int currentHealth;
    public int currentMovePoints = 0;
    public bool canAttack = false;

    private CharacterClass characterClass;

    public delegate void HealthChangedDelegate();
    public delegate void InventoryChangedDelegate();
    public delegate void EquipmentChangedDelegate();

    public event HealthChangedDelegate OnHealthChanged;
    public event InventoryChangedDelegate OnInventoryChanged;
    public event EquipmentChangedDelegate OnEquipmentChanged;

    public CharacterSheet(string name)
    {
        firstName = name;
        currentHealth = MaxHealth();
        inventory = new Inventory();
        equipment = new Equipment();
        
        // Subscribe to inventory/equipment changes
        inventory.OnInventoryChanged += () => OnInventoryChanged?.Invoke();
        equipment.OnEquipmentChanged += () => OnEquipmentChanged?.Invoke();
        
        // Add some test items for demonstration
        AddTestItems();
    }

    private int MoveSpeed()
    {
        return 20;
    }

    public int MaxHealth()
    {
        return (10 * level) + (5 * endurance) + strength;
    }

    public PlayerController CreateCombatAvatarAsPC(Vector3 location, Quaternion rotation)
    {
        GameObject combatant = CreateCombatAvatar(location, rotation);
        PlayerController c = combatant.AddComponent<PlayerController>();
        c.characterSheet = this;
        return c;
    }

    public EnemyController CreateCombatAvatarAsNPC(Vector3 location, Quaternion rotation)
    {
        GameObject combatant = CreateCombatAvatar(location, rotation);
        EnemyController c = combatant.AddComponent<EnemyController>();
        c.characterSheet = this;
        return c;
    }

    private GameObject CreateCombatAvatar(Vector3 location, Quaternion rotation)
    {
        combatPrefab = (GameObject)Resources.Load("Prefabs/combatant", typeof(GameObject));
        GameObject avatar = GameObject.Instantiate(combatPrefab, location, rotation) as GameObject;
        healthBar = avatar.transform.Find("Canvas").transform.Find("HealthBar").GetComponent<IndicatorBar>();
        healthBar.SetSliderMax(MaxHealth());
        healthBar.SetSlider(currentHealth);
        return avatar;
    }

    public bool CanDeploy()
    {
        return true;
    }

    public void BeginTurn()
    {
        currentMovePoints = MoveSpeed();
        canAttack = true;
    }

    public void DisplayPopupDuringCombat(string toDisplay)
    {

    }

    public int MinDamage()
    {
        return 2;
    }

    public int MaxDamage()
    {
        return 4;
    }

    public void ReceiveDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            dead = true;
            GameObject.Destroy(avatar);
        }
        if (healthBar != null)
        {
            healthBar.SetSlider(currentHealth);
        }
        OnHealthChanged?.Invoke();
    }

    // Equipment management
    public EquippableItem GetEquippedItem(EquippableItem.EquipmentSlot slot)
    {
        return equipment.Get(slot);
    }

    public bool TryEquipItem(EquippableItem item)
    {
        if (item == null) return false;
        
        // Replace any existing item in that slot via Equipment
        equipment.TryEquip(item);
        
        // Update legacy weapon reference for backwards compatibility
        if (item is EquippableWeapon weapon && (item.slot == EquippableItem.EquipmentSlot.RightHand || item.slot == EquippableItem.EquipmentSlot.LeftHand))
        {
            weaponEquipped = weapon;
        }
        return true;
    }

    public EquippableItem UnequipItem(EquippableItem.EquipmentSlot slot)
    {
        var item = equipment.Unequip(slot);
        if (item == null) return null;
        
        // Clear legacy weapon reference if needed
        if (item is EquippableWeapon && (slot == EquippableItem.EquipmentSlot.RightHand || slot == EquippableItem.EquipmentSlot.LeftHand))
        {
            weaponEquipped = null;
        }
        return item;
    }

    public IReadOnlyDictionary<EquippableItem.EquipmentSlot, EquippableItem> GetEquippedItems()
    {
        return equipment.GetAll();
    }

    public void PerformBasicAttack(CharacterSheet target)
    {
        int dam = MinDamage() + UnityEngine.Random.Range(0, 1 + MaxDamage() - MinDamage());
        target.ReceiveDamage(dam);
    }

    private void AddTestItems()
    {
        // Add some test inventory items
        inventory.TryAddItem(new InventoryItem("Health Potion", "Restores 50 HP", null, 10));
        inventory.TryAddItem(new InventoryItem("Mana Potion", "Restores 30 MP", null, 5));
        inventory.TryAddItem(new InventoryItem("Bread", "Basic food item", null, 20));
        inventory.TryAddItem(new InventoryItem("Gold Coin", "Currency", null, 99));
        
        // Add and equip a test weapon
        var testSword = new EquippableWeapon("Iron Sword", 3, 6, EquippableItem.EquipmentSlot.RightHand, "A basic iron sword");
        inventory.TryAddItem(testSword);
        TryEquipItem(testSword);
        
        // Add test armor
        var testArmor = new EquippableItem("Leather Armor", EquippableItem.EquipmentSlot.Armor, "Basic protection");
        inventory.TryAddItem(testArmor);
        TryEquipItem(testArmor);
    }
}
