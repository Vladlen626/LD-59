using System;
using Project.Gameplay.Interaction;
using Project.Gameplay.Station.Domain;
using UnityEngine;

namespace Project.Gameplay.Station.Views
{
	public sealed class StationDecisionToggleInteractableView : PlayerInteractableView
	{
		[SerializeField] private PlayerDecision _defaultDecision = PlayerDecision.AllowPassage;
		[SerializeField] private GameObject _allowVisual;
		[SerializeField] private GameObject _rejectVisual;

		private PlayerDecision _currentDecision;

		public event Action<PlayerDecision> Interacted;

		public PlayerDecision CurrentDecision => _currentDecision;

		protected override void Awake()
		{
			base.Awake();
			SetDecision(_defaultDecision);
		}

		public override void Interact()
		{
			Interacted?.Invoke(_currentDecision == PlayerDecision.AllowPassage
				? PlayerDecision.SendBack
				: PlayerDecision.AllowPassage);
		}

		public void ResetDecision()
		{
			SetDecision(_defaultDecision);
		}

		public void SetDecision(PlayerDecision decision)
		{
			_currentDecision = decision;
			var isAllow = decision == PlayerDecision.AllowPassage;

			if (_allowVisual)
			{
				_allowVisual.SetActive(isAllow);
			}

			if (_rejectVisual)
			{
				_rejectVisual.SetActive(!isAllow);
			}
		}
	}
}
