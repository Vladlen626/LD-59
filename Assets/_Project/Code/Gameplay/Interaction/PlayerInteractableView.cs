using UnityEngine;

namespace Project.Gameplay.Interaction
{
	public abstract class PlayerInteractableView : MonoBehaviour, IPlayerInteractable
	{
		[SerializeField] private Collider _interactionCollider;
		[SerializeField] private int _priority;

		public Collider InteractionCollider => _interactionCollider;
		public int Priority => _priority;
		public virtual bool CanInteract => isActiveAndEnabled;

		protected virtual void Awake()
		{
			if (!_interactionCollider)
			{
				_interactionCollider = GetComponent<Collider>();
			}

			if (!_interactionCollider)
			{
				_interactionCollider = GetComponentInChildren<Collider>();
			}
		}

		protected virtual void OnEnable()
		{
			if (_interactionCollider)
			{
				PlayerInteractableRegistry.Register(this);
			}
		}

		protected virtual void OnDisable()
		{
			if (_interactionCollider)
			{
				PlayerInteractableRegistry.Unregister(this);
			}
		}

		public abstract void Interact();
	}
}
