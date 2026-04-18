using System.Text;
using System;
using PlatformCore.Services.Audio;
using PlatformCore.Services.Input;
using PlatformCore.Services.UI;
using Project.Gameplay.Station.Controllers;
using Project.Gameplay.Station.Domain;
using Project.Gameplay.Station.Runtime;
using PlatformCore.Services;

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
		_cursorService = cursorService ?? throw new ArgumentNullException(nameof(cursorService));
		_stationFlowController = stationFlowController ?? throw new ArgumentNullException(nameof(stationFlowController));
		_runtimeContext = runtimeContext ?? throw new ArgumentNullException(nameof(runtimeContext));
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

		var casesBuilder = new StringBuilder();
		var errorsBuilder = new StringBuilder();

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

			if (casesBuilder.Length > 0)
			{
				casesBuilder.Append('\n');
			}

			casesBuilder.Append("Case #");
			casesBuilder.Append(record.CaseIndex);
			casesBuilder.Append(": Manifest='");
			casesBuilder.Append(scenario.Manifest.DeclaredCargoId);
			casesBuilder.Append("', Actual='");
			casesBuilder.Append(scenario.ActualCargoId);
			casesBuilder.Append("', Decision=");
			casesBuilder.Append(result.PlayerDecision);
			casesBuilder.Append(", Verdict=");
			casesBuilder.Append(result.Verdict);
			casesBuilder.Append(", Penalty=");
			casesBuilder.Append(penalty);
			casesBuilder.Append(", Reward=");
			casesBuilder.Append(reward);

			if (result.ErrorReasons.Count == 0)
			{
				continue;
			}

			hasErrors = true;
			for (var j = 0; j < result.ErrorReasons.Count; j++)
			{
				if (errorsBuilder.Length > 0)
				{
					errorsBuilder.Append('\n');
				}

				errorsBuilder.Append("Case #");
				errorsBuilder.Append(record.CaseIndex);
				errorsBuilder.Append(": ");
				errorsBuilder.Append(ToDisplayErrorReason(result.ErrorReasons[j]));
			}
		}

		if (casesBuilder.Length == 0)
		{
			casesBuilder.Append("No inspected cases.");
		}

		if (hasErrors == false)
		{
			errorsBuilder.Append("No errors.");
		}

		_context.SetSummary(
			_runtimeContext.StationDay.DayIndex,
			casesBuilder.ToString(),
			errorsBuilder.ToString(),
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
