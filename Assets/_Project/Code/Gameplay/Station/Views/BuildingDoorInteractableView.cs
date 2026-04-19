using System;
using PlatformCore.Infrastructure.SimpleTweens;
using Project.Gameplay.Interaction;
using UnityEngine;

namespace Project.Gameplay.Station.Views
{
	public sealed class BuildingDoorInteractableView : PlayerInteractableView, IPlayerInteractableHintProvider
	{
		[SerializeField] private Transform _doorPivot;
		[SerializeField] private float _rotateDuration = 0.45f;
		[SerializeField] private Vector3 _openLocalEulerOffset = new(0f, 90f, 0f);
		[SerializeField] private bool _startOpened;
		[SerializeField] private string _openHint = "Open Door";
		[SerializeField] private string _closeHint = "Close Door";

		private Vector3 _closedLocalEulerAngles;
		private bool _isOpened;

		public event Action<bool> OpenStateChanged;

		public bool IsOpened => _isOpened;
		public string InteractionHint => _isOpened ? _closeHint : _openHint;

		protected override void Awake()
		{
			base.Awake();
			_doorPivot ??= transform;
			_closedLocalEulerAngles = _doorPivot.localEulerAngles;
			ApplyOpenState(_startOpened, true);
		}

		public override void Interact()
		{
			ApplyOpenState(!_isOpened, false);
		}

		public void SetOpenState(bool isOpened)
		{
			ApplyOpenState(isOpened, false);
		}

		private void ApplyOpenState(bool isOpened, bool instant)
		{
			_isOpened = isOpened;
			var targetEuler = isOpened
				? _closedLocalEulerAngles + _openLocalEulerOffset
				: _closedLocalEulerAngles;

			if (instant || _rotateDuration <= 0f)
			{
				_doorPivot.localEulerAngles = targetEuler;
			}
			else
			{
				SimpleTweensTransform.RotateToLocalEulerAngles(_doorPivot, targetEuler, _rotateDuration);
			}

			OpenStateChanged?.Invoke(_isOpened);
		}
	}
}
