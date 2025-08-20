using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TooltipManager : MonoBehaviour
{
    [Header("Tooltip Settings")]
    [SerializeField] private float hoverDelay = 0.1f; // 100ms delay
    [SerializeField] private Vector2 tooltipOffset = new Vector2(10, 10);
    [SerializeField] private float maxTooltipWidth = 300f;
    
    [Header("UI References")]
    [SerializeField] private VisualTreeAsset tooltipTemplate;
    [SerializeField] private StyleSheet tooltipStyles;
    
    private VisualElement tooltipElement;
    private Label tooltipLabel;
    private bool isTooltipVisible = false;
    private Coroutine hoverCoroutine;
    private GameObject currentHoveredObject;
    
    private void Start()
    {
        CreateTooltipUI();
    }
    
    private void CreateTooltipUI()
    {
        // Create tooltip element if no template is provided
        if (tooltipTemplate == null)
        {
            tooltipElement = new VisualElement();
            tooltipElement.AddToClassList("ui-tooltip");
            tooltipElement.style.maxWidth = maxTooltipWidth;
            tooltipElement.style.display = DisplayStyle.None;

            tooltipLabel = new Label();
            tooltipLabel.AddToClassList("ui-tooltip__label");
            tooltipElement.Add(tooltipLabel);
        }
        else
        {
            tooltipElement = tooltipTemplate.CloneTree();
            tooltipLabel = tooltipElement.Q<Label>("TooltipLabel");
            if (tooltipLabel == null)
            {
                tooltipLabel = new Label();
                tooltipElement.Add(tooltipLabel);
            }
            
            // Ensure the tooltip starts hidden
            tooltipElement.style.display = DisplayStyle.None;
            
            // Ensure display hidden until shown
            tooltipElement.style.display = DisplayStyle.None;
            
            // Attach theme styles if available
            if (tooltipStyles != null)
            {
                tooltipElement.styleSheets.Add(tooltipStyles);
            }
        }
        
        // Add tooltip to the root visual element
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        if (root != null)
        {
            root.Add(tooltipElement);
        }
    }
    
    public void StartHover(GameObject hoveredObject)
    {
        if (hoveredObject == currentHoveredObject && isTooltipVisible)
        {
            return;
        }
            
        StopHover();
        
        currentHoveredObject = hoveredObject;
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
        }
        
        hoverCoroutine = StartCoroutine(HoverDelay());
    }
    
    public void StopHover()
    {
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
            hoverCoroutine = null;
        }
        
        HideTooltip();
        currentHoveredObject = null;
    }
    
    private IEnumerator HoverDelay()
    {
        yield return new WaitForSeconds(hoverDelay);
        
        // Check if we're still hovering the same object
        if (currentHoveredObject != null)
        {
            ShowTooltip();
        }
    }
    
    private void ShowTooltip()
    {
        if (tooltipElement == null || tooltipLabel == null || currentHoveredObject == null)
        {
            return;
        }
        
        // Get the tooltip text from the current hovered object
        string tooltipText = GetTooltipTextFromObject(currentHoveredObject);
        
        if (string.IsNullOrEmpty(tooltipText))
        {
            return;
        }
        
        tooltipLabel.text = tooltipText;
        tooltipElement.style.display = DisplayStyle.Flex;
        isTooltipVisible = true;
        
        // Position tooltip near mouse
        Vector2 mousePos = Input.mousePosition;
        Vector2 screenPos = new Vector2(mousePos.x + tooltipOffset.x, mousePos.y + tooltipOffset.y);
        
        // Convert screen position to UI position
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        if (root != null)
        {
            Vector2 uiPos = RuntimePanelUtils.ScreenToPanel(root.panel, screenPos);
            // Fix Y-axis inversion by subtracting from screen height
            uiPos.y = Screen.height - uiPos.y;
            tooltipElement.style.left = uiPos.x;
            tooltipElement.style.top = uiPos.y;
        }
    }
    
    private string GetTooltipTextFromObject(GameObject obj)
    {
        // Try to get tooltip text from TooltipProvider components
        TooltipProvider[] providers = obj.GetComponents<TooltipProvider>();
        foreach (TooltipProvider provider in providers)
        {
            string text = provider.GetTooltipText();
            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }
        }
        
        return "";
    }
    
    private void HideTooltip()
    {
        if (tooltipElement != null)
        {
            tooltipElement.style.display = DisplayStyle.None;
        }
        isTooltipVisible = false;
    }
    
    private void Update()
    {
        // Update tooltip position if visible
        if (isTooltipVisible && tooltipElement != null)
        {
            Vector2 mousePos = Input.mousePosition;
            Vector2 screenPos = new Vector2(mousePos.x + tooltipOffset.x, mousePos.y + tooltipOffset.y);
            
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            if (root != null)
            {
                Vector2 uiPos = RuntimePanelUtils.ScreenToPanel(root.panel, screenPos);
                // Fix Y-axis inversion by subtracting from screen height
                uiPos.y = Screen.height - uiPos.y;
                tooltipElement.style.left = uiPos.x;
                tooltipElement.style.top = uiPos.y;
            }
        }
    }
} 