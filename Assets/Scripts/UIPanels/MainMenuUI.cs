using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using UnityEngine.UIElements;

public class MainMenuUI : MonoBehaviour
{
    public VisualTreeAsset MainMenuTemplate;
    public StyleSheet MainMenuStyles;

    private Button newGameButton;
    private Button loadGameButton;
    private Button optionsButton;
    private Button quitButton;

    private VisualElement optionsPanel;
    private Slider tooltipDelaySlider;
    private Toggle tooltipNeverHoverToggle;
    private Label tooltipDelayValueLabel;
    private Button optionsCloseButton;

    private void Start()
    {
        // Load the UXML template and style sheet
        if (MainMenuTemplate == null)
        {
            Debug.LogError("MainMenuTemplate is not assigned in the Unity Editor.");
            return;
        }

        VisualElement visualTree = MainMenuTemplate.CloneTree();

        if (MainMenuStyles != null)
        {
            visualTree.styleSheets.Add(MainMenuStyles);
        }

        // Get references to the UI elements
        newGameButton = visualTree.Q<Button>("NewGameButton");
        loadGameButton = visualTree.Q<Button>("LoadGameButton");
        optionsButton = visualTree.Q<Button>("OptionsButton");
        quitButton = visualTree.Q<Button>("QuitButton");

        // Add event listeners to the buttons
        newGameButton.clicked += OnNewGameButtonClicked;
        loadGameButton.clicked += OnLoadGameButtonClicked;
        if (optionsButton != null) optionsButton.clicked += ToggleOptionsPanel;
        quitButton.clicked += OnQuitButtonClicked;

        // Options controls
        optionsPanel = visualTree.Q<VisualElement>("OptionsPanel");
        tooltipDelaySlider = visualTree.Q<Slider>("TooltipDelaySlider");
        tooltipNeverHoverToggle = visualTree.Q<Toggle>("TooltipNeverHoverToggle");
        tooltipDelayValueLabel = visualTree.Q<Label>("TooltipDelayValueLabel");
        optionsCloseButton = visualTree.Q<Button>("OptionsCloseButton");

        if (optionsCloseButton != null) optionsCloseButton.clicked += HideOptionsPanel;

        if (tooltipDelaySlider != null)
        {
            tooltipDelaySlider.lowValue = 0.2f;
            tooltipDelaySlider.highValue = 2.0f;
            tooltipDelaySlider.value = TooltipDetailSettings.DetailDelaySeconds;
            tooltipDelaySlider.RegisterValueChangedCallback(evt =>
            {
                TooltipDetailSettings.DetailDelaySeconds = evt.newValue;
                TooltipDetailSettings.Save();
                UpdateTooltipDelayLabel();
            });
        }
        if (tooltipNeverHoverToggle != null)
        {
            tooltipNeverHoverToggle.value = TooltipDetailSettings.NeverExpandOnHover;
            tooltipNeverHoverToggle.RegisterValueChangedCallback(evt =>
            {
                TooltipDetailSettings.NeverExpandOnHover = evt.newValue;
                TooltipDetailSettings.Save();
            });
        }
        UpdateTooltipDelayLabel();
        HideOptionsPanel();

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

    private void OnNewGameButtonClicked()
    {
        PlayerParty.Reset();
        // Add code here to handle the new game button click
        SceneManager.LoadScene("CombatScene");
    }

    private void OnLoadGameButtonClicked()
    {
        // Add code here to handle the load game button click
        Debug.Log("Load game button clicked");
    }

    private void OnQuitButtonClicked()
    {
        // Add code here to handle the quit button click
        Application.Quit();
    }

    private void ToggleOptionsPanel()
    {
        if (optionsPanel == null) return;
        bool showing = optionsPanel.style.display == DisplayStyle.Flex;
        optionsPanel.style.display = showing ? DisplayStyle.None : DisplayStyle.Flex;
        if (!showing)
        {
            if (tooltipDelaySlider != null) tooltipDelaySlider.value = TooltipDetailSettings.DetailDelaySeconds;
            if (tooltipNeverHoverToggle != null) tooltipNeverHoverToggle.value = TooltipDetailSettings.NeverExpandOnHover;
            UpdateTooltipDelayLabel();
        }
    }

    private void HideOptionsPanel()
    {
        if (optionsPanel == null) return;
        optionsPanel.style.display = DisplayStyle.None;
    }

    private void UpdateTooltipDelayLabel()
    {
        if (tooltipDelayValueLabel == null) return;
        tooltipDelayValueLabel.text = $"{TooltipDetailSettings.DetailDelaySeconds:0.0}s";
    }
}