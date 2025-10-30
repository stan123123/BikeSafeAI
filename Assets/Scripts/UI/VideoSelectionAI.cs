using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class VideoSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button selectVideoButton;
    [SerializeField] private TMP_InputField frameInputField;

    // Events to broadcast actions
    public static event Action OnBackButtonClicked;
    public static event Action OnSelectVideoButtonClicked;
    public static event Action<int> OnFrameInputChanged;

    public ImageProcesser imageProcesserRef;

    private void OnEnable()
    {
        if (backButton != null) backButton.onClick.AddListener(BackClicked);
        if (selectVideoButton != null) selectVideoButton.onClick.AddListener(SelectVideoClicked);
        if (frameInputField != null) frameInputField.onEndEdit.AddListener(FrameInputEdited);
    }

    private void OnDisable()
    {
        if (backButton != null) backButton.onClick.RemoveListener(BackClicked);
        if (selectVideoButton != null) selectVideoButton.onClick.RemoveListener(SelectVideoClicked);
        if (frameInputField != null) frameInputField.onEndEdit.RemoveListener(FrameInputEdited);
    }

    private void BackClicked()
    {
        OnBackButtonClicked?.Invoke();

        UIManager.RequestUIChange(UIManager.UIType.SelectFile);
    }

    private void SelectVideoClicked()
    {
        OnSelectVideoButtonClicked?.Invoke();

        ImageProcesser.RequestProcessVideoFrames();
    }

    private void FrameInputEdited(string input)
    {
        if (float.TryParse(input, out float seconds))
        {
            imageProcesserRef.ChangeAmountOfImagesToProcessVideo(seconds.ToString());

            OnFrameInputChanged?.Invoke(Mathf.RoundToInt(seconds));
        }
        else
        {
            Debug.LogError($"Invalid frame input: '{input}' is not a valid number.");
        }
    }
}