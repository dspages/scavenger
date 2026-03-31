using NUnit.Framework;

public class CharacterSheetTests
{
    private CharacterSheet MakeSheet(
        CharacterSheet.CharacterClass cls = CharacterSheet.CharacterClass.CLASS_SOLDIER,
        string name = "Test")
    {
        return new CharacterSheet(name, cls);
    }

    // --- Constructor & Identity ---

    [Test]
    public void Constructor_SetsNameAndClass()
    {
        var sheet = MakeSheet(CharacterSheet.CharacterClass.CLASS_ROGUE, "Rook");

        Assert.AreEqual("Rook", sheet.firstName);
        Assert.AreEqual(CharacterSheet.CharacterClass.CLASS_ROGUE, sheet.characterClass);
    }

    [Test]
    public void Constructor_StartsAtLevel1()
    {
        var sheet = MakeSheet();

        Assert.AreEqual(1, sheet.level);
        Assert.AreEqual(0, sheet.xp);
    }

    [Test]
    public void Constructor_InitializesHealthToMax()
    {
        var sheet = MakeSheet();

        Assert.AreEqual(sheet.MaxHealth(), sheet.currentHealth);
    }

    [Test]
    public void Constructor_InitializesManaToMax()
    {
        var sheet = MakeSheet();

        Assert.AreEqual(sheet.MaxMana(), sheet.currentMana);
    }

    [Test]
    public void Constructor_InitializesSanityToMax()
    {
        var sheet = MakeSheet();

        Assert.AreEqual(sheet.MaxSanity(), sheet.currentSanity);
    }

    [Test]
    public void Constructor_CreatesInventory()
    {
        var sheet = MakeSheet();

        Assert.IsNotNull(sheet.inventory);
    }

    [Test]
    public void Constructor_IsNotDead()
    {
        var sheet = MakeSheet();

        Assert.IsFalse(sheet.dead);
    }

    // --- Derived Stats ---

    [Test]
    public void MaxHealth_DefaultAttributes_ReturnsExpected()
    {
        var sheet = MakeSheet();
        // level=1, endurance=4 => 10*1 + 5*4 = 30
        Assert.AreEqual(30, sheet.MaxHealth());
    }

    [Test]
    public void MaxHealth_HighEndurance_ScalesCorrectly()
    {
        var sheet = MakeSheet();
        sheet.endurance = 10;
        // level=1, endurance=10 => 10 + 50 = 60
        Assert.AreEqual(60, sheet.MaxHealth());
    }

    [Test]
    public void MaxHealth_HigherLevel_ScalesCorrectly()
    {
        var sheet = MakeSheet();
        sheet.level = 5;
        // level=5, endurance=4 => 50 + 20 = 70
        Assert.AreEqual(70, sheet.MaxHealth());
    }

    [Test]
    public void MaxMana_DefaultAttributes_ReturnsExpected()
    {
        var sheet = MakeSheet();
        // level=1, willpower=4 => 10 + 20 = 30
        Assert.AreEqual(30, sheet.MaxMana());
    }

    [Test]
    public void MaxMana_HighWillpower_ScalesCorrectly()
    {
        var sheet = MakeSheet();
        sheet.willpower = 8;
        // level=1, willpower=8 => 10 + 40 = 50
        Assert.AreEqual(50, sheet.MaxMana());
    }

    [Test]
    public void GetVisionRange_DefaultPerception_ReturnsExpected()
    {
        var sheet = MakeSheet();
        // perception=4 => 4*2 = 8
        Assert.AreEqual(8, sheet.GetVisionRange());
    }

    [Test]
    public void GetVisionRange_HighPerception_ScalesCorrectly()
    {
        var sheet = MakeSheet();
        sheet.perception = 7;
        Assert.AreEqual(14, sheet.GetVisionRange());
    }

    // --- Damage & Health ---

    [Test]
    public void ReceiveDamage_ReducesHealth()
    {
        var sheet = MakeSheet();
        int before = sheet.currentHealth;

        sheet.ReceiveDamage(5);

        Assert.AreEqual(before - 5, sheet.currentHealth);
        Assert.IsFalse(sheet.dead);
    }

    [Test]
    public void ReceiveDamage_AtZero_SetsDead()
    {
        var sheet = MakeSheet();

        sheet.ReceiveDamage(sheet.currentHealth);

        Assert.AreEqual(0, sheet.currentHealth);
        Assert.IsTrue(sheet.dead);
    }

    [Test]
    public void ReceiveDamage_BeyondZero_ClampsToZero()
    {
        var sheet = MakeSheet();

        sheet.ReceiveDamage(sheet.currentHealth + 100);

        Assert.AreEqual(0, sheet.currentHealth);
        Assert.IsTrue(sheet.dead);
    }

    [Test]
    public void ReceiveDamage_FiresHealthChangedEvent()
    {
        var sheet = MakeSheet();
        bool fired = false;
        sheet.OnHealthChanged += () => fired = true;

        sheet.ReceiveDamage(1);

        Assert.IsTrue(fired);
    }

    [Test]
    public void ReceiveHealing_IncreasesHealth()
    {
        var sheet = MakeSheet();
        sheet.ReceiveDamage(10);
        int after = sheet.currentHealth;

        sheet.ReceiveHealing(5);

        Assert.AreEqual(after + 5, sheet.currentHealth);
    }

    [Test]
    public void ReceiveHealing_CapsAtMaxHealth()
    {
        var sheet = MakeSheet();
        sheet.ReceiveDamage(3);

        sheet.ReceiveHealing(100);

        Assert.AreEqual(sheet.MaxHealth(), sheet.currentHealth);
    }

    [Test]
    public void ReceiveHealing_FiresHealthChangedEvent()
    {
        var sheet = MakeSheet();
        sheet.ReceiveDamage(5);
        bool fired = false;
        sheet.OnHealthChanged += () => fired = true;

        sheet.ReceiveHealing(1);

        Assert.IsTrue(fired);
    }

    [Test]
    public void ReceivePureDamage_DelegatesToReceiveDamage()
    {
        var sheet = MakeSheet();
        int before = sheet.currentHealth;

        sheet.ReceivePureDamage(7);

        Assert.AreEqual(before - 7, sheet.currentHealth);
    }

    // --- Action Points ---

    [Test]
    public void SetActionPoints_UpdatesValue()
    {
        var sheet = MakeSheet();

        sheet.SetActionPoints(42);

        Assert.AreEqual(42, sheet.currentActionPoints);
    }

    [Test]
    public void SetActionPoints_FiresEvent()
    {
        var sheet = MakeSheet();
        bool fired = false;
        sheet.OnActionPointsChanged += () => fired = true;

        sheet.SetActionPoints(10);

        Assert.IsTrue(fired);
    }

    [Test]
    public void SetActionPoints_SameValue_DoesNotFireEvent()
    {
        var sheet = MakeSheet();
        sheet.SetActionPoints(10);
        bool fired = false;
        sheet.OnActionPointsChanged += () => fired = true;

        sheet.SetActionPoints(10);

        Assert.IsFalse(fired);
    }

    [Test]
    public void ModifyActionPoints_AddsToCurrentValue()
    {
        var sheet = MakeSheet();
        sheet.SetActionPoints(20);

        sheet.ModifyActionPoints(-5);

        Assert.AreEqual(15, sheet.currentActionPoints);
    }

    [Test]
    public void BeginTurn_SetsAPToMoveSpeed()
    {
        var sheet = MakeSheet();
        // speed=4 => MoveSpeed = 20 + 5*4 = 40

        sheet.BeginTurn();

        Assert.AreEqual(40, sheet.currentActionPoints);
    }

    [Test]
    public void BeginTurn_ReturnsTrue_WhenAlive()
    {
        var sheet = MakeSheet();

        Assert.IsTrue(sheet.BeginTurn());
    }

    // --- Status Effects ---

    [Test]
    public void RegisterStatusEffect_AddsToList()
    {
        var sheet = MakeSheet();

        new StatusEffect(StatusEffect.EffectType.REGENERATION, 3, sheet);

        Assert.IsTrue(sheet.HasStatusEffect(StatusEffect.EffectType.REGENERATION));
    }

    [Test]
    public void RemoveStatusEffect_RemovesFromList()
    {
        var sheet = MakeSheet();
        new StatusEffect(StatusEffect.EffectType.REGENERATION, 3, sheet);

        sheet.RemoveStatusEffect(StatusEffect.EffectType.REGENERATION);

        Assert.IsFalse(sheet.HasStatusEffect(StatusEffect.EffectType.REGENERATION));
    }

    [Test]
    public void StatusEffect_Regeneration_HealsOnTick()
    {
        var sheet = MakeSheet();
        sheet.ReceiveDamage(15);
        int hpBefore = sheet.currentHealth;
        new StatusEffect(StatusEffect.EffectType.REGENERATION, 3, sheet);

        sheet.BeginTurn();

        Assert.AreEqual(hpBefore + 5, sheet.currentHealth);
    }

    [Test]
    public void StatusEffect_Poisoned_DamagesOnTick()
    {
        var sheet = MakeSheet();
        int hpBefore = sheet.currentHealth;
        new StatusEffect(StatusEffect.EffectType.POISONED, 2, sheet);

        sheet.BeginTurn();

        Assert.AreEqual(hpBefore - 5, sheet.currentHealth);
    }

    [Test]
    public void StatusEffect_Knockdown_ZeroesAP()
    {
        var sheet = MakeSheet();
        new StatusEffect(StatusEffect.EffectType.KNOCKDOWN, 1, sheet);

        sheet.BeginTurn();

        Assert.AreEqual(0, sheet.currentActionPoints);
    }

    [Test]
    public void StatusEffect_Frozen_ZeroesAP()
    {
        var sheet = MakeSheet();
        new StatusEffect(StatusEffect.EffectType.FROZEN, 1, sheet);

        sheet.BeginTurn();

        Assert.AreEqual(0, sheet.currentActionPoints);
    }

    [Test]
    public void StatusEffect_Slowed_ReducesAP()
    {
        var sheet = MakeSheet();
        new StatusEffect(StatusEffect.EffectType.SLOWED, 1, sheet);

        sheet.BeginTurn();

        // MoveSpeed=40, SLOWED subtracts 4 => 36
        Assert.AreEqual(36, sheet.currentActionPoints);
    }

    [Test]
    public void StatusEffect_Mobility_IncreasesAP()
    {
        var sheet = MakeSheet();
        new StatusEffect(StatusEffect.EffectType.MOBILITY, 1, sheet);

        sheet.BeginTurn();

        // MoveSpeed=40, MOBILITY adds 2 => 42
        Assert.AreEqual(42, sheet.currentActionPoints);
    }

    [Test]
    public void StatusEffect_Expires_AfterDuration()
    {
        var sheet = MakeSheet();
        new StatusEffect(StatusEffect.EffectType.REGENERATION, 1, sheet);

        sheet.BeginTurn(); // duration=1, ticks once, then expires

        Assert.IsFalse(sheet.HasStatusEffect(StatusEffect.EffectType.REGENERATION));
    }

    [Test]
    public void StatusEffect_PersistsUntilDurationEnds()
    {
        var sheet = MakeSheet();
        sheet.ReceiveDamage(20); // make room for healing
        new StatusEffect(StatusEffect.EffectType.REGENERATION, 3, sheet);

        sheet.BeginTurn(); // tick 1 of 3

        Assert.IsTrue(sheet.HasStatusEffect(StatusEffect.EffectType.REGENERATION));
    }

    [Test]
    public void StatusEffect_Burning_DealsTenDamage()
    {
        var sheet = MakeSheet();
        int hpBefore = sheet.currentHealth;
        new StatusEffect(StatusEffect.EffectType.BURNING, 1, sheet);

        sheet.BeginTurn();

        Assert.AreEqual(hpBefore - 10, sheet.currentHealth);
    }

    [Test]
    public void StatusEffect_Poison_CanKill()
    {
        var sheet = MakeSheet();
        sheet.ReceiveDamage(sheet.currentHealth - 1); // 1 HP left
        new StatusEffect(StatusEffect.EffectType.POISONED, 3, sheet);

        bool alive = sheet.BeginTurn();

        Assert.IsFalse(alive);
        Assert.IsTrue(sheet.dead);
    }

    // --- Equipment ---

    [Test]
    public void TryEquipItem_Weapon_EquipsToRightHand()
    {
        var sheet = MakeSheet();
        var sword = new EquippableHandheld("Sword", EquippableHandheld.WeaponType.OneHanded, 5, 10,
            DamageType.Slashing);

        Assert.IsTrue(sheet.TryEquipItem(sword));

        var equipped = sheet.GetEquippedItem(EquippableItem.EquipmentSlot.RightHand);
        Assert.IsNotNull(equipped);
        Assert.AreEqual("Sword", equipped.itemName);
    }

    [Test]
    public void TryEquipItem_Armor_EquipsToArmorSlot()
    {
        var sheet = MakeSheet();
        var armor = new EquippableItem("Plate", EquippableItem.EquipmentSlot.Armor) { armorBonus = 5 };

        Assert.IsTrue(sheet.TryEquipItem(armor));

        var equipped = sheet.GetEquippedItem(EquippableItem.EquipmentSlot.Armor);
        Assert.IsNotNull(equipped);
        Assert.AreEqual("Plate", equipped.itemName);
    }

    [Test]
    public void TryEquipItemToSlot_LeftHand_Works()
    {
        var sheet = MakeSheet();
        var dagger = new EquippableHandheld("Dagger", EquippableHandheld.WeaponType.OneHanded, 2, 4,
            DamageType.Piercing);

        Assert.IsTrue(sheet.TryEquipItemToSlot(dagger, EquippableItem.EquipmentSlot.LeftHand));

        var equipped = sheet.GetEquippedItem(EquippableItem.EquipmentSlot.LeftHand);
        Assert.IsNotNull(equipped);
        Assert.AreEqual("Dagger", equipped.itemName);
    }

    [Test]
    public void UnequipItem_ReturnsItem()
    {
        var sheet = MakeSheet();
        var sword = new EquippableHandheld("Sword", EquippableHandheld.WeaponType.OneHanded, 5, 10,
            DamageType.Slashing);
        sheet.TryEquipItem(sword);

        var removed = sheet.UnequipItem(EquippableItem.EquipmentSlot.RightHand);

        Assert.IsNotNull(removed);
        Assert.AreEqual("Sword", removed.itemName);
        Assert.IsNull(sheet.GetEquippedItem(EquippableItem.EquipmentSlot.RightHand));
    }

    [Test]
    public void TryEquipItem_FiresEquipmentChangedEvent()
    {
        var sheet = MakeSheet();
        bool fired = false;
        sheet.OnEquipmentChanged += () => fired = true;
        var sword = new EquippableHandheld("Sword", EquippableHandheld.WeaponType.OneHanded, 5, 10,
            DamageType.Slashing);

        sheet.TryEquipItem(sword);

        Assert.IsTrue(fired);
    }

    // --- Special Actions ---

    [Test]
    public void LearnSpecialAction_AddsToKnownTypes()
    {
        var sheet = MakeSheet();

        sheet.LearnSpecialAction<ActionBulwark>();

        var known = new System.Collections.Generic.List<System.Type>(sheet.GetKnownSpecialActionTypes());
        Assert.IsTrue(known.Contains(typeof(ActionBulwark)));
    }

    [Test]
    public void LearnSpecialAction_NoDuplicates()
    {
        var sheet = MakeSheet();
        sheet.LearnSpecialAction<ActionBulwark>();
        sheet.LearnSpecialAction<ActionBulwark>();

        var known = new System.Collections.Generic.List<System.Type>(sheet.GetKnownSpecialActionTypes());
        int count = 0;
        foreach (var t in known) { if (t == typeof(ActionBulwark)) count++; }
        Assert.AreEqual(1, count);
    }

    // --- Data-Driven Abilities ---

    [Test]
    public void LearnAbility_AddsToKnownAbilities()
    {
        var sheet = MakeSheet();
        int before = sheet.GetKnownAbilities().Count;

        sheet.LearnAbility(AbilityCatalog.Fireball);

        Assert.AreEqual(before + 1, sheet.GetKnownAbilities().Count);
    }

    [Test]
    public void LearnAbility_NoDuplicates()
    {
        var sheet = MakeSheet();
        int before = sheet.GetKnownAbilities().Count;

        sheet.LearnAbility(AbilityCatalog.Fireball);
        sheet.LearnAbility(AbilityCatalog.Fireball);

        Assert.AreEqual(before + 1, sheet.GetKnownAbilities().Count);
    }

    [Test]
    public void LearnAbility_ById_ResolvesFromRegistry()
    {
        var sheet = MakeSheet();
        int before = sheet.GetKnownAbilities().Count;

        sheet.LearnAbility("stealth");

        Assert.AreEqual(before + 1, sheet.GetKnownAbilities().Count);
        Assert.AreEqual("Stealth", sheet.GetKnownAbilities()[before].displayName);
    }

    [Test]
    public void LearnAbility_InvalidId_DoesNotThrow()
    {
        var sheet = MakeSheet();
        int before = sheet.GetKnownAbilities().Count;

        sheet.LearnAbility("nonexistent_ability");

        Assert.AreEqual(before, sheet.GetKnownAbilities().Count);
    }

}
