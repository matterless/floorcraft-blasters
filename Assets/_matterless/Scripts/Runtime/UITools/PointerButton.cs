using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Matterless.Floorcraft.UI
{
    public class PointerButton : Button
    {
        public Action PointerUp;
        public Action PointerDown;
        public Action PointerExit;
        
        public override void OnPointerUp(PointerEventData eventData)
        {
            Debug.Log("Pointer Up");
            PointerUp?.Invoke();
            base.OnPointerUp(eventData);
        }
        
        public override void OnPointerDown(PointerEventData eventData)
        {
            Debug.Log("Pointer Down");
            PointerDown?.Invoke();
            base.OnPointerDown(eventData);
        }
        
        public override void OnPointerExit(PointerEventData eventData)
        {
            Debug.Log("Pointer Exit");
            PointerExit?.Invoke();
            base.OnPointerExit(eventData);
        }
    }
}