using TMPro;
using UnityEngine;

namespace Project.Gameplay.Station.Views
{
	public sealed class StationInspectionInfoView : MonoBehaviour
	{
		[Header("References")]
		[SerializeField] private TMP_Text _forbiddenCargoTitle;
		[SerializeField] private TMP_Text _forbiddenCargoValue;
		[SerializeField] private TMP_Text _manifestTitle;
		[SerializeField] private TMP_Text _manifestCargoValue;
		[SerializeField] private TMP_Text _manifestDescriptionValue;
		[SerializeField] private TMP_Text _dispatchStatusValue;

		[Header("Runtime Layout")]
		[SerializeField] private bool _autoCreateDefaultTexts = true;
		[SerializeField] private Vector3 _titleLocalScale = new Vector3(0.08f, 0.08f, 0.08f);
		[SerializeField] private Vector3 _valueLocalScale = new Vector3(0.075f, 0.075f, 0.075f);

		[Header("Fallback Text")]
		[SerializeField] private string _emptyForbiddenCargoText = "No bans today";
		[SerializeField] private string _emptyManifestCargoText = "No papers";
		[SerializeField] private string _emptyManifestDescriptionText = "-";

		private static readonly Color HeadingColor = new(0.93f, 0.84f, 0.55f);
		private static readonly Color ValueColor = new(0.9f, 0.88f, 0.8f);

		private void Awake()
		{
			EnsureDefaultTexts();
			HideDispatchStatus();
			Clear();
		}

		public void SetForbiddenCargo(string text)
		{
			if (_forbiddenCargoTitle)
			{
				_forbiddenCargoTitle.text = "Today's Bans";
			}

			if (_forbiddenCargoValue)
			{
				_forbiddenCargoValue.text = string.IsNullOrWhiteSpace(text)
					? _emptyForbiddenCargoText
					: text;
			}
		}

		public void SetManifest(string cargoName, string description)
		{
			if (_manifestTitle)
			{
				_manifestTitle.text = "Current Papers";
			}

			if (_manifestCargoValue)
			{
				_manifestCargoValue.text = string.IsNullOrWhiteSpace(cargoName)
					? _emptyManifestCargoText
					: cargoName;
			}

			if (_manifestDescriptionValue)
			{
				_manifestDescriptionValue.text = string.IsNullOrWhiteSpace(description)
					? _emptyManifestDescriptionText
					: description;
			}
		}

		public void ClearManifest()
		{
			SetManifest(string.Empty, string.Empty);
		}

		public void SetDispatchState(string status, DispatchState dispatchState)
		{
			HideDispatchStatus();
		}

		public void Clear()
		{
			SetForbiddenCargo(string.Empty);
			ClearManifest();
			SetDispatchState(string.Empty, DispatchState.Neutral);
		}

		private void EnsureDefaultTexts()
		{
			if (_autoCreateDefaultTexts == false)
			{
				return;
			}

			_forbiddenCargoTitle ??= CreateText(
				"Today's Bans Title",
				new Vector3(-0.62f, 0.82f, 0f),
				_titleLocalScale,
				1.8f,
				HeadingColor);
			_forbiddenCargoValue ??= CreateText(
				"Today's Bans Value",
				new Vector3(-0.62f, 0.48f, 0f),
				_valueLocalScale,
				1.45f,
				ValueColor);
			_manifestTitle ??= CreateText(
				"Current Papers Title",
				new Vector3(-0.62f, -0.1f, 0f),
				_titleLocalScale,
				1.8f,
				HeadingColor);
			_manifestCargoValue ??= CreateText(
				"Current Papers Cargo",
				new Vector3(-0.62f, -0.44f, 0f),
				_valueLocalScale,
				1.55f,
				ValueColor);
			_manifestDescriptionValue ??= CreateText(
				"Current Papers Description",
				new Vector3(-0.62f, -0.75f, 0f),
				_valueLocalScale,
				1.25f,
				new Color(0.82f, 0.79f, 0.72f));
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

		private void HideDispatchStatus()
		{
			if (_dispatchStatusValue)
			{
				_dispatchStatusValue.gameObject.SetActive(false);
			}
		}

		public enum DispatchState
		{
			Neutral = 0,
			Ready = 1,
			Locked = 2,
		}
	}
}
