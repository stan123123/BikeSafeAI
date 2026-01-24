using UnityEngine;
using System;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("UI canvases to be able to swtich to")]
    [SerializeField] private UIPage[] UIPages;

    private Stack<string> _navigationHistory = new Stack<string>();
    private string _currentUIName;

    public static event Action<string> OnUIChangeRequested;
    public static event Action OnBackRequested;

    [Serializable]
    private struct UIPage
    {
        public string name;
        public Canvas Canvas;
    }

    private void Start()
    {
        _currentUIName = GetCurrentUIPageName();
    }

    private void OnEnable()
    {
        OnUIChangeRequested += HandleUIChangeRequest;
        OnBackRequested += GoBack;
    }

    private void OnDisable()
    {
        OnUIChangeRequested -= HandleUIChangeRequest;
        OnBackRequested -= GoBack;
    }

    private void HandleUIChangeRequest(string UIName)
    {
        Debug.Log($"HandleUIChangeRequest called! UIName: {UIName}, Current: '{_currentUIName}'");

        if (_currentUIName == UIName) return;

        if (!string.IsNullOrEmpty(_currentUIName))
        {
            _navigationHistory.Push(_currentUIName);
        }

        SwitchUI(UIName);
    }

    public void GoBack()
    {
        if (_navigationHistory.Count > 0)
        {
            string previousPage = _navigationHistory.Pop();

            Debug.Log("Switching to previous UI page");

            int indexToGoTo = -1;
            for (int i = 0; i < UIPages.Length; i++)
            {
                if (UIPages[i].name == previousPage)
                {
                    indexToGoTo = i;
                    break;
                }
            }

            if (indexToGoTo != -1)
            {
                _currentUIName = previousPage;
                SwitchToUIPage(indexToGoTo);
            }
            else
            {
                Debug.LogError($"ERROR: No UI found with name {previousPage}");
            }
        }
        else
        {
            Debug.LogWarning("Navigation History is empty! Nowhere to go back to.");
        }

        Debug.Log("Debug.This is the end of the event");
    }

    private void SwitchUI(string UIName)
    {
        for (int i = 0; i < UIPages.Length; i++)
        {
            if (UIPages[i].name == UIName)
            {
                _currentUIName = UIName; 
                SwitchToUIPage(i);
                return;
            }
        }
        Debug.LogError($"ERROR: No UI found with name {UIName}");
    }

    private void SwitchToUIPage(int indexToSwitchTo)
    {
        for (int i = 0; i < UIPages.Length; i++)
        {
            UIPages[i].Canvas.gameObject.SetActive(i == indexToSwitchTo);
        }
    }

    public static void RequestBack()
    {
        OnBackRequested?.Invoke();
    }

    public static void RequestUIChange(string UIName)
    {
        OnUIChangeRequested?.Invoke(UIName);
    }

    private string GetCurrentUIPageName()
    {
        for (int i = 0; i < UIPages.Length; i++)
        {
            if (UIPages[i].Canvas.gameObject.activeInHierarchy)
            {
                return UIPages[i].name;
            }
        }
        return "";
    }
}