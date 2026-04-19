using PlatformCore.Services.UI;
using Project.Gameplay.Station.Runtime;
using UnityEngine;
using TMPro;

public sealed class UILetterView : UIBaseElement
{
	[Header("References")]
	[SerializeField] private TMP_Text _titleLabel;
	[SerializeField] private TMP_Text _bodyLabel;
	[SerializeField] private TMP_Text _hintLabel;
	[SerializeField] private TMP_Text _closePromptLabel;
	[SerializeField] private string _closePrompt = "E / Esc - Fold Letter";

	protected override void OnAwake()
	{
		ApplyCanvasGroupState();
		ClearLetter();
	}

	protected override void OnShow()
	{
		ApplyCanvasGroupState();
	}

	public void SetLetter(NightShiftLetterData letterData)
	{
		var title = string.IsNullOrWhiteSpace(letterData?.Title) ? string.Empty : letterData.Title;
		var body = string.IsNullOrWhiteSpace(letterData?.Body) ? string.Empty : letterData.Body;
		var hint = string.IsNullOrWhiteSpace(letterData?.Hint) ? string.Empty : letterData.Hint;

		if (_titleLabel)
		{
			_titleLabel.text = title;
		}

		if (_bodyLabel)
		{
			_bodyLabel.text = body;
		}

		if (_hintLabel)
		{
			_hintLabel.text = hint;
		}

		if (_closePromptLabel)
		{
			_closePromptLabel.text = _closePrompt;
		}
	}

	public void ClearLetter()
	{
		if (_titleLabel)
		{
			_titleLabel.text = string.Empty;
		}

		if (_bodyLabel)
		{
			_bodyLabel.text = string.Empty;
		}

		if (_hintLabel)
		{
			_hintLabel.text = string.Empty;
		}

		if (_closePromptLabel)
		{
			_closePromptLabel.text = string.Empty;
		}
	}

	private void ApplyCanvasGroupState()
	{
		_group.interactable = false;
		_group.blocksRaycasts = true;
	}
}
