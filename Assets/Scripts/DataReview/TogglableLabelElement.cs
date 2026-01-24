using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TogglableLabelElement : MonoBehaviour
{
    private string labelName = null;

    [SerializeField] private TMP_Text displayText;
    [SerializeField] private Toggle toggle;

    public void Initialize(string name, bool initialState)
    {
        labelName = name;
        displayText.text = name;

        toggle.SetIsOnWithoutNotify(initialState);
        toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool isEnabled)
    {
        if (isEnabled)
        {
            SelectLabels.BroadcastAdd(labelName);
        }
        else
        {
            SelectLabels.BroadcastRemove(labelName);
        }
    }

    private void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    public void OverrideSetEnabled(bool isEnabled)
    {
        toggle.isOn = isEnabled;
    }
}