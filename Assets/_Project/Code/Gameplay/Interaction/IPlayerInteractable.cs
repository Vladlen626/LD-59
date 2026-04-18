using UnityEngine;

namespace Project.Gameplay.Interaction
{
	public interface IPlayerInteractable
	{
		Collider InteractionCollider { get; }
		int Priority { get; }
		bool CanInteract { get; }
		void Interact();
	}
}
