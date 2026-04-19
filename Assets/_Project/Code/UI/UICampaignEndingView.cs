using PlatformCore.Services.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UICampaignEndingView : UIBaseElement
{
	[Header("References")]
	[SerializeField] private Image _overlay;
	[SerializeField] private Image _panel;
	[SerializeField] private TMP_Text _titleLabel;
	[SerializeField] private TMP_Text _bodyLabel;
	[SerializeField] private TMP_Text _promptLabel;

	protected override void OnAwake()
	{
		ApplyCanvasGroupState();
		ClearContent();
	}

	protected override void OnShow()
	{
		ApplyCanvasGroupState();
	}

	public void SetContent(string title, string body, string prompt)
	{
		if (_titleLabel)
		{
			_titleLabel.text = string.IsNullOrWhiteSpace(title) ? string.Empty : title.Trim();
		}

		if (_bodyLabel)
		{
			_bodyLabel.text = string.IsNullOrWhiteSpace(body) ? string.Empty : body.Trim();
		}

		if (_promptLabel)
		{
			_promptLabel.text = string.IsNullOrWhiteSpace(prompt) ? string.Empty : prompt.Trim();
		}
	}

	public void ClearContent()
	{
		if (_titleLabel)
		{
			_titleLabel.text = string.Empty;
		}

		if (_bodyLabel)
		{
			_bodyLabel.text = string.Empty;
		}

		if (_promptLabel)
		{
			_promptLabel.text = string.Empty;
		}
	}

	private void ApplyCanvasGroupState()
	{
		_group.interactable = false;
		_group.blocksRaycasts = true;
	}
}
