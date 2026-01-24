using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class ConfigurableLabelFilterElement : MonoBehaviour
{
    [SerializeField] private TMP_InputField minimumObjectsInput;
    [SerializeField] private TMP_InputField maximumObjectsInput;
    [SerializeField] private TMP_InputField desiredAccuracyInput;
    [SerializeField] private Toggle filterIsSelectedInput;
    [SerializeField] private TMP_Text name;

    private LabelFilterData filterData = new LabelFilterData("", false, 0f, 0, 0);

    public static event Action<LabelFilterData> OnAnyFilterChanged;

    private void OnEnable()
    {
        minimumObjectsInput.onValueChanged.AddListener(OnMinObjectsInputChanged);
        maximumObjectsInput.onValueChanged.AddListener(OnMaxObjectsInputChanged);
        desiredAccuracyInput.onValueChanged.AddListener(OnDesiredAccuracyInputChanged);
        filterIsSelectedInput.onValueChanged.AddListener(OnFilterIsSelectedInputChanged);
    }

    private void OnDisable()
    {
        minimumObjectsInput.onValueChanged.RemoveListener(OnMinObjectsInputChanged);
        maximumObjectsInput.onValueChanged.RemoveListener(OnMaxObjectsInputChanged);
        desiredAccuracyInput.onValueChanged.RemoveListener(OnDesiredAccuracyInputChanged);
        filterIsSelectedInput.onValueChanged.RemoveListener(OnFilterIsSelectedInputChanged);
    }

    public void Initialize(string nameReadable)
    {
        name.text = nameReadable;
        filterIsSelectedInput.SetIsOnWithoutNotify(false);
    }

    private void OnMinObjectsInputChanged(string minObjects)
    {
        SubmitFilterDetails();

        filterIsSelectedInput.isOn = true;
    }

    private void OnMaxObjectsInputChanged(string maxObjects)
    {
        SubmitFilterDetails();

        filterIsSelectedInput.isOn = true;
    }

    private void OnDesiredAccuracyInputChanged(string accuracy)
    {
        SubmitFilterDetails();

        filterIsSelectedInput.isOn = true;
    }

    private void OnFilterIsSelectedInputChanged(bool bIsEnabled)
    {
        SubmitFilterDetails();
    }

    private void SubmitFilterDetails()
    {
        float accuracy = 0f;
        int minObjects = 0;
        int maxObjects = 0;

        float.TryParse(desiredAccuracyInput.text, out accuracy);
        int.TryParse(minimumObjectsInput.text, out minObjects);
        int.TryParse(maximumObjectsInput.text, out maxObjects);

        filterData = new LabelFilterData
        (
            name.text,
            filterIsSelectedInput.isOn,
            accuracy,
            minObjects,
            maxObjects
        );

        OnAnyFilterChanged?.Invoke(filterData);
    }
}
