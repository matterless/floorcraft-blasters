using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Matterless.Floorcraft
{
    public class UiHandlerHelper : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
        // TODO:: implement more UI events
        //IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, 
        //IPointerMoveHandler, IPointerUpHandler,
        
    {
        public event Action<PointerEventData> onBeginDrag;
        public event Action<PointerEventData> onDrag;
        public event Action<PointerEventData> onEndDrag;

        public void OnBeginDrag(PointerEventData eventData) => onBeginDrag?.Invoke(eventData);
        public void OnDrag(PointerEventData eventData) => onDrag?.Invoke(eventData);
        public void OnEndDrag(PointerEventData eventData) => onEndDrag?.Invoke(eventData);
    }
}