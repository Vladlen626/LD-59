using PlatformCore.Services.Audio;
using PlatformCore.Services.Input;
using PlatformCore.Services.UI;

public sealed class UISettingsController : BaseContextController<UISettings>
{
	private readonly IAudioService _audioService;
	private readonly ICursorService _cursorService;
	private readonly IInputService _inputService;

	private bool _cursorUnlockedBySettings;

	public UISettingsController(
		IUIService uiService,
		IAudioService audioService,
		ICursorService cursorService,
		IInputService inputService)
		: base(uiService)
	{
		_audioService = audioService;
		_cursorService = cursorService;
		_inputService = inputService;
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		_context.Hide();

		_context.OnMasterChanged += OnMasterChangedHandler;
		_context.OnMusicChanged += OnMusicChangedHandler;
		_context.OnSfxChanged += OnSfxChangedHandler;
		_context.OnCloseClicked += OnCloseClickedHandler;
		_context.OnMainMenuClicked += OnMainMenuClickedHandler;
		_inputService.OnPausePressed += OnPausePressedHandler;
	}

	protected override void OnDeactivate()
	{
		_inputService.OnPausePressed -= OnPausePressedHandler;
		_context.OnMainMenuClicked -= OnMainMenuClickedHandler;
		_context.OnCloseClicked -= OnCloseClickedHandler;
		_context.OnSfxChanged -= OnSfxChangedHandler;
		_context.OnMusicChanged -= OnMusicChangedHandler;
		_context.OnMasterChanged -= OnMasterChangedHandler;

		base.OnDeactivate();
	}

	private void ShowContext()
	{
		_context.SetValues(_audioService.MasterVolume, _audioService.MusicVolume, _audioService.SfxVolume);
		_context.Show();

		_cursorUnlockedBySettings = false;
		if (_cursorService.IsCursorLocked)
		{
			_cursorService.UnlockCursor();
			_cursorUnlockedBySettings = !_cursorService.IsCursorLocked;
		}
	}

	private void HideContext()
	{
		_context.Hide();

		if (_cursorUnlockedBySettings && !_cursorService.IsCursorLocked)
		{
			_cursorService.LockCursor();
		}

		_cursorUnlockedBySettings = false;
	}

	private void OnMasterChangedHandler(float value)
	{
		_audioService.SetMasterVolume(value);
	}

	private void OnMusicChangedHandler(float value)
	{
		_audioService.SetMusicVolume(value);
	}

	private void OnSfxChangedHandler(float value)
	{
		_audioService.SetSfxVolume(value);
	}

	private void OnCloseClickedHandler()
	{
		HideContext();
	}

	private void OnMainMenuClickedHandler()
	{
		HideContext();
	}

	private void OnPausePressedHandler()
	{
		if (_context.IsShown())
		{
			HideContext();
		}
		else
		{
			ShowContext();
		}
	}
}
