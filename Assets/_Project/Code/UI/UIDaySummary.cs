using System;
using PrimeTween;
using PlatformCore.Services.UI;
using PlatformCore.Services.UI.Styles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIDaySummary : UIBaseElement
{
	private const string TitleFormat = "NIGHT {0:0}";
	private const string NumberValueFormat = "{0:0}";
	private const string NegativeValueFormat = "-{0:0}";
	private const string NoneValueText = "none";
	private const string NetPositiveValueFormat = "+{0:0}";
	private const string NetNegativeValueFormat = "-{0:0}";
	private const string NetNeutralValueText = "0";
	private const string InspectedNameText = "Wagons checked:";
	private const string IncomeNameText = "Earnings:";
	private const string FinesNameText = "Mistake fines:";
	private const string NetNameText = "Net result:";
	private const string DebtProgressNameText = "Debt payoff:";
	private const string DefaultLivingCostLabel = "Food and bills";
	private const string DefaultEndDayNote = "Night ledger closed. Report filed.";
	private const float DebtHighThreshold = 0.5f;
	private const float DebtMediumThreshold = 0.1f;

	[Header("Labels")]
	[SerializeField] private TMP_Text _titleLabel;
	[SerializeField] private UINameValueText _inspectedLabel;
	[SerializeField] private UINameValueText _incomeLabel;
	[SerializeField] private UINameValueText _mistakeFinesLabel;
	[SerializeField] private UINameValueText _livingCostLabel;
	[SerializeField] private UINameValueText _netLabel;
	[SerializeField] private UINameValueText _debtAfterSettlementLabel;
	[SerializeField] private TMP_Text _noteLabel;
	[SerializeField] private Slider _debtProgressSlider;
	[SerializeField] private Image _debtProgressFillImage;

	[Header("Tween")]
	[SerializeField] private float _numberTweenDuration = 0.45f;

	[Header("Colors")]
	[SerializeField] private ColorStyleRef _neutralColorRef;
	[SerializeField] private ColorStyleRef _positiveColorRef;
	[SerializeField] private ColorStyleRef _negativeColorRef;
	[SerializeField] private ColorStyleRef _warningColorRef;

	private Tween _debtProgressTween;

	public event Action OnCloseClicked;

	public void SetSummary(
		int dayIndex,
		int inspectedCount,
		int totalIncome,
		int totalFines,
		string livingCostLabel,
		int totalLivingCosts,
		int netResult,
		int initialDebt,
		int remainingDebt,
		string endDayNote)
	{
		if (_titleLabel)
		{
			_titleLabel.SetText(TitleFormat, dayIndex);
		}

		EnsureRuntimeEndDayNoteLabel();
		ApplyEndDayNote(endDayNote);

		ApplyColors(totalIncome, totalFines, totalLivingCosts, netResult, initialDebt, remainingDebt);

		ApplyNumber(_inspectedLabel, InspectedNameText, inspectedCount);
		ApplyNumber(_incomeLabel, IncomeNameText, Mathf.Max(0, totalIncome));
		ApplyExpenseOrNone(_mistakeFinesLabel, FinesNameText, totalFines);
		ApplyExpenseOrNone(
			_livingCostLabel,
			BuildLivingCostName(livingCostLabel),
			totalLivingCosts);
		ApplyNet(netResult);
		ApplyDebtProgress(initialDebt, remainingDebt);
	}

	public void StopTweens()
	{
		UINumberTweenUtils.Stop(_inspectedLabel ? _inspectedLabel.ValueText : null);
		UINumberTweenUtils.Stop(_incomeLabel ? _incomeLabel.ValueText : null);
		UINumberTweenUtils.Stop(_mistakeFinesLabel ? _mistakeFinesLabel.ValueText : null);
		UINumberTweenUtils.Stop(_livingCostLabel ? _livingCostLabel.ValueText : null);
		UINumberTweenUtils.Stop(_netLabel ? _netLabel.ValueText : null);
		UINumberTweenUtils.Stop(_debtAfterSettlementLabel ? _debtAfterSettlementLabel.ValueText : null);
		StopDebtProgressTween();
	}

	private void ApplyColors(
		int totalIncome,
		int totalFines,
		int totalLivingCosts,
		int netResult,
		int initialDebt,
		int remainingDebt)
	{
		SetColor(_inspectedLabel, _neutralColorRef);
		SetColor(_incomeLabel, totalIncome > 0 ? _positiveColorRef : _neutralColorRef);
		SetColor(_mistakeFinesLabel, totalFines > 0 ? _negativeColorRef : _neutralColorRef);
		SetColor(_livingCostLabel, totalLivingCosts > 0 ? _negativeColorRef : _neutralColorRef);
		SetColor(_netLabel, netResult > 0 ? _positiveColorRef : netResult < 0 ? _negativeColorRef : _neutralColorRef);
		var debtColorRef = ResolveDebtColor(initialDebt, remainingDebt);
		SetColor(_debtAfterSettlementLabel, debtColorRef);
		if (_debtProgressFillImage)
		{
			_debtProgressFillImage.color = debtColorRef.Value;
		}
	}

	private static string BuildLivingCostName(string livingCostLabel)
	{
		var normalizedLabel = string.IsNullOrWhiteSpace(livingCostLabel)
			? DefaultLivingCostLabel
			: livingCostLabel.Trim();
		return $"{normalizedLabel}:";
	}

	private static void SetColor(UINameValueText nameValueText, ColorStyleRef colorRef)
	{
		if (!nameValueText)
		{
			return;
		}

		var color = colorRef.Value;
		if (nameValueText.NameText)
		{
			nameValueText.NameText.color = color;
		}

		if (nameValueText.ValueText)
		{
			nameValueText.ValueText.color = color;
		}
	}

	private static void SetColor(TMP_Text text, ColorStyleRef colorRef)
	{
		if (!text)
		{
			return;
		}

		text.color = colorRef.Value;
	}

	private ColorStyleRef ResolveDebtColor(int initialDebt, int remainingDebt)
	{
		if (remainingDebt <= 0)
		{
			return _positiveColorRef;
		}

		var safeInitialDebt = Mathf.Max(1, initialDebt);
		var ratio = Mathf.Clamp01((float)remainingDebt / safeInitialDebt);
		if (ratio > DebtHighThreshold)
		{
			return _negativeColorRef;
		}

		if (ratio > DebtMediumThreshold)
		{
			return _warningColorRef;
		}

		return _neutralColorRef;
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

	private void ApplyNumber(UINameValueText label, string nameText, int value)
	{
		if (!label)
		{
			return;
		}

		label.SetName(nameText);
		UINumberTweenUtils.Animate(label.ValueText, NumberValueFormat, value, _numberTweenDuration);
	}

	private void ApplyExpenseOrNone(UINameValueText label, string nameText, int value)
	{
		if (!label)
		{
			return;
		}

		label.SetName(nameText);
		var safeValue = Mathf.Max(0, value);
		if (safeValue == 0)
		{
			UINumberTweenUtils.Stop(label.ValueText);
			label.SetValue(NoneValueText);
			return;
		}

		UINumberTweenUtils.Animate(label.ValueText, NegativeValueFormat, safeValue, _numberTweenDuration);
	}

	private void ApplyDebtProgress(int initialDebt, int remainingDebt)
	{
		var safeInitialDebt = Mathf.Max(0, initialDebt);
		var safeRemainingDebt = Mathf.Max(0, remainingDebt);
		var paidDebt = Mathf.Clamp(safeInitialDebt - safeRemainingDebt, 0, safeInitialDebt);

		if (_debtAfterSettlementLabel)
		{
			_debtAfterSettlementLabel.SetName(DebtProgressNameText);
			UINumberTweenUtils.Animate(
				_debtAfterSettlementLabel.ValueText,
				BuildDebtProgressValueFormat(safeInitialDebt),
				paidDebt,
				_numberTweenDuration);
		}

		ApplyDebtSlider(safeInitialDebt, paidDebt);
	}

	private static string BuildDebtProgressValueFormat(int initialDebt)
	{
		var safeInitialDebt = Mathf.Max(0, initialDebt);
		return $"{{0:0}}/{safeInitialDebt:0}";
	}

	private void ApplyDebtSlider(int initialDebt, int paidDebt)
	{
		if (!_debtProgressSlider)
		{
			return;
		}

		var targetValue = initialDebt <= 0 ? 1f : Mathf.Clamp01((float)paidDebt / initialDebt);
		StopDebtProgressTween();

		_debtProgressSlider.minValue = 0f;
		_debtProgressSlider.maxValue = 1f;
		if (_numberTweenDuration <= 0f)
		{
			_debtProgressSlider.value = targetValue;
			return;
		}

		_debtProgressTween = Tween.Custom(
			_debtProgressSlider,
			_debtProgressSlider.value,
			targetValue,
			_numberTweenDuration,
			static (slider, value) =>
			{
				if (slider)
				{
					slider.value = value;
				}
			});
	}

	private void StopDebtProgressTween()
	{
		if (_debtProgressTween.isAlive)
		{
			_debtProgressTween.Stop();
		}
	}

	private void ApplyNet(int netResult)
	{
		if (!_netLabel)
		{
			return;
		}

		_netLabel.SetName(NetNameText);
		if (netResult == 0)
		{
			UINumberTweenUtils.Stop(_netLabel.ValueText);
			_netLabel.SetValue(NetNeutralValueText);
			return;
		}

		UINumberTweenUtils.Animate(
			_netLabel.ValueText,
			netResult > 0 ? NetPositiveValueFormat : NetNegativeValueFormat,
			Mathf.Abs(netResult),
			_numberTweenDuration);
	}

	public void Close()
	{
		OnCloseClicked?.Invoke();
	}
}
