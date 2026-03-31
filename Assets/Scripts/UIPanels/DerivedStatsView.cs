using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Stats block from CharacterSheet. Inventory tab (compact): armor + weapon attacks. Character tab: full header, derived combat, ranges, damage, resistances.</summary>
public static class DerivedStatsView
{
    public const string ContainerName = "StatsBlockContainer";
    public const string ArmorLabelName = "StatsArmor";
    public const string DamageSummaryName = "StatsDamageSummary";
    public const string ResistancesName = "StatsResistances";

    /// <summary>Refresh or build the stats block under the given container. If container is null, does nothing. Creates inner structure if missing.</summary>
    public static void Refresh(VisualElement container, CharacterSheet sheet, bool compact = true)
    {
        if (container == null) return;

        container.Clear();

        if (sheet == null)
        {
            var noChar = new Label("No character selected.");
            noChar.AddToClassList("text-muted");
            container.Add(noChar);
            return;
        }

        if (compact)
        {
            AddArmorRow(container, sheet);
            AddWeaponDamageRow(container, sheet, withAttackLabel: true);
            return;
        }

        // Character tab: portrait, name, class, level, primary stats
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Column;

        var heroRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.FlexStart, marginBottom = 6 } };
        var portraitBox = new VisualElement();
        portraitBox.style.width = 64;
        portraitBox.style.height = 64;
        portraitBox.style.minWidth = 64;
        portraitBox.style.minHeight = 64;
        portraitBox.style.marginRight = 10;
        portraitBox.style.borderTopLeftRadius = 4;
        portraitBox.style.borderTopRightRadius = 4;
        portraitBox.style.borderBottomLeftRadius = 4;
        portraitBox.style.borderBottomRightRadius = 4;
        portraitBox.style.overflow = Overflow.Hidden;
        var resolvedPortrait = sheet.ResolvePortrait();
        if (resolvedPortrait != null)
        {
            portraitBox.style.backgroundImage = new StyleBackground(resolvedPortrait);
            portraitBox.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        }
        else
        {
            portraitBox.style.backgroundColor = new Color(0.15f, 0.17f, 0.22f, 1f);
            portraitBox.style.borderTopWidth = 1;
            portraitBox.style.borderBottomWidth = 1;
            portraitBox.style.borderLeftWidth = 1;
            portraitBox.style.borderRightWidth = 1;
            portraitBox.style.borderTopColor = new Color(0.4f, 0.45f, 0.55f, 0.6f);
            portraitBox.style.borderBottomColor = new Color(0.4f, 0.45f, 0.55f, 0.6f);
            portraitBox.style.borderLeftColor = new Color(0.4f, 0.45f, 0.55f, 0.6f);
            portraitBox.style.borderRightColor = new Color(0.4f, 0.45f, 0.55f, 0.6f);
        }
        heroRow.Add(portraitBox);

        var textCol = new VisualElement { style = { flexDirection = FlexDirection.Column, flexGrow = 1 } };
        var nameRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 2, flexWrap = Wrap.Wrap } };
        var nameLabel = new Label(string.IsNullOrEmpty(sheet.firstName) ? CharacterSheet.GetDefaultDisplayNameForClass(sheet.characterClass) : sheet.firstName);
        nameLabel.AddToClassList("text-title");
        nameRow.Add(nameLabel);
        var classLabel = new Label(FormatClassName(sheet.characterClass.ToString()));
        classLabel.AddToClassList("text-soft");
        classLabel.style.marginLeft = 6;
        nameRow.Add(classLabel);
        var levelLabel = new Label($"Lv {sheet.level}");
        levelLabel.AddToClassList("text-soft");
        levelLabel.style.marginLeft = 6;
        nameRow.Add(levelLabel);
        textCol.Add(nameRow);
        heroRow.Add(textCol);
        header.Add(heroRow);

        var statsRow = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginBottom = 4 } };
        statsRow.Add(MakeStatChip("STR", sheet.strength));
        statsRow.Add(MakeStatChip("AGI", sheet.agility));
        statsRow.Add(MakeStatChip("SPD", sheet.speed));
        statsRow.Add(MakeStatChip("INT", sheet.intellect));
        statsRow.Add(MakeStatChip("END", sheet.endurance));
        statsRow.Add(MakeStatChip("PER", sheet.perception));
        statsRow.Add(MakeStatChip("WIL", sheet.willpower));
        header.Add(statsRow);

        container.Add(header);

        AddArmorRow(container, sheet);

        // Derived combat
        var derivedRow = new VisualElement();
        derivedRow.style.flexDirection = FlexDirection.Row;
        derivedRow.style.flexWrap = Wrap.Wrap;
        derivedRow.style.marginBottom = 4;
        derivedRow.Add(MakeDerivedChip("Melee +" + sheet.GetMeleeDamageBonus()));
        derivedRow.Add(MakeDerivedChip("Crit " + sheet.GetCritChancePercent() + "%"));
        derivedRow.Add(MakeDerivedChip("Backstab +" + sheet.GetBackstabDamageBonus()));
        derivedRow.Add(MakeDerivedChip("Dodge +" + sheet.GetTotalGearDodgeBonus()));
        derivedRow.Add(MakeDerivedChip("Vision " + sheet.GetVisionRange()));
        container.Add(derivedRow);

        sheet.GetEquippedWeaponRangeSummaries(out var rangeLeft, out var rangeRight);
        var rangeRow = new VisualElement();
        rangeRow.style.flexDirection = FlexDirection.Row;
        rangeRow.style.flexWrap = Wrap.Wrap;
        rangeRow.style.marginBottom = 4;
        if (rangeRight.HasValue)
        {
            var rr = rangeRight.Value;
            rangeRow.Add(MakeDerivedChip($"Range {rr.label} {FormatRange(rr.minRange, rr.maxRange)}"));
        }
        if (rangeLeft.HasValue)
        {
            var rl = rangeLeft.Value;
            rangeRow.Add(MakeDerivedChip($"Range {rl.label} {FormatRange(rl.minRange, rl.maxRange)}"));
        }
        if (rangeRow.childCount > 0)
            container.Add(rangeRow);

        AddWeaponDamageRow(container, sheet, withAttackLabel: false);

        // Resistances (only nonzero; scrollable)
        var res = sheet.GetNonzeroResistances();
        if (res != null && res.Count > 0)
        {
            var resLabel = new Label("Resistances:");
            resLabel.style.marginTop = 6;
            resLabel.style.marginBottom = 2;
            container.Add(resLabel);
            var resScroll = new ScrollView(ScrollViewMode.Vertical);
            resScroll.name = ResistancesName;
            resScroll.style.maxHeight = 60;
            resScroll.style.marginBottom = 4;
            foreach (var kvp in res)
            {
                var line = new Label($"{kvp.Key}: {kvp.Value}%");
                line.style.color = DamageTypeColors.Get(kvp.Key);
                resScroll.Add(line);
            }
            container.Add(resScroll);
        }
    }

    private static void AddArmorRow(VisualElement container, CharacterSheet sheet)
    {
        var armorRow = new VisualElement();
        armorRow.style.flexDirection = FlexDirection.Row;
        armorRow.style.marginBottom = 4;
        armorRow.Add(new Label("Armor:") { name = "StatsArmorLabel" });
        var armorVal = new Label(sheet.GetTotalArmor().ToString());
        armorVal.name = ArmorLabelName;
        armorVal.style.marginLeft = 8;
        armorRow.Add(armorVal);
        container.Add(armorRow);
    }

    /// <summary>Weapon damage range chips per hand (same data as combat). Inventory tab can show an "Attack:" label.</summary>
    private static void AddWeaponDamageRow(VisualElement container, CharacterSheet sheet, bool withAttackLabel)
    {
        sheet.GetEquippedWeaponDamageSummary(out var left, out var right);
        var dmgRow = new VisualElement();
        dmgRow.style.flexDirection = FlexDirection.Row;
        dmgRow.style.flexWrap = Wrap.Wrap;
        dmgRow.style.alignItems = Align.Center;
        dmgRow.style.marginBottom = 4;
        dmgRow.name = DamageSummaryName;

        if (withAttackLabel)
        {
            var lab = new Label("Attack:");
            lab.AddToClassList("text-soft");
            dmgRow.Add(lab);
            var gap = new VisualElement();
            gap.style.width = 6;
            dmgRow.Add(gap);
        }

        var chips = new VisualElement();
        chips.style.flexDirection = FlexDirection.Row;
        chips.style.flexWrap = Wrap.Wrap;
        chips.style.alignItems = Align.Center;

        if (right.HasValue)
        {
            var r = right.Value;
            chips.Add(MakeDamageChip($"{r.minDamage}-{r.maxDamage} ({r.label})", r.damageType));
        }
        if (left.HasValue)
        {
            if (right.HasValue) chips.Add(new Label("  "));
            var l = left.Value;
            chips.Add(MakeDamageChip($"{l.minDamage}-{l.maxDamage} ({l.label})", l.damageType));
        }
        if (chips.childCount == 0)
            chips.Add(new Label("—"));

        dmgRow.Add(chips);
        container.Add(dmgRow);
    }

    private static string FormatRange(int minR, int maxR)
    {
        return minR == maxR ? $"{minR}" : $"{minR}–{maxR}";
    }

    private static Label MakeDerivedChip(string text)
    {
        var label = new Label(text);
        label.AddToClassList("text-soft");
        label.style.marginRight = 8;
        label.style.fontSize = 11;
        return label;
    }

    private static Label MakeDamageChip(string text, DamageType type)
    {
        var label = new Label(text);
        label.style.color = DamageTypeColors.Get(type);
        label.style.fontSize = 12;
        return label;
    }

    private static VisualElement MakeStatChip(string label, int value)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.marginRight = 6;
        row.style.marginBottom = 2;

        var name = new Label($"{label}:");
        name.AddToClassList("text-soft");
        var val = new Label(value.ToString());
        val.AddToClassList("text");
        val.style.marginLeft = 2;

        row.Add(name);
        row.Add(val);
        return row;
    }

    private static string FormatClassName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        // Strip leading "CLASS_" if present and replace underscores with spaces
        if (raw.StartsWith("CLASS_"))
            raw = raw.Substring("CLASS_".Length);
        raw = raw.Replace('_', ' ');
        return raw;
    }
}
