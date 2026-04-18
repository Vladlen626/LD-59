using System;
using PlatformCore.Services.UI;
using PlatformCore.Services.UI.Styles;
using TMPro;
using UnityEngine;

public sealed class UIDaySummary : UIBaseElement
{
	[Header("Labels")]
	[SerializeField] private TMP_Text _title;
	[SerializeField] private TMP_Text _checked;
	[SerializeField] private TMP_Text _correct;
	[SerializeField] private TMP_Text _mistakes;
	[SerializeField] private TMP_Text _fines;
	[SerializeField] private TMP_Text _pay;
	[SerializeField] private TMP_Text _debtLeft;

	[Header("Formats")]
	[SerializeField] private string _titleFormat = "DAY {0:0}";
	[SerializeField] private string _checkedFormat = "CHECKED {0:0}";
	[SerializeField] private string _correctFormat = "CORRECT {0:0}";
	[SerializeField] private string _mistakesFormat = "MISTAKES {0:0}";
	[SerializeField] private string _finesFormat = "FINES -{0:0}";
	[SerializeField] private string _payFormat = "PAY +{0:0}";
	[SerializeField] private string _debtLeftFormat = "DEBT LEFT {0:0}";

	[Header("Tween")]
	[SerializeField] private float _numberWriteDuration = 0.45f;

	[Header("Colors")]
	[SerializeField] private ColorStyleRef _defaultColor;
	[SerializeField] private ColorStyleRef _positiveColor;
	[SerializeField] private ColorStyleRef _negativeColor;

	public event Action OnCloseClicked;

	public void SetSummary(
		int dayIndex,
		int checkedCount,
		int correctCount,
		int mistakesCount,
		int totalFines,
		int totalPayout,
		int remainingDebt)
	{
		if (_title)
		{
			_title.SetText(_titleFormat, dayIndex);
		}

		ApplyColors(correctCount, mistakesCount, totalFines, totalPayout);

		UINumberTweenUtils.Animate(_checked, _checkedFormat, checkedCount, _numberWriteDuration);
		UINumberTweenUtils.Animate(_correct, _correctFormat, correctCount, _numberWriteDuration);
		UINumberTweenUtils.Animate(_mistakes, _mistakesFormat, mistakesCount, _numberWriteDuration);
		UINumberTweenUtils.Animate(_fines, _finesFormat, totalFines, _numberWriteDuration);
		UINumberTweenUtils.Animate(_pay, _payFormat, totalPayout, _numberWriteDuration);
		UINumberTweenUtils.Animate(_debtLeft, _debtLeftFormat, remainingDebt, _numberWriteDuration);
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
		int correctCount,
		int mistakesCount,
		int totalFines,
		int totalPayout)
	{
		SetColor(_checked, _defaultColor);
		SetColor(_correct, correctCount > 0 ? _positiveColor : _defaultColor);
		SetColor(_mistakes, mistakesCount > 0 ? _negativeColor : _defaultColor);
		SetColor(_fines, totalFines > 0 ? _negativeColor : _defaultColor);
		SetColor(_pay, totalPayout > 0 ? _positiveColor : _defaultColor);
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
