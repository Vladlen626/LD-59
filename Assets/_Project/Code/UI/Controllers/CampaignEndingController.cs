using PlatformCore.Core;
using PlatformCore.Core.Lifecycle;
using PlatformCore.Services;
using PlatformCore.Services.Input;
using PlatformCore.Services.UI;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class CampaignEndingController : IBaseController, IActivatable, IDeactivatable, IUpdatable
{
	private const string ClosePrompt = "E / Esc - Close Report";
	private const string WinTitle = "Debt Cleared";
	private const string LoseTitle = "Debt Unpaid";

	private readonly IInputService _inputService;
	private readonly ICursorService _cursorService;
	private readonly ILoggerService _loggerService;

	private UICampaignEndingView _view;
	private bool _isActive;
	private bool _isOpen;
	private bool _cursorUnlockedByEnding;
	private int _openedFrame;

	public CampaignEndingController(
		IInputService inputService,
		ICursorService cursorService,
		ILoggerService loggerService)
	{
		_inputService = inputService;
		_cursorService = cursorService;
		_loggerService = loggerService;
	}

	public bool IsOpen => _isOpen;

	public void Activate()
	{
		if (_isActive)
		{
			return;
		}

		_isActive = true;
		EnsureView();
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
			Object.Destroy(_view.gameObject);
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
			HideEnding();
		}
	}

	public void ShowEnding(bool isWin, int remainingDebt)
	{
		if (_isActive == false)
		{
			return;
		}

		EnsureView();
		if (!_view)
		{
			return;
		}

		_view.SetContent(
			isWin ? WinTitle : LoseTitle,
			BuildBody(isWin, remainingDebt),
			ClosePrompt);
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
			$"[CampaignEnding] Shown. Result={(isWin ? "Win" : "Lose")}, RemainingDebt={remainingDebt}.");
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

	private void EnsureView()
	{
		if (_view)
		{
			return;
		}

		var parent = ResolveCanvasTransform();
		if (!parent)
		{
			_loggerService?.LogWarning("[CampaignEnding] Could not resolve target canvas for runtime ending UI.");
			return;
		}

		var viewObject = new GameObject(
			nameof(UICampaignEndingView),
			typeof(RectTransform),
			typeof(CanvasGroup),
			typeof(UICampaignEndingView));
		viewObject.layer = parent.gameObject.layer;
		viewObject.transform.SetParent(parent, false);
		_view = viewObject.GetComponent<UICampaignEndingView>();
		viewObject.SetActive(false);
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

	private static string BuildBody(bool isWin, int remainingDebt)
	{
		if (isWin)
		{
			return "Dawn finds the ledger settled. The station has no more claim on your name. For the first time in a long while, the night is yours to leave behind.";
		}

		return
			"Morning comes before the account is cleared. The line stays open, the books stay red, and your name remains under the debt.\n\n" +
			$"Outstanding debt: {remainingDebt}.";
	}
}
