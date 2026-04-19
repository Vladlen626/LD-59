using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Core.Lifecycle;
using PlatformCore.Services;
using PlatformCore.Services.Factory;
using PlatformCore.Services.Input;
using PlatformCore.Services.UI;
using Project.Gameplay.Station.Runtime;
using Project.Infrastructure;
using UnityEngine;

public sealed class UILetterController : IBaseController, IActivatable, IDeactivatable, IUpdatable
{
	private readonly IInputService _inputService;
	private readonly ICursorService _cursorService;
	private readonly ILoggerService _loggerService;
	private readonly IObjectFactory _objectFactory;

	private UILetterView _view;
	private bool _isActive;
	private bool _isOpen;
	private bool _cursorUnlockedByLetter;
	private int _openedFrame;
	private UniTaskCompletionSource _closeSource;
	private UniTask _viewLoadTask;
	private bool _isViewLoadInProgress;

	public UILetterController(
		IInputService inputService,
		ICursorService cursorService,
		ILoggerService loggerService,
		IObjectFactory objectFactory)
	{
		_inputService = inputService;
		_cursorService = cursorService;
		_loggerService = loggerService;
		_objectFactory = objectFactory;
	}

	public event System.Action LetterOpened;
	public event System.Action LetterClosed;

	public bool IsOpen => _isOpen;

	public void Activate()
	{
		if (_isActive)
		{
			return;
		}

		_isActive = true;
		EnsureViewAsync().Forget();
		if (_view)
		{
			_view.Hide();
			_view.gameObject.SetActive(false);
		}
	}

	public void Deactivate()
	{
		if (_isActive == false)
		{
			return;
		}

		_isActive = false;
		HideLetter();

		if (_view)
		{
			_objectFactory.Destroy(_view.gameObject);
			_view = null;
		}
	}

	public void OnUpdate(float _)
	{
		if (_isOpen == false || Time.frameCount <= _openedFrame + 1)
		{
			return;
		}

		if (_inputService.IsInteractPressedThisFrame ||
		    _inputService.IsUISubmitPressedThisFrame ||
		    _inputService.IsUICancelPressedThisFrame)
		{
			HideLetter();
		}
	}

	public void ShowLetter(NightShiftLetterData letterData)
	{
		ShowLetterAsync(letterData).Forget();
	}

	private async UniTaskVoid ShowLetterAsync(NightShiftLetterData letterData)
	{
		if (_isActive == false)
		{
			return;
		}

		await EnsureViewAsync();
		if (!_view)
		{
			return;
		}

		_view.SetLetter(letterData);
		_view.gameObject.SetActive(true);
		_view.Show();
		_isOpen = true;
		_openedFrame = Time.frameCount;
		_closeSource = new UniTaskCompletionSource();

		_cursorUnlockedByLetter = false;
		if (_cursorService.IsCursorLocked)
		{
			_cursorService.UnlockCursor();
			_cursorUnlockedByLetter = !_cursorService.IsCursorLocked;
		}

		LetterOpened?.Invoke();
	}

	public UniTask WaitUntilClosedAsync()
	{
		return _isOpen && _closeSource != null
			? _closeSource.Task
			: UniTask.CompletedTask;
	}

	public void HideLetter()
	{
		if (_isOpen == false)
		{
			return;
		}

		_isOpen = false;
		if (_view)
		{
			_view.Hide();
			_view.ClearLetter();
			_view.gameObject.SetActive(false);
		}

		if (_cursorUnlockedByLetter && !_cursorService.IsCursorLocked)
		{
			_cursorService.LockCursor();
		}

		_cursorUnlockedByLetter = false;
		_closeSource?.TrySetResult();
		_closeSource = null;
		LetterClosed?.Invoke();
	}

	private async UniTask EnsureViewAsync()
	{
		if (_view)
		{
			return;
		}

		if (_isViewLoadInProgress)
		{
			await _viewLoadTask;
			return;
		}

		var parent = ResolveCanvasTransform();
		if (!parent)
		{
			_loggerService?.LogWarning("[UILetter] Could not resolve target canvas for runtime letter UI.");
			return;
		}

		_isViewLoadInProgress = true;
		_viewLoadTask = CreateViewAsync(parent);
		await _viewLoadTask;
	}

	private async UniTask CreateViewAsync(Transform parent)
	{
		try
		{
			var view = await _objectFactory.CreateAsync<UILetterView>(
				ProjectGameplayResourcePaths.UI_UILetterView,
				Vector3.zero,
				Quaternion.identity,
				parent);
			if (!view)
			{
				return;
			}

			if (_isActive == false)
			{
				_objectFactory.Destroy(view.gameObject);
				return;
			}

			view.gameObject.layer = parent.gameObject.layer;
			view.gameObject.SetActive(false);
			_view = view;
		}
		finally
		{
			_isViewLoadInProgress = false;
		}
	}

	private static Transform ResolveCanvasTransform()
	{
		var canvases = Object.FindObjectsByType<Canvas>();
		Canvas fallbackCanvas = null;

		for (var i = 0; i < canvases.Length; i++)
		{
			var canvas = canvases[i];
			if (!canvas || !canvas.isActiveAndEnabled)
			{
				continue;
			}

			if (canvas.sortingOrder == (int)UICanvasType.Modal)
			{
				return canvas.transform;
			}

			if (fallbackCanvas == null || canvas.sortingOrder > fallbackCanvas.sortingOrder)
			{
				fallbackCanvas = canvas;
			}
		}

		return fallbackCanvas ? fallbackCanvas.transform : null;
	}
}
