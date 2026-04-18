using PlatformCore.Services;
using PlatformCore.Services.UI;
using Project.Gameplay.Station.Controllers;
using Project.Gameplay.Station.Runtime;

public sealed class DaySummaryController : BaseContextController<UIDaySummary>
{
	private readonly ICursorService _cursorService;
	private readonly StationFlowController _stationFlowController;
	private readonly StationSharedRuntimeContext _runtimeContext;
	private readonly ILoggerService _loggerService;

	private bool _cursorUnlockedBySummary;

	public DaySummaryController(
		IUIService uiService,
		ICursorService cursorService,
		StationFlowController stationFlowController,
		StationSharedRuntimeContext runtimeContext,
		ILoggerService loggerService)
		: base(uiService)
	{
		_cursorService = cursorService;
		_stationFlowController = stationFlowController;
		_runtimeContext = runtimeContext;
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
		var records = journal.Records;

		var checkedCount = records.Count;
		var correctCount = 0;
		var mistakesCount = 0;
		var totalFines = 0;
		var totalPayout = 0;

		for (var i = 0; i < records.Count; i++)
		{
			var record = records[i];
			var result = record.Result;
			var scenario = record.Scenario;

			if (result.IsCorrect)
			{
				correctCount++;
				totalPayout += scenario.RewardOnCorrectDecision;
				continue;
			}

			mistakesCount++;
			totalFines += result.Penalty;
		}

		_context.SetSummary(
			_runtimeContext.StationDay.DayIndex,
			checkedCount,
			correctCount,
			mistakesCount,
			totalFines,
			totalPayout,
			_runtimeContext.Debt.RemainingDebt);

		ShowContext();
		_loggerService?.Log(
			$"[DaySummary] Shown. Checked={checkedCount}, Correct={correctCount}, Mistakes={mistakesCount}, Pay={totalPayout}, Fines={totalFines}, RemainingDebt={_runtimeContext.Debt.RemainingDebt}.");
	}

	private void OnCloseClickedHandler()
	{
		HideContext();
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
