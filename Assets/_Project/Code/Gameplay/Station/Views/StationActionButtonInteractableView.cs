using System;
using Project.Gameplay.Interaction;

namespace Project.Gameplay.Station.Views
{
	public sealed class StationActionButtonInteractableView : PlayerInteractableView
	{
		public event Action Interacted;

		public override void Interact()
		{
			Interacted?.Invoke();
		}
	}
}
