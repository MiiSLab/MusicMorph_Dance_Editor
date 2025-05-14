using UnityEngine;
using UnityEngine.UI;

public class VRDropSlot : MonoBehaviour
{
    public Image slotImage;             // Slot UI 圖片
    public Sprite defaultPurpleCircle;  // 預設紫色圈圈圖
    private GameObject currentCharacter;

    private void OnTriggerEnter(Collider other)
    {
        VRDraggable draggable = other.GetComponent<VRDraggable>();
        if (draggable != null && draggable.characterImage != null)
        {
            // 更換 slot 圖片
            slotImage.sprite = draggable.characterImage.sprite;
            slotImage.color = Color.white;

            currentCharacter = other.gameObject;
            Debug.Log($"[VR Drop] 圖片成功放入 slot：{name}");
        }
    }

    public void ClearSlot()
    {
        slotImage.sprite = defaultPurpleCircle;
        slotImage.color = Color.white;
    }
}
