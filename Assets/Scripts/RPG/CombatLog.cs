using System;
using System.Collections.Generic;

public static class CombatLog
{
    public const int MaxEntries = 200;

    private static readonly List<string> entries = new List<string>();

    public static event Action<string> OnEntryAdded;

    public static IReadOnlyList<string> Entries => entries;

    public static void Log(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        entries.Add(message);
        if (entries.Count > MaxEntries)
            entries.RemoveAt(0);
        OnEntryAdded?.Invoke(message);
    }

    public static void Clear()
    {
        entries.Clear();
    }
}
