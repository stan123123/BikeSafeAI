using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;


public class LabelDataFilterSetterManager : MonoBehaviour
{
    [SerializeField] private GameObject configurableLabelFilterElementParent;
    [SerializeField] private GameObject configurableLabelFilterElementPrefab;

    private List<GameObject> activeLabelFilterElements = new List<GameObject>();

    [SerializeField] private Button _continueButton;

    private void OnEnable()
    {
        DestroyLabelFilterElements();
        InitializeLabelFilterPrefabs();
        ClearLabelFilters();

        ConfigurableLabelFilterElement.OnAnyFilterChanged += UpdateLabelFilterData;

        CheckIfCanContinue();
    }

    private void OnDisable()
    {
        ConfigurableLabelFilterElement.OnAnyFilterChanged -= UpdateLabelFilterData;
    }

    private void InitializeLabelFilterPrefabs()
    {
        var config = ConfigLoader.LoadDefaultAnnotationConfig();

        foreach (var label in config.labels)
        {
            if (!ReviewUserSelectionManager.IncludedLabels.Contains(label.readable))
            {
                continue;
            }

            GameObject localGameObject = Instantiate(configurableLabelFilterElementPrefab, configurableLabelFilterElementParent.transform);
            var element = localGameObject.GetComponent<ConfigurableLabelFilterElement>();

            element.Initialize(label.readable);
            activeLabelFilterElements.Add(localGameObject);
        }
    }

    private void ClearLabelFilters()
    {
        ReviewUserSelectionManager.SetLabelFilters(new List<LabelFilterData>());
    }

    private void DestroyLabelFilterElements()
    {
        if (configurableLabelFilterElementParent == null) return;

        for (int i = configurableLabelFilterElementParent.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(configurableLabelFilterElementParent.transform.GetChild(i).gameObject);
        }

        activeLabelFilterElements.Clear();
    }

    private void UpdateLabelFilterData(LabelFilterData filterData)
    {
        var action = filterData.bIsIncludedInData
            ? (Action<LabelFilterData>)ReviewUserSelectionManager.AddLabelFilter
            : ReviewUserSelectionManager.RemoveLabelFilter;

        action(filterData);

        Debug.Log("Updating Label: " + filterData.nameReadable + "isIncluded: " + filterData.bIsIncludedInData);
        Debug.Log("Current total filters: " + ReviewUserSelectionManager.LabelFilters.Count);

        CheckIfCanContinue();
    }

    private void CheckIfCanContinue()
    {
        bool bCanContinue = ReviewUserSelectionManager.LabelFilters.Count > 0;

        _continueButton.interactable = bCanContinue;
    }
}
