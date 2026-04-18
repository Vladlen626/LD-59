using System;
using PlatformCore.Services.UI;
using UnityEngine.UI;
using UnityEngine;

public sealed class UIMainMenu : UIBaseElement
{
	[SerializeField] private Button _startButton;

	public event Action StartRequested;

	protected override void OnAwake()
	{
		_startButton ??= GetComponentInChildren<Button>(true);
		if (_startButton)
		{
			_startButton.onClick.AddListener(StartGame);
		}
	}

	public void StartGame()
	{
		StartRequested?.Invoke();
	}
}
