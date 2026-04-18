using System;
using PlatformCore.Infrastructure.SimpleTweens;
using Project.Gameplay.Interaction;
using UnityEngine;

namespace Project.Gameplay.Station.Views
{
	public sealed class WagonDoorInteractableView : PlayerInteractableView
	{
		[SerializeField] private Transform _doorTarget;
		[SerializeField] private float _moveDuration = 0.65f;

		private Vector3 _closedLocalPosition;

		public event Action Interacted;

		protected override void Awake()
		{
			base.Awake();
			_closedLocalPosition = transform.localPosition;
		}

		public override void Interact()
		{
			Interacted?.Invoke();
		}

		public void SetOpenState(bool isOpened)
		{
			var targetPosition = isOpened && _doorTarget
				? GetTargetLocalPosition()
				: _closedLocalPosition;

			SimpleTweensTransform.MoveToLocalPosition(transform, targetPosition, _moveDuration);
		}

		private Vector3 GetTargetLocalPosition()
		{
			var parent = transform.parent;
			return parent ? parent.InverseTransformPoint(_doorTarget.position) : _doorTarget.position;
		}
	}
}
