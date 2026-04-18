using System;
using PlatformCore.Services.UI;
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
		if (_master)
		{
			_master.onValueChanged.AddListener(OnMasterSliderChanged);
		}

		if (_music)
		{
			_music.onValueChanged.AddListener(OnMusicSliderChanged);
		}

		if (_sfx)
		{
			_sfx.onValueChanged.AddListener(OnSfxSliderChanged);
		}
	}

	public void SetValues(float master, float music, float sfx)
	{
		if (_master)
		{
			_master.SetValueWithoutNotify(master);
		}

		if (_music)
		{
			_music.SetValueWithoutNotify(music);
		}

		if (_sfx)
		{
			_sfx.SetValueWithoutNotify(sfx);
		}
	}

	public void Close()
	{
		OnCloseClicked?.Invoke();
	}

	public void MainMenu()
	{
		OnMainMenuClicked?.Invoke();
	}

	private void OnMasterSliderChanged(float value)
	{
		OnMasterChanged?.Invoke(value);
	}

	private void OnMusicSliderChanged(float value)
	{
		OnMusicChanged?.Invoke(value);
	}

	private void OnSfxSliderChanged(float value)
	{
		OnSfxChanged?.Invoke(value);
	}
}
