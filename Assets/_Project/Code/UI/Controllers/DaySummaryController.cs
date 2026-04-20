using Cysharp.Threading.Tasks;
using PlatformCore.Services;
using PlatformCore.Services.Audio;
using PlatformCore.Services.UI;
using PlatformCore.Services.UI.SplashScreen;
using Project.Gameplay.Station.Controllers;
using Project.Gameplay.Station.Runtime;
using Project.Infrastructure;

public sealed class DaySummaryController : BaseContextController<UIDaySummary>
{
	private readonly ICursorService _cursorService;
	private readonly StationFlowController _stationFlowController;
	private readonly StationSharedRuntimeContext _runtimeContext;
	private readonly UILetterController _uiLetterController;
	private readonly CampaignEndingController _campaignEndingController;
	private readonly ISplashScreenService _splashScreenService;
	private readonly IAudioService _audioService;
	private readonly ILoggerService _loggerService;
	private readonly System.Func<UniTask> _restartGameAsync;

	private bool _cursorUnlockedBySummary;
	private bool _isCloseTransitionInProgress;

	public DaySummaryController(
		IUIService uiService,
		ICursorService cursorService,
		StationFlowController stationFlowController,
		StationSharedRuntimeContext runtimeContext,
		UILetterController uiLetterController,
		CampaignEndingController campaignEndingController,
		ISplashScreenService splashScreenService,
		IAudioService audioService,
		ILoggerService loggerService,
		System.Func<UniTask> restartGameAsync)
		: base(uiService)
	{
		_cursorService = cursorService;
		_stationFlowController = stationFlowController;
		_runtimeContext = runtimeContext;
		_uiLetterController = uiLetterController;
		_campaignEndingController = campaignEndingController;
		_splashScreenService = splashScreenService;
		_audioService = audioService;
		_loggerService = loggerService;
		_restartGameAsync = restartGameAsync;
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		if (_context == null)
		{
			_loggerService?.LogWarning("[DaySummary] UIDaySummary context is missing. Summary screen is disabled.");
			return;
		}

		_context.Hide();
		_context.OnCloseClicked += OnCloseClickedHandler;
		_stationFlowController.DayFinished += OnDayFinishedHandler;
		if (_campaignEndingController != null)
		{
			_campaignEndingController.ContinueRequested += OnEndingContinueRequestedHandler;
			_campaignEndingController.CloseRequested += OnEndingCloseRequestedHandler;
		}
	}

	protected override void OnDeactivate()
	{
		_stationFlowController.DayFinished -= OnDayFinishedHandler;
		if (_campaignEndingController != null)
		{
			_campaignEndingController.ContinueRequested -= OnEndingContinueRequestedHandler;
			_campaignEndingController.CloseRequested -= OnEndingCloseRequestedHandler;
		}

		if (_context != null)
		{
			_context.OnCloseClicked -= OnCloseClickedHandler;
			HideContext();
		}

		base.OnDeactivate();
	}

	private void OnDayFinishedHandler()
	{
		if (_context == null)
		{
			return;
		}

		_uiLetterController?.HideLetter();
		_runtimeContext.RuntimeModel.SetCurrentWagonDoorOpen(false);

		var journal = _runtimeContext.InspectionJournal;
		var debt = _runtimeContext.Debt;
		var livingCost = _stationFlowController.CurrentDayContent?.LivingCost;

		_context.SetSummary(
			_runtimeContext.StationDay.DayIndex,
			journal.Count,
			debt.TotalEarnings,
			debt.TotalFines,
			livingCost?.Label,
			debt.TotalLivingCosts,
			debt.NetResult,
			debt.InitialDebt,
			debt.OutstandingDebt,
			BuildEndDayNote());

		ShowContext();
		_audioService?.PlaySound(ProjectAudioEventPaths.DayEnd);
		_loggerService?.Log(
			$"[DaySummary] Shown. Checked={journal.Count}, Income={debt.TotalEarnings}, Fines={debt.TotalFines}, " +
			$"LivingCosts={debt.TotalLivingCosts}, Net={debt.NetResult}, " +
			$"OutstandingDebt={debt.OutstandingDebt}, Balance={debt.RemainingDebt}.");
	}

	private void OnCloseClickedHandler()
	{
		HandleCloseClickedAsync().Forget();
	}

	private string BuildEndDayNote()
	{
		return string.IsNullOrWhiteSpace(_stationFlowController.DayFailureNote)
			? _stationFlowController.CurrentDayContent?.EndDayNote
			: _stationFlowController.DayFailureNote;
	}

	private async UniTaskVoid HandleCloseClickedAsync()
	{
		if (_isCloseTransitionInProgress)
		{
			return;
		}

		_isCloseTransitionInProgress = true;
		try
		{
			HideContext();
			if (_stationFlowController.ShouldRetryCurrentDay)
			{
				await _splashScreenService.FadeInAsync(ProjectSplashScreenSettings.DayAdvanceFadeInSeconds);
				if (_stationFlowController.TryRetryCurrentDay())
				{
					_loggerService?.Log("[DaySummary] Failure acknowledged. Retrying current day.");
					return;
				}

				await _splashScreenService.FadeOutAsync(ProjectSplashScreenSettings.DayAdvanceFallbackFadeOutSeconds);
				_loggerService?.LogWarning("[DaySummary] Current day retry was requested but failed.");
			}

			var hasNextDay = _stationFlowController.HasNextDay;
			if (hasNextDay)
			{
				await _splashScreenService.FadeInAsync(ProjectSplashScreenSettings.DayAdvanceFadeInSeconds);
			}

			if (await _stationFlowController.TryAdvanceToNextDayAsync())
			{
				_loggerService?.Log("[DaySummary] Continue requested. Advancing to next day.");
				return;
			}

			if (hasNextDay)
			{
				await _splashScreenService.FadeOutAsync(ProjectSplashScreenSettings.DayAdvanceFallbackFadeOutSeconds);
			}

			if (hasNextDay == false)
			{
				var isWin = _runtimeContext.Debt.IsCleared;
				_campaignEndingController.ShowEnding(
					isWin,
					_runtimeContext.Debt.OutstandingDebt,
					isWin && _stationFlowController.CanStartEndlessMode);
				_loggerService?.Log(
					$"[DaySummary] Campaign ended. Result={(isWin ? "Win" : "Lose")}, " +
					$"OutstandingDebt={_runtimeContext.Debt.OutstandingDebt}, Balance={_runtimeContext.Debt.RemainingDebt}.");
			}
		}
		finally
		{
			_isCloseTransitionInProgress = false;
		}
	}

	private void OnEndingContinueRequestedHandler()
	{
		HandleEndingContinueRequestedAsync().Forget();
	}

	private void OnEndingCloseRequestedHandler()
	{
		HandleEndingCloseRequestedAsync().Forget();
	}

	private async UniTaskVoid HandleEndingContinueRequestedAsync()
	{
		if (_isCloseTransitionInProgress)
		{
			return;
		}

		_isCloseTransitionInProgress = true;
		try
		{
			await _splashScreenService.FadeInAsync(ProjectSplashScreenSettings.DayAdvanceFadeInSeconds);
			if (await _stationFlowController.TryStartEndlessModeAsync())
			{
				_loggerService?.Log("[DaySummary] Endless mode requested from campaign ending.");
				return;
			}

			await _splashScreenService.FadeOutAsync(ProjectSplashScreenSettings.DayAdvanceFallbackFadeOutSeconds);
			_loggerService?.LogWarning("[DaySummary] Endless mode request failed.");
		}
		finally
		{
			_isCloseTransitionInProgress = false;
		}
	}

	private async UniTaskVoid HandleEndingCloseRequestedAsync()
	{
		if (_isCloseTransitionInProgress)
		{
			return;
		}

		_isCloseTransitionInProgress = true;
		try
		{
			if (_restartGameAsync != null)
			{
				_loggerService?.Log("[DaySummary] Campaign ending closed. Restarting game root.");
				await _restartGameAsync();
				return;
			}

			_loggerService?.LogWarning("[DaySummary] Campaign ending closed, but restart callback is missing.");
		}
		finally
		{
			_isCloseTransitionInProgress = false;
		}
	}

	private void ShowContext()
	{
		_context.Show();

		_cursorUnlockedBySummary = false;
		if (_cursorService.IsCursorLocked)
		{
			_cursorService.UnlockCursor();
			_cursorUnlockedBySummary = !_cursorService.IsCursorLocked;
		}
	}

	private void HideContext()
	{
		_context.StopTweens();

		if (_context.IsShown())
		{
			_context.Hide();
		}

		if (_cursorUnlockedBySummary && !_cursorService.IsCursorLocked)
		{
			_cursorService.LockCursor();
		}

		_cursorUnlockedBySummary = false;
	}
}
