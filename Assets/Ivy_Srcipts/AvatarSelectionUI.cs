using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AvatarSelectionUI : MonoBehaviour
{
    public GameObject avatarButtonPrefab; // 頭像按鈕 Prefab
    public Transform avatarContainer; // `GridLayout Group` 容器
    public List<Sprite> avatarImages; // 頭像圖片清單
    public List<GameObject> avatarModels; // 3D 角色模型清單
    public Button nextPageButton, prevPageButton;
    public Transform characterDisplayArea; // 角色顯示區域

    private int currentPage = 0;
    private int avatarsPerPage = 8; // 每頁最多顯示的頭像數量
    private List<GameObject> activeButtons = new List<GameObject>();
    private GameObject currentCharacter;
    private GameObject selectedButton = null; // 記錄當前選中的按鈕

    void Start()
    {
        UpdatePage();
        nextPageButton.onClick.AddListener(NextPage);
        prevPageButton.onClick.AddListener(PrevPage);
    }

    void UpdatePage()
    {
        foreach (var btn in activeButtons)
            Destroy(btn);
        activeButtons.Clear();

        int startIndex = currentPage * avatarsPerPage;
        int endIndex = Mathf.Min(startIndex + avatarsPerPage, avatarImages.Count);

        Debug.Log($"目前頁數: {currentPage}，顯示範圍: {startIndex} ~ {endIndex}");

        for (int i = startIndex; i < endIndex; i++)
        {
            GameObject buttonObj = Instantiate(avatarButtonPrefab, avatarContainer);
            Image avatarImage = buttonObj.transform.Find("AvatarImage").GetComponent<Image>();

            if (avatarImage != null)
            {
                avatarImage.sprite = avatarImages[i];
            }

            // 找到 Mask 並關閉 Show Mask Graphic（預設為不顯示）
            Mask mask = buttonObj.GetComponent<Mask>();
            if (mask != null)
            {
                mask.showMaskGraphic = false; // 預設不顯示
            }

            int index = i;
            buttonObj.GetComponent<Button>().onClick.RemoveAllListeners();
            buttonObj.GetComponent<Button>().onClick.AddListener(() => SelectAvatar(index, buttonObj));

            activeButtons.Add(buttonObj);
        }

        prevPageButton.interactable = currentPage > 0;
        nextPageButton.interactable = endIndex < avatarImages.Count;
    }

    public void NextPage()
    {
        int maxPage = Mathf.CeilToInt((float)avatarImages.Count / avatarsPerPage);
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

    void SelectAvatar(int index, GameObject buttonObj)
    {
        Debug.Log($"選擇角色：{index}");

        if (index < 0 || index >= avatarModels.Count)
        {
            Debug.LogError($"角色索引超出範圍！index: {index}, 總數: {avatarModels.Count}");
            return;
        }

        // 刪除當前角色
        if (currentCharacter != null)
        {
            Destroy(currentCharacter);
        }

        // 生成新角色
        currentCharacter = Instantiate(avatarModels[index], characterDisplayArea);
        currentCharacter.transform.localPosition = Vector3.zero;
        currentCharacter.transform.localRotation = Quaternion.identity;

        Debug.Log($"角色 {index} 生成成功！");

        // 清除之前選中的按鈕 Mask 顯示
        if (selectedButton != null)
        {
            Mask prevMask = selectedButton.GetComponent<Mask>();
            /*
            if (prevMask != null)
            {
                prevMask.showMaskGraphic = false; // 關閉上次選擇的圖像
            }
            */

        }

        // 設置當前選中的按鈕 Mask 顯示
        Mask mask = buttonObj.GetComponent<Mask>();
        if (mask != null)
        {
            mask.showMaskGraphic = true; // 開啟當前選擇的圖像
        }

        selectedButton = buttonObj; // 記住當前選中的按鈕
    }
}
