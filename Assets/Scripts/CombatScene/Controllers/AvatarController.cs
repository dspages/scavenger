using UnityEngine;

public class AvatarController : MonoBehaviour
{
    private CharacterSheet characterSheet;
    private IndicatorBar healthBar;

    public void Initialize(CharacterSheet sheet)
    {
        characterSheet = sheet;
        characterSheet.avatar = gameObject;

        var canvas = transform.Find("Canvas");
        if (canvas != null)
        {
            if (canvas.GetComponent<BillboardToCamera>() == null)
                canvas.gameObject.AddComponent<BillboardToCamera>();
            healthBar = canvas.Find("HealthBar").GetComponent<IndicatorBar>();
        }
        healthBar.SetSliderMax(sheet.MaxHealth());
        healthBar.SetSlider(sheet.currentHealth);

        characterSheet.OnHealthChanged += OnHealthChanged;
    }

    private void OnDestroy()
    {
        if (characterSheet != null)
        {
            characterSheet.OnHealthChanged -= OnHealthChanged;
        }
    }

    private void OnHealthChanged()
    {
        if (healthBar != null)
        {
            healthBar.SetSlider(characterSheet.currentHealth);
        }

        if (characterSheet.dead)
        {
            var cc = GetComponent<CombatController>();
            if (cc != null)
            {
                // Clear tile occupancy so pathfinding/targeting no longer sees this unit
                var tile = cc.GetCurrentTile();
                if (tile != null) tile.occupant = null;

                cc.Die();
            }
        }
    }

    public void DestroyAvatar()
    {
        Destroy(gameObject);
    }

    public void DisplayPopup(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        PopupTextController.CreatePopupText(text, transform);
    }

    public void DisplayPopupAfterDelay(float delay, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        PopupTextController.CreatePopupTextAfterDelay(text, transform, delay);
    }

    public static PlayerController CreatePC(CharacterSheet sheet, Vector3 position, Quaternion rotation, Tile tile)
    {
        GameObject combatant = CreateAvatar(sheet, position, rotation);
        PlayerController controller = combatant.AddComponent<PlayerController>();
        controller.SetCurrentTile(tile);
        controller.SetCharacterSheet(sheet);
        AttachKnownActions(sheet, combatant);
        return controller;
    }

    public static EnemyController CreateNPC(CharacterSheet sheet, Vector3 position, Quaternion rotation, Tile tile)
    {
        GameObject combatant = CreateAvatar(sheet, position, rotation);
        EnemyController controller = combatant.AddComponent<EnemyController>();
        controller.SetCurrentTile(tile);
        controller.SetCharacterSheet(sheet);
        AttachKnownActions(sheet, combatant);
        return controller;
    }

    private static GameObject CreateAvatar(CharacterSheet sheet, Vector3 position, Quaternion rotation)
    {
        GameObject prefab = (GameObject)Resources.Load("Prefabs/combatant", typeof(GameObject));
        GameObject avatar = Instantiate(prefab, position, rotation);

        AvatarController avatarController = avatar.AddComponent<AvatarController>();
        avatarController.Initialize(sheet);

        return avatar;
    }

    private static void AttachKnownActions(CharacterSheet sheet, GameObject go)
    {
        if (go == null) return;
        foreach (var actionType in sheet.GetKnownSpecialActionTypes())
        {
            if (actionType == null) continue;
            if (go.GetComponent(actionType) == null)
            {
                go.AddComponent(actionType);
            }
        }
        foreach (var ability in sheet.GetKnownAbilities())
        {
            AttachAbility(go, ability);
        }
    }

    private static void AttachAbility(GameObject go, AbilityData ability)
    {
        switch (ability.archetype)
        {
            case AbilityData.Archetype.MeleeAttack:
                var melee = go.AddComponent<ActionMeleeAttack>();
                melee.ConfigureFromAbility(ability);
                break;
            case AbilityData.Archetype.RangedAttack:
                var ranged = go.AddComponent<ActionRangedAttack>();
                ranged.ConfigureFromAbility(ability);
                break;
            case AbilityData.Archetype.GroundAttack:
                var ground = go.AddComponent<ActionGroundAttack>();
                ground.ConfigureFromAbility(ability);
                break;
            case AbilityData.Archetype.SelfCast:
                if (go.GetComponent<ActionSelfCast>() == null)
                    go.AddComponent<ActionSelfCast>();
                break;
            case AbilityData.Archetype.AllyBuff:
                var buff = go.AddComponent<ActionAllyBuff>();
                buff.ConfigureFromAbility(ability);
                break;
        }
    }
}
