using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSheet
{
    public enum CharacterClass
    {
        CLASS_SOLDIER, // Combat specialist that learns bonus maneuvers. Skill trees can focus on dual-wielding, polearms, bows, etc.
        CLASS_GUNSLINGER, // Firearm specialist
        CLASS_PALADIN, // Divinely powered tank abilities that synergize with heavy armor and heavy weapons
        CLASS_EARTHMAGE, // Focus on buff spells that multiply your melee combat ability, or spells that manipulate the terrain in your favor
        CLASS_HELLION, // Offensive abilities that stack poison, stun, etc. on your melee attacks.

        CLASS_HERBALIST, // Crafts and uses performance enhancing drugs on allies
        CLASS_SURGEON, // Utility-focused class that can heal damage between combats and gets bonus interactions with certain environmental doodads
        CLASS_BATTLEPRIEST,
        CLASS_WATERMAGE,
        CLASS_WITCHDOCTOR,

        CLASS_ROGUE, // Stealth and lockpicking, abilities tied to perception
        CLASS_TINKER, // 
        CLASS_CHARLETAN,
        CLASS_AIRMAGE,
        CLASS_SHADOWCULTIST, // Sneaky and infernal abilities

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

    public int strength; // Carrying capacity, melee damage, thrown range, small impact on life
    public int agility; // Move speed, dodge chance, crit chance
    public int intellect; // Controls how many special abilities you can learn and level up
    public int endurance; // Life points, physical resistances
    public int perception; // Fog of war clearing, ranged accuracy, bonus loot
    public int willpower; // Mental resistances, mana pool

    public int level = 1;
    public int xp = 0;

    public string firstName;

    public bool dead = false;

    public EquippableWeapon weaponEquipped;

    public int currentHealth;
    public int currentMovePoints = 0;
    public bool canAttack = false;

    private CharacterClass characterClass;

    public CharacterSheet(string name)
    {
        firstName = name;
        currentHealth = MaxHealth();
        inventory = new Inventory();
    }

    private int MoveSpeed()
    {
        return 8;
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
        healthBar.SetSlider(currentHealth);
    }

    public void PerformBasicAttack(CharacterSheet target)
    {
        int dam = MinDamage() + Random.Range(0, 1 + MaxDamage() - MinDamage());
        target.ReceiveDamage(dam);
    }
}
