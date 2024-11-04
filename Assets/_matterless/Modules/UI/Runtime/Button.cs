using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Graphic = UnityEngine.UI.Graphic;
using UButton = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

namespace Matterless.Module.UI
{
    [AddComponentMenu("Matterless/UI/Button", 31)]
    [ExecuteAlways]
    public class Button : UIBehaviour, IPointerEnterHandler, 
        IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, 
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region Event
        protected UButton.ButtonClickedEvent m_OnClick = new();

        public UButton.ButtonClickedEvent onClick
        {
            get { return m_OnClick; }
            set { m_OnClick = value; }
        }

        public class ButtonDraggedEvent : UnityEvent<Vector2> {}
        
        protected ButtonDraggedEvent m_OnDrag = new();
        
        public ButtonDraggedEvent onDrag
        {
            get { return m_OnDrag; }
            set { m_OnDrag = value; }
        }

        #endregion
        
        #region States
        private bool m_Interactable = true;

        [SerializeField] private bool m_IsDraggable = false;
        
        public bool interactable
        {
            get { return m_Interactable; }
            set
            {
                if (m_Interactable == value)
                    return;

                m_Interactable = value;

                OnSetProperty();
            }
        }
        
        protected enum State
        {
            Disabled,
            Normal,
            Pressed,
            Clicked,
            Dragged
        }

        private bool m_IsPointerDown;
        private bool m_IsPointerInside;
        private bool m_IsPointerDragged;
        protected State m_PreviousState;
#if UNITY_EDITOR
        private bool m_IsCalledFromOutFocus;
#endif
        
        #endregion

        [SerializeField] protected Graphic m_TargetGraphic;
        public Image image
        {
            get { return m_TargetGraphic as Image; }
            set { m_TargetGraphic = value; }
        }

        #region UIBehaviour
        
        protected override void Awake()
        {
            base.Awake();
            if (m_TargetGraphic == null)
                m_TargetGraphic = GetComponent<Graphic>();
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
#if UNITY_EDITOR
            if (!hasFocus)
                m_IsCalledFromOutFocus = true;
#endif
            if (!hasFocus && m_IsPointerDown)
                InstantClearState();
        }

        protected sealed override void OnEnable()
        {
            base.OnEnable();
            m_IsPointerDown = false;
            DoStateTransition(currentState, instant: true);
        }

        protected sealed override void OnDisable()
        {
            InstantClearState();
            base.OnDisable();
        }

        private void OnSetProperty()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DoStateTransition(currentState, true);
            else
#endif
                DoStateTransition(currentState);
        }

        #endregion

        #region Pointer Events

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData == null || eventData.pointerEnter == null ||
                eventData.pointerEnter.GetComponentInParent<Button>() != this)
                return;
            
            m_IsPointerInside = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!eventData.fullyExited)
                return;
            
            m_IsPointerInside = false;
            
            if (m_IsDraggable)
                return;
            
            EvaluateAndTransitionToState();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            
            m_IsPointerDown = true;
            EvaluateAndTransitionToState();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            
            m_IsPointerDown = false;
            EvaluateAndTransitionToState();
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!m_IsDraggable)
                return;
            
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            
            m_IsPointerDragged = true;
            EvaluateAndTransitionToState();
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (!m_IsDraggable)
                return;
            
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            
            m_OnDrag?.Invoke(eventData.delta);            
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!m_IsDraggable)
                return;
            
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            
            m_IsPointerDragged = false;
            EvaluateAndTransitionToState();
        }
        

        #endregion

        #region State Transitions

        private State currentState
        {
            get
            {
                if (m_Interactable == false)
                {
                    return State.Disabled;
                }
#if UNITY_EDITOR
                if (m_IsCalledFromOutFocus && m_IsPointerDown)
                {
                    m_IsCalledFromOutFocus = false;
                    return State.Pressed;
                }
#endif
                if (m_IsPointerDragged)
                    return State.Dragged;
                
                if (m_IsPointerInside)
                {
                    if (m_IsPointerDown)
                        return State.Pressed;

                    if (m_PreviousState == State.Pressed)
                        return State.Clicked;
                }

                if (m_PreviousState is State.Pressed or State.Disabled or State.Dragged)
                    return State.Normal;

                return m_PreviousState;
            }
        }

        private void EvaluateAndTransitionToState()
        {
            if (!IsActive() || !m_Interactable)
                return;
            DoStateTransition(currentState);
        }

        private void DoStateTransition(State state, bool instant = false)
        {
            if (!gameObject.activeInHierarchy)
                return;

            if (state == m_PreviousState)
                return;

            m_PreviousState = state;

            switch (state)
            {
                case State.Disabled:
                    OnButtonDisabled(instant);
                    break;
                case State.Dragged:
                    OnButtonDragged();
                    break;
                case State.Pressed:
                    OnButtonPressed();
                    break;
                case State.Clicked:
                    OnButtonClicked();
                    break;
                case State.Normal:
                    OnButtonReleased(instant);
                    break;
            }
        }

        /// <summary>
        /// Override to implement OnButtonDisabled visual. Do not call base.OnButtonDisabled().
        /// </summary> 
        protected virtual void OnButtonDisabled(bool instant)
        {
        }


        /// <summary>
        /// Override to implement OnButtonPressed visual. Do not call base.OnButtonPressed().
        /// </summary>
        protected virtual void OnButtonPressed()
        {
        }

        /// <summary>
        /// Override to implement OnButtonReleased visual. Do not call base.OnButtonReleased().
        /// </summary>
        protected virtual void OnButtonReleased(bool instant)
        {
        }

        /// <summary>
        /// Override to implement OnButtonClicked visual. Do not call base.Depress().
        /// Use finishWithClick to determine if the button was clicked or not
        /// </summary>
        protected virtual void OnButtonClicked()
        {
            if (m_TargetGraphic == null)
                return;
            m_OnClick.Invoke();
        }
        
        protected virtual void OnButtonDragged()
        {
        }

        /// <summary>
        /// If OnButtonPressed(), OnButtonReleased(), OnButtonClicked() or OnButtonDisabled()
        /// were overriden, you must override this method as well with your own implementation
        /// for clearing the visual state.
        /// Always call base.InstantClearState() before your own implementation.
        /// </summary>
        protected virtual void InstantClearState()
        {
            m_IsPointerDown = false;
            m_IsPointerInside = false;
            m_PreviousState = State.Normal;
        }
        
        #endregion
        
        #region Editor
#if UNITY_EDITOR

        protected sealed override void OnValidate()
        {
            base.OnValidate();
            if (isActiveAndEnabled)
                DoStateTransition(currentState, true);
        }
        protected sealed override void Reset()
        {
            m_TargetGraphic = GetComponent<Graphic>();
        }
#endif
        #endregion
    }
    
}