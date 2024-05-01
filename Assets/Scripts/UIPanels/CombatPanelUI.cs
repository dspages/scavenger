using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using UnityEngine.UIElements;

public class CombatPanelUI : MonoBehaviour
{
    public VisualTreeAsset CombatMenuTemplate;
    public StyleSheet CombatMenuStyles;

    private Button endTurnButton;

    private void Start()
    {
        // Load the UXML template and style sheet
        if (CombatMenuTemplate == null)
        {
            Debug.LogError("MainMenuTemplate is not assigned in the Unity Editor.");
            return;
        }

        VisualElement visualTree = CombatMenuTemplate.CloneTree();

        if (CombatMenuStyles != null)
        {
            visualTree.styleSheets.Add(CombatMenuStyles);
        }

        // Get references to the UI elements
        endTurnButton = visualTree.Q<Button>("EndTurnButton");

        // Add event listeners to the buttons
        endTurnButton.clicked += OnEndTurnButtonClicked;

        // Add the UI elements to the root visual element
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        if (root != null)
        {
            root.Add(visualTree);
        }
        else
        {
            Debug.LogError("Root visual element is null. Make sure the UIDocument component is attached to the same GameObject as this script.");
        }
    }

    private void OnEndTurnButtonClicked()
    {
        // Add code here to handle the load game button click
        Debug.Log("End turn button clicked");

        PlayerController[] pcs = FindObjectsOfType<PlayerController>();
        foreach (PlayerController pc in pcs)
        {
            // The controller is responsible for only ending turn if the click is valid.
            pc.EndTurnButtonClick();
        }
    }
}
