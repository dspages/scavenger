using System.Collections.Generic;

public static class ContentRegistry
{
    private static Dictionary<string, ItemData> items;
    private static Dictionary<StatusEffect.EffectType, StatusEffectData> effects;
    private static Dictionary<string, AbilityData> abilities;

    private static void EnsureInitialized()
    {
        if (items != null) return;
        items = new Dictionary<string, ItemData>();
        foreach (var item in ItemCatalog.All)
            items[item.id] = item;

        effects = new Dictionary<StatusEffect.EffectType, StatusEffectData>();
        foreach (var fx in StatusEffectCatalog.All)
            effects[fx.effectType] = fx;

        abilities = new Dictionary<string, AbilityData>();
        foreach (var ab in AbilityCatalog.All)
            abilities[ab.id] = ab;
    }

    // ---- Items ----

    public static ItemData GetItemData(string id)
    {
        EnsureInitialized();
        return items.TryGetValue(id, out var data) ? data : null;
    }

    public static InventoryItem CreateItem(string id)
    {
        var data = GetItemData(id);
        return data?.CreateInstance();
    }

    public static EquippableItem CreateEquippable(string id)
    {
        return CreateItem(id) as EquippableItem;
    }

    public static IEnumerable<ItemData> AllItems()
    {
        EnsureInitialized();
        return items.Values;
    }

    public static IEnumerable<T> AllOfType<T>() where T : ItemData
    {
        EnsureInitialized();
        foreach (var item in items.Values)
        {
            if (item is T typed) yield return typed;
        }
    }

    public static void Register(ItemData data)
    {
        EnsureInitialized();
        items[data.id] = data;
    }

    // ---- Status Effects ----

    public static StatusEffectData GetEffectData(StatusEffect.EffectType type)
    {
        EnsureInitialized();
        return effects.TryGetValue(type, out var data) ? data : null;
    }

    public static void Register(StatusEffectData data)
    {
        EnsureInitialized();
        effects[data.effectType] = data;
    }

    // ---- Abilities ----

    public static AbilityData GetAbilityData(string id)
    {
        EnsureInitialized();
        return abilities.TryGetValue(id, out var data) ? data : null;
    }

    public static IEnumerable<AbilityData> AllAbilities()
    {
        EnsureInitialized();
        return abilities.Values;
    }

    public static void Register(AbilityData data)
    {
        EnsureInitialized();
        abilities[data.id] = data;
    }
}
