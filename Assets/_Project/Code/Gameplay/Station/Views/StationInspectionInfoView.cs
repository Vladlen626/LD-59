using TMPro;
using UnityEngine;

namespace Project.Gameplay.Station.Views
{
	public sealed class StationInspectionInfoView : MonoBehaviour
	{
		[Header("References")]
		[SerializeField] private TMP_Text _forbiddenCargoValue;
		[SerializeField] private TMP_Text _manifestCargoValue;
		[SerializeField] private TMP_Text _manifestDescriptionValue;
		[SerializeField] private TMP_Text _dispatchStatusValue;

		[Header("Fallback Text")]
		[SerializeField] private string _emptyForbiddenCargoText = "None";
		[SerializeField] private string _emptyManifestCargoText = "No active manifest";
		[SerializeField] private string _emptyManifestDescriptionText = "-";
		[SerializeField] private string _emptyDispatchStatusText = "Waiting for train";

		[Header("Dispatch Colors")]
		[SerializeField] private Color _dispatchReadyColor = new Color(0.22f, 0.62f, 0.24f);
		[SerializeField] private Color _dispatchLockedColor = new Color(0.74f, 0.18f, 0.14f);
		[SerializeField] private Color _dispatchNeutralColor = new Color(0.85f, 0.82f, 0.72f);

		private void Awake()
		{
			Clear();
		}

		public void SetForbiddenCargo(string text)
		{
			if (_forbiddenCargoValue)
			{
				_forbiddenCargoValue.text = string.IsNullOrWhiteSpace(text)
					? _emptyForbiddenCargoText
					: text;
			}
		}

		public void SetManifest(string cargoName, string description)
		{
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
			if (!_dispatchStatusValue)
			{
				return;
			}

			_dispatchStatusValue.text = string.IsNullOrWhiteSpace(status)
				? _emptyDispatchStatusText
				: status;
			_dispatchStatusValue.color = ResolveDispatchColor(dispatchState);
		}

		public void Clear()
		{
			SetForbiddenCargo(string.Empty);
			ClearManifest();
			SetDispatchState(string.Empty, DispatchState.Neutral);
		}

		private Color ResolveDispatchColor(DispatchState dispatchState)
		{
			switch (dispatchState)
			{
				case DispatchState.Ready:
					return _dispatchReadyColor;
				case DispatchState.Locked:
					return _dispatchLockedColor;
				default:
					return _dispatchNeutralColor;
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
