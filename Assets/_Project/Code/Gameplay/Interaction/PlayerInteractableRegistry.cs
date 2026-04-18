using System.Collections.Generic;
using UnityEngine;

namespace Project.Gameplay.Interaction
{
	public static class PlayerInteractableRegistry
	{
		private static readonly Dictionary<Collider, IPlayerInteractable> InteractablesByCollider = new(32);

		public static void Register(IPlayerInteractable interactable)
		{
			InteractablesByCollider[interactable.InteractionCollider] = interactable;
		}

		public static void Unregister(IPlayerInteractable interactable)
		{
			InteractablesByCollider.Remove(interactable.InteractionCollider);
		}

		public static bool TryGet(Collider collider, out IPlayerInteractable interactable)
		{
			return InteractablesByCollider.TryGetValue(collider, out interactable);
		}
	}
}
