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

    public EquippableHandheld weaponEquipped;
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
        if (item is EquippableHandheld weapon && (item.slot == EquippableItem.EquipmentSlot.RightHand || item.slot == EquippableItem.EquipmentSlot.LeftHand))
        {
            weaponEquipped = weapon;
        }
        return true;
    }

    public bool TryEquipItemToSlot(EquippableItem item, EquippableItem.EquipmentSlot slot)
    {
        if (item == null) return false;
        
        // For hand slots, allow hand-held items regardless of their default slot
        if (slot == EquippableItem.EquipmentSlot.LeftHand || slot == EquippableItem.EquipmentSlot.RightHand)
        {
            if (item is EquippableHandheld)
            {
                // Temporarily change the item's slot to the target slot for equipping
                var originalSlot = item.slot;
                item.slot = slot;
                
                var success = equipment.TryEquipToSlot(item, slot, out var previous);
                
                if (success)
                {
                    // Update legacy weapon reference for backwards compatibility
                    if (slot == EquippableItem.EquipmentSlot.RightHand || slot == EquippableItem.EquipmentSlot.LeftHand)
                    {
                        weaponEquipped = item as EquippableHandheld;
                    }
                    return true;
                }
                else
                {
                    // Restore original slot if equipping failed
                    item.slot = originalSlot;
                    return false;
                }
            }
        }
        
        // For non-hand slots or non-hand-held items, use the default equipping logic
        if (item.slot != slot) return false;
        return TryEquipItem(item);
    }

    public EquippableItem UnequipItem(EquippableItem.EquipmentSlot slot)
    {
        var item = equipment.Unequip(slot);
        if (item == null) return null;
        
        // Clear legacy weapon reference if needed
        if (item is EquippableHandheld && (slot == EquippableItem.EquipmentSlot.RightHand || slot == EquippableItem.EquipmentSlot.LeftHand))
        {
            weaponEquipped = null;
        }
        return item;
    }

    public IReadOnlyDictionary<EquippableItem.EquipmentSlot, EquippableItem> GetEquippedItems()
    {
        return equipment.GetAll();
    }

    public bool IsSlotCompatible(EquippableItem.EquipmentSlot slot, InventoryItem item)
    {
        return equipment.IsSlotCompatible(slot, item);
    }

    public void PerformBasicAttack(CharacterSheet target)
    {
        int dam = MinDamage() + UnityEngine.Random.Range(0, 1 + MaxDamage() - MinDamage());
        target.ReceiveDamage(dam);
    }

    private void AddTestItems()
    {
        // Add some test inventory items
        inventory.TryAddItem(new InventoryItem("Health Potion") { 
            description = "Restores 50 HP", 
            stackSize = 10 
        });
        inventory.TryAddItem(new InventoryItem("Mana Potion") { 
            description = "Restores 30 MP", 
            stackSize = 5 
        });
        inventory.TryAddItem(new InventoryItem("Bread") { 
            description = "Basic food item", 
            stackSize = 20 
        });
        inventory.TryAddItem(new InventoryItem("Gold Coin") { 
            description = "Currency", 
            stackSize = 99 
        });
        
        // Add various weapon types for testing
        var cutlass = new EquippableHandheld(
            name: "Cutlass", 
            type: EquippableHandheld.WeaponType.OneHanded,
            minDmg: 3,
            maxDmg: 6,
            minRange: 1,
            maxRange: 1,
            dmgType: EquippableHandheld.DamageType.Slashing
        ) { 
            description = "A basic sword",
            actionPointCost = 10,
            rangeType = EquippableHandheld.RangeType.Melee
        };
        
        var steelShield = new EquippableHandheld(
            name: "Steel Shield", 
            type: EquippableHandheld.WeaponType.Shield, 
            minDmg: 0, 
            maxDmg: 0,
            minRange: 1,
            maxRange: 1,
            dmgType: EquippableHandheld.DamageType.Bludgeoning
        ) { 
            description = "A sturdy steel shield",
            armorBonus = 2,
            dodgeBonus = 1,
            rangeType = EquippableHandheld.RangeType.Melee
        };
        
        var pike = new EquippableHandheld(
            name: "Reach Pike", 
            type: EquippableHandheld.WeaponType.TwoHanded, 
            minDmg: 8, 
            maxDmg: 12,
            minRange: 2,
            maxRange: 2,
            dmgType: EquippableHandheld.DamageType.Piercing
        ) { 
            description = "A long pike that requires distance to use effectively",
            actionPointCost = 15,
            rangeType = EquippableHandheld.RangeType.Melee
        };
        inventory.TryAddItem(pike);
        
        // Note: minRange prevents weapons from being used at point-blank
        // This prevents self-harm with explosives and creates tactical positioning
        var musket = new EquippableHandheld(
            name: "Long Musket", 
            type: EquippableHandheld.WeaponType.TwoHanded, 
            minDmg: 12, 
            maxDmg: 18,
            minRange: 2,
            maxRange: 10,
            dmgType: EquippableHandheld.DamageType.Bludgeoning
        ) { 
            description = "A musket that requires distance to avoid muzzle flash",
            actionPointCost = 30,
            rangeType = EquippableHandheld.RangeType.Ranged,
            requiresAmmo = true,
            ammoType = "Musket Ball"
        };
        inventory.TryAddItem(musket);
        
        var dagger = new EquippableHandheld(
            name: "Iron Dagger", 
            type: EquippableHandheld.WeaponType.OneHanded, 
            minDmg: 2, 
            maxDmg: 4,
            minRange: 1,
            maxRange: 1,
            dmgType: EquippableHandheld.DamageType.Piercing
        ) { 
            description = "A quick dagger",
            actionPointCost = 4,
            rangeType = EquippableHandheld.RangeType.Melee
        };
        inventory.TryAddItem(dagger);

        var grenade = new EquippableHandheld(
            name: "Frag Grenade", 
            type: EquippableHandheld.WeaponType.OneHanded, 
            minDmg: 12,
            maxDmg: 18,
            minRange: 3,
            maxRange: 8,
            dmgType: EquippableHandheld.DamageType.Fire
        ) { 
            description = "A grenade that explodes on impact - keep your distance!",
            actionPointCost = 20,
            splashRadius = 2,
            rangeType = EquippableHandheld.RangeType.Ranged,
            isConsumable = true
        };
        inventory.TryAddItem(grenade);
        
        // Add ammo for ranged weapons
        inventory.TryAddItem(new InventoryItem("Musket Ball") { 
            description = "Ammunition for muskets",
            stackSize = 50 
        });
        
        // Equip starting gear (these items go directly to equipment, not inventory)
        TryEquipItem(cutlass);
        TryEquipItem(steelShield);
        
        // Add test armor
        var leatherArmor = new EquippableItem(
            name: "Leather Armor", 
            equipSlot: EquippableItem.EquipmentSlot.Armor
        ) { 
            description = "Basic protection" 
        };
        TryEquipItem(leatherArmor);
    }
}
