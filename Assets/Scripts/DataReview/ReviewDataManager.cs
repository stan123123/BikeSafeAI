using UnityEngine;
using System;

/// <summary>
/// Currently acts as single source of truth/and manager of the higher level stuff when it comes to data preview
/// </summary>

public class ReviewDataManager : MonoBehaviour
{
    private string selectedDataPath; // path to the selected packaged data to review

    const int maxLabelCount = 20; // hard limit to different amount of labels we can have in the data preview, if selection exceeds this, higher indexes are cut off from the preview

    public static event Action<string> OnDataPathBroadcast;

    private LabelFilterData[] currentFilterSettings;

    private void OnEnable()
    {
        OnDataPathBroadcast += UpdateSelectedDataPath;

        DataFilterManager.OnLabelFilterUpdated += UpdateFilterSetting;
    }

    private void OnDisable()
    {
        OnDataPathBroadcast -= UpdateSelectedDataPath;

        DataFilterManager.OnLabelFilterUpdated -= UpdateFilterSetting;
    }

    private void UpdateSelectedDataPath(string newPath)
    {
        selectedDataPath = newPath;
        Debug.Log($"[ReviewDataManager] Selected data path updated to: {selectedDataPath}");
    }

    public static void BroadcastDataPath(string path)
    {
        OnDataPathBroadcast?.Invoke(path);
    }

    private void UpdateFilterSetting(int labelIndex, LabelFilterData newData)
    {
        if (currentFilterSettings == null || labelIndex < 0 || labelIndex >= currentFilterSettings.Length)
            return;

        currentFilterSettings[labelIndex] = newData;
        Debug.Log($"[ReviewDataManager] Filter for label {labelIndex} updated. Included={newData.bIsIncludedInData}, Accuracy={newData.percentageAccuracyFilter}");
    }
}
