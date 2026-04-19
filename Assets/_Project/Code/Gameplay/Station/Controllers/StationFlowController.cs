using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Core.Lifecycle;
using PlatformCore.Services;
using PlatformCore.Services.Notifications;
using Project.Gameplay.Station.Configs;
using Project.Gameplay.Station.Domain;
using Project.Gameplay.Station.Runtime;
using Project.Gameplay.Train;

namespace Project.Gameplay.Station.Controllers
{
	public sealed class StationFlowController : IBaseController, IActivatable, IDeactivatable
	{
		private readonly StationRuntimeModel _runtimeModel;
		private readonly InspectionJudge _inspectionJudge;
		private readonly StationCampaignProgressModel _campaignProgress;
		private readonly TrainSpawnController _trainSpawnController;
		private readonly TrainMovementController _trainMovementController;
		private readonly TrainCargoPresentationController _trainCargoPresentationController;
		private readonly IGlobalNotificationService _globalNotificationService;
		private readonly ILoggerService _loggerService;
		private bool _isActive;
		private bool _isTrainCallInProgress;
		private bool _isDayFinished;
		private string _forbiddenCargoLogValue = "<none>";

		public event Action<StationDayContent> DayStarted;
		public event Action DayFinished;

		public bool IsAwaitingTrainCall => _isActive &&
			_isTrainCallInProgress == false &&
			_runtimeModel.FlowState == StationFlowState.AwaitingTrainCall &&
			_runtimeModel.HasActiveScenario == false;

		public bool IsAwaitingDecision => _isActive &&
			_runtimeModel.FlowState == StationFlowState.AwaitingDecision &&
			_runtimeModel.HasActiveScenario;

		public bool IsDayIntroPending => _isActive &&
			_runtimeModel.FlowState == StationFlowState.DayIntroPending &&
			_runtimeModel.HasActiveScenario == false;

		public bool HasNextDay => _campaignProgress.HasNextDay;
		public bool IsCurrentDayLastAuthored => _campaignProgress.IsCurrentDayLastAuthored;
		public StationDayContent CurrentDayContent => _campaignProgress.CurrentDay;

		public StationFlowController(
			StationSharedRuntimeContext runtimeContext,
			InspectionJudge inspectionJudge,
			StationCampaignProgressModel campaignProgress,
			TrainSpawnController trainSpawnController,
			TrainCargoPresentationController trainCargoPresentationController,
			TrainMovementController trainMovementController,
			IGlobalNotificationService globalNotificationService,
			ILoggerService loggerService)
		{
			_runtimeModel = runtimeContext.RuntimeModel;
			_inspectionJudge = inspectionJudge;
			_campaignProgress = campaignProgress ?? new StationCampaignProgressModel(
				Array.Empty<StationDayContent>(),
				StationDayConfig.MaxTrainScenariosPerDay,
				false,
				6,
				StationDayConfig.MaxTrainScenariosPerDay);
			_trainSpawnController = trainSpawnController;
			_trainCargoPresentationController = trainCargoPresentationController;
			_trainMovementController = trainMovementController;
			_globalNotificationService = globalNotificationService;
			_loggerService = loggerService;
		}

		public void Activate()
		{
			if (_isActive)
			{
				return;
			}

			_isActive = true;
			StartDay();
		}

		public void Deactivate()
		{
			if (_isActive == false)
			{
				return;
			}

			_isActive = false;
			_isTrainCallInProgress = false;
			_trainSpawnController.ClearCurrentTrain();
			_runtimeModel.ClearActiveScenario();
			_runtimeModel.SetActiveCaseIndex(-1);
			_runtimeModel.SetTrainTransitState(TrainTransitState.None);
			_runtimeModel.SetFlowState(StationFlowState.Inactive);
		}

		public void StartDay()
		{
			if (_isActive == false)
			{
				return;
			}

			var currentDay = _campaignProgress.CurrentDay;
			if (currentDay == null)
			{
				_loggerService?.LogError("[StationFlow] No campaign day is available to start.");
				FinishDay();
				return;
			}

			_runtimeModel.StationDay.SetDay(currentDay.RuleSet);
			_runtimeModel.Debt.Reset(currentDay.InitialDebt);
			_runtimeModel.InspectionJournal.Clear();
			_trainSpawnController.ClearCurrentTrain();
			_runtimeModel.ClearActiveScenario();
			_runtimeModel.SetActiveCaseIndex(-1);
			_runtimeModel.SetTrainTransitState(TrainTransitState.None);
			_runtimeModel.SetFlowState(StationFlowState.DayIntroPending);
			_isDayFinished = false;
			UpdateForbiddenCargoLogValue();
			_loggerService?.Log(
				$"[StationFlow] Day {_runtimeModel.StationDay.DayIndex} started. " +
				$"CampaignDay={_campaignProgress.CurrentDayOrder}/{_campaignProgress.TotalDays}, " +
				$"Cases={CurrentDayScenarios.Count}, InitialDebt={_runtimeModel.Debt.InitialDebt}.");
			if (DayStarted == null)
			{
				TryCompleteDayIntro();
				return;
			}

			DayStarted?.Invoke(currentDay);
		}

		public bool TryAdvanceToNextDay()
		{
			if (_isActive == false || _isDayFinished == false)
			{
				return false;
			}

			if (_campaignProgress.TryAdvanceToNextDay() == false)
			{
				return false;
			}

			StartDay();
			return true;
		}

		public bool TryCallNextTrain()
		{
			if (!IsAwaitingTrainCall)
			{
				return false;
			}

			var nextIndex = FindNextCaseIndex(_runtimeModel.ActiveCaseIndex + 1);
			if (nextIndex < 0)
			{
				FinishDay();
				return false;
			}

			CallNextTrainAsync().Forget();
			_loggerService?.Log(
				$"[StationFlow] Train call accepted. Preparing case {nextIndex + 1}.");
			return true;
		}

		public bool TryCompleteDayIntro()
		{
			if (_isActive == false || _isDayFinished || IsDayIntroPending == false)
			{
				return false;
			}

			_loggerService?.Log(
				$"[StationFlow] Day intro completed for day {_runtimeModel.StationDay.DayIndex}. Gameplay unlocked.");
			EnterAwaitingTrainCall();
			return true;
		}

		private async UniTaskVoid CallNextTrainAsync()
		{
			_isTrainCallInProgress = true;

			try
			{
				if (_isActive == false)
				{
					return;
				}

				_runtimeModel.SetFlowState(StationFlowState.CallNextTrain);

				var nextIndex = FindNextCaseIndex(_runtimeModel.ActiveCaseIndex + 1);
				while (_isActive && nextIndex >= 0)
				{
					var nextScenario = CurrentDayScenarios[nextIndex];
					_runtimeModel.SetActiveCaseIndex(nextIndex);
					_runtimeModel.SetActiveScenario(nextScenario);

					var trainView = await _trainSpawnController.SpawnCurrentTrainAsync();
					if (_isActive == false)
					{
						return;
					}
					if (!trainView)
					{
						_loggerService?.LogWarning(
							$"[StationFlow] Failed to spawn train for case index {nextIndex}. Skipping case.");
						_runtimeModel.ClearActiveScenario();
						nextIndex = FindNextCaseIndex(nextIndex + 1);
						continue;
					}

					var presented = await _trainCargoPresentationController.PresentScenarioAsync(trainView, nextScenario);
					if (_isActive == false)
					{
						return;
					}
					if (!presented)
					{
						_loggerService?.LogWarning(
							$"[StationFlow] Failed to present cargo for case index {nextIndex}. Skipping case.");
						_trainSpawnController.ClearCurrentTrain();
						_runtimeModel.ClearActiveScenario();
						nextIndex = FindNextCaseIndex(nextIndex + 1);
						continue;
					}

					var arrived = await _trainMovementController.PlayArrivalAsync();
					if (_isActive == false)
					{
						return;
					}
					if (!arrived)
					{
						_loggerService?.LogWarning(
							$"[StationFlow] Train arrival was interrupted for case index {nextIndex}. Skipping case.");
						_trainSpawnController.ClearCurrentTrain();
						_runtimeModel.ClearActiveScenario();
						nextIndex = FindNextCaseIndex(nextIndex + 1);
						continue;
					}

					LogCaseDetails(nextScenario, nextIndex);
					BeginInspection();
					return;
				}

				FinishDay();
			}
			finally
			{
				_isTrainCallInProgress = false;
			}
		}

		public bool BeginInspection()
		{
			if (_isActive == false || _runtimeModel.HasActiveScenario == false)
			{
				return false;
			}

			_runtimeModel.SetFlowState(StationFlowState.BeginInspection);
			EnterAwaitingDecision();
			return true;
		}

		public bool ResolveCase(PlayerDecision playerDecision, out InspectionResult result)
		{
			result = null;

			if (_isActive == false ||
				_runtimeModel.FlowState != StationFlowState.AwaitingDecision ||
				_runtimeModel.HasActiveScenario == false)
			{
				return false;
			}

			if (_runtimeModel.HasInspectedCurrentWagon == false)
			{
				_loggerService?.LogWarning(
					$"[StationFlow] Dispatch blocked for case {_runtimeModel.ActiveCaseIndex + 1}. Inspect the wagon first.");
				return false;
			}

			if (_runtimeModel.IsCurrentWagonDoorOpen)
			{
				_loggerService?.LogWarning(
					$"[StationFlow] Dispatch blocked for case {_runtimeModel.ActiveCaseIndex + 1}. Close the wagon door first.");
				return false;
			}

			_runtimeModel.SetFlowState(StationFlowState.ResolveCase);

			var scenario = _runtimeModel.ActiveScenario;
			result = _inspectionJudge.Evaluate(_runtimeModel.StationDay.CurrentRuleSet, scenario, playerDecision);
			_runtimeModel.Debt.ApplyInspectionOutcome(result, scenario.RewardOnCorrectDecision);

			var caseRecord = new InspectionCaseRecord(
				_runtimeModel.StationDay.DayIndex,
				_runtimeModel.ActiveCaseIndex + 1,
				scenario,
				result);
			_runtimeModel.InspectionJournal.AddRecord(caseRecord);
			_runtimeModel.StationDay.RegisterCompletedCase();
			LogResolveResult(result, scenario);
			ShowMistakeNotification(result);

			_runtimeModel.ClearActiveScenario();
			CompleteResolvedCaseAsync().Forget();
			return true;
		}

		private async UniTaskVoid CompleteResolvedCaseAsync()
		{
			await _trainMovementController.PlayDepartureAsync();
			if (_isActive == false)
			{
				return;
			}

			EnterAwaitingTrainCall();
		}

		private void EnterAwaitingDecision()
		{
			_runtimeModel.SetFlowState(StationFlowState.AwaitingDecision);
		}

		private void EnterAwaitingTrainCall()
		{
			var nextIndex = FindNextCaseIndex(_runtimeModel.ActiveCaseIndex + 1);
			if (nextIndex < 0)
			{
				FinishDay();
				return;
			}

			_runtimeModel.SetFlowState(StationFlowState.AwaitingTrainCall);
			_loggerService?.Log(
				$"[StationFlow] Awaiting train call for case {nextIndex + 1}.");
		}

		private void FinishDay()
		{
			if (_isDayFinished)
			{
				return;
			}

			var livingCost = CurrentDayContent?.LivingCost;
			_runtimeModel.Debt.ApplyLivingCost(livingCost?.Amount ?? 0);
			_isDayFinished = true;
			_trainSpawnController.ClearCurrentTrain();
			_runtimeModel.ClearActiveScenario();
			_runtimeModel.SetFlowState(StationFlowState.FinishDay);
			DayFinished?.Invoke();
			_loggerService?.Log(
				$"[StationFlow] Day {_runtimeModel.StationDay.DayIndex} finished. " +
				$"Cases={_runtimeModel.StationDay.CompletedCasesCount}, " +
				$"Debt={_runtimeModel.Debt.RemainingDebt}, Fines={_runtimeModel.Debt.TotalFines}, Earnings={_runtimeModel.Debt.TotalEarnings}, " +
				$"LivingCosts={_runtimeModel.Debt.TotalLivingCosts}, Net={_runtimeModel.Debt.NetResult}, " +
				$"HasNextDay={HasNextDay}.");
		}

		private int FindNextCaseIndex(int startIndex)
		{
			if (CurrentDayScenarios.Count == 0)
			{
				return -1;
			}

			var index = Math.Max(0, startIndex);
			for (; index < CurrentDayScenarios.Count; index++)
			{
				if (CurrentDayScenarios[index] != null)
				{
					return index;
				}
			}

			return -1;
		}

		private void LogCaseDetails(TrainScenario scenario, int caseIndex)
		{
			var ruleSet = _runtimeModel.StationDay.CurrentRuleSet;
			var isForbidden = HasForbiddenCargo(ruleSet, scenario.ActualCargo);

			_loggerService?.Log(
				$"[StationFlow] Begin inspection case {caseIndex + 1}: " +
				$"ManifestCargo='{scenario.Manifest.DeclaredCargo.ToDebugString()}', " +
				$"ManifestDesc='{scenario.Manifest.DeclaredDescription}', " +
				$"ActualCargo='{scenario.ActualCargo.ToDebugString()}', HiddenContraband={scenario.HasHiddenContraband}, " +
				$"IllegalMarker={scenario.HasIllegalMarker}, ForbiddenCargo=[{_forbiddenCargoLogValue}], " +
				$"ActualCargoForbidden={isForbidden}, " +
				$"Penalties(Missed={ruleSet.MissedContrabandPenalty}, Wrongful={ruleSet.WrongfulRejectionPenalty}).");
		}

		private void LogResolveResult(InspectionResult result, TrainScenario scenario)
		{
			var reasons = result.ErrorReasons.Count == 0
				? "<none>"
				: string.Join(", ", result.ErrorReasons);
			var reward = result.IsCorrect ? scenario.RewardOnCorrectDecision : 0;

			_loggerService?.Log(
				$"[StationFlow] Resolve case {_runtimeModel.ActiveCaseIndex + 1}: " +
				$"Player={result.PlayerDecision}, Expected={result.ExpectedDecision}, Verdict={result.Verdict}, " +
				$"Penalty={result.Penalty}, Reward={reward}, Reasons=[{reasons}], " +
				$"Debt={_runtimeModel.Debt.RemainingDebt}, JournalCount={_runtimeModel.InspectionJournal.Count}.");
		}

		private void ShowMistakeNotification(InspectionResult result)
		{
			if (result == null || result.IsCorrect || _globalNotificationService == null)
			{
				return;
			}

			_globalNotificationService.EnqueueToast(BuildMistakeNotificationMessage(result), true);
		}

		private static string BuildMistakeNotificationMessage(InspectionResult result)
		{
			if (result.PlayerDecision == PlayerDecision.SendBack)
			{
				return "Wrong reject. This train was clean.";
			}

			for (var i = 0; i < result.ErrorReasons.Count; i++)
			{
				switch (result.ErrorReasons[i])
				{
					case InspectionErrorReason.AllowedForbiddenCargo:
						return "Wrong allow. Banned cargo got through.";
					case InspectionErrorReason.AllowedManifestMismatch:
						return "Wrong allow. Papers did not match the cargo load.";
					case InspectionErrorReason.AllowedHiddenContraband:
						return "Wrong allow. Hidden contraband got through.";
					case InspectionErrorReason.AllowedIllegalMarker:
						return "Wrong allow. Illegal cargo got through.";
				}
			}

			return "Wrong allow. This train should have been rejected.";
		}

		private void UpdateForbiddenCargoLogValue()
		{
			var forbiddenCargoIds = _runtimeModel.StationDay.CurrentRuleSet.ForbiddenCargoIds;
			_forbiddenCargoLogValue = forbiddenCargoIds.Count == 0
				? "<none>"
				: string.Join(", ", forbiddenCargoIds);
		}

		private static bool HasForbiddenCargo(DayRuleSet ruleSet, CargoLoadout actualCargo)
		{
			if (ruleSet == null || actualCargo == null || actualCargo.IsEmpty)
			{
				return false;
			}

			for (var i = 0; i < actualCargo.Entries.Count; i++)
			{
				if (ruleSet.IsCargoForbidden(actualCargo.Entries[i].CargoId))
				{
					return true;
				}
			}

			return false;
		}

		private IReadOnlyList<TrainScenario> CurrentDayScenarios
		{
			get
			{
				var currentDay = _campaignProgress.CurrentDay;
				return currentDay?.Scenarios ?? Array.Empty<TrainScenario>();
			}
		}
	}
}
