using System;
using PlatformCore.Services.UI;
using PlatformCore.Services.UI.Styles;
using TMPro;
using UnityEngine;

public sealed class UIDaySummary : UIBaseElement
{
	private const string TitleFormat = "SHIFT {0:0} REPORT";
	private const string InspectedFormat = "INSPECTED {0:0}";
	private const string IncomeFormat = "INCOME +{0:0}";
	private const string FinesFormat = "FINES -{0:0}";
	private const string NetPositiveFormat = "NET +{0:0}";
	private const string NetNegativeFormat = "NET -{0:0}";
	private const string NetNeutralFormat = "NET {0:0}";
	private const string DebtAfterSettlementFormat = "DEBT AFTER SETTLEMENT {0:0}";
	private const string DefaultLivingCostLabel = "LIVING COSTS";

	[Header("Labels")]
	[SerializeField] private TMP_Text _title;
	[SerializeField] private TMP_Text _checked;
	[SerializeField] private TMP_Text _correct;
	[SerializeField] private TMP_Text _mistakes;
	[SerializeField] private TMP_Text _fines;
	[SerializeField] private TMP_Text _pay;
	[SerializeField] private TMP_Text _debtLeft;

	[Header("Tween")]
	[SerializeField] private float _numberWriteDuration = 0.45f;

	[Header("Colors")]
	[SerializeField] private ColorStyleRef _defaultColor;
	[SerializeField] private ColorStyleRef _positiveColor;
	[SerializeField] private ColorStyleRef _negativeColor;

	public event Action OnCloseClicked;

	public void SetSummary(
		int dayIndex,
		int inspectedCount,
		int totalIncome,
		int totalFines,
		string livingCostLabel,
		int totalLivingCosts,
		int netResult,
		int remainingDebt)
	{
		if (_title)
		{
			_title.SetText(TitleFormat, dayIndex);
		}

		ApplyColors(totalIncome, totalFines, totalLivingCosts, netResult, remainingDebt);

		UINumberTweenUtils.Animate(_checked, InspectedFormat, inspectedCount, _numberWriteDuration);
		UINumberTweenUtils.Animate(_correct, IncomeFormat, totalIncome, _numberWriteDuration);
		UINumberTweenUtils.Animate(_mistakes, FinesFormat, totalFines, _numberWriteDuration);
		UINumberTweenUtils.Animate(_fines, BuildLivingCostFormat(livingCostLabel), totalLivingCosts, _numberWriteDuration);
		UINumberTweenUtils.Animate(_pay, ResolveNetFormat(netResult), Mathf.Abs(netResult), _numberWriteDuration);
		UINumberTweenUtils.Animate(_debtLeft, DebtAfterSettlementFormat, remainingDebt, _numberWriteDuration);
	}

	public void StopTweens()
	{
		UINumberTweenUtils.Stop(_checked);
		UINumberTweenUtils.Stop(_correct);
		UINumberTweenUtils.Stop(_mistakes);
		UINumberTweenUtils.Stop(_fines);
		UINumberTweenUtils.Stop(_pay);
		UINumberTweenUtils.Stop(_debtLeft);
	}

	private void ApplyColors(
		int totalIncome,
		int totalFines,
		int totalLivingCosts,
		int netResult,
		int remainingDebt)
	{
		SetColor(_checked, _defaultColor);
		SetColor(_correct, totalIncome > 0 ? _positiveColor : _defaultColor);
		SetColor(_mistakes, totalFines > 0 ? _negativeColor : _defaultColor);
		SetColor(_fines, totalLivingCosts > 0 ? _negativeColor : _defaultColor);
		SetColor(_pay, netResult > 0 ? _positiveColor : netResult < 0 ? _negativeColor : _defaultColor);
		SetColor(_debtLeft, remainingDebt <= 0 ? _positiveColor : _negativeColor);
	}

	private static string BuildLivingCostFormat(string livingCostLabel)
	{
		var normalizedLabel = string.IsNullOrWhiteSpace(livingCostLabel)
			? DefaultLivingCostLabel
			: livingCostLabel.Trim().ToUpperInvariant();
		return $"{normalizedLabel} -{{0:0}}";
	}

	private static string ResolveNetFormat(int netResult)
	{
		if (netResult > 0)
		{
			return NetPositiveFormat;
		}

		if (netResult < 0)
		{
			return NetNegativeFormat;
		}

		return NetNeutralFormat;
	}

	private static void SetColor(TMP_Text text, ColorStyleRef colorRef)
	{
		if (!text)
		{
			return;
		}

		text.color = colorRef.Value;
	}

	public void Close()
	{
		OnCloseClicked?.Invoke();
	}
}
