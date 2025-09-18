﻿using UnityEditor;
using UnityEngine;

public class PopupTextController : MonoBehaviour
{
    private static PopupText popupTextPrefab;
    private static string canvasName = "Canvas";
    private static GameObject canvas;
    private static PopupTextController runner;

    private static void EnsureInitialized()
    {
        if (runner == null)
        {
            var go = GameObject.Find("PopupTextControllerRunner");
            if (go == null)
            {
                go = new GameObject("PopupTextControllerRunner");
                runner = go.AddComponent<PopupTextController>();
                DontDestroyOnLoad(go);
            }
            else
            {
                runner = go.GetComponent<PopupTextController>();
                if (runner == null)
                {
                    runner = go.AddComponent<PopupTextController>();
                }
            }
        }
    }

    public static void CreatePopupText(string text, Transform transform)
    {
        PopupText instance = Instantiate(popupTextPrefab);
        Vector2 screenPosition = Camera.main.WorldToScreenPoint(transform.position);

        instance.transform.SetParent(canvas.transform, false);
        instance.transform.position = screenPosition;
        instance.SetText(text);
    }

    public static void CreatePopupTextAfterDelay(string text, Transform transform, float delay)
    {
        EnsureInitialized();
        runner.StartCoroutine(runner.RunAfterDelay(text, transform, delay));
    }

    private System.Collections.IEnumerator RunAfterDelay(string text, Transform transform, float delay)
    {
        yield return new WaitForSeconds(delay);
        CreatePopupText(text, transform);
    }
}