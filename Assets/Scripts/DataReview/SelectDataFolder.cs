using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using SFB;

public class SelectDataFolder : MonoBehaviour
{
    private string localSelectedDataPath = null;

    [SerializeField] private Button selectFolderButton;
    [SerializeField] private Button continueButton;

    [SerializeField] private TMP_Text selectedFolderText;

    public static event Action OnFolderSelected;

    void Start()
    {
        continueButton.interactable = false;
        selectedFolderText.text = "";
    }

    private void OnEnable()
    {
        selectFolderButton.onClick.AddListener(OnSelectFolderButtonPressed);
    }

    private void OnDisable()
    {
        selectFolderButton.onClick.RemoveListener(OnSelectFolderButtonPressed);
    }

    private void OnSelectFolderButtonPressed()
    {
        string[] folderPaths = StandaloneFileBrowser.OpenFolderPanel("Select a folder", "", false);

        if (folderPaths != null && folderPaths.Length > 0 && !string.IsNullOrEmpty(folderPaths[0]))
        {
            localSelectedDataPath = folderPaths[0];

            selectedFolderText.text = localSelectedDataPath;

            continueButton.interactable = true;

            OnFolderSelected?.Invoke();

            ReviewDataManager.BroadcastDataPath(localSelectedDataPath);

            Debug.Log($"Folder selected: {localSelectedDataPath}");
        }
        else
        {
            localSelectedDataPath = null;
            selectedFolderText.text = "";
            continueButton.interactable = false;
            Debug.Log("No folder selected.");
        }
    }
}