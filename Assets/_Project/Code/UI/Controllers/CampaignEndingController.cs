using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Core.Lifecycle;
using PlatformCore.Services;
using PlatformCore.Services.Factory;
using PlatformCore.Services.Input;
using PlatformCore.Services.UI;
using Project.Infrastructure;
using UnityEngine;

public sealed class CampaignEndingController : IBaseController, IActivatable, IDeactivatable, IUpdatable
{
	private const string ClosePrompt = "E / Esc - Restart";
	private const string ContinuePrompt = "Continue Shift";
	private const string WinTitle = "Debt Cleared";
	private const string LoseTitle = "Debt Unpaid";

	private readonly IInputService _inputService;
	private readonly ICursorService _cursorService;
	private readonly ILoggerService _loggerService;
	private readonly IObjectFactory _objectFactory;

	private UICampaignEndingView _view;
	private bool _isActive;
	private bool _isOpen;
	private bool _cursorUnlockedByEnding;
	private int _openedFrame;
	private UniTask _viewLoadTask;
	private bool _isViewLoadInProgress;

	public event System.Action ContinueRequested;
	public event System.Action CloseRequested;

	public CampaignEndingController(
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
		HideEnding();

		if (_view)
		{
			_view.ContinueClicked -= OnContinueClicked;
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
			RequestClose();
		}
	}

	public void ShowEnding(bool isWin, int outstandingDebt, bool canContinue = false)
	{
		ShowEndingAsync(isWin, outstandingDebt, canContinue).Forget();
	}

	private async UniTaskVoid ShowEndingAsync(bool isWin, int outstandingDebt, bool canContinue)
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

		_view.SetContent(
			isWin ? WinTitle : LoseTitle,
			BuildBody(isWin, outstandingDebt),
			ClosePrompt);
		_view.SetContinueVisible(canContinue, ContinuePrompt);
		_view.gameObject.SetActive(true);
		_view.Show();
		_isOpen = true;
		_openedFrame = Time.frameCount;

		_cursorUnlockedByEnding = false;
		if (_cursorService.IsCursorLocked)
		{
			_cursorService.UnlockCursor();
			_cursorUnlockedByEnding = !_cursorService.IsCursorLocked;
		}

		_loggerService?.Log(
			$"[CampaignEnding] Shown. Result={(isWin ? "Win" : "Lose")}, OutstandingDebt={outstandingDebt}, " +
			$"CanContinue={canContinue}.");
	}

	public void HideEnding()
	{
		if (_isOpen == false)
		{
			return;
		}

		_isOpen = false;
		if (_view)
		{
			_view.Hide();
			_view.ClearContent();
			_view.gameObject.SetActive(false);
		}

		if (_cursorUnlockedByEnding && !_cursorService.IsCursorLocked)
		{
			_cursorService.LockCursor();
		}

		_cursorUnlockedByEnding = false;
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

		_isViewLoadInProgress = true;
		_viewLoadTask = CreateViewAsync();
		await _viewLoadTask;
	}

	private async UniTask CreateViewAsync()
	{
		try
		{
			var view = await _objectFactory.CreateAsync<UICampaignEndingView>(
				ProjectGameplayResourcePaths.UI_UICampaignEndingView,
				Vector3.zero,
				Quaternion.identity);
			if (!view)
			{
				return;
			}

			if (_isActive == false)
			{
				_objectFactory.Destroy(view.gameObject);
				return;
			}

			var parent = UICanvasResolver.Resolve(view.CanvasType);
			if (!parent)
			{
				_loggerService?.LogWarning($"[CampaignEnding] Could not resolve canvas '{view.CanvasType}' for runtime ending UI.");
				_objectFactory.Destroy(view.gameObject);
				return;
			}

			view.transform.SetParent(parent, false);
			view.gameObject.layer = parent.gameObject.layer;
			view.gameObject.SetActive(false);
			view.ContinueClicked += OnContinueClicked;
			_view = view;
		}
		finally
		{
			_isViewLoadInProgress = false;
		}
	}

	private static string BuildBody(bool isWin, int outstandingDebt)
	{
		if (isWin)
		{
			return "Dawn finds the ledger settled. The station has no more claim on your name. For the first time in a long while, the night is yours to leave behind.";
		}

		return
			"Morning comes before the account is cleared. The line stays open, the books stay red, and your name remains under the debt.\n\n" +
			$"Outstanding debt: {outstandingDebt}.";
	}

	private void OnContinueClicked()
	{
		if (_isOpen == false)
		{
			return;
		}

		ContinueRequested?.Invoke();
		HideEnding();
	}

	private void RequestClose()
	{
		if (_isOpen == false)
		{
			return;
		}

		HideEnding();
		CloseRequested?.Invoke();
	}
}
