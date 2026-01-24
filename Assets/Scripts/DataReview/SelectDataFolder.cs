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
        selectedFolderText.text = "";
        checkIfFolderIsSelected();
    }

    private void OnEnable()
    {
        selectFolderButton.onClick.AddListener(OnSelectFolderButtonPressed);

        checkIfFolderIsSelected();
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

            ReviewUserSelectionManager.SetPath(localSelectedDataPath);

            OnFolderSelected?.Invoke();

            Debug.Log($"Folder selected: {localSelectedDataPath}");
        }
        else
        {
            localSelectedDataPath = null;
            selectedFolderText.text = "";
            Debug.Log("No folder selected.");
        }

        checkIfFolderIsSelected();
    }

    private void checkIfFolderIsSelected()
    {
        bool bButtonShouldBeEnabled = ReviewUserSelectionManager.SelectedPath != null && ReviewUserSelectionManager.SelectedPath != "";

        continueButton.enabled = bButtonShouldBeEnabled;
    }
}