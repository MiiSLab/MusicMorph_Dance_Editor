using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("拖曳設定")]
    [Tooltip("拖曳時的透明度")]
    [Range(0.1f, 1.0f)]
    public float dragAlpha = 0.6f;
    
    [Tooltip("拖曳時的縮放")]
    [Range(0.5f, 2.0f)]
    public float dragScale = 1.2f;
    
    [Tooltip("拖曳時圖像是否跟隨游標")]
    public bool followCursor = true;
    
    [Tooltip("松開時圖像是否彈回原位")]
    public bool returnToOriginalPosition = true;
    
    [Header("參考")]
    public Image characterImage;
    
    // 用於追蹤拖曳狀態
    private bool isDragging = false;
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private CanvasGroup canvasGroup;
    
    // 用於改善拖曳靈敏度
    private Canvas parentCanvas;
    private RectTransform rectTransform;
    
    // 當前所在的槽位
    private DropSlot currentSlot;
    
    // 防止單次拖曳重複觸發
    private bool processingDrop = false;

    private void Awake()
    {
        // 獲取必要組件
        if (characterImage == null)
        {
            characterImage = GetComponent<Image>();
        }
        
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // 查找父畫布
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            if (canvases.Length > 0)
            {
                parentCanvas = canvases[0];
            }
        }
        
        // 儲存原始縮放
        originalScale = rectTransform.localScale;
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 如果正在處理放置，忽略此次拖曳
        if (processingDrop)
        {
            eventData.pointerDrag = null;
            return;
        }
        
        isDragging = true;
        originalPosition = transform.position;
        
        // 透明度變化
        canvasGroup.alpha = dragAlpha;
        
        // 確保這個物件接收事件但不阻擋底下的物件
        canvasGroup.blocksRaycasts = false;
        
        // 縮放效果
        rectTransform.localScale = originalScale * dragScale;
        
        // 提升层级
        transform.SetAsLastSibling();
        
        // 註冊為當前拖曳的物件
        DropSlot.RegisterDraggingObject(gameObject);
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || !followCursor) return;
        
        // 實現更靈敏的拖曳
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            parentCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera, 
            out Vector3 position))
        {
            transform.position = position;
        }
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        isDragging = false;
        
        // 清除當前拖曳的物件引用
        DropSlot.ClearDraggingObject(gameObject);
        
        // 恢復透明度
        canvasGroup.alpha = 1f;
        
        // 恢復射線檢測
        canvasGroup.blocksRaycasts = true;
        
        // 恢復原始縮放
        rectTransform.localScale = originalScale;
        
        // 如果沒有放置在有效的 DropSlot 上，則返回原位
        bool droppedOnSlot = (eventData.pointerEnter != null && eventData.pointerEnter.GetComponent<DropSlot>() != null);
        if (returnToOriginalPosition && !droppedOnSlot)
        {
            transform.position = originalPosition;
        }
    }
    
    // 支持點擊選擇
    public void OnPointerClick(PointerEventData eventData)
    {
        // 忽略拖曳結束時的點擊
        if (isDragging) return;
        
        // 如果使用者單擊，可以執行選擇操作
        Debug.Log($"已選中圖片: {characterImage.sprite.name}");
    }
    
    // 被放入槽位時的回調
    public void OnDroppedIntoSlot(DropSlot slot)
    {
        // 設置正在處理放置標誌
        processingDrop = true;
        
        // 更新當前槽位
        currentSlot = slot;
        
        // 立即停止拖曳並回到原位置，避免殘影
        isDragging = false;
        transform.position = originalPosition;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        rectTransform.localScale = originalScale;
        
        // 短暫延遲後解除處理標誌，允許再次拖曳
        Invoke("ResetProcessingFlag", 0.2f);
    }
    
    // 從槽位移除時的回調
    public void OnRemovedFromSlot(DropSlot slot)
    {
        // 只處理匹配的槽位
        if (currentSlot == slot)
        {
            currentSlot = null;
        }
    }
    
    // 重置處理標誌，允許再次拖曳
    private void ResetProcessingFlag()
    {
        processingDrop = false;
    }
    
    // 增強拖曳靈敏度的輔助方法
    private void AdjustDragThreshold()
    {
        if (parentCanvas != null)
        {
            // 針對不同類型的畫布調整拖曳閾值
            if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // 螢幕空間疊加模式下閾值可以較小
                EventSystem.current.pixelDragThreshold = 5;
            }
            else
            {
                // 其他模式下可能需要更大閾值
                EventSystem.current.pixelDragThreshold = 10;
            }
        }
    }
    
    private void Start()
    {
        AdjustDragThreshold();
    }
    
    // 當物件被禁用時清除狀態
    private void OnDisable()
    {
        if (isDragging)
        {
            isDragging = false;
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            rectTransform.localScale = originalScale;
            DropSlot.ClearDraggingObject(gameObject);
        }
        
        processingDrop = false;
    }
}