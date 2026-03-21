using System;
using System.Collections.Generic;

public static class CombatLog
{
    public const int MaxEntries = 200;

    private static readonly List<string> entries = new List<string>();
    private static readonly List<EquippableHandheld.DamageType?> entryDamageTypes = new List<EquippableHandheld.DamageType?>();

    public static event Action<string, EquippableHandheld.DamageType?> OnEntryAdded;

    public static IReadOnlyList<string> Entries => entries;

    public static void Log(string message, EquippableHandheld.DamageType? damageType = null)
    {
        if (string.IsNullOrEmpty(message)) return;
        entries.Add(message);
        entryDamageTypes.Add(damageType);
        if (entries.Count > MaxEntries)
        {
            entries.RemoveAt(0);
            entryDamageTypes.RemoveAt(0);
        }
        OnEntryAdded?.Invoke(message, damageType);
    }

    public static void Clear()
    {
        entries.Clear();
        entryDamageTypes.Clear();
    }

    public static EquippableHandheld.DamageType? GetEntryDamageType(int index)
    {
        if (index < 0 || index >= entryDamageTypes.Count) return null;
        return entryDamageTypes[index];
    }
}
