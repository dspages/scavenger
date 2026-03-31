using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public partial class CharacterSheet
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

    public GameObject avatar;
    public Inventory inventory;

    public List<StatusEffect> statusEffects = new List<StatusEffect>();

    public int strength = 4; // Carrying capacity, melee damage, thrown range
    public int agility = 4; // dodge chance, crit chance, backstab bonus damage
    public int speed = 4; // Action point pool, turn order
    public int intellect = 4; // Controls how many special abilities you can learn and level up
    public int endurance = 4; // Life points, physical resistances
    public int perception = 4; // Fog of war clearing (see GetVisionRange), bonus loot
    public int willpower = 4; // Mental resistances, mana pool

    public int level = 1;
    public int xp = 0;
    /// <summary>Unspent attribute points from level-ups. Spend via +/- in Character Sheet. Cap per attribute = 5 + level/2.</summary>
    public int unspentLevelUpPoints = 0;

    /// <summary>Individual display name. If omitted at construction, defaults to a plain-English name derived from <see cref="characterClass"/>.</summary>
    public string firstName;

    /// <summary>Individual portrait (runtime reference). If null, <see cref="ResolvePortrait"/> tries <see cref="portraitResourcePath"/> then a class default Resources path.</summary>
    public Sprite portrait;

    /// <summary>Resources path (no extension) for this character's portrait, e.g. <c>Portraits/hero_mara</c>. Optional; class default used when empty and <see cref="portrait"/> is null.</summary>
    public string portraitResourcePath = "";

    public bool dead = false;

    public Equipment equipment { get; private set; }

    public int currentHealth;
    public int currentMana;
    /// <summary>Morale / sanity pool for arcane costs. May be spent below zero by player choice.</summary>
    public int currentSanity;
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
    private readonly List<AbilityData> knownAbilities = new List<AbilityData>();

    public CharacterSheet(string name, CharacterClass characterClass, bool assignDefaults = true)
    {
        this.characterClass = characterClass;
        firstName = string.IsNullOrWhiteSpace(name)
            ? GetDefaultDisplayNameForClass(characterClass)
            : name.Trim();
        currentHealth = MaxHealth();
        currentMana = MaxMana();
        currentSanity = MaxSanity();
        inventory = new Inventory();
        equipment = new Equipment();
        
        inventory.OnInventoryChanged += () => OnInventoryChanged?.Invoke();
        equipment.OnEquipmentChanged += () => OnEquipmentChanged?.Invoke();

        if (assignDefaults)
        {
            CharacterSetup.AssignStartingGear(this);
            CharacterSetup.AssignStartingAbilities(this);
        }
    }

    /// <summary>Individual or class-default name for UI lists.</summary>
    public string DisplayName()
    {
        return string.IsNullOrWhiteSpace(firstName)
            ? GetDefaultDisplayNameForClass(characterClass)
            : firstName.Trim();
    }

    /// <summary>Plain-English default name when no individual name is set (matches class identity).</summary>
    public static string GetDefaultDisplayNameForClass(CharacterClass cls)
    {
        var raw = cls.ToString();
        if (raw.StartsWith("CLASS_", StringComparison.Ordinal))
            raw = raw.Substring("CLASS_".Length);
        raw = raw.Replace('_', ' ').Trim();
        if (raw.Length == 0)
            return "Unknown";
        return char.ToUpperInvariant(raw[0]) + raw.Substring(1).ToLowerInvariant();
    }

    /// <summary>Resources path for a class default portrait: <c>Portraits/{enum}</c> (e.g. <c>Portraits/CLASS_FIREMAGE</c>). Add sprites under Resources/Portraits/ to use.</summary>
    public static string GetDefaultPortraitResourcePath(CharacterClass cls) => "Portraits/" + cls;

    /// <summary>Portrait for UI: individual sprite, then <see cref="portraitResourcePath"/>, then class default Resources path.</summary>
    public Sprite ResolvePortrait()
    {
        if (portrait != null)
            return portrait;
        if (!string.IsNullOrEmpty(portraitResourcePath))
        {
            var fromPath = Resources.Load<Sprite>(portraitResourcePath);
            if (fromPath != null)
                return fromPath;
        }
        return Resources.Load<Sprite>(GetDefaultPortraitResourcePath(characterClass));
    }

    private int MoveSpeed()
    {
        return 20 + 5 * speed;
    }

    public int MaxHealth()
    {
        return (10 * level) + (5 * endurance);
    }

    public int MaxMana()
    {
        return (10 * level) + (5 * willpower);
    }

    public int MaxSanity()
    {
        return (10 * level) + (5 * willpower);
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

    public void ReceiveDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            dead = true;
        }
        OnHealthChanged?.Invoke();
    }

    public void DisplayPopup(string text)
    {
        if (string.IsNullOrEmpty(text) || avatar == null) return;
        var ac = avatar.GetComponent<AvatarController>();
        if (ac != null) ac.DisplayPopup(text);
    }

    public void DisplayPopupAfterDelay(float time, string text)
    {
        if (string.IsNullOrEmpty(text) || avatar == null) return;
        var ac = avatar.GetComponent<AvatarController>();
        if (ac != null) ac.DisplayPopupAfterDelay(time, text);
    }

    // --- Derived stats (base + equipment + status). Single source of truth for UI and combat. ---

    /// <summary>Total armor from all equipped items and BULWARK status. Used by AttackResolver and UI.</summary>
    public int GetTotalArmor()
    {
        int total = 0;
        foreach (var kvp in equipment.GetAll())
            total += kvp.Value.armorBonus;
        foreach (var effect in statusEffects)
        {
            if (effect.type == StatusEffect.EffectType.BULWARK)
                total += effect.PowerLevel;
        }
        return total;
    }

    /// <summary>Nonzero resistances (percent) from equipment and status. Keys are damage types; values are percent reduction. Empty if none.</summary>
    public IReadOnlyDictionary<DamageType, int> GetNonzeroResistances()
    {
        var dict = new Dictionary<DamageType, int>();
        // TODO: add resistance from equipment/status when those fields exist; for now base stats could contribute
        return dict;
    }

    /// <summary>Display damage summary for one hand: min, max, damage type. Used by inventory/character UI (both hands, no combat selection).</summary>
    public struct WeaponDamageSummary
    {
        public int minDamage;
        public int maxDamage;
        public DamageType damageType;
        public string label; // e.g. "R" or "L"
    }

    /// <summary>Returns damage summary for left and right hand in order. Unarmed uses AttackResolver.UNARMED_DAMAGE with Bludgeoning.</summary>
    /// <summary>Attribute cap for level-up: 5 + level/2 (e.g. level 1 → 5, level 4 → 7).</summary>
    public int GetAttributeCap() => 5 + (level / 2);

    /// <summary>True if the character can spend a point on the given attribute (has unspent points and attribute is below cap).</summary>
    public bool CanSpendOnAttribute(int currentAttributeValue)
    {
        return unspentLevelUpPoints > 0 && currentAttributeValue < GetAttributeCap();
    }

    public void GetEquippedWeaponDamageSummary(out WeaponDamageSummary? left, out WeaponDamageSummary? right)
    {
        left = null;
        right = null;
        var leftItem = equipment.Get(EquippableItem.EquipmentSlot.LeftHand) as EquippableHandheld;
        var rightItem = equipment.Get(EquippableItem.EquipmentSlot.RightHand) as EquippableHandheld;
        if (leftItem != null)
            left = new WeaponDamageSummary { minDamage = Mathf.Max(1, leftItem.damage - 1), maxDamage = leftItem.damage + 1, damageType = leftItem.damageType, label = "L" };
        if (rightItem != null)
            right = new WeaponDamageSummary { minDamage = Mathf.Max(1, rightItem.damage - 1), maxDamage = rightItem.damage + 1, damageType = rightItem.damageType, label = "R" };
        if (left == null && right == null)
        {
            int u = AttackResolver.UNARMED_DAMAGE;
            right = new WeaponDamageSummary { minDamage = Mathf.Max(1, u - 1), maxDamage = u + 1, damageType = DamageType.Bludgeoning, label = "R" };
        }
    }

    // Equipment pass-throughs (Equipment is the authority on rules)
    public EquippableItem GetEquippedItem(EquippableItem.EquipmentSlot slot) => equipment.Get(slot);
    public IReadOnlyDictionary<EquippableItem.EquipmentSlot, EquippableItem> GetEquippedItems() => equipment.GetAll();
    public bool IsSlotCompatible(EquippableItem.EquipmentSlot slot, InventoryItem item) => equipment.IsSlotCompatible(slot, item);
    public bool TryEquipItem(EquippableItem item) => item != null && equipment.TryEquip(item);
    public bool TryEquipItemToSlot(EquippableItem item, EquippableItem.EquipmentSlot slot) => item != null && equipment.TryEquipToSlot(item, slot, out _);
    public EquippableItem UnequipItem(EquippableItem.EquipmentSlot slot) => equipment.Unequip(slot);

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

    public void LearnAbility(AbilityData ability)
    {
        if (ability == null) return;
        foreach (var a in knownAbilities)
        {
            if (a.id == ability.id) return;
        }
        knownAbilities.Add(ability);
    }

    public void LearnAbility(string abilityId)
    {
        var data = ContentRegistry.GetAbilityData(abilityId);
        if (data != null) LearnAbility(data);
    }

    public IReadOnlyList<AbilityData> GetKnownAbilities()
    {
        return knownAbilities;
    }
}
