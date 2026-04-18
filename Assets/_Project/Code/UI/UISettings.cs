using System;
using PlatformCore.Services.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISettings : UIBaseElement
{
	[Header("Controls")]
	[SerializeField] private Slider _master;
	[SerializeField] private Slider _music;
	[SerializeField] private Slider _sfx;

	public event Action<float> OnMasterChanged;
	public event Action<float> OnMusicChanged;
	public event Action<float> OnSfxChanged;
	public event Action OnCloseClicked;
	public event Action OnMainMenuClicked;

	protected override void OnAwake()
	{
		_master.onValueChanged.AddListener(v => OnMasterChanged?.Invoke(v));
		_music.onValueChanged.AddListener(v => OnMusicChanged?.Invoke(v));
		_sfx.onValueChanged.AddListener(v => OnSfxChanged?.Invoke(v));
	}

	public void SetValues(float master, float music, float sfx)
	{
		_master.SetValueWithoutNotify(master);
		_music.SetValueWithoutNotify(music);
		_sfx.SetValueWithoutNotify(sfx);
	}

	public void Close()
	{
		OnCloseClicked?.Invoke();
	}

	public void MainMenu()
	{
		OnMainMenuClicked?.Invoke();
	}
}

public sealed class UIDaySummary : UIBaseElement
{
	[Header("Labels")]
	[SerializeField] private TMP_Text _dayTitleText;
	[SerializeField] private TMP_Text _casesListText;
	[SerializeField] private TMP_Text _errorsListText;
	[SerializeField] private TMP_Text _totalFinesText;
	[SerializeField] private TMP_Text _totalPayoutText;
	[SerializeField] private TMP_Text _remainingDebtText;

	[Header("Formats")]
	[SerializeField] private string _dayTitleFormat = "Day {0} Summary";
	[SerializeField] private string _finesFormat = "Total Fines: {0}";
	[SerializeField] private string _payoutFormat = "Net Payout: {0:+#;-#;0}";
	[SerializeField] private string _remainingDebtFormat = "Remaining Debt: {0}";

	public event Action OnCloseClicked;

	public void SetSummary(
		int dayIndex,
		string casesList,
		string errorsList,
		int totalFines,
		int totalPayout,
		int remainingDebt)
	{
		if (_dayTitleText)
		{
			_dayTitleText.SetText(_dayTitleFormat, dayIndex);
		}

		if (_casesListText)
		{
			_casesListText.text = casesList;
		}

		if (_errorsListText)
		{
			_errorsListText.text = errorsList;
		}

		if (_totalFinesText)
		{
			_totalFinesText.SetText(_finesFormat, totalFines);
		}

		if (_totalPayoutText)
		{
			_totalPayoutText.SetText(_payoutFormat, totalPayout);
		}

		if (_remainingDebtText)
		{
			_remainingDebtText.SetText(_remainingDebtFormat, remainingDebt);
		}
	}

	public void Close()
	{
		OnCloseClicked?.Invoke();
	}
}
