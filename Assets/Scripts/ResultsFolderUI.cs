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

    private string resultsFolderPath;

    private void Start()
    {
        // Formulate the absolute path to the Results folder, relative to project root
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        resultsFolderPath = Path.Combine(root, "Output", "Results");

        // Ensure folder exists
        if (!Directory.Exists(resultsFolderPath))
            Directory.CreateDirectory(resultsFolderPath);

        // Display path in TMP text
        if (resultsPathText != null)
            resultsPathText.text = resultsFolderPath;

        // Set up button listener
        if (openFolderButton != null)
            openFolderButton.onClick.AddListener(OpenResultsFolder);
    }

    private void OpenResultsFolder()
    {
        if (Directory.Exists(resultsFolderPath))
        {
            // Open folder in OS file explorer
            Process.Start(new ProcessStartInfo()
            {
                FileName = resultsFolderPath,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        else
        {
            UnityEngine.Debug.LogWarning("Results folder does not exist: " + resultsFolderPath);
        }
    }
}