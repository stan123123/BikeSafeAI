using UnityEngine;
using UnityEngine.UI;

public class DataPreviewPanel : MonoBehaviour
{
    [SerializeField] GameObject DataPreviewColorPrefab;

    private int darkestColorCount = 4;

    /// <summary>
    /// Initializes the panel by spawning color indicators based on integer values.
    /// </summary>
    /// <param name="values">Array of integers (expected 0-20) representing intensity.</param>
    public void Initialize(int[] values)
    {
        // Clear existing children first
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        foreach (int value in values)
        {
            // Clamp value between 0 and 20 to avoid errors
            int clampedValue = Mathf.Clamp(value, 0, darkestColorCount);

            // Instantiate the prefab as a child
            GameObject instance = Instantiate(DataPreviewColorPrefab, transform);

            // Get the Image component from the prefab
            Image img = instance.GetComponent<Image>();
            if (img != null)
            {
                // Calculate red intensity from 0 (black) to 1 (full red)
                float redIntensity = clampedValue / (float)darkestColorCount;

                // Set the color: red channel increases, green and blue stay 0
                img.color = new Color(redIntensity, 0f, 0f);
            }
            else
            {
                Debug.LogWarning("DataPreviewColorPrefab has no Image component!");
            }
        }
    }
}
