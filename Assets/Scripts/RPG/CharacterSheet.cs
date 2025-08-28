using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

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

    public List<StatusEffect> statusEffects = new List<StatusEffect>();

    public int strength = 4; // Carrying capacity, melee damage, thrown range
    public int agility = 4; // dodge chance, crit chance
    public int speed = 4; // Action point pool, turn order
    public int intellect = 4; // Controls how many special abilities you can learn and level up
    public int endurance = 4; // Life points, physical resistances
    public int perception = 4; // Fog of war clearing, ranged accuracy, bonus loot
    public int willpower = 4; // Mental resistances, mana pool

    public int level = 1;
    public int xp = 0;

    public string firstName;

    public bool dead = false;

    public EquippableHandheld weaponEquipped;
    private Equipment equipment;

    public int currentHealth;
    public int currentActionPoints = 0;

    public CharacterClass characterClass;

    public delegate void HealthChangedDelegate();
    public delegate void InventoryChangedDelegate();
    public delegate void EquipmentChangedDelegate();
    public delegate void ActionPointsChangedDelegate();

    public event HealthChangedDelegate OnHealthChanged;
    public event InventoryChangedDelegate OnInventoryChanged;
    public event EquipmentChangedDelegate OnEquipmentChanged;
    public event ActionPointsChangedDelegate OnActionPointsChanged;

    // Special actions known by this character (types); instances attach to avatar components
    private readonly List<System.Type> knownSpecialActionTypes = new List<System.Type>();

    public CharacterSheet(string name, CharacterClass characterClass)
    {
        firstName = name;
        this.characterClass = characterClass;
        currentHealth = MaxHealth();
        inventory = new Inventory();
        equipment = new Equipment();
        
        // Subscribe to inventory/equipment changes
        inventory.OnInventoryChanged += () => OnInventoryChanged?.Invoke();
        equipment.OnEquipmentChanged += () => OnEquipmentChanged?.Invoke();
        CharacterSetup.AssignStartingGear(this);
        CharacterSetup.AssignStartingAbilities(this);
    }

    private int MoveSpeed()
    {
        return 20 + 5 * speed;
    }

    public int MaxHealth()
    {
        return (10 * level) + (5 * endurance);
    }

    public PlayerController CreateCombatAvatarAsPC(Vector3 location, Quaternion rotation, Tile tile)
    {
        GameObject combatant = CreateCombatAvatar(location, rotation);
        PlayerController c = combatant.AddComponent<PlayerController>();
        c.SetCurrentTile(tile);
        c.SetCharacterSheet(this);
        AttachKnownActionsToAvatar(combatant);
        return c;
    }

    public EnemyController CreateCombatAvatarAsNPC(Vector3 location, Quaternion rotation, Tile tile)
    {
        GameObject combatant = CreateCombatAvatar(location, rotation);
        EnemyController c = combatant.AddComponent<EnemyController>();
        c.SetCurrentTile(tile);
        c.SetCharacterSheet(this);
        AttachKnownActionsToAvatar(combatant);
        return c;
    }

    private GameObject CreateCombatAvatar(Vector3 location, Quaternion rotation)
    {
        combatPrefab = (GameObject)Resources.Load("Prefabs/combatant", typeof(GameObject));
        GameObject avatar = GameObject.Instantiate(combatPrefab, location, rotation) as GameObject;
        // Track the instantiated avatar on this character sheet
        this.avatar = avatar;
        healthBar = avatar.transform.Find("Canvas").transform.Find("HealthBar").GetComponent<IndicatorBar>();
        healthBar.SetSliderMax(MaxHealth());
        healthBar.SetSlider(currentHealth);
        return avatar;
    }

    public bool CanDeploy()
    {
        return true;
    }

    // Returns true if the unit is still alive.
    public bool BeginTurn()
    {
        SetActionPoints(MoveSpeed());
        foreach (StatusEffect effect in statusEffects)
        {
            SetActionPoints(effect.PerRoundEffect(currentActionPoints));
            // The PerRoundEffect may have killed the unit (poison, burning).
            if (dead) return false;
        }
        statusEffects.RemoveAll(e => e.expired);
        return true;
    }
    
    public void SetActionPoints(int newValue)
    {
        if (currentActionPoints != newValue)
        {
            currentActionPoints = newValue;
            OnActionPointsChanged?.Invoke();
        }
    }
    
    public void ModifyActionPoints(int deltaValue)
    {
        SetActionPoints(currentActionPoints + deltaValue);
    }

    public void DisplayPopupDuringCombat(string toDisplay)
    {

    }

    public int GetVisionRange()
    {
        // Later, maybe equipment and status (blinded, eagle-eyed, etc.) can change this.
        return perception * 2;
    }

    public void ReceiveHealing(int amount)
    {
        currentHealth += amount;
        if (currentHealth > MaxHealth())
        {
            currentHealth = MaxHealth();
        }
        if (healthBar != null)
        {
            healthBar.SetSlider(currentHealth);
        }
        OnHealthChanged?.Invoke();
    }

    public void ReceivePureDamage(int amount)
    {
        ReceiveDamage(amount);
    }

    public void RegisterStatusEffect(StatusEffect effect)
    {
        // Add the status effect to the list
        statusEffects.Add(effect);
        
        // Note: Vision system will be notified through the CombatController
        // when the status effect is applied
    }
    
    public void RemoveStatusEffect(StatusEffect.EffectType effectType, bool notify = true)
    {
        // Remove the status effect
        StatusEffect.RemoveStatusEffect(statusEffects, effectType);

        // Notify the CombatController if HIDDEN status was removed (unless suppressed)
        if (notify && effectType == StatusEffect.EffectType.HIDDEN && avatar != null)
        {
            CombatController combatController = avatar.GetComponent<CombatController>();
            if (combatController != null)
            {
                combatController.NotifyStatusEffectChanged(StatusEffect.EffectType.HIDDEN);
            }
        }
    }

    public void RefreshStatusIcons()
    {
        // TODO: Implement status icon cleanup
    }

    public bool HasStatusEffect(StatusEffect.EffectType effectType)
    {
        return StatusEffect.HasEffectType(ref statusEffects, effectType);
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

    // Special actions knowledge and attachment
    public void LearnSpecialAction<T>() where T : Action
    {
        var t = typeof(T);
        if (!knownSpecialActionTypes.Contains(t))
        {
            knownSpecialActionTypes.Add(t);
        }
    }

    public IEnumerable<System.Type> GetKnownSpecialActionTypes()
    {
        return knownSpecialActionTypes;
    }

    private void AttachKnownActionsToAvatar(GameObject go)
    {
        if (go == null) return;
        foreach (var t in knownSpecialActionTypes)
        {
            if (t == null) continue;
            if (go.GetComponent(t) == null)
            {
                go.AddComponent(t);
            }
        }
    }
}
