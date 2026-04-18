using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Core.Lifecycle;
using PlatformCore.Services;
using Project.Gameplay.Station.Domain;
using Project.Gameplay.Station.Runtime;
using Project.Gameplay.Train;

namespace Project.Gameplay.Station.Controllers
{
	public sealed class StationFlowController : IBaseController, IActivatable, IDeactivatable
	{
		private readonly StationRuntimeModel _runtimeModel;
		private readonly InspectionJudge _inspectionJudge;
		private readonly TrainSpawnController _trainSpawnController;
		private readonly IReadOnlyList<TrainScenario> _dayScenarios;
		private readonly ILoggerService _loggerService;
		private bool _isActive;
		private bool _isTrainCallInProgress;

		public StationFlowController(
			StationRuntimeModel runtimeModel,
			InspectionJudge inspectionJudge,
			TrainSpawnController trainSpawnController,
			IReadOnlyList<TrainScenario> dayScenarios,
			ILoggerService loggerService)
		{
			_runtimeModel = runtimeModel ?? throw new ArgumentNullException(nameof(runtimeModel));
			_inspectionJudge = inspectionJudge ?? throw new ArgumentNullException(nameof(inspectionJudge));
			_trainSpawnController = trainSpawnController ?? throw new ArgumentNullException(nameof(trainSpawnController));
			_dayScenarios = dayScenarios ?? Array.Empty<TrainScenario>();
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
			_runtimeModel.SetFlowState(StationFlowState.Inactive);
		}

		public void StartDay()
		{
			if (_isActive == false)
			{
				return;
			}

			_runtimeModel.StationDay.SetDay(_runtimeModel.StationDay.CurrentRuleSet);
			_runtimeModel.InspectionJournal.Clear();
			_trainSpawnController.ClearCurrentTrain();
			_runtimeModel.ClearActiveScenario();
			_runtimeModel.SetActiveCaseIndex(-1);
			_runtimeModel.SetFlowState(StationFlowState.StartDay);

			CallNextTrain();
		}

		public void CallNextTrain()
		{
			if (_isActive == false || _isTrainCallInProgress)
			{
				return;
			}

			CallNextTrainAsync().Forget();
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

				var nextIndex = _runtimeModel.ActiveCaseIndex;
				while (_isActive)
				{
					nextIndex++;
					if (nextIndex >= _dayScenarios.Count)
					{
						FinishDay();
						return;
					}

					var nextScenario = _dayScenarios[nextIndex];
					if (nextScenario == null)
					{
						continue;
					}

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
						continue;
					}

					BeginInspection();
					return;
				}
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
			_trainSpawnController.ClearCurrentTrain();
			_runtimeModel.ClearActiveScenario();

			CallNextTrain();
			return true;
		}

		private void EnterAwaitingDecision()
		{
			_runtimeModel.SetFlowState(StationFlowState.AwaitingDecision);
		}

		private void FinishDay()
		{
			_trainSpawnController.ClearCurrentTrain();
			_runtimeModel.ClearActiveScenario();
			_runtimeModel.SetFlowState(StationFlowState.FinishDay);
			_loggerService?.Log(
				$"[StationFlow] Day {_runtimeModel.StationDay.DayIndex} finished. " +
				$"Cases={_runtimeModel.StationDay.CompletedCasesCount}, " +
				$"Debt={_runtimeModel.Debt.RemainingDebt}, Fines={_runtimeModel.Debt.TotalFines}, Earnings={_runtimeModel.Debt.TotalEarnings}.");
		}
	}
}
