using UnityEngine;
using System.IO;

public class StartupTempCleaner : MonoBehaviour
{
    void Start()
    {
        CleanTempFolders();
    }

    private void CleanTempFolders()
    {
        CleanDirectory(PathConfig.UsedImagesFolder);
        CleanDirectory(PathConfig.AnnotatedImagesFolder);
    }

    private void CleanDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
                Debug.Log($"[StartupTempCleaner] Cleaned: {path}");
            }

            // Recreate the empty directory
            Directory.CreateDirectory(path);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StartupTempCleaner] Failed to clean {path}: {e.Message}");
        }
    }
}