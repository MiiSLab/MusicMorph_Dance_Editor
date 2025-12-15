using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageGalleryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject imageButtonPrefab;
    [SerializeField] private Transform imageContainer;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private Button prevPageButton;
    
    [Header("Gallery Settings")]
    [SerializeField] private List<Sprite> imageList;
    [SerializeField] private int imagesPerPage = 6;
    
    private List<GameObject> activeButtons = new List<GameObject>();
    private int currentPage = 0;
    
    // 計算屬性：總頁數
    private int TotalPages => Mathf.CeilToInt((float)imageList.Count / imagesPerPage);

    private void Start()
    {
        // 設置分頁按鈕監聽器
        nextPageButton.onClick.AddListener(NextPage);
        prevPageButton.onClick.AddListener(PrevPage);
        
        // 初始顯示第一頁
        UpdatePage();
    }

    private void UpdatePage()
    {
        // 清理現有按鈕
        ClearButtons();
        
        // 計算當前頁的索引範圍
        int startIndex = currentPage * imagesPerPage;
        int endIndex = Mathf.Min(startIndex + imagesPerPage, imageList.Count);

        // 日誌輸出當前頁面信息
        Debug.Log($"當前頁面: {currentPage+1}/{TotalPages}, 顯示範圍: {startIndex+1} ~ {endIndex}");

        // 為當前頁面創建按鈕
        for (int i = startIndex; i < endIndex; i++)
        {
            // 創建按鈕並設置圖片
            GameObject buttonObj = CreateImageButton(imageList[i], i);
            activeButtons.Add(buttonObj);
        }

        // 更新導航按鈕的可用狀態
        UpdateNavigationButtons();
    }

    private GameObject CreateImageButton(Sprite sprite, int index)
    {
        // 實例化按鈕
        GameObject buttonObj = Instantiate(imageButtonPrefab, imageContainer);
        
        // 設置圖片
        Image imageComponent = buttonObj.GetComponent<Image>();
        if (imageComponent != null)
        {
            imageComponent.sprite = sprite;
        }
        
        // 確保有 CanvasGroup 組件
        if (!buttonObj.TryGetComponent<CanvasGroup>(out _))
        {
            buttonObj.AddComponent<CanvasGroup>();
        }
        
        // 設置拖拽處理
        DragHandler dragHandler = buttonObj.GetComponent<DragHandler>();
        if (dragHandler == null)
        {
            dragHandler = buttonObj.AddComponent<DragHandler>();
        }
        dragHandler.characterImage = imageComponent;
        
        // 設置點擊事件
        Button button = buttonObj.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SelectImage(index));
        
        return buttonObj;
    }

    private void ClearButtons()
    {
        // 銷毀所有當前活動的按鈕
        foreach (var button in activeButtons)
        {
            Destroy(button);
        }
        activeButtons.Clear();
    }

    private void UpdateNavigationButtons()
    {
        // 禁用/啟用上一頁按鈕
        prevPageButton.interactable = currentPage > 0;
        
        // 禁用/啟用下一頁按鈕
        nextPageButton.interactable = currentPage < TotalPages - 1;
    }

    public void NextPage()
    {
        if (currentPage < TotalPages - 1)
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

    private void SelectImage(int index)
    {
        Debug.Log($"選擇圖片: {index+1}");
        // 這裡可以添加選擇後的處理邏輯
    }
}