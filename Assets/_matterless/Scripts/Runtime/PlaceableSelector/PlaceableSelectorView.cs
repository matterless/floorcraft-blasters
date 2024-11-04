using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Matterless.Floorcraft
{
	public class PlaceableSelectorView : UIView<PlaceableSelectorView>
	{
		public event Action<PointerEventData> onDrag;
		public event Action<PointerEventData> onEndDrag;
		public event Action onBackButtonClicked;
		
		[SerializeField] private Transform m_Parent;
		[SerializeField] private Camera m_Camera;
		[SerializeField] private UiHandlerHelper m_HandlerHelper;
		[SerializeField] private Button m_SelectorBackButton;
		[SerializeField] private Button m_SelectorSelectButton;
		[SerializeField] private Text m_SelectorButtonLabelView;
		private Action m_OnSelect;
		
		public override void Show()
		{
			base.Show();
			m_Camera.gameObject.SetActive(true);
		}

		public void RegisterOnSelect(Action action)
		{
			m_OnSelect += action;
		}
		
		public override void Hide()
		{
			base.Hide();
			m_Camera.gameObject.SetActive(false);
		}
		
		public Transform parentTransform => m_Parent;
		
		public override PlaceableSelectorView Init()
		{
			m_HandlerHelper.onDrag += OnDrag;
			m_HandlerHelper.onEndDrag += OnEndDrag;
			m_SelectorBackButton.onClick.AddListener(() => onBackButtonClicked.Invoke());
			m_SelectorSelectButton.onClick.AddListener(() =>
			{
				m_OnSelect.Invoke();
			});
            
			// add overlay camera to main camera stack
			var cameraData = Camera.main.GetUniversalAdditionalCameraData();
			cameraData.cameraStack.Add(m_Camera);

			return this;
		}

		private void OnDrag(PointerEventData data) => onDrag?.Invoke(data);

		private void OnEndDrag(PointerEventData data) => onEndDrag?.Invoke(data);
		public void UpdateSelectButtonText(string text)
		{
			m_SelectorButtonLabelView.text = text;
		}
	}
}