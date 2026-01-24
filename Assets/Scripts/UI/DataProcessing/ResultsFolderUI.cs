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
    [SerializeField] private Button toMenuButton;

    private const string MenuSceneName = "MainMenu";

    private void OnEnable()
    {
        // Ensure folder exists (PathConfig handles directory creation)
        if (!Directory.Exists(PathConfig.PackagedDataFolder))
            Directory.CreateDirectory(PathConfig.PackagedDataFolder);

        // Display path in TMP text
        if (resultsPathText != null)
            resultsPathText.text = PathConfig.PackagedDataFolder;

        // Set up button listener
        if (openFolderButton != null)
            openFolderButton.onClick.AddListener(OpenResultsFolder);

        if(toMenuButton != null)
        {
            toMenuButton.onClick.AddListener(ToMenu);
        }
    }

    private void OnDisable()
    {
        // Set up button listener
        if (openFolderButton != null)
            openFolderButton.onClick.RemoveListener(OpenResultsFolder);

        if (toMenuButton != null)
        {
            toMenuButton.onClick.RemoveListener(ToMenu);
        }
    }

    private void OpenResultsFolder()
    {
        if (Directory.Exists(PathConfig.PackagedDataFolder))
        {
            // Open folder in OS file explorer
            Process.Start(new ProcessStartInfo()
            {
                FileName = PathConfig.PackagedDataFolder,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        else
        {
            UnityEngine.Debug.LogWarning("Results folder does not exist: " + PathConfig.PackagedDataFolder);
        }
    }

    private void ToMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(MenuSceneName);
    }
}