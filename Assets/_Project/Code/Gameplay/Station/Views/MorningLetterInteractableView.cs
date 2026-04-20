using System;
using Project.Gameplay.Interaction;
using UnityEngine;

namespace Project.Gameplay.Station.Views
{
	public sealed class MorningLetterInteractableView : PlayerInteractableView, IPlayerInteractableHintProvider
	{
		[SerializeField] private bool _createRuntimeVisual = true;
		[SerializeField] private bool _startAvailable;
		[SerializeField] private string _hint = "Read Letter";
		[SerializeField] private Vector3 _paperVisualSize = new(0.24f, 0.015f, 0.34f);
		[SerializeField] private Vector3 _interactionColliderSize = new(0.32f, 0.08f, 0.42f);
		[SerializeField] private Color _paperVisualColor = new(0.92f, 0.87f, 0.73f, 1f);

		private Collider _cachedInteractionCollider;
		private bool _isAvailable;
		private int _dayIndex;

		public event Action Interacted;
		public string InteractionHint
		{
			get
			{
				var hint = string.IsNullOrWhiteSpace(_hint) ? "Read Letter" : _hint.Trim();
				return _dayIndex > 0 ? $"{hint}\nDay {_dayIndex}" : hint;
			}
		}

		public override bool CanInteract => base.CanInteract && _isAvailable;

		protected override void Awake()
		{
			EnsureRuntimeVisual();
			EnsureInteractionCollider();
			base.Awake();
			_cachedInteractionCollider = InteractionCollider;
			SetAvailable(_startAvailable);
		}

		public override void Interact()
		{
			if (!CanInteract)
			{
				return;
			}

			Interacted?.Invoke();
		}

		public void SetAvailable(bool isAvailable)
		{
			_isAvailable = isAvailable;

			if (_cachedInteractionCollider)
			{
				_cachedInteractionCollider.enabled = isAvailable;
			}
		}

		public void SetDayIndex(int dayIndex)
		{
			_dayIndex = Mathf.Max(0, dayIndex);
		}

		public void SetVisible(bool isVisible)
		{
			gameObject.SetActive(isVisible);
		}

		private void EnsureRuntimeVisual()
		{
			if (!_createRuntimeVisual || GetComponentInChildren<Renderer>())
			{
				return;
			}

			var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
			visual.name = "PaperVisual";
			visual.transform.SetParent(transform, false);
			visual.transform.localPosition = Vector3.zero;
			visual.transform.localRotation = Quaternion.identity;
			visual.transform.localScale = _paperVisualSize;
			visual.layer = gameObject.layer;

			var visualCollider = visual.GetComponent<Collider>();
			if (visualCollider)
			{
				Destroy(visualCollider);
			}

			var renderer = visual.GetComponent<Renderer>();
			if (!renderer)
			{
				return;
			}

			renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			renderer.receiveShadows = false;
			if (renderer.material)
			{
				renderer.material.color = _paperVisualColor;
			}
		}

		private void EnsureInteractionCollider()
		{
			if (GetComponent<Collider>())
			{
				return;
			}

			var boxCollider = gameObject.AddComponent<BoxCollider>();
			boxCollider.isTrigger = true;
			boxCollider.size = _interactionColliderSize;
		}
	}
}
