using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Buttonevent : MonoBehaviour
{
    public List<DropSlot> allSlots;

    public void ClearAllSlots()
    {
        Debug.Log("Cancel==========");

        foreach (var slot in allSlots)
        {
            slot.ClearSlot();
        }
    }
    public void PrintSomething()
    {
        Debug.Log("Cancel ??????I");
    }

}
