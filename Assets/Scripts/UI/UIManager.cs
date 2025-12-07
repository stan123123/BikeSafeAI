using UnityEngine;
using System;

public class UIManager : MonoBehaviour
{
    [Header("Assign all canvases in order")]
    public Canvas[] canvases;


    [Header("UI canvases to be able to swtich to")]
    [SerializeField]private UIPage[] UIPages;

    public static event Action<int> OnUIChangeRequested;

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

    private void SwitchUI(int index)
    {
        if (index < 0 || index >= canvases.Length)
        {
            Debug.LogWarning($"UI index {index} is out of range!");
            return;
        }

        for (int i = 0; i < canvases.Length; i++)
            canvases[i].gameObject.SetActive(i == index);
    }

    public static void RequestUIChange(int index)
    {
        OnUIChangeRequested?.Invoke(index);
    }
}