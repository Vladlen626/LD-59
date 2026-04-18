using System;
using UnityEngine;

namespace Project.Gameplay.Station.Views
{
	public sealed class WagonDoorInteractableView : MonoBehaviour, IStationInteractableView
	{
		[SerializeField] private Transform _doorPivot;
		[SerializeField] private Vector3 _openedLocalEuler = new Vector3(0f, 110f, 0f);

		private Quaternion _closedLocalRotation;

		public event Action Interacted;

		private void Awake()
		{
			if (!_doorPivot)
			{
				_doorPivot = transform;
			}

			_closedLocalRotation = _doorPivot.localRotation;
		}

		public void Interact()
		{
			Interacted?.Invoke();
		}

		public void SetOpenState(bool isOpened)
		{
			if (!_doorPivot)
			{
				return;
			}

			_doorPivot.localRotation = isOpened
				? _closedLocalRotation * Quaternion.Euler(_openedLocalEuler)
				: _closedLocalRotation;
		}
	}
}
