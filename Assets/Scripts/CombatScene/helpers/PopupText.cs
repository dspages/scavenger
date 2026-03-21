using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PopupText : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private Component textComponent;
    [Header("Visuals")]
    [SerializeField] private float fontSize = 26f;
    [SerializeField] private FontWeight fontWeight = FontWeight.Black;
    private const float DefaultLifetime = 1f;

    void Awake()
    {
        // Prefer TextMeshPro (better quality); fall back to legacy UI Text
        var tmp = GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            textComponent = tmp;

            // Ensure the popup isn't using an overly-large/thin default.
            // This is especially important after Unity upgrades that may reset TMP defaults.
            tmp.fontSize = fontSize;

            // Some prefab setups may not have an explicit TMP font asset assigned.
            // In that case, forcing a specific font weight can result in missing glyphs (invisible text).
            // Only override fontWeight when we have a font asset to back it up.
            if (tmp.font != null)
                tmp.fontWeight = fontWeight;

            // Force correct centering regardless of string width (e.g. "Crit 8" vs "5").
            tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
            tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
            tmp.alignment = TextAlignmentOptions.Center;
        }
        else
        {
            var legacy = GetComponentInChildren<Text>(true);
            textComponent = legacy ?? GetComponent<Text>();
        }

        float lifetime = DefaultLifetime;
        if (animator != null)
        {
            var clipInfo = animator.GetCurrentAnimatorClipInfo(0);
            if (clipInfo != null && clipInfo.Length > 0 && clipInfo[0].clip != null)
                lifetime = clipInfo[0].clip.length;
        }
        Destroy(gameObject, lifetime);
    }

    public void SetText(string text)
    {
        if (textComponent == null) return;
        if (textComponent is TextMeshProUGUI tmp)
            tmp.text = text;
        else if (textComponent is Text legacy)
            legacy.text = text;
    }

    public void SetColor(Color color)
    {
        if (textComponent == null) return;
        if (textComponent is TextMeshProUGUI tmp)
            tmp.color = color;
        else if (textComponent is Text legacy)
            legacy.color = color;
    }
}
