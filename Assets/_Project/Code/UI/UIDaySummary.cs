using System;
using PlatformCore.Services.UI;
using TMPro;
using UnityEngine;

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
