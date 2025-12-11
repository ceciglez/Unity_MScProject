using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// TextMeshPro-compatible version of BiodiversityUI
/// This is a helper script that adds TMP support to the BiodiversityUI class
/// </summary>
public static class BiodiversityUI_TMP_Helper
{
    // Get text from either Legacy or TMP input field
    public static string GetInputText(InputField legacyInput, TMP_InputField tmpInput)
    {
        if (tmpInput != null)
            return tmpInput.text;
        if (legacyInput != null)
            return legacyInput.text;
        return "";
    }

    // Check if input field is null (both types)
    public static bool IsInputNull(InputField legacyInput, TMP_InputField tmpInput)
    {
        return legacyInput == null && tmpInput == null;
    }

    // Activate input field (both types)
    public static void ActivateInput(InputField legacyInput, TMP_InputField tmpInput)
    {
        if (tmpInput != null)
        {
            tmpInput.ActivateInputField();
            tmpInput.Select();
        }
        else if (legacyInput != null)
        {
            legacyInput.ActivateInputField();
            legacyInput.Select();
        }
    }

    // Deactivate input field (both types)
    public static void DeactivateInput(InputField legacyInput, TMP_InputField tmpInput)
    {
        if (tmpInput != null)
            tmpInput.DeactivateInputField();
        else if (legacyInput != null)
            legacyInput.DeactivateInputField();
    }

    // Get input field GameObject (both types)
    public static GameObject GetInputGameObject(InputField legacyInput, TMP_InputField tmpInput)
    {
        if (tmpInput != null)
            return tmpInput.gameObject;
        if (legacyInput != null)
            return legacyInput.gameObject;
        return null;
    }

    // Set status text (both types)
    public static void SetStatusText(Text legacyText, TMP_Text tmpText, string message)
    {
        if (tmpText != null)
            tmpText.text = message;
        else if (legacyText != null)
            legacyText.text = message;
    }

    // Get status text (both types)
    public static string GetStatusText(Text legacyText, TMP_Text tmpText)
    {
        if (tmpText != null)
            return tmpText.text;
        if (legacyText != null)
            return legacyText.text;
        return "";
    }

    // Check if input field is interactable (both types)
    public static bool IsInteractable(InputField legacyInput, TMP_InputField tmpInput)
    {
        if (tmpInput != null)
            return tmpInput.interactable;
        if (legacyInput != null)
            return legacyInput.interactable;
        return false;
    }

    // Check if input field is enabled (both types)
    public static bool IsEnabled(InputField legacyInput, TMP_InputField tmpInput)
    {
        if (tmpInput != null)
            return tmpInput.enabled;
        if (legacyInput != null)
            return legacyInput.enabled;
        return false;
    }

    // Get input field name (both types)
    public static string GetInputName(InputField legacyInput, TMP_InputField tmpInput)
    {
        if (tmpInput != null)
            return tmpInput.name;
        if (legacyInput != null)
            return legacyInput.name;
        return "null";
    }
}
