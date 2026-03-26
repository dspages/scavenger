using UnityEngine;

public static class CharacterSetup
{
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
        inv.TryAddItem(ContentRegistry.CreateItem("mana_potion"));
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
}


