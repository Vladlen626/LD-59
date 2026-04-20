using System;
using PlatformCore.Services.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UICampaignEndingView : UIBaseElement
{
	private const string DefaultContinueButtonText = "Continue Shift";

	[Header("References")]
	[SerializeField] private TMP_Text _titleLabel;
	[SerializeField] private TMP_Text _bodyLabel;
	[SerializeField] private TMP_Text _promptLabel;
	[SerializeField] private GameObject _continueButtonRoot;
	[SerializeField] private Button _continueButton;
	[SerializeField] private TMP_Text _continueButtonLabel;

	public event Action ContinueClicked;

	protected override void OnAwake()
	{
		BindContinueButton();
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
			_titleLabel.text = string.IsNullOrWhiteSpace(title) ? string.Empty : title;
		}

		if (_bodyLabel)
		{
			_bodyLabel.text = string.IsNullOrWhiteSpace(body) ? string.Empty : body;
		}

		if (_promptLabel)
		{
			_promptLabel.text = string.IsNullOrWhiteSpace(prompt) ? string.Empty : prompt;
		}
	}

	public void SetContinueVisible(bool isVisible, string label = DefaultContinueButtonText)
	{
		var buttonRoot = ResolveContinueButtonRoot();
		if (buttonRoot)
		{
			buttonRoot.SetActive(isVisible);
		}

		if (_continueButton)
		{
			_continueButton.interactable = isVisible;
		}

		if (_continueButtonLabel)
		{
			_continueButtonLabel.text = string.IsNullOrWhiteSpace(label)
				? DefaultContinueButtonText
				: label;
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

		SetContinueVisible(false);
	}

	private void ApplyCanvasGroupState()
	{
		_group.interactable = true;
		_group.blocksRaycasts = true;
	}

	private void BindContinueButton()
	{
		if (!_continueButton)
		{
			return;
		}

		_continueButton.onClick.RemoveListener(OnContinueButtonClicked);
		_continueButton.onClick.AddListener(OnContinueButtonClicked);
	}

	private GameObject ResolveContinueButtonRoot()
	{
		if (_continueButtonRoot)
		{
			return _continueButtonRoot;
		}

		return _continueButton ? _continueButton.gameObject : null;
	}

	private void OnContinueButtonClicked()
	{
		ContinueClicked?.Invoke();
	}
}
