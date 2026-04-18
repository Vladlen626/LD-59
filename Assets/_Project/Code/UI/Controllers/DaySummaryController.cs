using System.Text;
using PlatformCore.Services;
using PlatformCore.Services.UI;
using Project.Gameplay.Station.Controllers;
using Project.Gameplay.Station.Domain;
using Project.Gameplay.Station.Runtime;

public sealed class DaySummaryController : BaseContextController<UIDaySummary>
{
	private readonly ICursorService _cursorService;
	private readonly StationFlowController _stationFlowController;
	private readonly StationSharedRuntimeContext _runtimeContext;
	private readonly ILoggerService _loggerService;
	private readonly StringBuilder _casesBuilder = new StringBuilder(256);
	private readonly StringBuilder _errorsBuilder = new StringBuilder(128);

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

		_casesBuilder.Clear();
		_errorsBuilder.Clear();

		var totalFines = 0;
		var totalPayout = 0;
		var hasErrors = false;

		for (var i = 0; i < records.Count; i++)
		{
			var record = records[i];
			var result = record.Result;
			var scenario = record.Scenario;

			var reward = result.IsCorrect ? scenario.RewardOnCorrectDecision : 0;
			var penalty = result.IsCorrect ? 0 : result.Penalty;
			totalFines += penalty;
			totalPayout += reward - penalty;

			if (_casesBuilder.Length > 0)
			{
				_casesBuilder.Append('\n');
			}

			_casesBuilder.Append("Case #");
			_casesBuilder.Append(record.CaseIndex);
			_casesBuilder.Append(": Manifest='");
			_casesBuilder.Append(scenario.Manifest.DeclaredCargoId);
			_casesBuilder.Append("', Actual='");
			_casesBuilder.Append(scenario.ActualCargoId);
			_casesBuilder.Append("', Decision=");
			_casesBuilder.Append(result.PlayerDecision);
			_casesBuilder.Append(", Verdict=");
			_casesBuilder.Append(result.Verdict);
			_casesBuilder.Append(", Penalty=");
			_casesBuilder.Append(penalty);
			_casesBuilder.Append(", Reward=");
			_casesBuilder.Append(reward);

			if (result.ErrorReasons.Count == 0)
			{
				continue;
			}

			hasErrors = true;
			for (var j = 0; j < result.ErrorReasons.Count; j++)
			{
				if (_errorsBuilder.Length > 0)
				{
					_errorsBuilder.Append('\n');
				}

				_errorsBuilder.Append("Case #");
				_errorsBuilder.Append(record.CaseIndex);
				_errorsBuilder.Append(": ");
				_errorsBuilder.Append(ToDisplayErrorReason(result.ErrorReasons[j]));
			}
		}

		if (_casesBuilder.Length == 0)
		{
			_casesBuilder.Append("No inspected cases.");
		}

		if (hasErrors == false)
		{
			_errorsBuilder.Append("No errors.");
		}

		_context.SetSummary(
			_runtimeContext.StationDay.DayIndex,
			_casesBuilder.ToString(),
			_errorsBuilder.ToString(),
			totalFines,
			totalPayout,
			_runtimeContext.Debt.RemainingDebt);

		ShowContext();
		_loggerService?.Log(
			$"[DaySummary] Shown. Cases={records.Count}, TotalFines={totalFines}, NetPayout={totalPayout}, RemainingDebt={_runtimeContext.Debt.RemainingDebt}.");
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

	private static string ToDisplayErrorReason(InspectionErrorReason reason)
	{
		switch (reason)
		{
			case InspectionErrorReason.AllowedForbiddenCargo:
				return "Allowed forbidden cargo.";
			case InspectionErrorReason.AllowedManifestMismatch:
				return "Allowed manifest mismatch.";
			case InspectionErrorReason.AllowedHiddenContraband:
				return "Allowed hidden contraband.";
			case InspectionErrorReason.AllowedIllegalMarker:
				return "Allowed illegal marker.";
			case InspectionErrorReason.RejectedCleanTrain:
				return "Rejected clean train.";
			default:
				return "Unknown error.";
		}
	}
}
