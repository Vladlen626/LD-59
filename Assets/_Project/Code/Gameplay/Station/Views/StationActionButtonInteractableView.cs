using System;
using System.Collections;
using TMPro;
using Project.Gameplay.Interaction;
using UnityEngine;

namespace Project.Gameplay.Station.Views
{
	public sealed class StationActionButtonInteractableView : PlayerInteractableView
	{
		[SerializeField] private TMP_Text _labelText;
		[SerializeField] private Vector3 _labelLocalPosition = new(0f, 1.25f, 0f);
		[SerializeField] private Vector3 _labelLocalScale = new(0.08f, 0.08f, 0.08f);
		[SerializeField] private float _labelFontSize = 2.8f;
		[SerializeField] private string _defaultLabel = "Stand by";
		[SerializeField] private Color _readyColor = new(0.82f, 0.88f, 0.7f);
		[SerializeField] private Color _blockedColor = new(0.9f, 0.45f, 0.35f);
		[SerializeField] private Color _neutralColor = new(0.85f, 0.82f, 0.72f);

		private Coroutine _feedbackRoutine;
		private string _currentLabel;
		private DisplayState _currentState = DisplayState.Neutral;

		public event Action Interacted;

		protected override void Awake()
		{
			base.Awake();
			EnsureLabel();
			SetDisplay(_defaultLabel, DisplayState.Neutral);
		}

		public override void Interact()
		{
			Interacted?.Invoke();
		}

		public void SetDisplay(string label, DisplayState state)
		{
			_currentLabel = string.IsNullOrWhiteSpace(label) ? _defaultLabel : label;
			_currentState = state;

			if (_feedbackRoutine == null)
			{
				ApplyDisplay(_currentLabel, _currentState);
			}
		}

		public void ShowTemporaryFeedback(string label, DisplayState state, float duration = 1.15f)
		{
			EnsureLabel();
			if (!_labelText)
			{
				return;
			}

			if (_feedbackRoutine != null)
			{
				StopCoroutine(_feedbackRoutine);
			}

			_feedbackRoutine = StartCoroutine(ShowTemporaryFeedbackRoutine(label, state, duration));
		}

		private IEnumerator ShowTemporaryFeedbackRoutine(string label, DisplayState state, float duration)
		{
			ApplyDisplay(label, state);
			yield return new WaitForSeconds(Mathf.Max(0.05f, duration));
			_feedbackRoutine = null;
			ApplyDisplay(_currentLabel, _currentState);
		}

		private void EnsureLabel()
		{
			_labelText ??= StationWorldTextFactory.CreateBillboardLabel(
				transform,
				"ActionLabel",
				_labelLocalPosition,
				_labelLocalScale,
				_labelFontSize,
				TextAlignmentOptions.Center);
		}

		private void ApplyDisplay(string label, DisplayState state)
		{
			EnsureLabel();
			if (!_labelText)
			{
				return;
			}

			_labelText.text = string.IsNullOrWhiteSpace(label) ? _defaultLabel : label;
			_labelText.color = ResolveColor(state);
		}

		private Color ResolveColor(DisplayState state)
		{
			switch (state)
			{
				case DisplayState.Ready:
					return _readyColor;
				case DisplayState.Blocked:
					return _blockedColor;
				default:
					return _neutralColor;
			}
		}

		public enum DisplayState
		{
			Neutral = 0,
			Ready = 1,
			Blocked = 2,
		}
	}
}
