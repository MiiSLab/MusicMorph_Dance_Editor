using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropSlot : MonoBehaviour, IDropHandler
{
    public Image slotImage; // slot 上的 Image 元件（紫圈中間）
    private GameObject currentCharacter; // 目前佔用者（可用於替換）
    public Sprite defaultPurpleCircle;

    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        if (dropped != null)
        {
            DragHandler dragHandler = dropped.GetComponent<DragHandler>();
            if (dragHandler != null && dragHandler.characterImage != null)
            {
                // 設定插槽圖片
                slotImage.sprite = dragHandler.characterImage.sprite;
                slotImage.color = Color.white; // 避免圖片透明

                // 可選：記錄目前佔用角色
                currentCharacter = dropped;
                Debug.Log($"角色放置於：{name}");
            }
        }
    }

    public void ClearSlot()
    {
        slotImage.sprite = defaultPurpleCircle;
        slotImage.color = Color.white;
    }
}
