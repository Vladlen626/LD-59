using System;
using UnityEngine;

namespace Project.Gameplay.Station.Views
{
	public sealed class DispatchButtonInteractableView : MonoBehaviour, IStationInteractableView
	{
		public event Action Interacted;

		public void Interact()
		{
			Interacted?.Invoke();
		}
	}

	public sealed class TrainCallInteractableView : MonoBehaviour, IStationInteractableView
	{
		public event Action Interacted;

		public void Interact()
		{
			Interacted?.Invoke();
		}
	}
}
