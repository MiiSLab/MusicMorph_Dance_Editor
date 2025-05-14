using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageGalleryUI : MonoBehaviour
{
    public GameObject imageButtonPrefab; // 圖片按鈕 Prefab
    public Transform imageContainer; // GridLayoutGroup 容器
    public List<Sprite> imageList; // 圖片清單
    public Button nextPageButton, prevPageButton;

    private int currentPage = 0;
    private int imagesPerPage = 6; // 每頁顯示 6 張圖片
    private List<GameObject> activeButtons = new List<GameObject>();

    void Start()
    {
        UpdatePage();
        nextPageButton.onClick.AddListener(NextPage);
        prevPageButton.onClick.AddListener(PrevPage);
    }

    void UpdatePage()
    {
        // 清除當前按鈕
        foreach (var btn in activeButtons)
            Destroy(btn);
        activeButtons.Clear();

        int startIndex = currentPage * imagesPerPage;
        int endIndex = Mathf.Min(startIndex + imagesPerPage, imageList.Count);

        Debug.Log($"目前頁數: {currentPage}，顯示範圍: {startIndex} ~ {endIndex}");

        for (int i = startIndex; i < endIndex; i++)
        {
            GameObject buttonObj = Instantiate(imageButtonPrefab, imageContainer);
            Image imageComponent = buttonObj.transform.GetComponent<Image>();

            if (imageComponent != null)
            {
                imageComponent.sprite = imageList[i];
            }

            CanvasGroup group = buttonObj.GetComponent<CanvasGroup>();
            if (group == null)
                group = buttonObj.AddComponent<CanvasGroup>();

            DragHandler dragHandler = buttonObj.GetComponent<DragHandler>();
            if (dragHandler == null)
                dragHandler = buttonObj.AddComponent<DragHandler>();
            dragHandler.characterImage = imageComponent;


            int index = i;
            buttonObj.GetComponent<Button>().onClick.RemoveAllListeners();
            buttonObj.GetComponent<Button>().onClick.AddListener(() => SelectImage(index));

            activeButtons.Add(buttonObj);
        }

        prevPageButton.interactable = currentPage > 0;
        nextPageButton.interactable = endIndex < imageList.Count;
    }

    public void NextPage()
    {
        int maxPage = Mathf.CeilToInt((float)imageList.Count / imagesPerPage);
        if (currentPage < maxPage - 1)
        {
            currentPage++;
            UpdatePage();
        }
    }

    public void PrevPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            UpdatePage();
        }
    }

    void SelectImage(int index)
    {
        Debug.Log($"選擇圖片：{index}");
    }
}
