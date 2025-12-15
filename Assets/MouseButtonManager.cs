using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MouseButtonManager : MonoBehaviour
{
    void Update()
    {
        // Check if mouse is over UI
        if (EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("Mouse is over UI element");
        }

        // Log mouse clicks
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Left mouse button clicked");
        }
    }
}
