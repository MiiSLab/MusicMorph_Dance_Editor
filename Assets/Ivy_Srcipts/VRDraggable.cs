using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VRDraggable : MonoBehaviour
{
    public Image characterImage; // 用於 slot 的圖片（UI Image）
    private bool isBeingHeld = false;
    private Transform handTransform;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;

    void Start()
    {
        originalParent = transform.parent;
        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    void Update()
    {
        if (isBeingHeld && handTransform != null)
        {
            transform.position = handTransform.position;
            transform.rotation = handTransform.rotation;
        }
    }

    public void Grab(Transform hand)
    {
        handTransform = hand;
        isBeingHeld = true;
        transform.SetParent(null); // 暫時脫離 parent
    }

    public void Release()
    {
        isBeingHeld = false;
        transform.SetParent(originalParent);
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        handTransform = null;
    }
}
