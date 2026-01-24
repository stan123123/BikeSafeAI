using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using SFB;
using UnityEngine.InputSystem;
using System.IO;

public class ImageAreaSelector : MonoBehaviour
{
    [SerializeField] private Button continueButton;

    [SerializeField] private Toggle includeWholeImage;

    private Vector2 startSelection;
    private Vector2 endSelection;

    [SerializeField] private TMP_InputField inputBottomLeftX;
    [SerializeField] private TMP_InputField inputBottomLeftY;
    [SerializeField] private TMP_InputField inputTopRightX;
    [SerializeField] private TMP_InputField inputTopRightY;

    [SerializeField] private Image reviewImage;

    private Image selectedAreaPreview;
    [SerializeField]private Canvas selectedAreaParent;

    private bool bIsSelectingArea = false;

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame) startAreaSelection();
        if (Mouse.current.leftButton.wasReleasedThisFrame) endAreaSelection();

        if (bIsSelectingArea)
        {
            UpdateSelectionArea();
            UpdateInputFieldsFromSelection();
        }
    }

    private void Start()
    {
        InitializeSelectionArea();
    }

    private void OnEnable()
    {
        inputBottomLeftX.onValueChanged.AddListener(_ => UpdateSelectionFromInputFields());
        inputBottomLeftY.onValueChanged.AddListener(_ => UpdateSelectionFromInputFields());
        inputTopRightX.onValueChanged.AddListener(_ => UpdateSelectionFromInputFields());
        inputTopRightY.onValueChanged.AddListener(_ => UpdateSelectionFromInputFields());

        includeWholeImage.onValueChanged.AddListener(changeHasSelectedWholeArea);

        InitializePreviewImage();
    }

    private void OnDisable()
    {
        inputBottomLeftX.onValueChanged.RemoveListener(_ => UpdateSelectionFromInputFields());
        inputBottomLeftY.onValueChanged.RemoveListener(_ => UpdateSelectionFromInputFields());
        inputTopRightX.onValueChanged.RemoveListener(_ => UpdateSelectionFromInputFields());
        inputTopRightY.onValueChanged.RemoveListener(_ => UpdateSelectionFromInputFields());

        includeWholeImage.onValueChanged.RemoveListener(changeHasSelectedWholeArea);
    }

    private void changeHasSelectedWholeArea(bool bIsSelectingWholeArea)
    {
        selectedAreaPreview.enabled = !bIsSelectingWholeArea;
        includeWholeImage.isOn = bIsSelectingWholeArea;

        SubmitSelection();

    }

    public void InitializePreviewImage()
    {
        string selectedPath = ReviewUserSelectionManager.SelectedPath;
        if (string.IsNullOrEmpty(selectedPath) || !Directory.Exists(selectedPath))
        {
            Debug.LogWarning("Selected path is invalid or does not exist.");
            return;
        }

        string usedImagesPath = Path.Combine(selectedPath, PathConfig.UsedImagesFolderName);
        if (!Directory.Exists(usedImagesPath))
        {
            Debug.LogWarning($"UsedImages folder not found at: {usedImagesPath}");
            return;
        }

        string[] files = Directory.GetFiles(usedImagesPath, "*.*", SearchOption.TopDirectoryOnly);
        string imageFile = null;
        foreach (var f in files)
        {
            string ext = Path.GetExtension(f).ToLower();
            if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
            {
                imageFile = f;
                break;
            }
        }

        if (imageFile == null)
        {
            Debug.LogWarning("No image found in the folder.");
            return;
        }

        byte[] imageData = File.ReadAllBytes(imageFile);
        Texture2D tex = new Texture2D(2, 2);
        if (!tex.LoadImage(imageData))
        {
            Debug.LogWarning("Failed to load image into texture.");
            return;
        }

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        reviewImage.sprite = sprite;
    }

    private void InitializeSelectionArea()
    {
        GameObject imageGO = new GameObject("SelectedAreaPreview");
        imageGO.transform.SetParent(selectedAreaParent.transform, false);

        selectedAreaPreview = imageGO.AddComponent<Image>();
        selectedAreaPreview.color = new Color(0, 1, 0, 0.5f);


        RectTransform rt = selectedAreaPreview.rectTransform;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = new Vector2(-500, 500);
    }

    private void startAreaSelection()
    {
        if (!CheckIfMouseIsOverSelection(Mouse.current.position.ReadValue())) return;

        changeHasSelectedWholeArea(false);

        bIsSelectingArea = true;
        startSelection = Mouse.current.position.ReadValue();

        selectedAreaPreview.rectTransform.sizeDelta = Vector2.zero;
    }

    private void endAreaSelection()
    {
        if (!bIsSelectingArea) return;

        bIsSelectingArea = false;
        endSelection = Mouse.current.position.ReadValue();
        UpdateSelectionArea();

        SubmitSelection();
    }

    private bool CheckIfMouseIsOverSelection(Vector2 mousePos)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            reviewImage.rectTransform,
            mousePos,
            selectedAreaParent.worldCamera
        );
    }

    private void UpdateSelectionArea()
    {
        Vector2 currentMousePos = Mouse.current.position.ReadValue();

        if (!RectTransformUtility.RectangleContainsScreenPoint(
                reviewImage.rectTransform,
                currentMousePos,
                selectedAreaParent.worldCamera))
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            selectedAreaParent.GetComponent<RectTransform>(),
            startSelection,
            selectedAreaParent.worldCamera,
            out Vector2 localStart
        );
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            selectedAreaParent.GetComponent<RectTransform>(),
            currentMousePos,
            selectedAreaParent.worldCamera,
            out Vector2 localCurrent
        );

        float xMin = Mathf.Min(localStart.x, localCurrent.x);
        float xMax = Mathf.Max(localStart.x, localCurrent.x);
        float yMin = Mathf.Min(localStart.y, localCurrent.y);
        float yMax = Mathf.Max(localStart.y, localCurrent.y);

        RectTransform rt = selectedAreaPreview.rectTransform;
        rt.sizeDelta = new Vector2(xMax - xMin, yMax - yMin);
        rt.anchoredPosition = new Vector2(xMin + (xMax - xMin) / 2, yMin + (yMax - yMin) / 2);
    }

    private void UpdateInputFieldsFromSelection()
    {
        RectTransform reviewRect = reviewImage.rectTransform;
        RectTransform selectionRect = selectedAreaPreview.rectTransform;

        Vector2 localPos = selectionRect.anchoredPosition - reviewRect.anchoredPosition;
        Vector2 halfSize = selectionRect.sizeDelta / 2;

        Vector2 bottomLeft = localPos - halfSize;
        Vector2 topRight = localPos + halfSize;

        float scaleX = 1920f / reviewRect.sizeDelta.x;
        float scaleY = 1080f / reviewRect.sizeDelta.y;

        int blX = Mathf.RoundToInt(Mathf.Clamp((bottomLeft.x + reviewRect.sizeDelta.x / 2) * scaleX, 0, 1920));
        int blY = Mathf.RoundToInt(Mathf.Clamp((bottomLeft.y + reviewRect.sizeDelta.y / 2) * scaleY, 0, 1080));
        int trX = Mathf.RoundToInt(Mathf.Clamp((topRight.x + reviewRect.sizeDelta.x / 2) * scaleX, 0, 1920));
        int trY = Mathf.RoundToInt(Mathf.Clamp((topRight.y + reviewRect.sizeDelta.y / 2) * scaleY, 0, 1080));

        inputBottomLeftX.text = blX.ToString();
        inputBottomLeftY.text = blY.ToString();
        inputTopRightX.text = trX.ToString();
        inputTopRightY.text = trY.ToString();
    }

    private void UpdateSelectionFromInputFields()
    {
        if (bIsSelectingArea) return;

        RectTransform reviewRect = reviewImage.rectTransform;
        RectTransform selectionRect = selectedAreaPreview.rectTransform;

        int.TryParse(inputBottomLeftX.text, out int blX);
        int.TryParse(inputBottomLeftY.text, out int blY);
        int.TryParse(inputTopRightX.text, out int trX);
        int.TryParse(inputTopRightY.text, out int trY);

        blX = Mathf.Clamp(blX, 0, 1920);
        blY = Mathf.Clamp(blY, 0, 1080);
        trX = Mathf.Clamp(trX, 0, 1920);
        trY = Mathf.Clamp(trY, 0, 1080);

        int left = Mathf.Min(blX, trX);
        int right = Mathf.Max(blX, trX);
        int bottom = Mathf.Min(blY, trY);
        int top = Mathf.Max(blY, trY);

        float scaleX = reviewRect.sizeDelta.x / 1920f;
        float scaleY = reviewRect.sizeDelta.y / 1080f;

        Vector2 size = new Vector2((right - left) * scaleX, (top - bottom) * scaleY);

        Vector2 blLocal = (Vector2)reviewRect.localPosition - reviewRect.sizeDelta / 2f + new Vector2(left * scaleX, bottom * scaleY);

        selectionRect.sizeDelta = size;
        selectionRect.anchoredPosition = blLocal + size / 2f;

        SubmitSelection();
    }

    public void SubmitSelection()
    {
        ReviewUserSelectionManager.SetVideoName(includeWholeImage.isOn);

        int blX = 0, blY = 0, trX = 0, trY = 0;
        int.TryParse(inputBottomLeftX.text, out blX);
        int.TryParse(inputBottomLeftY.text, out blY);
        int.TryParse(inputTopRightX.text, out trX);
        int.TryParse(inputTopRightY.text, out trY);

        Vector2Int bottomLeft = new Vector2Int(blX, blY);
        Vector2Int topRight = new Vector2Int(trX, trY);

        ReviewUserSelectionManager.SetBottomLeft(bottomLeft);
        ReviewUserSelectionManager.SetBottomRight(topRight);
    }
}
