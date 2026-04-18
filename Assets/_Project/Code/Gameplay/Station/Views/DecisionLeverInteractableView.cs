using System;
using Project.Gameplay.Station.Domain;
using UnityEngine;

namespace Project.Gameplay.Station.Views
{
	public sealed class DecisionLeverInteractableView : MonoBehaviour, IStationInteractableView
	{
		[SerializeField] private PlayerDecision _decision = PlayerDecision.AllowPassage;

		public event Action<DecisionLeverInteractableView> Interacted;

		public PlayerDecision Decision => _decision;

		public void Interact()
		{
			Interacted?.Invoke(this);
		}
	}
}
