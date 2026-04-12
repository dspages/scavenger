using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Weekly rotating recruit offers at Home Base. Cleared and refilled when the campaign week advances;
/// also ensured when opening Home Base if the pool was never generated for <see cref="CampaignMeta.CurrentWeek"/>.
/// </summary>
public static class RecruitPool
{
    public const int OffersPerWeek = 2;

    public static int LastRefreshedWeek { get; private set; }

    public static List<CharacterSheet> OfferedRecruits { get; } = new List<CharacterSheet>();

    public static void ResetForNewCampaign()
    {
        LastRefreshedWeek = 0;
        OfferedRecruits.Clear();
    }

    /// <summary>Call when the mission ends and the campaign week has just incremented.</summary>
    public static void RefreshPoolForNewWeek()
    {
        FillPoolForCurrentWeek();
    }

    /// <summary>Ensures offers exist for the current week (e.g. first Home Base visit or after load).</summary>
    public static void EnsureFreshForCurrentWeek()
    {
        if (LastRefreshedWeek == CampaignMeta.CurrentWeek)
            return;
        FillPoolForCurrentWeek();
    }

    static void FillPoolForCurrentWeek()
    {
        OfferedRecruits.Clear();
        for (int i = 0; i < OffersPerWeek; i++)
            OfferedRecruits.Add(GenerateRecruit());
        LastRefreshedWeek = CampaignMeta.CurrentWeek;
    }

    public static void RestoreFromSave(int week, List<CharacterSaveData> offers)
    {
        LastRefreshedWeek = week;
        OfferedRecruits.Clear();
        if (offers == null) return;
        foreach (var cd in offers)
        {
            if (cd != null)
                OfferedRecruits.Add(cd.ToSheet());
        }
    }

    /// <summary>Hires the recruit at <paramref name="index"/> into the party if there is room.</summary>
    public static bool TryHireFromPool(int index, out string failReason)
    {
        failReason = null;
        if (index < 0 || index >= OfferedRecruits.Count)
        {
            failReason = "Invalid recruit.";
            return false;
        }

        PlayerParty.EnsureInitialized();
        var party = PlayerParty.partyMembers;
        if (party == null)
        {
            failReason = "No party.";
            return false;
        }

        if (party.Count >= PlayerParty.MaxRosterMembers)
        {
            failReason = "Roster is full.";
            return false;
        }

        var sheet = OfferedRecruits[index];
        OfferedRecruits.RemoveAt(index);
        party.Add(sheet);
        PlayerParty.SanitizeExcursionSquad();
        return true;
    }

    static CharacterSheet GenerateRecruit()
    {
        var cls = PickRandomClass();
        var name = RecruitNamePool.PickRandomFirstName();
        var sheet = new CharacterSheet(name, cls);
        sheet.species = PickRecruitSpecies();
        SpeciesRules.ApplyRecruitStatModifiers(sheet);
        CharacterSetup.ApplyStartingPlayerStatVariance(sheet);
        return sheet;
    }

    static string PickRecruitSpecies()
    {
        // V0 distribution: 75% human, 25% random non-human.
        if (Globals.rng.NextDouble() < 0.75d)
            return SpeciesRules.Human;
        return PickRandomNonHumanSpecies();
    }

    static string PickRandomNonHumanSpecies()
    {
        // V0 list (expand later).
        return SpeciesRules.Langurii;
    }

    static CharacterSheet.CharacterClass PickRandomClass()
    {
        var values = (CharacterSheet.CharacterClass[])Enum.GetValues(typeof(CharacterSheet.CharacterClass));
        if (values == null || values.Length == 0)
            return CharacterSheet.CharacterClass.CLASS_SOLDIER;
        int idx = Globals.rng.Next(values.Length);
        return values[Mathf.Clamp(idx, 0, values.Length - 1)];
    }
}
