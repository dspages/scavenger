using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ActionSelector
{
    private readonly GameObject owner;
    private readonly Func<CharacterSheet> getSheet;

    private Action selectedAction;
    private string selectedActionKey;

    public Action SelectedAction => selectedAction;
    public string SelectedActionKey => selectedActionKey;

    public void SetAction(Action action)
    {
        selectedAction = action;
    }

    public ActionSelector(GameObject owner, Func<CharacterSheet> getSheet)
    {
        this.owner = owner;
        this.getSheet = getSheet;
    }

    public string GetSelectedActionClassName()
    {
        return selectedAction != null ? selectedAction.GetType().Name : null;
    }

    public string GetDefaultActionClassName()
    {
        var sheet = getSheet();
        if (sheet == null) return nameof(ActionMeleeAttack);
        var right = sheet.GetEquippedItem(EquippableItem.EquipmentSlot.RightHand) as EquippableHandheld;
        if (right != null && !string.IsNullOrEmpty(right.associatedActionClass))
            return right.associatedActionClass;
        return nameof(ActionMeleeAttack);
    }

    public void ResetToDefault()
    {
        string className = GetDefaultActionClassName();
        Action act = GetOrAddByName(className);
        if (act == null)
            act = GetOrAddByType(typeof(ActionMeleeAttack));
        selectedAction = act;

        var sheet = getSheet();
        var right = sheet != null
            ? sheet.GetEquippedItem(EquippableItem.EquipmentSlot.RightHand) as EquippableHandheld
            : null;
        selectedActionKey = right != null ? $"{className}:RightHand" : className;

        if (!ConfigureFromEquippedItem())
            InitializeSpellAction();
    }

    public void SelectByType(Type t)
    {
        Action act = GetOrAddByType(t);
        if (act != null)
        {
            selectedAction = act;
            if (!ConfigureFromEquippedItem())
                InitializeSpellAction();
        }
    }

    public void SelectByName(string className)
    {
        Action act = GetOrAddByName(className);
        if (act != null)
        {
            selectedAction = act;
            if (!ConfigureFromEquippedItem())
                InitializeSpellAction();
        }
    }

    public void Select(string key, string className)
    {
        SelectByName(className);
        ConfigureCurrent(key);
    }

    public void SelectSilent(string key, string className)
    {
        Action act = GetOrAddByName(className);
        if (act != null)
        {
            selectedAction = act;
            ConfigureCurrent(key);
        }
    }

    public void EnsureStillValid()
    {
        var sheet = getSheet();
        if (sheet == null) return;

        bool dependsOnRight = !string.IsNullOrEmpty(selectedActionKey) && selectedActionKey.EndsWith(":RightHand");
        bool dependsOnLeft = !string.IsNullOrEmpty(selectedActionKey) && selectedActionKey.EndsWith(":LeftHand");
        bool isPunchSelected = string.IsNullOrEmpty(selectedActionKey) || selectedActionKey == nameof(ActionMeleeAttack);

        var right = sheet.GetEquippedItem(EquippableItem.EquipmentSlot.RightHand) as EquippableHandheld;
        var left = sheet.GetEquippedItem(EquippableItem.EquipmentSlot.LeftHand) as EquippableHandheld;

        bool needsReselection = false;
        if ((dependsOnRight && right == null) || (dependsOnLeft && left == null))
            needsReselection = true;
        else if (isPunchSelected && (right != null || left != null))
            needsReselection = true;
        else if (string.IsNullOrEmpty(selectedActionKey))
            needsReselection = true;

        if (needsReselection)
        {
            if (right != null)
            {
                string cls = string.IsNullOrEmpty(right.associatedActionClass) ? nameof(ActionMeleeAttack) : right.associatedActionClass;
                Select($"{cls}:RightHand", cls);
            }
            else if (left != null)
            {
                string cls = string.IsNullOrEmpty(left.associatedActionClass) ? nameof(ActionMeleeAttack) : left.associatedActionClass;
                Select($"{cls}:LeftHand", cls);
            }
            else
            {
                Select(nameof(ActionMeleeAttack), nameof(ActionMeleeAttack));
            }
        }
    }

    // --- Component helpers ---

    public Action GetOrAddByType(Type t)
    {
        if (t == null || owner == null) return null;
        var existing = owner.GetComponent(t) as Action;
        if (existing != null) return existing;
        return owner.AddComponent(t) as Action;
    }

    public Action GetOrAddByName(string className)
    {
        if (string.IsNullOrEmpty(className)) return null;
        var t = FindTypeByName(className);
        return GetOrAddByType(t);
    }

    private static readonly Dictionary<string, Type> typeCache = new Dictionary<string, Type>();

    private static Type FindTypeByName(string className)
    {
        if (typeCache.TryGetValue(className, out var cached))
            return cached;

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetTypes().FirstOrDefault(x => x.Name == className);
            if (t != null)
            {
                typeCache[className] = t;
                return t;
            }
        }
        return null;
    }

    // --- Configuration helpers ---

    private void ConfigureCurrent(string key)
    {
        selectedActionKey = key;
        if (!ConfigureFromEquippedItem())
            InitializeSpellAction();
    }

    private bool ConfigureFromEquippedItem()
    {
        var sheet = getSheet();
        if (sheet == null || selectedAction == null) return false;

        EquippableHandheld equipped = GetEquippedWeaponForSelection(sheet);
        if (equipped == null) return false;
        return equipped.ConfigureAction(selectedAction);
    }

    private EquippableHandheld GetEquippedWeaponForSelection(CharacterSheet sheet)
    {
        if (string.IsNullOrEmpty(selectedActionKey)) return null;

        if (selectedActionKey.EndsWith(":RightHand"))
            return sheet.GetEquippedItem(EquippableItem.EquipmentSlot.RightHand) as EquippableHandheld;
        if (selectedActionKey.EndsWith(":LeftHand"))
            return sheet.GetEquippedItem(EquippableItem.EquipmentSlot.LeftHand) as EquippableHandheld;

        return sheet.GetEquippedItem(EquippableItem.EquipmentSlot.RightHand) as EquippableHandheld;
    }

    private void InitializeSpellAction()
    {
        selectedAction?.ConfigureAction();
    }
}
