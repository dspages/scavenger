using UnityEngine;

public class TooltipProvider : MonoBehaviour
{
    [Header("Tooltip Content")]
    [SerializeField] private string tooltipText = "";
    [SerializeField] private bool useDynamicText = false;
    
    [Header("Dynamic Text Settings")]
    [SerializeField] private string dynamicTextFormat = "";
    
    private TooltipManager tooltipManager;
    
    protected virtual void Start()
    {
        // Find the TooltipManager in the scene
        tooltipManager = FindObjectOfType<TooltipManager>();
        if (tooltipManager == null)
        {
            Debug.LogWarning("TooltipProvider: No TooltipManager found in scene!");
        }
    }
    
    public void OnMouseEnter()
    {
        if (tooltipManager != null)
        {
            tooltipManager.StartHover(gameObject);
        }
    }
    
    public void OnMouseExit()
    {
        if (tooltipManager != null)
        {
            tooltipManager.StopHover();
        }
    }
    
    public string GetTooltipText()
    {
        if (useDynamicText)
        {
            return GenerateDynamicText();
        }
        return tooltipText;
    }
    
    protected virtual string GenerateDynamicText()
    {
        // Override this method in derived classes for dynamic content
        return tooltipText;
    }
    
    // Public method to set tooltip text at runtime
    public void SetTooltipText(string text)
    {
        tooltipText = text;
    }
} 