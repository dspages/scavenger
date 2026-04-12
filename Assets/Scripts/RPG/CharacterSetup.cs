using UnityEngine;

public static class CharacterSetup
{
    public const int PlayerStartingStatCount = 7;

    /// <summary>
    /// Player roster characters get +1 and −1 on two independent random base stats (0–6),
    /// so the same pick twice leaves stats unchanged. Call after racial modifiers.
    /// </summary>
    public static void ApplyStartingPlayerStatVariance(CharacterSheet sheet)
    {
        if (sheet == null) return;
        int addIdx = Globals.rng.Next(PlayerStartingStatCount);
        int subIdx = Globals.rng.Next(PlayerStartingStatCount);
        AddToStatByIndex(sheet, addIdx, 1);
        AddToStatByIndex(sheet, subIdx, -1);
        sheet.FillResourcePoolsToMax();
    }

    static void AddToStatByIndex(CharacterSheet sheet, int index, int delta)
    {
        switch (index)
        {
            case 0: sheet.strength += delta; break;
            case 1: sheet.agility += delta; break;
            case 2: sheet.speed += delta; break;
            case 3: sheet.intellect += delta; break;
            case 4: sheet.endurance += delta; break;
            case 5: sheet.perception += delta; break;
            case 6: sheet.willpower += delta; break;
            default: break;
        }
    }

    public static void AssignStartingAbilities(CharacterSheet sheet)
    {
        if (sheet == null) return;
        switch (sheet.characterClass)
        {
            case CharacterSheet.CharacterClass.CLASS_FIREMAGE:
                sheet.LearnAbility("fireball");
                break;
            case CharacterSheet.CharacterClass.CLASS_ROGUE:
                sheet.LearnAbility("stealth");
                break;
            default:
                sheet.LearnAbility("bulwark");
                break;
        }
    }

    public static void AssignStartingGear(CharacterSheet sheet)
    {
        if (sheet == null) return;

        var inv = sheet.inventory;

        inv.TryAddItem(ContentRegistry.CreateItem("health_potion"));
        TryAddManaCrystalStack(inv, 5);
        inv.TryAddItem(ContentRegistry.CreateItem("bread"));
        inv.TryAddItem(ContentRegistry.CreateItem("gold_coin"));
        inv.TryAddItem(ContentRegistry.CreateItem("musket_ball"));

        inv.TryAddItem(ContentRegistry.CreateItem("steel_shield"));
        inv.TryAddItem(ContentRegistry.CreateItem("reach_pike"));
        inv.TryAddItem(ContentRegistry.CreateItem("long_musket"));
        inv.TryAddItem(ContentRegistry.CreateItem("iron_dagger"));
        inv.TryAddItem(ContentRegistry.CreateItem("frag_grenade"));
        inv.TryAddItem(ContentRegistry.CreateItem("torch"));

        sheet.TryEquipItem(ContentRegistry.CreateEquippable("cutlass"));
        sheet.TryEquipItem(ContentRegistry.CreateEquippable("leather_armor"));

        if (sheet.avatar != null)
        {
            CombatController cc = sheet.avatar.GetComponent<CombatController>();
            if (cc != null)
            {
                VisionSystem vs = GameObject.FindFirstObjectByType<VisionSystem>(FindObjectsInactive.Exclude);
                if (vs != null) vs.UpdateVision();
            }
        }
    }

    /// <summary>
    /// Adds a stack of mana crystals (same stack size as the former full mana potion stack per starter).
    /// </summary>
    static void TryAddManaCrystalStack(Inventory inv, int stackCount)
    {
        if (inv == null || stackCount <= 0) return;
        var data = ContentRegistry.GetItemData("mana_crystal");
        if (data == null) return;
        var item = ContentRegistry.CreateItem("mana_crystal");
        if (item == null) return;
        item.ConfigureStacks(data.MaxStack, stackCount);
        inv.TryAddItem(item);
    }
}


