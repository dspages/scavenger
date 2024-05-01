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
    private Button quitButton;

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
        quitButton = visualTree.Q<Button>("QuitButton");

        // Add event listeners to the buttons
        newGameButton.clicked += OnNewGameButtonClicked;
        loadGameButton.clicked += OnLoadGameButtonClicked;
        quitButton.clicked += OnQuitButtonClicked;

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
}