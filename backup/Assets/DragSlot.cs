using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class DragSlot : MonoBehaviour, IDropHandler
{
     public DragContainer containerIcon;
     public GameObject backIcons;
     public bool isFull = false;
     public int indexData = 0;
     public void OnDrop(PointerEventData eventData)
     {
          
     }

     public void resetIcon()
     {
          isFull = false;
          indexData = 0;
     }
}
