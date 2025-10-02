using UnityEngine;

public class UI_SelectImages : MonoBehaviour
{
    public void SelectProcessingSingleImage()
    {
        ImageProcesser.RequestProcessSingleImage();
    }

    public void SelectProcessFolderOfImages()
    {
        ImageProcesser.RequestProcessFolderOfImages();
    }

    public void SelectProcessVideoFrames()
    {
        //ImageProcesser.RequestProcessVideoFrames(); <- Delete later, keeping this here as a reminder
        UIManager.RequestUIChange(UIManager.UIType.SelectVideo);
    }
}