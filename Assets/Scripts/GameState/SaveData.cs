using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveData
{
    public List<CharacterSaveData> party = new List<CharacterSaveData>();

    public static GameSaveData FromCurrentState()
    {
        var save = new GameSaveData();
        if (PlayerParty.partyMembers != null)
        {
            foreach (var sheet in PlayerParty.partyMembers)
            {
                save.party.Add(CharacterSaveData.FromSheet(sheet));
            }
        }
        return save;
    }

    public void RestoreState()
    {
        PlayerParty.partyMembers = new List<CharacterSheet>();
        foreach (var cd in party)
        {
            PlayerParty.partyMembers.Add(cd.ToSheet());
        }
    }
}

[Serializable]
public class CharacterSaveData
{
    public string firstName;
    public CharacterSheet.CharacterClass characterClass;
    public int level;
    public int xp;

    public int strength;
    public int agility;
    public int speed;
    public int intellect;
    public int endurance;
    public int perception;
    public int willpower;

    public int currentHealth;
    public int currentMana;
    public bool dead;

    public List<ItemSaveData> inventorySlots = new List<ItemSaveData>();
    public List<EquipmentSlotSaveData> equippedItems = new List<EquipmentSlotSaveData>();
    public List<string> abilityIds = new List<string>();

    public static CharacterSaveData FromSheet(CharacterSheet sheet)
    {
        var data = new CharacterSaveData
        {
            firstName = sheet.firstName,
            characterClass = sheet.characterClass,
            level = sheet.level,
            xp = sheet.xp,
            strength = sheet.strength,
            agility = sheet.agility,
            speed = sheet.speed,
            intellect = sheet.intellect,
            endurance = sheet.endurance,
            perception = sheet.perception,
            willpower = sheet.willpower,
            currentHealth = sheet.currentHealth,
            currentMana = sheet.currentMana,
            dead = sheet.dead,
        };

        foreach (var item in sheet.inventory.Items)
        {
            data.inventorySlots.Add(ItemSaveData.FromItem(item));
        }

        foreach (var kvp in sheet.GetEquippedItems())
        {
            data.equippedItems.Add(new EquipmentSlotSaveData
            {
                slot = kvp.Key,
                item = ItemSaveData.FromItem(kvp.Value),
            });
        }

        foreach (var ability in sheet.GetKnownAbilities())
        {
            data.abilityIds.Add(ability.id);
        }

        return data;
    }

    public CharacterSheet ToSheet()
    {
        var sheet = new CharacterSheet(firstName, characterClass, assignDefaults: false);

        sheet.level = level;
        sheet.xp = xp;
        sheet.strength = strength;
        sheet.agility = agility;
        sheet.speed = speed;
        sheet.intellect = intellect;
        sheet.endurance = endurance;
        sheet.perception = perception;
        sheet.willpower = willpower;
        sheet.currentHealth = currentHealth;
        sheet.currentMana = currentMana;
        sheet.dead = dead;

        RestoreInventory(sheet);
        RestoreEquipment(sheet);
        RestoreAbilities(sheet);

        return sheet;
    }

    private void RestoreInventory(CharacterSheet sheet)
    {
        for (int i = 0; i < inventorySlots.Count && i < Inventory.MaxSlots; i++)
        {
            var slotData = inventorySlots[i];
            if (slotData == null || slotData.isEmpty)
            {
                sheet.inventory.SetItemAt(i, null);
                continue;
            }
            var item = slotData.ToItem();
            sheet.inventory.SetItemAt(i, item);
        }
    }

    private void RestoreEquipment(CharacterSheet sheet)
    {
        foreach (var esd in equippedItems)
        {
            var item = esd.item?.ToItem() as EquippableItem;
            if (item != null)
            {
                sheet.TryEquipItemToSlot(item, esd.slot);
            }
        }
    }

    private void RestoreAbilities(CharacterSheet sheet)
    {
        foreach (var id in abilityIds)
        {
            sheet.LearnAbility(id);
        }
    }
}

[Serializable]
public class ItemSaveData
{
    public bool isEmpty = true;
    public string registryId;
    public int currentStack = 1;

    public string itemName;
    public string description;
    public int stackSize;
    public int weight;

    public bool isWeapon;
    public int damage;
    public int minRange;
    public int maxRange;
    public int actionPointCost;
    public int armorBonus;
    public int dodgeBonus;
    public string equipSlot;

    public static ItemSaveData FromItem(InventoryItem item)
    {
        if (item == null) return new ItemSaveData { isEmpty = true };

        var data = new ItemSaveData
        {
            isEmpty = false,
            currentStack = item.currentStack,
            itemName = item.itemName,
            description = item.description,
            stackSize = item.stackSize,
            weight = item.weight,
        };

        var regId = FindRegistryId(item);
        if (regId != null)
        {
            data.registryId = regId;
            return data;
        }

        if (item is EquippableHandheld wep)
        {
            data.isWeapon = true;
            data.damage = wep.damage;
            data.minRange = wep.minRange;
            data.maxRange = wep.maxRange;
            data.actionPointCost = wep.actionPointCost;
            data.armorBonus = wep.armorBonus;
            data.dodgeBonus = wep.dodgeBonus;
            data.equipSlot = wep.slot.ToString();
        }
        else if (item is EquippableItem eq)
        {
            data.armorBonus = eq.armorBonus;
            data.dodgeBonus = eq.dodgeBonus;
            data.equipSlot = eq.slot.ToString();
        }

        return data;
    }

    public InventoryItem ToItem()
    {
        if (isEmpty) return null;

        if (!string.IsNullOrEmpty(registryId))
        {
            var item = ContentRegistry.CreateItem(registryId);
            if (item != null)
            {
                item.currentStack = currentStack;
                return item;
            }
        }

        if (isWeapon)
        {
            var wep = new EquippableHandheld(itemName,
                EquippableHandheld.WeaponType.OneHanded, damage, minRange, maxRange,
                actionPointCost, EquippableHandheld.DamageType.Slashing)
            {
                description = description,
                stackSize = stackSize,
                weight = weight,
                currentStack = currentStack,
                armorBonus = armorBonus,
                dodgeBonus = dodgeBonus,
            };
            return wep;
        }

        if (!string.IsNullOrEmpty(equipSlot) &&
            System.Enum.TryParse<EquippableItem.EquipmentSlot>(equipSlot, out var slot))
        {
            return new EquippableItem(itemName, slot)
            {
                description = description,
                stackSize = stackSize,
                weight = weight,
                currentStack = currentStack,
                armorBonus = armorBonus,
                dodgeBonus = dodgeBonus,
            };
        }

        return new InventoryItem(itemName)
        {
            description = description,
            stackSize = stackSize,
            weight = weight,
            currentStack = currentStack,
        };
    }

    private static string FindRegistryId(InventoryItem item)
    {
        foreach (var data in ContentRegistry.AllItems())
        {
            if (data.displayName == item.itemName)
                return data.id;
        }
        return null;
    }
}

[Serializable]
public class EquipmentSlotSaveData
{
    public EquippableItem.EquipmentSlot slot;
    public ItemSaveData item;
}
