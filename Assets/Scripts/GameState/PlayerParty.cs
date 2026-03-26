using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerParty
{
    public const int MaxExcursionSquad = 4;

    public static List<CharacterSheet> partyMembers;

    /// <summary>Shared camp stash (Home Base preparation, future vendors).</summary>
    public static Inventory sharedStash;

    /// <summary>Indices into <see cref="partyMembers"/> for the next mission, in spawn order (max <see cref="MaxExcursionSquad"/>).</summary>
    public static List<int> excursionSquadIndices;

    public static void Reset()
    {
        partyMembers = new List<CharacterSheet> { };
        // null/empty name defaults to plain-English class name; set firstName/portrait per individual when needed
        partyMembers.Add(new CharacterSheet(null, CharacterSheet.CharacterClass.CLASS_ROGUE));
        partyMembers.Add(new CharacterSheet(null, CharacterSheet.CharacterClass.CLASS_SOLDIER));
        partyMembers.Add(new CharacterSheet(null, CharacterSheet.CharacterClass.CLASS_FIREMAGE));
        sharedStash = new Inventory();
        excursionSquadIndices = new List<int>();
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
            if (excursionSquadIndices == null)
                excursionSquadIndices = new List<int>();
            SanitizeExcursionSquad();
        }
    }

    /// <summary>True if this roster index is included on the excursion squad for the next deployment.</summary>
    public static bool IsOnExcursionSquad(int partyIndex)
    {
        return excursionSquadIndices != null && excursionSquadIndices.Contains(partyIndex);
    }

    /// <summary>Add or remove a member from the excursion squad. Returns false if <paramref name="on"/> is true but the squad is full.</summary>
    public static bool TrySetExcursionSquad(int partyIndex, bool on)
    {
        if (partyMembers == null || partyIndex < 0 || partyIndex >= partyMembers.Count)
            return false;
        if (excursionSquadIndices == null)
            excursionSquadIndices = new List<int>();
        if (on)
        {
            if (excursionSquadIndices.Contains(partyIndex))
                return true;
            if (excursionSquadIndices.Count >= MaxExcursionSquad)
                return false;
            excursionSquadIndices.Add(partyIndex);
            return true;
        }

        excursionSquadIndices.Remove(partyIndex);
        return true;
    }

    static void FillDefaultExcursionSquad()
    {
        excursionSquadIndices ??= new List<int>();
        excursionSquadIndices.Clear();
        if (partyMembers == null) return;
        for (int i = 0; i < partyMembers.Count && i < MaxExcursionSquad; i++)
            excursionSquadIndices.Add(i);
    }

    /// <summary>Remove invalid indices, dedupe, and refill if empty.</summary>
    public static void SanitizeExcursionSquad()
    {
        if (partyMembers == null) return;
        excursionSquadIndices ??= new List<int>();
        excursionSquadIndices.RemoveAll(idx => idx < 0 || idx >= partyMembers.Count);
        var seen = new HashSet<int>();
        for (int i = excursionSquadIndices.Count - 1; i >= 0; i--)
        {
            if (!seen.Add(excursionSquadIndices[i]))
                excursionSquadIndices.RemoveAt(i);
        }

        if (excursionSquadIndices.Count == 0)
            FillDefaultExcursionSquad();
    }

    public static void SpawnPartyMembers(TurnManager turnManager)
    {
        TileManager tileManager = GameObject.FindFirstObjectByType<TileManager>(FindObjectsInactive.Exclude);
        int xPos = 1;
        int yPos = 1;
        Quaternion facing = new Quaternion(0f, 0f, 0f, 0f);
        SanitizeExcursionSquad();
        if (excursionSquadIndices == null || excursionSquadIndices.Count == 0) return;
        for (int i = 0; i < excursionSquadIndices.Count; i++)
        {
            int idx = excursionSquadIndices[i];
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
