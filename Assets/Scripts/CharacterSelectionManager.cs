using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 角色選擇管理器 - 處理清除和應用角色選擇
/// </summary>
public class CharacterSelectionManager : MonoBehaviour
{
    [Header("槽位設置")]
    [Tooltip("所有角色放置槽位")]
    public List<DropSlot> characterSlots;

    [Header("按鈕設置")]
    [Tooltip("清除按鈕")]
    public Button clearButton;

    [Tooltip("應用按鈕")]
    public Button applyButton;

    [Header("角色設置")]
    [Tooltip("所有可用角色物件 (包含骨架作為子物件)")]
    public CharacterInfo[] characterInfos;

    [Header("位置設置")]
    [Tooltip("左側角色位置")]
    public Vector3 leftPosition = new Vector3(-1.5f, 0f, 5f);

    [Tooltip("中間角色位置")]
    public Vector3 centerPosition = new Vector3(0f, 0f, 4f);

    [Tooltip("右側角色位置")]
    public Vector3 rightPosition = new Vector3(1.5f, 0f, 5f);

    [Tooltip("未選中角色的隱藏位置")]
    public Vector3 hiddenPosition = new Vector3(0f, -100f, 0f);

    /// <summary>
    /// 角色信息結構
    /// </summary>
    [System.Serializable]
    public class CharacterInfo
    {
        [Tooltip("角色ID/名稱 (與圖像名稱匹配)")]
        public string characterID;

        [Tooltip("角色物件 (包含骨架作為子物件)")]
        public GameObject characterObject;
    }

    // 存儲當前選中的角色ID
    public HashSet<string> selectedIDs = new HashSet<string>();

    private void Start()
    {
        // 初始化時將所有角色移動到隱藏位置
        MoveAllCharactersToHiddenPosition();

        // 綁定按鈕事件
        if (clearButton != null)
        {
            clearButton.onClick.AddListener(ClearAllSlots);
        }

        if (applyButton != null)
        {
            applyButton.onClick.AddListener(ApplyCharacterSelection);
        }
    }

    /// <summary>
    /// 將所有角色移動到隱藏位置
    /// </summary>
    private void MoveAllCharactersToHiddenPosition()
    {
        Debug.Log("===== 將所有角色移動到隱藏位置 =====");
        foreach (var info in characterInfos)
        {
            if (info.characterObject != null)
            {
                info.characterObject.transform.position = hiddenPosition;
                Debug.Log($"隱藏角色: {info.characterID}");
            }
        }
    }

    /// <summary>
    /// 清除所有角色選擇
    /// </summary>
    public void ClearAllSlots()
    {
        foreach (var slot in characterSlots)
        {
            if (slot != null)
            {
                slot.ClearSlot();
            }
        }

        // 清除選中集合
        selectedIDs.Clear();

        // 將所有角色移動到隱藏位置
        MoveAllCharactersToHiddenPosition();

        Debug.Log("已清除所有角色選擇");
    }

    /// <summary>
    /// 應用選擇的角色
    /// </summary>
    public void ApplyCharacterSelection()
    {
        // 清空選中集合
        selectedIDs.Clear();

        // 收集所有選中的角色 ID
        foreach (var slot in characterSlots)
        {
            if (slot != null && !string.IsNullOrEmpty(slot.imageName))
            {
                selectedIDs.Add(slot.imageName);
            }
        }

        Debug.Log($"選中的角色數量: {selectedIDs.Count}");
        foreach (var id in selectedIDs)
        {
            Debug.Log($"選中角色: {id}");
        }

        // 首先將所有角色移動到隱藏位置
        MoveAllCharactersToHiddenPosition();

        // 如果沒有選擇角色，結束
        if (selectedIDs.Count == 0)
        {
            Debug.Log("沒有選擇任何角色，所有角色都被隱藏");
            return;
        }

        // 然後將選中的角色移動到顯示位置
        SetupSelectedCharacters();

        Debug.Log("已應用角色選擇");
    }

    /// <summary>
    /// 設置選中的角色位置
    /// </summary>
    private void SetupSelectedCharacters()
    {
        Debug.Log("===== 設置選中的角色位置 =====");

        // 轉換為列表以便訪問索引
        List<string> selectedCharIDs = new List<string>(selectedIDs);
        int count = selectedCharIDs.Count;

        // 限制最多處理3個角色
        int characterCount = Mathf.Min(count, 3);

        // 根據選擇的角色數量決定位置
        for (int i = 0; i < characterCount; i++)
        {
            string charID = selectedCharIDs[i];
            Vector3 position;

            // 決定位置
            if (count == 1)
            {
                position = centerPosition; // 只有一個角色，放中間
            }
            else if (count == 2)
            {
                position = (i == 0) ? leftPosition : rightPosition; // 兩個角色，放左右
            }
            else
            {
                // 三個或更多角色
                if (i == 0) position = leftPosition;
                else if (i == 1) position = centerPosition;
                else position = rightPosition;
            }

            // 將這個角色移動到顯示位置
            bool found = false;
            foreach (var info in characterInfos)
            {
                if (info.characterID == charID && info.characterObject != null)
                {
                    info.characterObject.transform.position = position;
                    Debug.Log($"顯示角色: {charID}, 位置: {position}");
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Debug.LogError($"找不到角色: {charID}");
            }
        }

        // 檢查最終狀態
        Debug.Log("===== 最終角色位置 =====");
        foreach (var info in characterInfos)
        {
            if (info.characterObject != null)
            {
                bool isSelected = selectedIDs.Contains(info.characterID);
                Vector3 pos = info.characterObject.transform.position;

                Debug.Log($"角色: {info.characterID}, 是否選中: {isSelected}, 位置: {pos}");
            }
        }
    }

    private void OnDestroy()
    {
        // 解除按鈕事件綁定
        if (clearButton != null)
        {
            clearButton.onClick.RemoveListener(ClearAllSlots);
        }

        if (applyButton != null)
        {
            applyButton.onClick.RemoveListener(ApplyCharacterSelection);
        }
    }
}