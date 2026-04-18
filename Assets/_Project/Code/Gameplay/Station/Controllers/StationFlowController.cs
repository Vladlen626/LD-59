using System;
using System.Collections.Generic;
using PlatformCore.Core;
using PlatformCore.Core.Lifecycle;
using PlatformCore.Services;
using Project.Gameplay.Station.Domain;
using Project.Gameplay.Station.Runtime;

namespace Project.Gameplay.Station.Controllers
{
	public sealed class StationFlowController : IBaseController, IActivatable, IDeactivatable
	{
		private readonly StationRuntimeModel _runtimeModel;
		private readonly InspectionJudge _inspectionJudge;
		private readonly IReadOnlyList<TrainScenario> _dayScenarios;
		private readonly ILoggerService _loggerService;
		private bool _isActive;

		public StationFlowController(
			StationRuntimeModel runtimeModel,
			InspectionJudge inspectionJudge,
			IReadOnlyList<TrainScenario> dayScenarios,
			ILoggerService loggerService)
		{
			_runtimeModel = runtimeModel ?? throw new ArgumentNullException(nameof(runtimeModel));
			_inspectionJudge = inspectionJudge ?? throw new ArgumentNullException(nameof(inspectionJudge));
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
			_runtimeModel.ClearActiveScenario();
			_runtimeModel.SetActiveCaseIndex(-1);
			_runtimeModel.SetFlowState(StationFlowState.StartDay);

			CallNextTrain();
		}

		public bool CallNextTrain()
		{
			if (_isActive == false)
			{
				return false;
			}

			_runtimeModel.SetFlowState(StationFlowState.CallNextTrain);

			var nextIndex = _runtimeModel.ActiveCaseIndex;
			while (true)
			{
				nextIndex++;
				if (nextIndex >= _dayScenarios.Count)
				{
					FinishDay();
					return false;
				}

				var nextScenario = _dayScenarios[nextIndex];
				if (nextScenario == null)
				{
					continue;
				}

				_runtimeModel.SetActiveCaseIndex(nextIndex);
				_runtimeModel.SetActiveScenario(nextScenario);
				BeginInspection();
				return true;
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
			_runtimeModel.ClearActiveScenario();
			_runtimeModel.SetFlowState(StationFlowState.FinishDay);
			_loggerService?.Log(
				$"[StationFlow] Day {_runtimeModel.StationDay.DayIndex} finished. " +
				$"Cases={_runtimeModel.StationDay.CompletedCasesCount}, " +
				$"Debt={_runtimeModel.Debt.RemainingDebt}, Fines={_runtimeModel.Debt.TotalFines}, Earnings={_runtimeModel.Debt.TotalEarnings}.");
		}
	}
}
