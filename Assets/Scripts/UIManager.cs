using UnityEngine;
using System;

public class UIManager : MonoBehaviour
{
    // Enum for UI types
    public enum UIType
    {
        SelectFile,
        ProcessingImages,
        DoneProcessing,
        SelectAiState,
        SelectVideo   // <-- new UI state
    }

    [Header("Assign Canvases in Editor")]
    public Canvas[] canvases; // Assign 5 canvases in inspector now

    // Event you can subscribe to from other scripts
    public static event Action<UIType> OnUIChangeRequested;

    private void OnEnable()
    {
        // Subscribe to the event
        OnUIChangeRequested += SwitchUI;
    }

    private void OnDisable()
    {
        OnUIChangeRequested -= SwitchUI;
    }

    // Switches canvases based on enum
    private void SwitchUI(UIType type)
    {
        // Disable all canvases first
        for (int i = 0; i < canvases.Length; i++)
        {
            canvases[i].gameObject.SetActive(false);
        }

        // Enable the selected canvas
        switch (type)
        {
            case UIType.SelectFile:
                canvases[0].gameObject.SetActive(true);
                break;
            case UIType.ProcessingImages:
                canvases[1].gameObject.SetActive(true);
                break;
            case UIType.DoneProcessing:
                canvases[2].gameObject.SetActive(true);
                break;
            case UIType.SelectAiState:
                canvases[3].gameObject.SetActive(true);
                break;
            case UIType.SelectVideo:  // <-- new case
                canvases[4].gameObject.SetActive(true);
                break;
        }
    }

    public static void RequestUIChange(UIType type)
    {
        OnUIChangeRequested?.Invoke(type);
    }
}