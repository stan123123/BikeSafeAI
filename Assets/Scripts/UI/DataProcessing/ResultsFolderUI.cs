using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Diagnostics;
using System.IO;

public class ResultsFolderUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI resultsPathText;
    [SerializeField] private Button openFolderButton;

    private void Start()
    {
        // Ensure folder exists (PathConfig handles directory creation)
        if (!Directory.Exists(PathConfig.AnnotatedImagesFolder))
            Directory.CreateDirectory(PathConfig.AnnotatedImagesFolder);

        // Display path in TMP text
        if (resultsPathText != null)
            resultsPathText.text = PathConfig.AnnotatedImagesFolder;

        // Set up button listener
        if (openFolderButton != null)
            openFolderButton.onClick.AddListener(OpenResultsFolder);
    }

    private void OpenResultsFolder()
    {
        if (Directory.Exists(PathConfig.AnnotatedImagesFolder))
        {
            // Open folder in OS file explorer
            Process.Start(new ProcessStartInfo()
            {
                FileName = PathConfig.AnnotatedImagesFolder,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        else
        {
            UnityEngine.Debug.LogWarning("Results folder does not exist: " + PathConfig.AnnotatedImagesFolder);
        }
    }
}