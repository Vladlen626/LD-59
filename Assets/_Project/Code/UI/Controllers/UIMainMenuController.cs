using PlatformCore.Services.UI;
using Project.Bootstrap;

public sealed class UIMainMenuController : BaseContextController<UIMainMenu>
{
	private readonly ICursorService _cursorService;
	private readonly GameStartModel _gameStartModel;

	private bool _cursorUnlockedByMenu;

	public UIMainMenuController(
		IUIService uiService,
		ICursorService cursorService,
		GameStartModel gameStartModel)
		: base(uiService)
	{
		_cursorService = cursorService;
		_gameStartModel = gameStartModel;
	}

	protected override bool ShouldUnloadOnDeactivate => false;

	protected override void OnActivate()
	{
		if (_context == null)
		{
			return;
		}

		_context.StartRequested += OnStartRequestedHandler;
		_context.Show();

		_cursorUnlockedByMenu = false;
		if (_cursorService != null && _cursorService.IsCursorLocked)
		{
			_cursorService.UnlockCursor();
			_cursorUnlockedByMenu = !_cursorService.IsCursorLocked;
		}
	}

	protected override void OnDeactivate()
	{
		if (_context != null)
		{
			_context.StartRequested -= OnStartRequestedHandler;
			_context.Hide();
		}

		if (_cursorUnlockedByMenu && _cursorService != null && !_cursorService.IsCursorLocked)
		{
			_cursorService.LockCursor();
		}

		_cursorUnlockedByMenu = false;
	}

	private void OnStartRequestedHandler()
	{
		_gameStartModel?.Start();

		if (_context != null && _context.IsShown())
		{
			_context.Hide();
		}
	}
}
