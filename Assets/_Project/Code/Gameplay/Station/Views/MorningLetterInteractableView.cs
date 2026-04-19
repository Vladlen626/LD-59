using System;
using Project.Gameplay.Interaction;
using UnityEngine;

	namespace Project.Gameplay.Station.Views
	{
	public sealed class MorningLetterInteractableView : PlayerInteractableView, IPlayerInteractableHintProvider
	{
		[SerializeField] private bool _autoCreateRuntimeVisual = true;
		[SerializeField] private bool _availableOnStart;
		[SerializeField] private string _interactionHint = "Read Letter";
		[SerializeField] private Vector3 _paperSize = new(0.24f, 0.015f, 0.34f);
		[SerializeField] private Vector3 _interactionBoxSize = new(0.32f, 0.08f, 0.42f);
		[SerializeField] private Color _paperColor = new(0.92f, 0.87f, 0.73f, 1f);

		private Collider _interactionCollider;
		private bool _isAvailable;

		public event Action Interacted;
		public string InteractionHint => _interactionHint;

		public override bool CanInteract => base.CanInteract && _isAvailable;

		protected override void Awake()
		{
			EnsureRuntimeVisual();
			EnsureInteractionCollider();
			base.Awake();
			_interactionCollider = InteractionCollider;
			SetAvailable(_availableOnStart);
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

			if (_interactionCollider)
			{
				_interactionCollider.enabled = isAvailable;
			}
		}

		public void SetVisible(bool isVisible)
		{
			gameObject.SetActive(isVisible);
		}

		private void EnsureRuntimeVisual()
		{
			if (!_autoCreateRuntimeVisual || GetComponentInChildren<Renderer>())
			{
				return;
			}

			var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
			visual.name = "PaperVisual";
			visual.transform.SetParent(transform, false);
			visual.transform.localPosition = Vector3.zero;
			visual.transform.localRotation = Quaternion.identity;
			visual.transform.localScale = _paperSize;
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
				renderer.material.color = _paperColor;
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
			boxCollider.size = _interactionBoxSize;
		}
	}
}
