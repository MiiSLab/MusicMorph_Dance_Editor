using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class DropSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("圖像設定")]
    public Image slotImage;
    public Sprite defaultPurpleCircle;
    
    [Header("放置設定")]
    [Tooltip("啟用懸停自動放置 (不需要放開滑鼠)")]
    public bool enableHoverDrop = true;
    
    [Tooltip("啟用高亮效果顯示拖曳目標")]
    public bool highlightOnHover = true;
    
    [Tooltip("高亮顏色")]
    public Color hoverHighlightColor = new Color(0.8f, 0.8f, 1f, 1f);
    
    [Tooltip("放置冷卻時間 (秒)")]
    public float dropCooldown = 0.3f;
    
    // 儲存圖片名稱
    public string imageName { get; private set; } = "";
    
    // 追蹤目前放置的物件
    private GameObject currentCharacter;
    
    // 原始顏色
    private Color originalColor;
    
    // 是否有物件懸停中
    private bool isObjectHovering = false;
    private GameObject hoveringObject = null;
    
    // 冷卻狀態
    private bool isInCooldown = false;
    
    // 靜態引用當前活動的拖曳處理器，防止衝突
    private static GameObject currentlyDragging = null;

    private void Start()
    {
        // 確保有預設圖像
        if (slotImage == null)
        {
            slotImage = GetComponent<Image>();
        }
        
        // 儲存原始顏色
        if (slotImage != null)
        {
            originalColor = slotImage.color;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        // 如果在冷卻中，忽略此次放置
        if (isInCooldown) return;
        
        // 確保這是當前活動的拖曳物件
        if (eventData.pointerDrag == currentlyDragging)
        {
            AcceptDraggedObject(eventData.pointerDrag);
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 如果在冷卻中，忽略此次進入
        if (isInCooldown) return;
        
        // 如果有東西正在被拖曳，記錄它
        if (eventData.pointerDrag != null)
        {
            isObjectHovering = true;
            hoveringObject = eventData.pointerDrag;
            
            // 應用高亮效果
            if (highlightOnHover && slotImage != null)
            {
                slotImage.color = hoverHighlightColor;
            }
            
            // 如果啟用了懸停自動放置，且這是當前活動的拖曳物件
            if (enableHoverDrop && eventData.pointerDrag == currentlyDragging)
            {
                // 使用協程延遲處理，避免多次觸發
                StartCoroutine(DelayedAccept(eventData.pointerDrag));
            }
        }
    }
    
    // 延遲處理避免同時觸發多個事件
    private IEnumerator DelayedAccept(GameObject obj)
    {
        yield return new WaitForEndOfFrame(); // 等待本幀結束
        
        // 再次檢查是否仍然懸停和不在冷卻中
        if (isObjectHovering && hoveringObject == obj && !isInCooldown)
        {
            AcceptDraggedObject(obj);
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        // 重置狀態
        isObjectHovering = false;
        
        if (eventData.pointerDrag == hoveringObject)
        {
            hoveringObject = null;
        }
        
        // 恢復原始顏色
        if (highlightOnHover && slotImage != null && !isInCooldown)
        {
            slotImage.color = originalColor;
        }
    }
    
    // 接受被拖曳的物件
    private void AcceptDraggedObject(GameObject dropped)
    {
        if (dropped == null) return;
        
        DragHandler dragHandler = dropped.GetComponent<DragHandler>();
        if (dragHandler != null && dragHandler.characterImage != null)
        {
            // 立即進入冷卻狀態
            EnterCooldown();
            
            // 更新槽位圖像
            slotImage.sprite = dragHandler.characterImage.sprite;
            slotImage.color = Color.white;
            
            // 如果之前有物件，解除其關聯
            if (currentCharacter != null && currentCharacter != dropped)
            {
                DragHandler previousHandler = currentCharacter.GetComponent<DragHandler>();
                if (previousHandler != null)
                {
                    // 通知之前的處理器已經不再與此槽位關聯
                    previousHandler.OnRemovedFromSlot(this);
                }
            }
            
            // 更新當前物件引用
            currentCharacter = dropped;

            // 更新圖像名稱
            imageName = dragHandler.characterImage.sprite.name;
            
            // 發出聲音反饋或動畫效果 (可選)
            PlayDropEffect();
            
            Debug.Log($"Slot [{name}] 放入圖片：{imageName}");
            
            // 通知 DragHandler 它已被放入此槽位
            dragHandler.OnDroppedIntoSlot(this);
            
            // 清除當前活動的拖曳物件
            currentlyDragging = null;
        }
    }
    
    // 進入冷卻狀態
    private void EnterCooldown()
    {
        isInCooldown = true;
        StartCoroutine(CooldownRoutine());
    }
    
    // 冷卻倒數協程
    private IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(dropCooldown);
        isInCooldown = false;
    }
    
    // 播放放置效果
    private void PlayDropEffect()
    {
        if (slotImage != null)
        {
            StartCoroutine(PulseAnimation());
        }
    }
    
    // 簡單的脈衝動畫
    private System.Collections.IEnumerator PulseAnimation()
    {
        RectTransform rect = slotImage.rectTransform;
        Vector3 originalScale = rect.localScale;
        
        // 放大
        float duration = 0.1f;
        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            rect.localScale = Vector3.Lerp(originalScale, originalScale * 1.1f, t);
            yield return null;
        }
        
        // 縮回
        time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            rect.localScale = Vector3.Lerp(originalScale * 1.1f, originalScale, t);
            yield return null;
        }
        
        rect.localScale = originalScale;
    }
    
    // 清除槽位
    public void ClearSlot()
    {
        // 如果有物件，通知它已被移除
        if (currentCharacter != null)
        {
            DragHandler dragHandler = currentCharacter.GetComponent<DragHandler>();
            if (dragHandler != null)
            {
                dragHandler.OnRemovedFromSlot(this);
            }
        }
        
        slotImage.sprite = defaultPurpleCircle;
        slotImage.color = Color.white;
        imageName = "";
        currentCharacter = null;
        isInCooldown = false; // 清除時也重置冷卻狀態
        
        // 清除任何正在進行的協程
        StopAllCoroutines();
    }
    
    // 取得當前放置的物件
    public GameObject GetCurrentCharacter()
    {
        return currentCharacter;
    }
    
    // 靜態方法註冊當前拖曳的物件
    public static void RegisterDraggingObject(GameObject obj)
    {
        currentlyDragging = obj;
    }
    
    // 靜態方法清除當前拖曳的物件
    public static void ClearDraggingObject(GameObject obj)
    {
        // 只清除匹配的物件
        if (currentlyDragging == obj)
        {
            currentlyDragging = null;
        }
    }
}