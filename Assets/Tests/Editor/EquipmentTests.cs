using NUnit.Framework;

public class EquipmentTests
{
    private EquippableHandheld MakeSword(string name = "Sword")
    {
        return new EquippableHandheld(name, EquippableHandheld.WeaponType.OneHanded, 5, 10,
            EquippableHandheld.DamageType.Slashing);
    }

    private EquippableHandheld MakeShield(string name = "Shield")
    {
        return new EquippableHandheld(name, EquippableHandheld.WeaponType.Shield, 1, 8,
            EquippableHandheld.DamageType.Bludgeoning);
    }

    private EquippableHandheld MakeTwoHander(string name = "Pike")
    {
        return new EquippableHandheld(name, EquippableHandheld.WeaponType.TwoHanded, 10, 15,
            EquippableHandheld.DamageType.Piercing);
    }

    // --- Basic equip/unequip ---

    [Test]
    public void TryEquip_PlacesItemInSlot()
    {
        var equipment = new Equipment();
        var sword = MakeSword();

        Assert.IsTrue(equipment.TryEquip(sword));
        Assert.AreEqual(sword, equipment.Get(EquippableItem.EquipmentSlot.RightHand));
    }

    [Test]
    public void Unequip_ReturnsItemAndClearsSlot()
    {
        var equipment = new Equipment();
        var sword = MakeSword();
        equipment.TryEquip(sword);

        var removed = equipment.Unequip(EquippableItem.EquipmentSlot.RightHand);

        Assert.AreEqual(sword, removed);
        Assert.IsNull(equipment.Get(EquippableItem.EquipmentSlot.RightHand));
    }

    [Test]
    public void Unequip_EmptySlot_ReturnsNull()
    {
        var equipment = new Equipment();

        Assert.IsNull(equipment.Unequip(EquippableItem.EquipmentSlot.LeftHand));
    }

    [Test]
    public void TryEquip_FiresEvent()
    {
        var equipment = new Equipment();
        bool fired = false;
        equipment.OnEquipmentChanged += () => fired = true;

        equipment.TryEquip(MakeSword());

        Assert.IsTrue(fired);
    }

    [Test]
    public void Unequip_FiresEvent()
    {
        var equipment = new Equipment();
        equipment.TryEquip(MakeSword());
        bool fired = false;
        equipment.OnEquipmentChanged += () => fired = true;

        equipment.Unequip(EquippableItem.EquipmentSlot.RightHand);

        Assert.IsTrue(fired);
    }

    // --- Dual-wield compatibility ---

    [Test]
    public void DualWield_TwoOneHanded_Allowed()
    {
        var sword = MakeSword("Sword A");
        var dagger = MakeSword("Dagger");

        Assert.IsTrue(sword.CanDualWieldWith(dagger));
    }

    [Test]
    public void DualWield_OneHandedPlusShield_Allowed()
    {
        var sword = MakeSword();
        var shield = MakeShield();

        Assert.IsTrue(sword.CanDualWieldWith(shield));
        Assert.IsTrue(shield.CanDualWieldWith(sword));
    }

    [Test]
    public void DualWield_TwoHanded_BlocksOtherHand()
    {
        var pike = MakeTwoHander();
        var sword = MakeSword();

        Assert.IsFalse(pike.CanDualWieldWith(sword));
        Assert.IsFalse(sword.CanDualWieldWith(pike));
    }

    [Test]
    public void DualWield_WithNull_AlwaysAllowed()
    {
        var sword = MakeSword();
        var pike = MakeTwoHander();

        Assert.IsTrue(sword.CanDualWieldWith(null));
        Assert.IsTrue(pike.CanDualWieldWith(null));
    }

    [Test]
    public void DualWield_TwoShields_NotAllowed()
    {
        var shield1 = MakeShield("Shield A");
        var shield2 = MakeShield("Shield B");

        Assert.IsFalse(shield1.CanDualWieldWith(shield2));
    }

    // --- Slot compatibility ---

    [Test]
    public void IsSlotCompatible_HandheldInRightHand_True()
    {
        var equipment = new Equipment();
        var sword = MakeSword();

        Assert.IsTrue(equipment.IsSlotCompatible(EquippableItem.EquipmentSlot.RightHand, sword));
    }

    [Test]
    public void IsSlotCompatible_HandheldInLeftHand_True()
    {
        var equipment = new Equipment();
        var sword = MakeSword();

        Assert.IsTrue(equipment.IsSlotCompatible(EquippableItem.EquipmentSlot.LeftHand, sword));
    }

    [Test]
    public void IsSlotCompatible_ArmorInHandSlot_False()
    {
        var equipment = new Equipment();
        var armor = new EquippableItem("Plate", EquippableItem.EquipmentSlot.Armor);

        Assert.IsFalse(equipment.IsSlotCompatible(EquippableItem.EquipmentSlot.RightHand, armor));
    }

    [Test]
    public void IsSlotCompatible_ArmorInArmorSlot_True()
    {
        var equipment = new Equipment();
        var armor = new EquippableItem("Plate", EquippableItem.EquipmentSlot.Armor);

        Assert.IsTrue(equipment.IsSlotCompatible(EquippableItem.EquipmentSlot.Armor, armor));
    }

    [Test]
    public void IsSlotCompatible_NonEquippable_False()
    {
        var equipment = new Equipment();
        var potion = new InventoryItem("Potion");

        Assert.IsFalse(equipment.IsSlotCompatible(EquippableItem.EquipmentSlot.RightHand, potion));
    }

    // --- TryEquipToSlot with dual-wield checks ---

    [Test]
    public void TryEquipToSlot_OneHandedInLeftWithOneHandedInRight_Succeeds()
    {
        var equipment = new Equipment();
        var sword = MakeSword("Sword");
        var dagger = MakeSword("Dagger");
        equipment.TryEquipToSlot(sword, EquippableItem.EquipmentSlot.RightHand, out _);

        bool result = equipment.TryEquipToSlot(dagger, EquippableItem.EquipmentSlot.LeftHand, out _);

        Assert.IsTrue(result);
        Assert.AreEqual(dagger, equipment.Get(EquippableItem.EquipmentSlot.LeftHand));
        Assert.AreEqual(sword, equipment.Get(EquippableItem.EquipmentSlot.RightHand));
    }

    [Test]
    public void TryEquipToSlot_TwoHandedBlocksOtherHand()
    {
        var equipment = new Equipment();
        var pike = MakeTwoHander();
        equipment.TryEquipToSlot(pike, EquippableItem.EquipmentSlot.RightHand, out _);

        var dagger = MakeSword("Dagger");
        bool result = equipment.TryEquipToSlot(dagger, EquippableItem.EquipmentSlot.LeftHand, out _);

        Assert.IsFalse(result);
    }

    [Test]
    public void TryEquipToSlot_ReplacesExistingItem()
    {
        var equipment = new Equipment();
        var sword1 = MakeSword("Old Sword");
        var sword2 = MakeSword("New Sword");
        equipment.TryEquipToSlot(sword1, EquippableItem.EquipmentSlot.RightHand, out _);

        equipment.TryEquipToSlot(sword2, EquippableItem.EquipmentSlot.RightHand, out var previous);

        Assert.AreEqual(sword1, previous);
        Assert.AreEqual(sword2, equipment.Get(EquippableItem.EquipmentSlot.RightHand));
    }

    // --- GetAll ---

    [Test]
    public void GetAll_ReturnsAllEquipped()
    {
        var equipment = new Equipment();
        var sword = MakeSword();
        var armor = new EquippableItem("Plate", EquippableItem.EquipmentSlot.Armor);
        equipment.TryEquip(sword);
        equipment.TryEquip(armor);

        var all = equipment.GetAll();

        Assert.AreEqual(2, all.Count);
    }
}
