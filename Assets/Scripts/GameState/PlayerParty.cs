using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerParty
{
    public static List<CharacterSheet> partyMembers;

    public static void Reset()
    {
        partyMembers = new List<CharacterSheet> { };
        partyMembers.Add(new CharacterSheet("Main Character"));
    }

    public static void SpawnPartyMembers(TurnManager turnManager)
    {
        TileManager tileManager = GameObject.FindObjectOfType<TileManager>();
        int xPos = 1;
        int yPos = 1;
        Quaternion facing = new Quaternion(0f, 0f, 0f, 0f);
        foreach (CharacterSheet c in partyMembers)
        {
            if (c.CanDeploy())
            {
                PlayerController avatar = c.CreateCombatAvatarAsPC(new Vector3(xPos, yPos, 0f), facing);
                avatar.SetCurrentTile(tileManager.getTile(xPos, yPos));
                avatar.transform.SetParent(turnManager.transform);
                xPos += 1;
            }
        }
    }
}
