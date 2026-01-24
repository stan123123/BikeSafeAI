using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using SFB;

public class VideoSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button _selectVideoButton;
    [SerializeField] private TMP_InputField _frameInputField;
    [SerializeField] private TMP_Text _selectedVideoPathText;
    [SerializeField] private TMP_InputField _folderNameInputField;
    [SerializeField] private Button _continueButton;

    public static event Action OnStartProcessing;

    private bool _hasSelectedValidVideoPath = false;

    // Events to broadcast stuff we change for other scripts to listen to

    public ImageProcesser imageProcesserRef;

    private string nameForNewPackagedDataFolder = null;

    private void OnEnable()
    {
        if (_selectVideoButton != null) _selectVideoButton.onClick.AddListener(SelectVideoClicked);
        if (_frameInputField != null) _frameInputField.onEndEdit.AddListener(FrameInputEdited);
        if (_folderNameInputField != null) _folderNameInputField.onEndEdit.AddListener(NameInputEdited);
        if (_continueButton != null) _continueButton.onClick.AddListener(OnContinueButtonPressed);

        // nullifying user inputs in case the user wants to process more data again.
        _hasSelectedValidVideoPath = false;
        nameForNewPackagedDataFolder = null;

        // will always set interactable to be false, but i think its nice this way.
        CheckCanProceed();
    }

    private void OnDisable()
    {
        if (_selectVideoButton != null) _selectVideoButton.onClick.RemoveListener(SelectVideoClicked);
        if (_frameInputField != null) _frameInputField.onEndEdit.RemoveListener(FrameInputEdited);
        if (_folderNameInputField != null) _folderNameInputField.onEndEdit.RemoveListener(NameInputEdited);
        if (_continueButton != null) _continueButton.onClick.RemoveListener(OnContinueButtonPressed);
    }

    private void SelectVideoClicked()
    {
        //ImageProcesser.RequestProcessVideoFrames();

        var extensions = new[] { new SFB.ExtensionFilter("Video Files", "mp4", "mov", "avi") };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select a video", "", extensions, false);
        if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
            return;

        string videoPath = paths[0];

        _selectedVideoPathText.text = videoPath;
        _hasSelectedValidVideoPath = true;

        CheckCanProceed();

        ProcessingUserSelectionManager.SetVideoPath(videoPath);
    }

    private void FrameInputEdited(string input)
    {
        if (float.TryParse(input, out float seconds))
        {
            CheckCanProceed();

            ProcessingUserSelectionManager.SetDeltaSeconds(seconds);
        }
        else
        {
            Debug.LogError($"Invalid frame input: '{input}' is not a valid number.");
        }
    }

    private void NameInputEdited(string input)
    {
        if(!ProcessedFolderNameValidator.IsFolderNameAvailable(input))
        {
            Debug.Log("Filled in name invalid: " + input);
            return;
        }

        nameForNewPackagedDataFolder = input;

        CheckCanProceed();

        ProcessingUserSelectionManager.SetVideoName(input);
    }

    private void CheckCanProceed()
    {
        if (nameForNewPackagedDataFolder == null)
        {
            SetContinueButtonInteractability(false);
            return;
        }

        if (!_hasSelectedValidVideoPath)
        {
            SetContinueButtonInteractability(false);
            return;
        }

        SetContinueButtonInteractability(true);
    }

    private void SetContinueButtonInteractability (bool isInteractable)
    {
        _continueButton.interactable = isInteractable;  
    }

    private void OnContinueButtonPressed()
    {
        OnStartProcessing.Invoke();
    }
}