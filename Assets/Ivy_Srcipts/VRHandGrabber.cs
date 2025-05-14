using UnityEngine;

public class VRHandGrabber : MonoBehaviour
{
    public OVRHand hand;
    private VRDraggable grabbedObject;

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger)) // 改成你要的手勢或手部輸入
        {
            TryGrab();
        }
        else if (OVRInput.GetUp(OVRInput.RawButton.RIndexTrigger))
        {
            if (grabbedObject != null)
            {
                grabbedObject.Release();
                grabbedObject = null;
            }
        }
    }

    void TryGrab()
    {
        Collider[] hits = Physics.OverlapSphere(hand.PointerPose.position, 0.05f);
        foreach (var hit in hits)
        {
            VRDraggable draggable = hit.GetComponent<VRDraggable>();
            if (draggable != null)
            {
                grabbedObject = draggable;
                grabbedObject.Grab(hand.PointerPose);
                break;
            }
        }
    }
}
