using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System;

public class DataReviewPointOfInterestBar : MonoBehaviour
{
    [SerializeField] private Image pointOfInterestBar;
    [SerializeField] private TextMeshProUGUI beginTimeText;
    [SerializeField] private TextMeshProUGUI endTimeText;
    [SerializeField] private TextMeshProUGUI currentTimeDisplay;
    [SerializeField] private GameObject timeDisplayObject;
    [SerializeField] private GameObject timeDisplayContainer;

    [Header("POI Settings")]
    [SerializeField] private Image poiPrefab; // Yellow image prefab for POIs

    [Header("Events")]
    public UnityEvent<float> OnBarDragged; // Triggered with 0-1 value

    private float videoDurationSeconds;
    private bool isDragging = false;

    private List<Image> poiImages = new List<Image>();

    private void OnEnable()
    {
        DataReviewManager.OnDataProcessingCompleted += SpawnPOIs;
        DataReviewVideoManager.OnVideoDurationReady += UpdateEndTime;
    }

    private void OnDisable()
    {
        DataReviewManager.OnDataProcessingCompleted -= SpawnPOIs;
        DataReviewVideoManager.OnVideoDurationReady -= UpdateEndTime;
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (IsMouseOverBar(out float normalizedValue))
                isDragging = true;
        }

        if (isDragging)
        {
            if (IsMouseOverBar(out float normalizedValue))
                OnBarDragged?.Invoke(normalizedValue);

            if (Mouse.current.leftButton.wasReleasedThisFrame)
                isDragging = false;
        }
    }

    private bool IsMouseOverBar(out float normalizedValue)
    {
        normalizedValue = 0f;

        RectTransform rectTransform = pointOfInterestBar.rectTransform;
        Vector2 localPoint;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Mouse.current.position.ReadValue(), null, out localPoint))
        {
            Rect rect = rectTransform.rect;
            if (!rect.Contains(localPoint))
                return false;

            float width = rect.width;
            float pivotX = rectTransform.pivot.x;
            float leftEdge = -width * pivotX;
            float rightEdge = width * (1 - pivotX);

            float clampedX = Mathf.Clamp(localPoint.x, leftEdge, rightEdge);
            normalizedValue = Mathf.InverseLerp(leftEdge, rightEdge, clampedX);

            return true;
        }

        return false;
    }

    private void UpdateEndTime(float totalSeconds)
    {
        videoDurationSeconds = totalSeconds;
        endTimeText.text = FloatSecondsToStringTime(totalSeconds);
    }

    private string FloatSecondsToStringTime(float seconds)
    {
        int intMinutes = Mathf.FloorToInt(seconds / 60f);
        int intSeconds = Mathf.FloorToInt(seconds % 60f);
        return string.Format("{0:D2}:{1:D2}", intMinutes, intSeconds);
    }

    public void UpdateVideoTimeDisplay(float seconds)
    {
        if (videoDurationSeconds <= 0f)
            return;

        currentTimeDisplay.text = FloatSecondsToStringTime(seconds);

        float normalized = Mathf.Clamp01(seconds / videoDurationSeconds);

        RectTransform barRect = pointOfInterestBar.rectTransform;
        RectTransform markerRect = timeDisplayObject.GetComponent<RectTransform>();

        float width = barRect.rect.width;
        float pivotX = barRect.pivot.x;
        float leftEdge = -width * pivotX;
        float rightEdge = width * (1f - pivotX);

        float xPos = Mathf.Lerp(leftEdge, rightEdge, normalized);

        Vector2 anchoredPos = markerRect.anchoredPosition;
        anchoredPos.x = xPos;
        markerRect.anchoredPosition = anchoredPos;
    }

    private void SpawnPOIs(List<PointOfInterest> points, ProcessedVideoData videoData)
    {
        Debug.Log("[POI Bar] SpawnPOIs called");

        // Clear previous POIs
        foreach (var img in poiImages)
            Destroy(img.gameObject);
        poiImages.Clear();

        if (points == null || points.Count == 0)
        {
            Debug.Log("[POI Bar] No points of interest to display.");
            return;
        }

        if (videoData.videoDurationSeconds <= 0f)
        {
            Debug.LogWarning("[POI Bar] Video duration invalid: " + videoData.videoDurationSeconds);
            return;
        }

        Debug.Log("[POI Bar] Video Duration: " + videoData.videoDurationSeconds + "s, Points: " + points.Count);

        RectTransform barRect = pointOfInterestBar.rectTransform;
        float barWidth = barRect.rect.width;
        float pivotX = barRect.pivot.x;
        float leftEdge = -barWidth * pivotX;
        float rightEdge = barWidth * (1f - pivotX);

        foreach (var poi in points)
        {
            // Spawn under timeDisplayContainer instead of the bar
            Image poiImg = Instantiate(poiPrefab, timeDisplayContainer.transform);
            poiImg.color = Color.yellow;

            RectTransform poiRect = poiImg.rectTransform;

            float startNorm = Mathf.Clamp01(poi.startTimeInVideoSeconds / videoData.videoDurationSeconds);
            float endTime = poi.startTimeInVideoSeconds + poi.amountOfFrames * videoData.derivedFrameDeltaSeconds;
            float endNorm = Mathf.Clamp01(endTime / videoData.videoDurationSeconds);

            float xMin = Mathf.Lerp(leftEdge, rightEdge, startNorm);
            float xMax = Mathf.Lerp(leftEdge, rightEdge, endNorm);

            // Keep horizontal position relative to the bar
            Vector2 localPos = new Vector2((xMin + xMax) / 2f, 0f);
            poiRect.anchoredPosition = localPos;

            // Width relative to POI duration, height matches the bar
            poiRect.sizeDelta = new Vector2(Mathf.Max(xMax - xMin, 2f), barRect.rect.height);

            // Center vertically
            poiRect.pivot = new Vector2(poiRect.pivot.x, 0.5f);

            poiImages.Add(poiImg);
        }
    }
}