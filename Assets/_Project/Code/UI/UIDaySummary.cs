using System;
using PlatformCore.Services.UI;
using PlatformCore.Services.UI.Styles;
using TMPro;
using UnityEngine;

public sealed class UIDaySummary : UIBaseElement
{
	private const string TitleFormat = "DAY {0:0}";
	private const string InspectedFormat = "Wagons checked: {0:0}";
	private const string IncomeFormat = "Earnings: {0:0}";
	private const string FinesFormat = "Mistake fines: {0:0}";
	private const string FinesNoneText = "Mistake fines: none";
	private const string NetPositiveFormat = "Net result: +{0:0}";
	private const string NetNegativeFormat = "Net result: -{0:0}";
	private const string NetNeutralText = "Net result: 0";
	private const string DebtAfterSettlementFormat = "Debt remaining: {0:0}";
	private const string DefaultLivingCostLabel = "Food and bills";
	private const string LivingCostNoneText = "Food and bills: none";
	private const string DefaultEndDayNote = "Night ledger closed. Report filed.";

	[Header("Labels")]
	[SerializeField] private TMP_Text _titleLabel;
	[SerializeField] private TMP_Text _inspectedLabel;
	[SerializeField] private TMP_Text _incomeLabel;
	[SerializeField] private TMP_Text _mistakeFinesLabel;
	[SerializeField] private TMP_Text _livingCostLabel;
	[SerializeField] private TMP_Text _netLabel;
	[SerializeField] private TMP_Text _debtAfterSettlementLabel;
	[SerializeField] private TMP_Text _noteLabel;

	[Header("Tween")]
	[SerializeField] private float _numberTweenDuration = 0.45f;

	[Header("Colors")]
	[SerializeField] private ColorStyleRef _neutralColorRef;
	[SerializeField] private ColorStyleRef _positiveColorRef;
	[SerializeField] private ColorStyleRef _negativeColorRef;

	public event Action OnCloseClicked;

	public void SetSummary(
		int dayIndex,
		int inspectedCount,
		int totalIncome,
		int totalFines,
		string livingCostLabel,
		int totalLivingCosts,
		int netResult,
		int remainingDebt,
		string endDayNote)
	{
		if (_titleLabel)
		{
			_titleLabel.SetText(TitleFormat, dayIndex);
		}

		EnsureRuntimeEndDayNoteLabel();
		ApplyEndDayNote(endDayNote);

		ApplyColors(totalIncome, totalFines, totalLivingCosts, netResult, remainingDebt);

		UINumberTweenUtils.Animate(_inspectedLabel, InspectedFormat, inspectedCount, _numberTweenDuration);
		UINumberTweenUtils.Animate(_incomeLabel, IncomeFormat, Mathf.Max(0, totalIncome), _numberTweenDuration);
		ApplyAmountOrNone(_mistakeFinesLabel, FinesFormat, FinesNoneText, totalFines);
		ApplyAmountOrNone(
			_livingCostLabel,
			BuildLivingCostFormat(livingCostLabel),
			BuildLivingCostNoneText(livingCostLabel),
			totalLivingCosts);
		ApplyNet(netResult);
		UINumberTweenUtils.Animate(_debtAfterSettlementLabel, DebtAfterSettlementFormat, remainingDebt, _numberTweenDuration);
	}

	public void StopTweens()
	{
		UINumberTweenUtils.Stop(_inspectedLabel);
		UINumberTweenUtils.Stop(_incomeLabel);
		UINumberTweenUtils.Stop(_mistakeFinesLabel);
		UINumberTweenUtils.Stop(_livingCostLabel);
		UINumberTweenUtils.Stop(_netLabel);
		UINumberTweenUtils.Stop(_debtAfterSettlementLabel);
	}

	private void ApplyColors(
		int totalIncome,
		int totalFines,
		int totalLivingCosts,
		int netResult,
		int remainingDebt)
	{
		SetColor(_inspectedLabel, _neutralColorRef);
		SetColor(_incomeLabel, totalIncome > 0 ? _positiveColorRef : _neutralColorRef);
		SetColor(_mistakeFinesLabel, totalFines > 0 ? _negativeColorRef : _neutralColorRef);
		SetColor(_livingCostLabel, totalLivingCosts > 0 ? _negativeColorRef : _neutralColorRef);
		SetColor(_netLabel, netResult > 0 ? _positiveColorRef : netResult < 0 ? _negativeColorRef : _neutralColorRef);
		SetColor(_debtAfterSettlementLabel, remainingDebt <= 0 ? _positiveColorRef : _negativeColorRef);
	}

	private static string BuildLivingCostFormat(string livingCostLabel)
	{
		var normalizedLabel = string.IsNullOrWhiteSpace(livingCostLabel)
			? DefaultLivingCostLabel
			: livingCostLabel.Trim();
		return $"{normalizedLabel}: {{0:0}}";
	}

	private static string BuildLivingCostNoneText(string livingCostLabel)
	{
		var normalizedLabel = string.IsNullOrWhiteSpace(livingCostLabel)
			? DefaultLivingCostLabel
			: livingCostLabel.Trim();
		return $"{normalizedLabel}: none";
	}

	private static void SetColor(TMP_Text text, ColorStyleRef colorRef)
	{
		if (!text)
		{
			return;
		}

		text.color = colorRef.Value;
	}

	private void EnsureRuntimeEndDayNoteLabel()
	{
		if (_noteLabel || !_debtAfterSettlementLabel)
		{
			return;
		}

		var noteObject = new GameObject(
			"EndDayNote",
			typeof(RectTransform),
			typeof(CanvasRenderer),
			typeof(TextMeshProUGUI));
		var noteRect = noteObject.GetComponent<RectTransform>();
		noteRect.SetParent(_debtAfterSettlementLabel.transform.parent, false);
		noteRect.anchorMin = new Vector2(0f, 0f);
		noteRect.anchorMax = new Vector2(1f, 0f);
		noteRect.pivot = new Vector2(0.5f, 0f);
		noteRect.sizeDelta = new Vector2(0f, 56f);
		noteRect.anchoredPosition = new Vector2(0f, 16f);

		_noteLabel = noteObject.GetComponent<TextMeshProUGUI>();
		_noteLabel.fontSize = 24f;
		_noteLabel.alignment = TextAlignmentOptions.Center;
		_noteLabel.fontStyle = FontStyles.Italic;
		_noteLabel.textWrappingMode = TextWrappingModes.Normal;
		_noteLabel.overflowMode = TextOverflowModes.Overflow;
		_noteLabel.raycastTarget = false;
		if (TMP_Settings.defaultFontAsset)
		{
			_noteLabel.font = TMP_Settings.defaultFontAsset;
		}
	}

	private void ApplyEndDayNote(string endDayNote)
	{
		if (!_noteLabel)
		{
			return;
		}

		_noteLabel.text = string.IsNullOrWhiteSpace(endDayNote)
			? DefaultEndDayNote
			: endDayNote.Trim();
		SetColor(_noteLabel, _neutralColorRef);
	}

	private void ApplyAmountOrNone(TMP_Text label, string format, string noneText, int value)
	{
		if (!label)
		{
			return;
		}

		var safeValue = Mathf.Max(0, value);
		if (safeValue == 0)
		{
			UINumberTweenUtils.Stop(label);
			label.SetText(noneText);
			return;
		}

		UINumberTweenUtils.Animate(label, format, safeValue, _numberTweenDuration);
	}

	private void ApplyNet(int netResult)
	{
		if (!_netLabel)
		{
			return;
		}

		if (netResult == 0)
		{
			UINumberTweenUtils.Stop(_netLabel);
			_netLabel.SetText(NetNeutralText);
			return;
		}

		UINumberTweenUtils.Animate(
			_netLabel,
			netResult > 0 ? NetPositiveFormat : NetNegativeFormat,
			Mathf.Abs(netResult),
			_numberTweenDuration);
	}

	public void Close()
	{
		OnCloseClicked?.Invoke();
	}
}
