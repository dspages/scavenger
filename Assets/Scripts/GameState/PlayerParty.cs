using System.Collections.Generic;
using UnityEngine;

public static class PlayerParty
{
    public const int MaxExcursionSquad = 4;
    /// <summary>Maximum party size at Home Base (recruits, roster UI).</summary>
    public const int MaxRosterMembers = 6;

    public static List<CharacterSheet> partyMembers;

    /// <summary>Shared camp stash (Home Base preparation, future vendors).</summary>
    public static Inventory sharedStash;

    /// <summary>Fixed excursion slots (spawn order = index 0..3). -1 = empty.</summary>
    public static int[] excursionSquadSlots;

    /// <summary>Stub weekly base jobs (workbench, etc.). Not on excursion.</summary>
    public static Dictionary<int, WeeklyBaseJobKind> weeklyBaseJobByPartyIndex;

    /// <summary>
    /// Legacy ordered list of party indices on the excursion squad (non-empty slots only, in spawn order).
    /// Kept in sync with <see cref="excursionSquadSlots"/> for callers that still read the list.
    /// </summary>
    public static List<int> excursionSquadIndices;

    public static void Reset()
    {
        string[] starterNames = RecruitNamePool.PickDistinctFirstNames(3);
        partyMembers = new List<CharacterSheet>
        {
            new CharacterSheet(starterNames[0], CharacterSheet.CharacterClass.CLASS_ROGUE),
            new CharacterSheet(starterNames[1], CharacterSheet.CharacterClass.CLASS_SOLDIER),
            new CharacterSheet(starterNames[2], CharacterSheet.CharacterClass.CLASS_FIREMAGE),
        };
        foreach (var member in partyMembers)
            CharacterSetup.ApplyStartingPlayerStatVariance(member);
        sharedStash = new Inventory();
        weeklyBaseJobByPartyIndex = new Dictionary<int, WeeklyBaseJobKind>();
        EnsureSlotsArray();
        FillDefaultExcursionSquad();
    }

    /// <summary>Ensures a party exists (e.g. opening Home Base without going through New Game).</summary>
    public static void EnsureInitialized()
    {
        if (partyMembers == null || partyMembers.Count == 0)
            Reset();
        else
        {
            if (sharedStash == null)
                sharedStash = new Inventory();
            if (weeklyBaseJobByPartyIndex == null)
                weeklyBaseJobByPartyIndex = new Dictionary<int, WeeklyBaseJobKind>();
            MigrateSlotsFromLegacyListIfNeeded();
            if (excursionSquadIndices == null)
                excursionSquadIndices = new List<int>();
            SanitizeExcursionSquad();
        }
    }

    static void EnsureSlotsArray()
    {
        if (excursionSquadSlots == null || excursionSquadSlots.Length != MaxExcursionSquad)
        {
            excursionSquadSlots = new int[MaxExcursionSquad];
            for (int i = 0; i < MaxExcursionSquad; i++)
                excursionSquadSlots[i] = -1;
        }
        excursionSquadIndices ??= new List<int>();
    }

    /// <summary>One-time fill from legacy <see cref="excursionSquadIndices"/> when fixed slots are all empty.</summary>
    static void MigrateSlotsFromLegacyListIfNeeded()
    {
        EnsureSlotsArray();
        bool anyOccupied = false;
        for (int i = 0; i < MaxExcursionSquad; i++)
        {
            if (excursionSquadSlots[i] >= 0)
            {
                anyOccupied = true;
                break;
            }
        }
        if (anyOccupied) return;
        if (excursionSquadIndices == null || excursionSquadIndices.Count == 0) return;
        for (int i = 0; i < excursionSquadIndices.Count && i < MaxExcursionSquad; i++)
            excursionSquadSlots[i] = excursionSquadIndices[i];
        SyncLegacyListFromSlots();
    }

    /// <summary>Hook for slot rules (HP, stash components, etc.). Stub: always true.</summary>
    public static bool CanAssignExcursionSlot(CharacterSheet character, int slotIndex)
    {
        if (character == null) return false;
        if (slotIndex < 0 || slotIndex >= MaxExcursionSquad) return false;
        return true;
    }

    /// <summary>Dropdown lists unassigned heroes: not on excursion and not given a base job this week.</summary>
    public static bool IsEligibleForExcursionDropdown(int partyIndex)
    {
        if (partyMembers == null || partyIndex < 0 || partyIndex >= partyMembers.Count)
            return false;
        if (FindExcursionSlotForPartyIndex(partyIndex) >= 0)
            return false;
        return !weeklyBaseJobByPartyIndex.TryGetValue(partyIndex, out var job) || job == WeeklyBaseJobKind.None;
    }

    static int FindExcursionSlotForPartyIndex(int partyIndex)
    {
        EnsureSlotsArray();
        for (int i = 0; i < MaxExcursionSquad; i++)
            if (excursionSquadSlots[i] == partyIndex)
                return i;
        return -1;
    }

    /// <summary>Party index at fixed slot, or -1 if empty.</summary>
    public static int GetExcursionSlotPartyIndex(int slotIndex)
    {
        EnsureSlotsArray();
        if (slotIndex < 0 || slotIndex >= MaxExcursionSquad) return -1;
        return excursionSquadSlots[slotIndex];
    }

    /// <summary>
    /// Assigns a character to a fixed excursion slot. Displaces the previous occupant (unassigned).
    /// Removes assignee from any previous slot and from weekly base jobs. Fails if <see cref="CanAssignExcursionSlot"/> returns false.
    /// </summary>
    public static bool TryAssignExcursionSlot(int slotIndex, int partyIndex)
    {
        if (partyMembers == null || partyIndex < 0 || partyIndex >= partyMembers.Count)
            return false;
        if (slotIndex < 0 || slotIndex >= MaxExcursionSquad)
            return false;
        EnsureSlotsArray();

        var ch = partyMembers[partyIndex];
        if (!CanAssignExcursionSlot(ch, slotIndex))
            return false;

        if (excursionSquadSlots[slotIndex] == partyIndex)
        {
            SyncLegacyListFromSlots();
            return true;
        }

        for (int i = 0; i < MaxExcursionSquad; i++)
        {
            if (excursionSquadSlots[i] == partyIndex)
                excursionSquadSlots[i] = -1;
        }

        weeklyBaseJobByPartyIndex.Remove(partyIndex);

        int atTarget = excursionSquadSlots[slotIndex];
        if (atTarget >= 0)
            excursionSquadSlots[slotIndex] = -1;

        excursionSquadSlots[slotIndex] = partyIndex;
        SyncLegacyListFromSlots();
        return true;
    }

    public static void TryClearExcursionSlot(int slotIndex)
    {
        EnsureSlotsArray();
        if (slotIndex < 0 || slotIndex >= MaxExcursionSquad) return;
        excursionSquadSlots[slotIndex] = -1;
        SyncLegacyListFromSlots();
    }

    static void SyncLegacyListFromSlots()
    {
        EnsureSlotsArray();
        excursionSquadIndices.Clear();
        for (int i = 0; i < MaxExcursionSquad; i++)
        {
            int p = excursionSquadSlots[i];
            if (p >= 0)
                excursionSquadIndices.Add(p);
        }
    }

    /// <summary>True if this roster index is included on the excursion squad for the next deployment.</summary>
    public static bool IsOnExcursionSquad(int partyIndex)
    {
        return FindExcursionSlotForPartyIndex(partyIndex) >= 0;
    }

    /// <summary>Add or remove a member from the excursion squad (first empty slot / remove from any slot).</summary>
    public static bool TrySetExcursionSquad(int partyIndex, bool on)
    {
        if (partyMembers == null || partyIndex < 0 || partyIndex >= partyMembers.Count)
            return false;
        EnsureSlotsArray();
        if (on)
        {
            if (FindExcursionSlotForPartyIndex(partyIndex) >= 0)
                return true;
            for (int s = 0; s < MaxExcursionSquad; s++)
            {
                if (excursionSquadSlots[s] < 0)
                    return TryAssignExcursionSlot(s, partyIndex);
            }
            return false;
        }

        for (int s = 0; s < MaxExcursionSquad; s++)
        {
            if (excursionSquadSlots[s] == partyIndex)
                excursionSquadSlots[s] = -1;
        }
        SyncLegacyListFromSlots();
        return true;
    }

    static void FillDefaultExcursionSquad()
    {
        EnsureSlotsArray();
        for (int i = 0; i < MaxExcursionSquad; i++)
            excursionSquadSlots[i] = -1;
        if (partyMembers == null) return;
        int n = Mathf.Min(MaxExcursionSquad, partyMembers.Count);
        for (int i = 0; i < n; i++)
            excursionSquadSlots[i] = i;
        SyncLegacyListFromSlots();
    }

    /// <summary>Remove invalid indices, dedupe slots, refill if empty.</summary>
    public static void SanitizeExcursionSquad()
    {
        if (partyMembers == null) return;
        EnsureSlotsArray();
        var seen = new HashSet<int>();
        for (int i = 0; i < MaxExcursionSquad; i++)
        {
            int p = excursionSquadSlots[i];
            if (p < 0 || p >= partyMembers.Count || !seen.Add(p))
                excursionSquadSlots[i] = -1;
        }

        bool any = false;
        for (int i = 0; i < MaxExcursionSquad; i++)
        {
            if (excursionSquadSlots[i] >= 0)
            {
                any = true;
                break;
            }
        }

        if (!any)
            FillDefaultExcursionSquad();
        else
            SyncLegacyListFromSlots();
    }

    public static void SpawnPartyMembers(TurnManager turnManager)
    {
        TileManager tileManager = GameObject.FindFirstObjectByType<TileManager>(FindObjectsInactive.Exclude);
        int xPos = 1;
        int yPos = 1;
        Quaternion facing = new Quaternion(0f, 0f, 0f, 0f);
        SanitizeExcursionSquad();
        EnsureSlotsArray();
        if (partyMembers == null) return;
        for (int i = 0; i < MaxExcursionSquad; i++)
        {
            int idx = excursionSquadSlots[i];
            if (idx < 0 || idx >= partyMembers.Count) continue;
            CharacterSheet c = partyMembers[idx];
            if (c.CanDeploy())
            {
                PlayerController avatar = AvatarController.CreatePC(c, new Vector3(xPos, yPos, 0f), facing, tileManager.getTile(xPos, yPos));
                avatar.transform.SetParent(turnManager.transform);
                xPos += 1;
            }
        }
    }
}

public enum WeeklyBaseJobKind
{
    None = 0,
    Workbench = 1,
    Barracks = 2,
    Altar = 3,
}
