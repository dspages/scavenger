using UnityEngine;

/// <summary>Settings for tiered tooltips: delay before showing detailed (stat block / flavor) content. Options menu should read/write these.</summary>
public static class TooltipDetailSettings
{
    private const string PrefsKeyDelay = "TooltipDetailDelay";
    private const string PrefsKeyNever = "TooltipDetailNeverHover";

    /// <summary>Seconds to hover before expanding to detailed tooltip. Default 1. If NeverExpandOnHover is true, only right-click shows detail.</summary>
    public static float DetailDelaySeconds
    {
        get => PlayerPrefs.GetFloat(PrefsKeyDelay, 1f);
        set => PlayerPrefs.SetFloat(PrefsKeyDelay, Mathf.Clamp(value, 0.2f, 2f));
    }

    /// <summary>If true, hover never expands to detailed tooltip; only right-click (or Inspect) shows detail.</summary>
    public static bool NeverExpandOnHover
    {
        get => PlayerPrefs.GetInt(PrefsKeyNever, 0) != 0;
        set => PlayerPrefs.SetInt(PrefsKeyNever, value ? 1 : 0);
    }

    public static void Save() => PlayerPrefs.Save();
}
