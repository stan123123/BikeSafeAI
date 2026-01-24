using UnityEngine;
using TMPro;

public class DetailsDisplayElement : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _nameField;
    [SerializeField] private TextMeshProUGUI _countField;

    public void SetName(string name)
    {
        _nameField.text = name;
    }

    public void SetCount(int count)
    {
        _countField.text = count.ToString();
    }

    public string GetName()
    {
        return _nameField.text; 
    }
}
