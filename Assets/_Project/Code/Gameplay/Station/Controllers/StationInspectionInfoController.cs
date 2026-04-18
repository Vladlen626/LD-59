using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Core.Lifecycle;
using PlatformCore.Services;
using Project.Gameplay.Scene;
using Project.Gameplay.Station.Configs;
using Project.Gameplay.Station.Domain;
using Project.Gameplay.Station.Runtime;
using Project.Gameplay.Station.Views;

namespace Project.Gameplay.Station.Controllers
{
	public sealed class StationInspectionInfoController : IBaseController, IActivatable, IDeactivatable
	{
		private const string WaitingForTrainStatus = "Waiting for train";
		private const string TrainInTransitStatus = "Train in transit";
		private const string InspectWagonFirstStatus = "Locked: inspect wagon first";
		private const string ReadyToDispatchStatus = "Ready to dispatch";
		private const string ReviewManifestStatus = "Review manifest";
		private const string NoneText = "None";

		private readonly SceneContext _sceneContext;
		private readonly StationRuntimeModel _runtimeModel;
		private readonly CargoCatalogLoader _cargoCatalogLoader;
		private readonly ILoggerService _loggerService;

		private IReadOnlyDictionary<string, CargoTypeConfig> _cargoConfigs;
		private string _forbiddenCargoText = NoneText;
		private bool _hasForbiddenCargoText;
		private bool _isCatalogLoadInProgress;
		private bool _isActive;

		public StationInspectionInfoController(
			SceneContext sceneContext,
			StationSharedRuntimeContext runtimeContext,
			CargoCatalogLoader cargoCatalogLoader,
			ILoggerService loggerService)
		{
			_sceneContext = sceneContext;
			_runtimeModel = runtimeContext?.RuntimeModel;
			_cargoCatalogLoader = cargoCatalogLoader;
			_loggerService = loggerService;
		}

		public void Activate()
		{
			if (_isActive)
			{
				return;
			}

			_isActive = true;

			if (_runtimeModel != null)
			{
				_runtimeModel.ActiveScenarioChanged += OnRuntimeStateChanged;
				_runtimeModel.FlowStateChanged += OnFlowStateChanged;
				_runtimeModel.CurrentWagonInspectionChanged += OnInspectionStateChanged;
			}

			if (_sceneContext && !_sceneContext.InspectionInfoView)
			{
				_loggerService?.LogWarning(
					"[StationInspectionInfo] SceneContext.InspectionInfoView is not assigned. World inspection UI is disabled.");
			}

			_hasForbiddenCargoText = false;
			RefreshView();
			EnsureCargoCatalogLoadedAsync().Forget();
		}

		public void Deactivate()
		{
			if (_isActive == false)
			{
				return;
			}

			if (_runtimeModel != null)
			{
				_runtimeModel.ActiveScenarioChanged -= OnRuntimeStateChanged;
				_runtimeModel.FlowStateChanged -= OnFlowStateChanged;
				_runtimeModel.CurrentWagonInspectionChanged -= OnInspectionStateChanged;
			}

			_isActive = false;

			if (_sceneContext && _sceneContext.InspectionInfoView)
			{
				_sceneContext.InspectionInfoView.Clear();
			}
		}

		private void OnRuntimeStateChanged(TrainScenario _)
		{
			RefreshView();
		}

		private void OnFlowStateChanged(StationFlowState _)
		{
			RefreshView();
		}

		private void OnInspectionStateChanged(bool _)
		{
			RefreshView();
		}

		private void RefreshView()
		{
			if (_isActive == false || !_sceneContext || !_sceneContext.InspectionInfoView || _runtimeModel == null)
			{
				return;
			}

			var view = _sceneContext.InspectionInfoView;
			var forbiddenCargoText = BuildForbiddenCargoText();
			var activeScenario = _runtimeModel.ActiveScenario;
			var manifestCargoText = string.Empty;
			var manifestDescriptionText = string.Empty;
			if (activeScenario != null)
			{
				manifestCargoText = BuildManifestCargoText(activeScenario.Manifest.DeclaredCargoId);
				manifestDescriptionText = activeScenario.Manifest.DeclaredDescription;
			}

			view.SetForbiddenCargo(forbiddenCargoText);

			if (activeScenario != null)
			{
				view.SetManifest(manifestCargoText, manifestDescriptionText);
			}
			else
			{
				view.ClearManifest();
			}

			var dispatchStatus = BuildDispatchStatus();
			view.SetDispatchState(dispatchStatus.Text, dispatchStatus.State);
		}

		private async UniTaskVoid EnsureCargoCatalogLoadedAsync()
		{
			if (_cargoConfigs != null || _isCatalogLoadInProgress || _cargoCatalogLoader == null)
			{
				return;
			}

			_isCatalogLoadInProgress = true;

			try
			{
				_cargoConfigs = await _cargoCatalogLoader.LoadAsync();
				_hasForbiddenCargoText = false;
			}
			finally
			{
				_isCatalogLoadInProgress = false;
			}

			RefreshView();
		}

		private string BuildForbiddenCargoText()
		{
			if (_hasForbiddenCargoText)
			{
				return _forbiddenCargoText;
			}

			var ruleSet = _runtimeModel.StationDay.CurrentRuleSet;
			if (ruleSet == null || ruleSet.ForbiddenCargoIds.Count == 0)
			{
				_forbiddenCargoText = NoneText;
				_hasForbiddenCargoText = true;
				return _forbiddenCargoText;
			}

			var builder = new StringBuilder();
			foreach (var cargoId in ruleSet.ForbiddenCargoIds)
			{
				if (builder.Length > 0)
				{
					builder.Append(", ");
				}

				builder.Append(ResolveCargoDisplayName(cargoId));
			}

			_forbiddenCargoText = builder.Length == 0 ? NoneText : builder.ToString();
			_hasForbiddenCargoText = true;
			return _forbiddenCargoText;
		}

		private string BuildManifestCargoText(string cargoId)
		{
			var cargoDisplayName = ResolveCargoDisplayName(cargoId);
			return string.IsNullOrWhiteSpace(cargoId) || string.Equals(cargoDisplayName, cargoId, StringComparison.Ordinal)
				? cargoDisplayName
				: $"{cargoDisplayName} ({cargoId})";
		}

		private string ResolveCargoDisplayName(string cargoId)
		{
			var normalizedId = string.IsNullOrWhiteSpace(cargoId) ? string.Empty : cargoId.Trim();
			if (string.IsNullOrEmpty(normalizedId))
			{
				return NoneText;
			}

			if (_cargoConfigs == null ||
				_cargoConfigs.TryGetValue(normalizedId, out var cargoConfig) == false ||
				string.IsNullOrWhiteSpace(cargoConfig.display_name))
			{
				return normalizedId;
			}

			return cargoConfig.display_name;
		}

		private DispatchStatus BuildDispatchStatus()
		{
			if (_runtimeModel.HasActiveScenario == false)
			{
				return new DispatchStatus(WaitingForTrainStatus, StationInspectionInfoView.DispatchState.Neutral);
			}

			if (_runtimeModel.IsTrainInTransit)
			{
				return new DispatchStatus(TrainInTransitStatus, StationInspectionInfoView.DispatchState.Neutral);
			}

			if (_runtimeModel.FlowState != StationFlowState.AwaitingDecision)
			{
				return new DispatchStatus(ReviewManifestStatus, StationInspectionInfoView.DispatchState.Neutral);
			}

			if (_runtimeModel.HasInspectedCurrentWagon == false)
			{
				return new DispatchStatus(InspectWagonFirstStatus, StationInspectionInfoView.DispatchState.Locked);
			}

			return new DispatchStatus(ReadyToDispatchStatus, StationInspectionInfoView.DispatchState.Ready);
		}

		private readonly struct DispatchStatus
		{
			public DispatchStatus(string text, StationInspectionInfoView.DispatchState state)
			{
				Text = text;
				State = state;
			}

			public string Text { get; }
			public StationInspectionInfoView.DispatchState State { get; }
		}
	}
}
