using PlatformCore.Core.Lifecycle;
using PlatformCore.Services.Input;
using PlatformCore.Services.UI;
using Project.Bootstrap;
using Project.Gameplay.Scene;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class UIMainMenuController : BaseContextController<UIMainMenu>, IUpdatable
{
	private const float MaxWorldClickDistance = 100f;

	private readonly ICursorService _cursorService;
	private readonly IInputService _inputService;
	private readonly GameStartModel _gameStartModel;
	private readonly SceneContext _sceneContext;

	private bool _cursorUnlockedByMenu;
	private bool _isStartRequested;

	public UIMainMenuController(
		IUIService uiService,
		ICursorService cursorService,
		IInputService inputService,
		SceneContext sceneContext,
		GameStartModel gameStartModel)
		: base(uiService)
	{
		_cursorService = cursorService;
		_inputService = inputService;
		_sceneContext = sceneContext;
		_gameStartModel = gameStartModel;
	}

	protected override bool ShouldUnloadOnDeactivate => false;

	protected override void OnActivate()
	{
		_isStartRequested = false;
		if (_context != null)
		{
			_context.StartRequested += OnStartRequestedHandler;
			_context.Show();
		}

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

	public void OnUpdate(float _)
	{
		if (_isStartRequested || _inputService == null || _inputService.IsPrimaryActionPressedThisFrame == false)
		{
			return;
		}
		if (_cursorService != null && _cursorService.IsCursorLocked)
		{
			return;
		}

		var mouse = Mouse.current;
		if (mouse == null)
		{
			return;
		}

		var camera = Camera.main;
		if (!camera)
		{
			return;
		}

		var ray = camera.ScreenPointToRay(mouse.position.ReadValue());
		if (Physics.Raycast(ray, out var hit, MaxWorldClickDistance) == false)
		{
			return;
		}

		if (hit.collider == null)
		{
			return;
		}

		var configuredStart = _sceneContext ? _sceneContext.MainMenuStartInteractable : null;
		if (configuredStart)
		{
			if (hit.collider.GetComponentInParent<MainMenuStartInteractableView>() == configuredStart)
			{
				OnStartRequestedHandler();
			}

			return;
		}

		if (hit.collider.GetComponentInParent<MainMenuStartInteractableView>())
		{
			OnStartRequestedHandler();
		}
	}

	private void OnStartRequestedHandler()
	{
		if (_isStartRequested)
		{
			return;
		}

		_isStartRequested = true;
		_gameStartModel?.Start();

		if (_context != null && _context.IsShown())
		{
			_context.Hide();
		}
	}
}
