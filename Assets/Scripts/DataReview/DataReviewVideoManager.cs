using UnityEngine;
using TMPro;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;
using System.IO;

public class DataReviewVideoManager : MonoBehaviour
{
    [SerializeField] private VideoPlayer _videoPlayer;
    [SerializeField] private DataReviewPointOfInterestBar _pointOfInterestBar;
    [SerializeField] private DetailsPanel _detailsPanel;
    [SerializeField] public RawImage _videoDisplay;
    [SerializeField] private Image videoPausedImage;

    public static event Action<float> OnVideoDurationReady;

    private bool lastPlayingState = false;



    private void Start()
    {
        DisplaySelectedVideo();
    }

    private void OnEnable()
    {
        if (_pointOfInterestBar != null)
            _pointOfInterestBar.OnBarDragged.AddListener(SetVideoTimeNormalized);

        if (_videoDisplay != null)
            _videoDisplay.gameObject.AddComponent<VideoClickHandler>().Initialize(this);
    }

    private void OnDisable()
    {
        if (_pointOfInterestBar != null)
            _pointOfInterestBar.OnBarDragged.RemoveListener(SetVideoTimeNormalized);
    }

    private void Update()
    {
        if (_pointOfInterestBar != null && _videoPlayer.isPrepared && _detailsPanel != null)
        {
            _detailsPanel.UpdatePreviewDetails((float)_videoPlayer.time);
            _pointOfInterestBar.UpdateVideoTimeDisplay((float)_videoPlayer.time);
        }

        // Poll for pause/unpause
        if (_videoPlayer.isPrepared && lastPlayingState != _videoPlayer.isPlaying)
        {
            UpdatePauseImage();
            lastPlayingState = _videoPlayer.isPlaying;
        }
    }

    public void SetVideoTimeNormalized(float normalizedValue)
    {
        if (!_videoPlayer.isPrepared || _videoPlayer.length <= 0)
            return;

        normalizedValue = Mathf.Clamp01(normalizedValue);
        _videoPlayer.time = normalizedValue * _videoPlayer.length;
    }

    private void LoadVideoFromPath(string url)
    {
        _videoPlayer.source = VideoSource.Url;
        _videoPlayer.url = url;

        _videoPlayer.prepareCompleted += OnVideoPrepared;
        _videoPlayer.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnVideoPrepared;
        OnVideoDurationReady?.Invoke((float)vp.length);
        vp.Play();
        lastPlayingState = true;
        UpdatePauseImage();
    }

    private void UpdatePauseImage()
    {
        if (videoPausedImage != null)
            videoPausedImage.enabled = !_videoPlayer.isPlaying;
    }

    public void TogglePlayPause()
    {
        if (!_videoPlayer.isPrepared)
            return;

        if (_videoPlayer.isPlaying)
            _videoPlayer.Pause();
        else
            _videoPlayer.Play();

        UpdatePauseImage();
        lastPlayingState = _videoPlayer.isPlaying;
    }

    private void DisplaySelectedVideo()
    {
        string folderPath = ReviewUserSelectionManager.SelectedPath;
        if (!Directory.Exists(folderPath))
        {
            Debug.LogError($"Folder not found: {folderPath}");
            return;
        }

        string[] videoExtensions = { "*.mp4", "*.mov", "*.wmv", "*.avi" };
        string foundVideoPath = null;

        foreach (var ext in videoExtensions)
        {
            string[] files = Directory.GetFiles(folderPath, ext);
            if (files.Length > 0)
            {
                foundVideoPath = files[0];
                break;
            }
        }

        if (!string.IsNullOrEmpty(foundVideoPath))
            LoadVideoFromPath(foundVideoPath);
        else
            Debug.LogError($"No valid video files found in: {folderPath}");
    }
}

public class VideoClickHandler : MonoBehaviour
{
    private DataReviewVideoManager _manager;
    private RectTransform _rectTransform;

    public void Initialize(DataReviewVideoManager manager)
    {
        _manager = manager;
        _rectTransform = _manager._videoDisplay.rectTransform;
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();

            if (_rectTransform != null && RectTransformUtility.RectangleContainsScreenPoint(_rectTransform, mousePos))
            {
                _manager?.TogglePlayPause();
            }
        }
    }
}