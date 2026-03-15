using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
public class SaveDataTests
{
    private CharacterSheet MakeSheet(
        string name = "Test",
        CharacterSheet.CharacterClass cls = CharacterSheet.CharacterClass.CLASS_SOLDIER)
    {
        return new CharacterSheet(name, cls, assignDefaults: false);
    }

    [Test]
    public void CharacterSaveData_RoundTrip_PreservesIdentity()
    {
        var original = MakeSheet("Hero", CharacterSheet.CharacterClass.CLASS_ROGUE);
        original.level = 5;
        original.xp = 1200;

        var save = CharacterSaveData.FromSheet(original);
        var restored = save.ToSheet();

        Assert.AreEqual("Hero", restored.firstName);
        Assert.AreEqual(CharacterSheet.CharacterClass.CLASS_ROGUE, restored.characterClass);
        Assert.AreEqual(5, restored.level);
        Assert.AreEqual(1200, restored.xp);
    }

    [Test]
    public void CharacterSaveData_RoundTrip_PreservesStats()
    {
        var original = MakeSheet();
        original.strength = 8;
        original.agility = 6;
        original.speed = 7;
        original.intellect = 5;
        original.endurance = 9;
        original.perception = 3;
        original.willpower = 10;

        var save = CharacterSaveData.FromSheet(original);
        var restored = save.ToSheet();

        Assert.AreEqual(8, restored.strength);
        Assert.AreEqual(6, restored.agility);
        Assert.AreEqual(7, restored.speed);
        Assert.AreEqual(5, restored.intellect);
        Assert.AreEqual(9, restored.endurance);
        Assert.AreEqual(3, restored.perception);
        Assert.AreEqual(10, restored.willpower);
    }

    [Test]
    public void CharacterSaveData_RoundTrip_PreservesHealthMana()
    {
        var original = MakeSheet();
        original.ReceiveDamage(15);

        var save = CharacterSaveData.FromSheet(original);
        var restored = save.ToSheet();

        Assert.AreEqual(original.currentHealth, restored.currentHealth);
        Assert.AreEqual(original.currentMana, restored.currentMana);
    }

    [Test]
    public void CharacterSaveData_RoundTrip_PreservesDeadState()
    {
        var original = MakeSheet();
        original.ReceiveDamage(99999);

        var save = CharacterSaveData.FromSheet(original);
        var restored = save.ToSheet();

        Assert.IsTrue(restored.dead);
        Assert.AreEqual(0, restored.currentHealth);
    }

    [Test]
    public void CharacterSaveData_RoundTrip_PreservesInventory()
    {
        var original = MakeSheet();
        original.inventory.TryAddItem(ContentRegistry.CreateItem("health_potion"));
        original.inventory.TryAddItem(ContentRegistry.CreateItem("cutlass"));

        var save = CharacterSaveData.FromSheet(original);
        var restored = save.ToSheet();

        var items = restored.inventory.Items;
        Assert.AreEqual("Health Potion", items[0].itemName);
        Assert.IsInstanceOf<EquippableHandheld>(items[1]);
    }

    [Test]
    public void CharacterSaveData_RoundTrip_PreservesEquipment()
    {
        var original = MakeSheet();
        original.TryEquipItem(ContentRegistry.CreateEquippable("cutlass"));
        original.TryEquipItem(ContentRegistry.CreateEquippable("leather_armor"));

        var save = CharacterSaveData.FromSheet(original);
        var restored = save.ToSheet();

        var weapon = restored.GetEquippedItem(EquippableItem.EquipmentSlot.RightHand);
        Assert.IsNotNull(weapon);
        Assert.AreEqual("Cutlass", weapon.itemName);

        var armor = restored.GetEquippedItem(EquippableItem.EquipmentSlot.Armor);
        Assert.IsNotNull(armor);
        Assert.AreEqual("Leather Armor", armor.itemName);
    }

    [Test]
    public void CharacterSaveData_RoundTrip_PreservesAbilities()
    {
        var original = MakeSheet();
        original.LearnAbility("fireball");
        original.LearnAbility("stealth");

        var save = CharacterSaveData.FromSheet(original);
        var restored = save.ToSheet();

        var abilities = restored.GetKnownAbilities();
        Assert.AreEqual(2, abilities.Count);
        Assert.AreEqual("fireball", abilities[0].id);
        Assert.AreEqual("stealth", abilities[1].id);
    }

    [Test]
    public void CharacterSaveData_RoundTrip_PreservesStackCounts()
    {
        var original = MakeSheet();
        var potion = ContentRegistry.CreateItem("health_potion");
        potion.currentStack = 7;
        original.inventory.TryAddItem(potion);

        var save = CharacterSaveData.FromSheet(original);
        var restored = save.ToSheet();

        Assert.AreEqual(7, restored.inventory.Items[0].currentStack);
    }

    [Test]
    public void CharacterSaveData_EmptySheet_RoundTrips()
    {
        var original = MakeSheet();
        var save = CharacterSaveData.FromSheet(original);
        var restored = save.ToSheet();

        Assert.AreEqual(original.firstName, restored.firstName);
        Assert.AreEqual(original.characterClass, restored.characterClass);
        Assert.AreEqual(original.currentHealth, restored.currentHealth);
    }

    [Test]
    public void GameSaveData_RoundTrip_PreservesParty()
    {
        PlayerParty.partyMembers = new List<CharacterSheet>
        {
            new CharacterSheet("Alice", CharacterSheet.CharacterClass.CLASS_ROGUE, false),
            new CharacterSheet("Bob", CharacterSheet.CharacterClass.CLASS_FIREMAGE, false),
        };

        var save = GameSaveData.FromCurrentState();
        save.RestoreState();

        Assert.AreEqual(2, PlayerParty.partyMembers.Count);
        Assert.AreEqual("Alice", PlayerParty.partyMembers[0].firstName);
        Assert.AreEqual("Bob", PlayerParty.partyMembers[1].firstName);
        Assert.AreEqual(CharacterSheet.CharacterClass.CLASS_ROGUE,
            PlayerParty.partyMembers[0].characterClass);
        Assert.AreEqual(CharacterSheet.CharacterClass.CLASS_FIREMAGE,
            PlayerParty.partyMembers[1].characterClass);
    }

    [Test]
    public void ItemSaveData_EmptySlot_RoundTrips()
    {
        var data = ItemSaveData.FromItem(null);
        Assert.IsTrue(data.isEmpty);
        Assert.IsNull(data.ToItem());
    }

    [Test]
    public void ItemSaveData_RegistryItem_RoundTrips()
    {
        var original = ContentRegistry.CreateItem("cutlass");
        var data = ItemSaveData.FromItem(original);
        Assert.AreEqual("cutlass", data.registryId);

        var restored = data.ToItem();
        Assert.IsNotNull(restored);
        Assert.AreEqual("Cutlass", restored.itemName);
        Assert.IsInstanceOf<EquippableHandheld>(restored);
    }
}
