using UnityEngine;
using TMPro;

public class LabelFilterUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI HeaderText;

    private int labelIndex;
    private LabelData labelData;

    public static LabelFilterChangeEvent OnFilterChanged = new LabelFilterChangeEvent();

    private LabelFilterData currentFilterData;

    /// <summary>
    /// Initializes the UI element with label data
    /// </summary>
    public void InitializeElement(LabelData data, int index)
    {
        labelData = data;
        labelIndex = index;

        if (HeaderText != null)
        {
            HeaderText.text = data.readable;
        }
        else
        {
            Debug.LogWarning($"HeaderText not assigned for label: {data.readable}");
        }
    }

    /// <summary>
    /// Backward compatibility - auto-assigns index as -1 if not provided
    /// </summary>
    public void InitializeElement(LabelData data)
    {
        InitializeElement(data, -1);
    }

    /// <summary>
    /// Gets the label index this UI element represents
    /// </summary>
    public int GetLabelIndex()
    {
        return labelIndex;
    }

    /// <summary>
    /// Gets the label data for this UI element
    /// </summary>
    public LabelData GetLabelData()
    {
        return labelData;
    }

    public void SetDesiredAccuracy(string newDesiredAccuracy)
    {
        if (float.TryParse(newDesiredAccuracy, out float value))
        {
            currentFilterData.percentageAccuracyFilter = Mathf.Clamp01(value);
            OnFilterChanged.Invoke(labelIndex, currentFilterData);
        }
    }

    public void setMinAmountOfObjects(string newAmount)
    {
        if (int.TryParse(newAmount, out int value))
        {
            currentFilterData.minAmountOfObjects = Mathf.Max(0, value);
            OnFilterChanged.Invoke(labelIndex, currentFilterData);
        }
    }

    public void setMaxAmountOfObjects(string newAmount)
    {
        if (int.TryParse(newAmount, out int value))
        {
            currentFilterData.maxAmountOfObjects = Mathf.Max(0, value);
            OnFilterChanged.Invoke(labelIndex, currentFilterData);
        }
    }

    public void setShouldInclude(bool shouldInclude)
    {
        currentFilterData.bIsIncludedInData = shouldInclude;
        OnFilterChanged.Invoke(labelIndex, currentFilterData);
    }
}