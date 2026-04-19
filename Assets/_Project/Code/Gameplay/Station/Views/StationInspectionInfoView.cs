using TMPro;
using UnityEngine;

namespace Project.Gameplay.Station.Views
{
	public sealed class StationInspectionInfoView : MonoBehaviour
	{
		[Header("Refs")]
		[SerializeField] private TMP_Text _bansTitleText;
		[SerializeField] private TMP_Text _bansValueText;
		[SerializeField] private TMP_Text _allowedTitleText;
		[SerializeField] private TMP_Text _allowedValueText;

		[Header("Runtime Layout")]
		[SerializeField] private bool _autoCreateDefaultTexts = true;
		[SerializeField] private Vector3 _titleLocalScale = new(0.08f, 0.08f, 0.08f);
		[SerializeField] private Vector3 _valueLocalScale = new(0.075f, 0.075f, 0.075f);

		[Header("Fallback Text")]
		[SerializeField] private string _emptyBansText = "No bans today";
		[SerializeField] private string _emptyAllowedText = "No papers";

		private static readonly Color HeadingColor = new(0.93f, 0.84f, 0.55f);
		private static readonly Color ValueColor = new(0.9f, 0.88f, 0.8f);

		private void Awake()
		{
			EnsureDefaultTexts();
			Clear();
		}

		public void SetBans(string text)
		{
			if (_bansTitleText)
			{
				_bansTitleText.text = "Today's Bans";
			}

			if (_bansValueText)
			{
				_bansValueText.text = string.IsNullOrWhiteSpace(text)
					? _emptyBansText
					: text;
			}
		}

		public void SetAllowedCargo(string text)
		{
			if (_allowedTitleText)
			{
				_allowedTitleText.text = "Manifest";
			}

			if (_allowedValueText)
			{
				_allowedValueText.text = string.IsNullOrWhiteSpace(text)
					? _emptyAllowedText
					: text;
			}
		}

		public void ClearAllowedCargo()
		{
			SetAllowedCargo(string.Empty);
		}

		public void Clear()
		{
			SetBans(string.Empty);
			ClearAllowedCargo();
		}

		private void EnsureDefaultTexts()
		{
			if (_autoCreateDefaultTexts == false)
			{
				return;
			}

			_bansTitleText ??= CreateText(
				"Bans Title",
				new Vector3(-0.62f, 0.72f, 0f),
				_titleLocalScale,
				1.8f,
				HeadingColor);
			_bansValueText ??= CreateText(
				"Bans Value",
				new Vector3(-0.62f, 0.36f, 0f),
				_valueLocalScale,
				1.45f,
				ValueColor);
			_allowedTitleText ??= CreateText(
				"Allowed Title",
				new Vector3(-0.62f, -0.12f, 0f),
				_titleLocalScale,
				1.8f,
				HeadingColor);
			_allowedValueText ??= CreateText(
				"Allowed Value",
				new Vector3(-0.62f, -0.46f, 0f),
				_valueLocalScale,
				1.5f,
				ValueColor);
		}

		private TMP_Text CreateText(
			string name,
			Vector3 localPosition,
			Vector3 localScale,
			float fontSize,
			Color color)
		{
			var text = StationWorldTextFactory.CreateBillboardLabel(
				transform,
				name,
				localPosition,
				localScale,
				fontSize,
				TextAlignmentOptions.TopLeft);
			text.color = color;
			return text;
		}
	}
}
