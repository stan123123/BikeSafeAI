using UnityEngine;
using System;

public class UIManager : MonoBehaviour
{
    [Header("UI canvases to be able to swtich to")]
    [SerializeField]private UIPage[] UIPages;

    public static event Action<string> OnUIChangeRequested;

    [Serializable]
    private struct UIPage
    {
        public string name;
        public Canvas Canvas;
    }

    private void OnEnable()
    {
        OnUIChangeRequested += SwitchUI;
    }

    private void OnDisable()
    {
        OnUIChangeRequested -= SwitchUI;
    }

    private void SwitchUI(string UIName)
    {
        for (int i = 0; i < UIPages.Length; i++)
        {
            if (UIPages[i].name == UIName)
            {
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

    public static void RequestUIChange(string UIName)
    {
        OnUIChangeRequested?.Invoke(UIName);
    }
}