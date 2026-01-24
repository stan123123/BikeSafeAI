using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class SelectLabels : MonoBehaviour
{
    [SerializeField] private GameObject toglableLablelPrefab;
    [SerializeField] private GameObject toglableLablelParent;

    private List<TogglableLabelElement> spawnedElements = new List<TogglableLabelElement>();

    public static event Action<string> OnLabelAdded;
    public static event Action<string> OnLabelRemoved;

    public static void BroadcastAdd(string label) => OnLabelAdded?.Invoke(label);
    public static void BroadcastRemove(string label) => OnLabelRemoved?.Invoke(label);

    [SerializeField] private Button continueButton;
    [SerializeField] private Button selectAllButton;
    [SerializeField] private Button clearAllButton;

    private void Start()
    {
        InitializeUI();

        // Setup button listeners
        selectAllButton.onClick.AddListener(() => SetAllElements(true));
        clearAllButton.onClick.AddListener(() => SetAllElements(false));
    }

    private void InitializeUI()
    {
        var config = ConfigLoader.LoadDefaultAnnotationConfig();

        foreach (var label in config.labels)
        {
            GameObject go = Instantiate(toglableLablelPrefab, toglableLablelParent.transform);
            var element = go.GetComponent<TogglableLabelElement>();

            element.Initialize(label.readable, false);
            spawnedElements.Add(element);
        }
    }

    private void SetAllElements(bool shouldBeEnabled)
    {
        foreach (var element in spawnedElements)
        {
            element.OverrideSetEnabled(shouldBeEnabled);
        }
    }

    private void OnEnable()
    {
        OnLabelAdded += HandleAddRequest;
        OnLabelRemoved += HandleRemoveRequest;

        CheckIfFiltersArentEmpty();
    }

    private void OnDisable()
    {
        OnLabelAdded -= HandleAddRequest;
        OnLabelRemoved -= HandleRemoveRequest;
    }

    private void HandleAddRequest(string element)
    {
        ReviewUserSelectionManager.AddLabel(element);
        Debug.Log($"Logic: {element} added to Global Selection Manager.");
        CheckIfFiltersArentEmpty();
    }

    private void HandleRemoveRequest(string element)
    {
        ReviewUserSelectionManager.RemoveLabel(element);
        Debug.Log($"Logic: {element} removed from Global Selection Manager.");
        CheckIfFiltersArentEmpty();
    }

    private void CheckIfFiltersArentEmpty()
    {
        bool canContinue = ReviewUserSelectionManager.IncludedLabels.Count > 0;
        continueButton.interactable = canContinue;
    }
}