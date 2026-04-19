using UnityEngine;
using System;
using PlatformCore.Services.UI.Styles;
using TMPro;

namespace Project.Gameplay.Scene
{
	[DisallowMultipleComponent]
	public sealed class MainMenuStartInteractableView : MonoBehaviour
	{
		public event Action Clicked;

		[SerializeField] private TMP_Text _startLabel;
		[SerializeField] private ColorStyleRef _idleColorRef;
		[SerializeField] private ColorStyleRef _hoverColorRef;
		[SerializeField] private float _hoverScaleMultiplier = 1.08f;
		[SerializeField] private float _pressScaleMultiplier = 0.92f;
		[SerializeField] private float _scaleLerpSpeed = 14f;
		[SerializeField] private float _colorLerpSpeed = 10f;
		[SerializeField] private float _pressDuration = 0.08f;

		private Vector3 _baseScale;
		private bool _isHovered;
		private float _pressRemaining;

		private void Awake()
		{
			_baseScale = transform.localScale;
		}

		private void OnEnable()
		{
			_baseScale = transform.localScale;
			_startLabel.color = _idleColorRef.Value;
		}

		private void OnDisable()
		{
			_isHovered = false;
			_pressRemaining = 0f;
			transform.localScale = _baseScale;
		}

		private void OnMouseEnter()
		{
			_isHovered = true;
		}

		private void OnMouseExit()
		{
			_isHovered = false;
		}

		private void OnMouseDown()
		{
			_pressRemaining = _pressDuration;
			Clicked?.Invoke();
		}

		private void Update()
		{
			if (_pressRemaining > 0f)
			{
				_pressRemaining -= Time.deltaTime;
			}

			var multiplier = _isHovered ? _hoverScaleMultiplier : 1f;
			if (_pressRemaining > 0f)
			{
				multiplier *= _pressScaleMultiplier;
			}

			var targetScale = _baseScale * multiplier;
			transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * _scaleLerpSpeed);

			var targetColor = _isHovered ? _hoverColorRef.Value : _idleColorRef.Value;
			_startLabel.color = Color.Lerp(_startLabel.color, targetColor, Time.deltaTime * _colorLerpSpeed);
		}

	}
}
