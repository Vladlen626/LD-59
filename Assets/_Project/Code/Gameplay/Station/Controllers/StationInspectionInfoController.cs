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
using UnityEngine;

namespace Project.Gameplay.Station.Controllers
{
	public sealed class StationInspectionInfoController : IBaseController, IActivatable, IDeactivatable
	{
		private const string NoBansText = "No bans today";
		private const string NoAllowedCargoText = "No papers";

		private readonly SceneContext _sceneContext;
		private readonly StationDayModel _stationDayModel;
		private readonly StationRuntimeModel _runtimeModel;
		private readonly CargoCatalogLoader _cargoCatalogLoader;
		private readonly ILoggerService _loggerService;

		private IReadOnlyDictionary<string, CargoTypeConfig> _cargoConfigs;
		private StationInspectionInfoView _fallbackView;
		private string _forbiddenCargoText = NoBansText;
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
			_stationDayModel = runtimeContext?.StationDay;
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
			}

			if (_stationDayModel != null)
			{
				_stationDayModel.DayChanged += OnDayChanged;
			}

			if (ResolveView() == null)
			{
				_loggerService?.LogWarning("[StationInspectionInfo] Inspection info view is missing. World board is disabled.");
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
			}

			if (_stationDayModel != null)
			{
				_stationDayModel.DayChanged -= OnDayChanged;
			}

			_isActive = false;

			var view = GetExistingView();
			if (view)
			{
				view.Clear();
			}

			if (_fallbackView)
			{
				Object.Destroy(_fallbackView.gameObject);
				_fallbackView = null;
			}
		}

		private void OnRuntimeStateChanged(TrainScenario _)
		{
			RefreshView();
		}

		private void OnDayChanged(DayRuleSet _)
		{
			_hasForbiddenCargoText = false;
			RefreshView();
		}

		private void RefreshView()
		{
			if (_isActive == false || _runtimeModel == null)
			{
				return;
			}

			var view = ResolveView();
			if (!view)
			{
				return;
			}

			var forbiddenCargoText = BuildForbiddenCargoText();
			var activeScenario = _runtimeModel.ActiveScenario;
			if (activeScenario != null)
			{
				view.SetAllowedCargo(BuildAllowedCargoText(activeScenario.Manifest));
			}
			else
			{
				view.ClearAllowedCargo();
			}

			view.SetBans(forbiddenCargoText);
		}

		private StationInspectionInfoView ResolveView()
		{
			if (_sceneContext && _sceneContext.InspectionInfoView)
			{
				return _sceneContext.InspectionInfoView;
			}

			if (_fallbackView)
			{
				return _fallbackView;
			}

			if (!_sceneContext || !_sceneContext.ActionButton)
			{
				return null;
			}

			var fallbackObject = new GameObject("StationInspectionInfoFallback");
			var fallbackTransform = fallbackObject.transform;
			fallbackTransform.SetParent(_sceneContext.ActionButton.transform, false);
			fallbackTransform.localPosition = new Vector3(1.6f, 1.6f, 0f);
			fallbackTransform.localRotation = Quaternion.identity;
			fallbackTransform.localScale = Vector3.one;

			_fallbackView = fallbackObject.AddComponent<StationInspectionInfoView>();
			return _fallbackView;
		}

		private StationInspectionInfoView GetExistingView()
		{
			if (_sceneContext && _sceneContext.InspectionInfoView)
			{
				return _sceneContext.InspectionInfoView;
			}

			return _fallbackView;
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
				_forbiddenCargoText = NoBansText;
				_hasForbiddenCargoText = true;
				return _forbiddenCargoText;
			}

			var builder = new StringBuilder();
			foreach (var cargoId in ruleSet.ForbiddenCargoIds)
			{
				if (builder.Length > 0)
				{
					builder.Append('\n');
				}

				builder.Append("- ");
				builder.Append(ResolveCargoDisplayName(cargoId));
			}

			_forbiddenCargoText = builder.Length == 0 ? NoBansText : builder.ToString();
			_hasForbiddenCargoText = true;
			return _forbiddenCargoText;
		}

		private string BuildAllowedCargoText(ManifestData manifest)
		{
			if (manifest == null || manifest.DeclaredCargo == null || manifest.DeclaredCargo.IsEmpty)
			{
				return NoAllowedCargoText;
			}

			var entries = manifest.DeclaredCargo.Entries;
			if (entries.Count == 1)
			{
				return FormatCargoLine(entries[0], false);
			}

			var builder = new StringBuilder();
			for (var i = 0; i < entries.Count; i++)
			{
				if (i > 0)
				{
					builder.Append('\n');
				}

				builder.Append(FormatCargoLine(entries[i], true));
			}

			return builder.ToString();
		}

		private string FormatCargoLine(CargoQuantity cargoEntry, bool includeBullet)
		{
			if (cargoEntry == null || cargoEntry.IsValid == false)
			{
				return NoAllowedCargoText;
			}

			var prefix = includeBullet ? "- " : string.Empty;
			return $"{prefix}{ResolveCargoDisplayName(cargoEntry.CargoId)} x{cargoEntry.Quantity}";
		}

		private string ResolveCargoDisplayName(string cargoId)
		{
			var normalizedId = string.IsNullOrWhiteSpace(cargoId) ? string.Empty : cargoId.Trim();
			if (string.IsNullOrEmpty(normalizedId))
			{
				return NoAllowedCargoText;
			}

			if (_cargoConfigs == null ||
				_cargoConfigs.TryGetValue(normalizedId, out var cargoConfig) == false ||
				string.IsNullOrWhiteSpace(cargoConfig.display_name))
			{
				return normalizedId;
			}

			return cargoConfig.display_name;
		}
	}
}
