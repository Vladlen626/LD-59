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
	private readonly CampaignEndingController _campaignEndingController;
	private readonly ISplashScreenService _splashScreenService;
	private readonly IAudioService _audioService;
	private readonly ILoggerService _loggerService;

	private bool _cursorUnlockedBySummary;
	private bool _isCloseTransitionInProgress;

	public DaySummaryController(
		IUIService uiService,
		ICursorService cursorService,
		StationFlowController stationFlowController,
		StationSharedRuntimeContext runtimeContext,
		CampaignEndingController campaignEndingController,
		ISplashScreenService splashScreenService,
		IAudioService audioService,
		ILoggerService loggerService)
		: base(uiService)
	{
		_cursorService = cursorService;
		_stationFlowController = stationFlowController;
		_runtimeContext = runtimeContext;
		_campaignEndingController = campaignEndingController;
		_splashScreenService = splashScreenService;
		_audioService = audioService;
		_loggerService = loggerService;
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
	}

	protected override void OnDeactivate()
	{
		_stationFlowController.DayFinished -= OnDayFinishedHandler;

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
			debt.RemainingDebt,
			_stationFlowController.CurrentDayContent?.EndDayNote);

		ShowContext();
		_audioService?.PlaySound(ProjectAudioEventPaths.DayEnd);
		_loggerService?.Log(
			$"[DaySummary] Shown. Checked={journal.Count}, Income={debt.TotalEarnings}, Fines={debt.TotalFines}, " +
			$"LivingCosts={debt.TotalLivingCosts}, Net={debt.NetResult}, RemainingDebt={debt.RemainingDebt}.");
	}

	private void OnCloseClickedHandler()
	{
		HandleCloseClickedAsync().Forget();
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
			var hasNextDay = _stationFlowController.HasNextDay;
			if (hasNextDay)
			{
				await _splashScreenService.FadeInAsync(ProjectSplashScreenSettings.DayAdvanceFadeInSeconds);
			}

			if (_stationFlowController.TryAdvanceToNextDay())
			{
				_loggerService?.Log("[DaySummary] Continue requested. Advancing to next authored day.");
				return;
			}

			if (hasNextDay)
			{
				await _splashScreenService.FadeOutAsync(ProjectSplashScreenSettings.DayAdvanceFallbackFadeOutSeconds);
			}

			if (hasNextDay == false)
			{
				var isWin = _runtimeContext.Debt.RemainingDebt <= 0;
				_campaignEndingController.ShowEnding(isWin, _runtimeContext.Debt.RemainingDebt);
				_loggerService?.Log(
					$"[DaySummary] Campaign ended. Result={(isWin ? "Win" : "Lose")}, RemainingDebt={_runtimeContext.Debt.RemainingDebt}.");
			}
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
