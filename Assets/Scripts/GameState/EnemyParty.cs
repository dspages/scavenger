using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EnemyParty
{
    public static List<CharacterSheet> partyMembers;

    public static void GenerateNewEnemies()
    {
        partyMembers = new List<CharacterSheet> { };
        partyMembers.Add(new CharacterSheet("Enemy Rogue", CharacterSheet.CharacterClass.CLASS_ROGUE));
        partyMembers.Add(new CharacterSheet("Enemy Warrior", CharacterSheet.CharacterClass.CLASS_SOLDIER));
        partyMembers.Add(new CharacterSheet("Enemy Fire Mage", CharacterSheet.CharacterClass.CLASS_FIREMAGE));
    }

    public static void SpawnPartyMembers(TurnManager turnManager)
    {
        TileManager tileManager = GameObject.FindObjectOfType<TileManager>();
        int xPos = Globals.COMBAT_WIDTH - 2;
        int yPos = Globals.COMBAT_HEIGHT - 2;
        Quaternion facing = new Quaternion(0f, 0f, 0f, 0f);
        foreach (CharacterSheet c in partyMembers)
        {
            if (c.CanDeploy())
            {
                EnemyController avatar = c.CreateCombatAvatarAsNPC(new Vector3(xPos, yPos, 0f), facing, tileManager.getTile(xPos, yPos));
                avatar.transform.SetParent(turnManager.transform);
                xPos -= 1;
            }
        }
    }
}
