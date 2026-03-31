using System.Collections.Generic;
using NUnit.Framework;

[TestFixture]
public class ContentRegistryTests
{
    [Test]
    public void GetItemData_ValidId_ReturnsData()
    {
        var data = ContentRegistry.GetItemData("cutlass");
        Assert.IsNotNull(data);
        Assert.AreEqual("Cutlass", data.displayName);
    }

    [Test]
    public void GetItemData_InvalidId_ReturnsNull()
    {
        Assert.IsNull(ContentRegistry.GetItemData("nonexistent_item"));
    }

    [Test]
    public void CreateItem_BasicItem_ReturnsInventoryItem()
    {
        var item = ContentRegistry.CreateItem("health_potion");
        Assert.IsNotNull(item);
        Assert.AreEqual("Health Potion", item.itemName);
        Assert.AreEqual(10, item.MaxStack);
    }

    [Test]
    public void CreateItem_Weapon_ReturnsEquippableHandheld()
    {
        var item = ContentRegistry.CreateItem("cutlass");
        Assert.IsInstanceOf<EquippableHandheld>(item);

        var weapon = (EquippableHandheld)item;
        Assert.AreEqual(5, weapon.damage);
        Assert.AreEqual(EquippableHandheld.WeaponType.OneHanded, weapon.weaponType);
        Assert.AreEqual(DamageType.Slashing, weapon.damageType);
    }

    [Test]
    public void CreateItem_Armor_ReturnsEquippableItem()
    {
        var item = ContentRegistry.CreateItem("leather_armor");
        Assert.IsInstanceOf<EquippableItem>(item);

        var armor = (EquippableItem)item;
        Assert.AreEqual(EquippableItem.EquipmentSlot.Armor, armor.slot);
        Assert.AreEqual(2, armor.armorBonus);
    }

    [Test]
    public void CreateEquippable_Weapon_ReturnsCorrectType()
    {
        var equip = ContentRegistry.CreateEquippable("steel_shield");
        Assert.IsNotNull(equip);
        Assert.IsInstanceOf<EquippableHandheld>(equip);
        Assert.AreEqual(2, equip.armorBonus);
    }

    [Test]
    public void CreateEquippable_NonEquippable_ReturnsNull()
    {
        var equip = ContentRegistry.CreateEquippable("health_potion");
        Assert.IsNull(equip);
    }

    [Test]
    public void CreateItem_MusketProperties_Correct()
    {
        var item = ContentRegistry.CreateItem("long_musket") as EquippableHandheld;
        Assert.IsNotNull(item);
        Assert.AreEqual(12, item.damage);
        Assert.AreEqual(2, item.minRange);
        Assert.AreEqual(10, item.maxRange);
        Assert.IsTrue(item.requiresAmmo);
        Assert.AreEqual("musket_ball", item.ammoType);
        Assert.AreEqual(EquippableHandheld.RangeType.Ranged, item.rangeType);
    }

    [Test]
    public void CreateItem_Grenade_HasSplashAndConsumable()
    {
        var item = ContentRegistry.CreateItem("frag_grenade") as EquippableHandheld;
        Assert.IsNotNull(item);
        Assert.AreEqual(2, item.splashRadius);
        Assert.IsTrue(item.isConsumable);
        Assert.AreEqual(nameof(ActionGroundAttack), item.associatedActionClass);
    }

    [Test]
    public void CreateItem_Torch_HasIllumination()
    {
        var item = ContentRegistry.CreateItem("torch") as EquippableHandheld;
        Assert.IsNotNull(item);
        Assert.IsTrue(item.providesIllumination);
        Assert.AreEqual(12, item.illuminationRange);
    }

    [Test]
    public void AllItems_ContainsAllCatalogEntries()
    {
        var catalogIds = new HashSet<string>();
        foreach (var catalogItem in ItemCatalog.All)
        {
            catalogIds.Add(catalogItem.id);
            Assert.AreSame(
                catalogItem,
                ContentRegistry.GetItemData(catalogItem.id),
                $"Catalog item {catalogItem.id} must be registered.");
        }
        int fromCatalog = 0;
        foreach (var item in ContentRegistry.AllItems())
        {
            if (catalogIds.Contains(item.id))
                fromCatalog++;
        }
        Assert.AreEqual(ItemCatalog.All.Length, fromCatalog);
    }

    [Test]
    public void AllOfType_WeaponData_ReturnsOnlyWeapons()
    {
        foreach (var w in ContentRegistry.AllOfType<WeaponData>())
        {
            Assert.IsNotNull(w);
            Assert.IsInstanceOf<WeaponData>(w);
        }
    }

    [Test]
    public void CreateItem_ReturnsIndependentInstances()
    {
        var a = ContentRegistry.CreateItem("cutlass");
        var b = ContentRegistry.CreateItem("cutlass");
        Assert.AreNotSame(a, b);
    }

    [Test]
    public void Register_CustomItem_IsRetrievable()
    {
        var custom = new ItemData
        {
            id = "test_custom_item",
            displayName = "Test Custom",
            description = "A test item",
        };
        ContentRegistry.Register(custom);

        var retrieved = ContentRegistry.GetItemData("test_custom_item");
        Assert.IsNotNull(retrieved);
        Assert.AreEqual("Test Custom", retrieved.displayName);
    }
}
