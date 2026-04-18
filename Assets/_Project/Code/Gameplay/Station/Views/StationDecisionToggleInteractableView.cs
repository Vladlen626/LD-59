using System;
using TMPro;
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
		[SerializeField] private TMP_Text _labelText;
		[SerializeField] private Vector3 _labelLocalPosition = new(0f, 1.2f, 0f);
		[SerializeField] private Vector3 _labelLocalScale = new(0.08f, 0.08f, 0.08f);
		[SerializeField] private float _labelFontSize = 2.6f;
		[SerializeField] private Color _allowColor = new(0.72f, 0.88f, 0.67f);
		[SerializeField] private Color _rejectColor = new(0.95f, 0.63f, 0.45f);

		private PlayerDecision _currentDecision;

		public event Action<PlayerDecision> Interacted;

		public PlayerDecision CurrentDecision => _currentDecision;

		protected override void Awake()
		{
			base.Awake();
			EnsureLabel();
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

			if (_labelText)
			{
				_labelText.text = isAllow
					? "Verdict: Allow\nPull to change"
					: "Verdict: Send Back\nPull to change";
				_labelText.color = isAllow ? _allowColor : _rejectColor;
			}
		}

		private void EnsureLabel()
		{
			_labelText ??= StationWorldTextFactory.CreateBillboardLabel(
				transform,
				"VerdictLabel",
				_labelLocalPosition,
				_labelLocalScale,
				_labelFontSize,
				TextAlignmentOptions.Center);
		}
	}
}
