using System;
using TMPro;
using PlatformCore.Infrastructure.SimpleTweens;
using Project.Gameplay.Interaction;
using UnityEngine;

namespace Project.Gameplay.Station.Views
{
	public sealed class WagonDoorInteractableView : PlayerInteractableView
	{
		[SerializeField] private Transform _doorTarget;
		[SerializeField] private float _moveDuration = 0.65f;
		[SerializeField] private TMP_Text _hintText;
		[SerializeField] private Vector3 _hintLocalPosition = new(0f, 1.75f, 0f);
		[SerializeField] private Vector3 _hintLocalScale = new(0.08f, 0.08f, 0.08f);
		[SerializeField] private float _hintFontSize = 2.5f;
		[SerializeField] private Color _hintColor = new(0.88f, 0.84f, 0.76f);

		private Vector3 _closedLocalPosition;
		private bool _isOpened;
		private bool _isHintVisible;

		public event Action Interacted;

		protected override void Awake()
		{
			base.Awake();
			_closedLocalPosition = transform.localPosition;
			EnsureHintText();
			RefreshHint();
		}

		public override void Interact()
		{
			Interacted?.Invoke();
		}

		public void SetOpenState(bool isOpened)
		{
			_isOpened = isOpened;
			var targetPosition = isOpened && _doorTarget
				? GetTargetLocalPosition()
				: _closedLocalPosition;

			SimpleTweensTransform.MoveToLocalPosition(transform, targetPosition, _moveDuration);
			RefreshHint();
		}

		public void SetHintVisible(bool isVisible)
		{
			_isHintVisible = isVisible;
			RefreshHint();
		}

		private Vector3 GetTargetLocalPosition()
		{
			var parent = transform.parent;
			return parent ? parent.InverseTransformPoint(_doorTarget.position) : _doorTarget.position;
		}

		private void EnsureHintText()
		{
			_hintText ??= StationWorldTextFactory.CreateBillboardLabel(
				transform,
				"DoorHint",
				_hintLocalPosition,
				_hintLocalScale,
				_hintFontSize,
				TextAlignmentOptions.Center);
			_hintText.color = _hintColor;
		}

		private void RefreshHint()
		{
			if (!_hintText)
			{
				return;
			}

			_hintText.gameObject.SetActive(_isHintVisible);
			if (_isHintVisible == false)
			{
				return;
			}

			_hintText.text = _isOpened ? "Close Wagon" : "Open Wagon";
		}
	}
}
