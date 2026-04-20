using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Core.Lifecycle;
using PlatformCore.Services;
using PlatformCore.Services.Factory;
using Project.Gameplay.Station.Configs;
using Project.Gameplay.Station.Domain;
using UnityEngine;

namespace Project.Gameplay.Train
{
	public sealed class TrainCargoPresentationController : IBaseController, IActivatable, IDeactivatable
	{
		private const int ExpectedCargoSlotCount = 10;

		private readonly IObjectFactory _objectFactory;
		private readonly TrainSpawnController _trainSpawnController;
		private readonly CargoCatalogLoader _cargoCatalogLoader;
		private readonly ILoggerService _loggerService;
		private readonly List<GameObject> _spawnedObjects = new List<GameObject>(ExpectedCargoSlotCount);
		private readonly List<Transform> _cargoAnchors = new List<Transform>(ExpectedCargoSlotCount);
		private readonly List<int> _slotOrder = new List<int>(ExpectedCargoSlotCount);
		private readonly System.Random _random = new System.Random();

		private bool _isActive;
		private bool _hasLoggedInsufficientCargoSlots;
		private int _presentationRevision;
		private TrainView _presentedTrain;

		public TrainCargoPresentationController(
			IObjectFactory objectFactory,
			TrainSpawnController trainSpawnController,
			CargoCatalogLoader cargoCatalogLoader,
			ILoggerService loggerService)
		{
			_objectFactory = objectFactory;
			_trainSpawnController = trainSpawnController;
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
			_trainSpawnController.CurrentTrainChanged += OnCurrentTrainChanged;
			OnCurrentTrainChanged(_trainSpawnController.CurrentTrain);
		}

		public void Deactivate()
		{
			if (_isActive == false)
			{
				return;
			}

			_trainSpawnController.CurrentTrainChanged -= OnCurrentTrainChanged;
			_isActive = false;
			_presentationRevision++;
			ClearPresentationState();
		}

		public async UniTask<bool> PresentScenarioAsync(TrainView trainView, TrainScenario scenario)
		{
			if (_isActive == false || !trainView || scenario == null)
			{
				return false;
			}

			var revision = ++_presentationRevision;
			ClearPresentationState();
			_presentedTrain = trainView;

			if (scenario.ActualCargo == null || scenario.ActualCargo.IsEmpty)
			{
				_loggerService?.LogError(
					$"[TrainCargoPresentation] Scenario '{scenario.ScenarioId}' has no actual cargo entries.");
				ClearPresentationState();
				return false;
			}

			var cargoConfigs = await _cargoCatalogLoader.LoadAsync();
			if (IsPresentationValid(revision, trainView) == false)
			{
				return false;
			}

			var cargoAnchors = ResolveCargoAnchors(trainView);
			var totalCargoUnits = scenario.ActualCargo.TotalQuantity;
			if (totalCargoUnits > cargoAnchors.Count)
			{
				_loggerService?.LogError(
					$"[TrainCargoPresentation] Scenario '{scenario.ScenarioId}' requires {totalCargoUnits} " +
					$"cargo slots, but train has {cargoAnchors.Count}.");
				ClearPresentationState();
				return false;
			}

			BuildRandomizedSlotOrder(cargoAnchors.Count);
			var slotIndex = 0;
			for (var i = 0; i < scenario.ActualCargo.Entries.Count; i++)
			{
				var cargoEntry = scenario.ActualCargo.Entries[i];
				if (cargoConfigs.TryGetValue(cargoEntry.CargoId, out var cargoConfig) == false ||
					cargoConfig == null ||
					string.IsNullOrWhiteSpace(cargoConfig.prefab_resource_path))
				{
					_loggerService?.LogError(
						$"[TrainCargoPresentation] Cargo config is missing for id '{cargoEntry.CargoId}'.");
					ClearPresentationState();
					return false;
				}

				for (var quantityIndex = 0; quantityIndex < cargoEntry.Quantity; quantityIndex++)
				{
					var randomizedSlotIndex = ResolveRandomizedSlotIndex(slotIndex);
					var anchor = ResolveCargoAnchor(cargoAnchors, randomizedSlotIndex);
					var cargoInstance = await SpawnAttachedPrefabAsync(
						revision,
						trainView,
						cargoConfig.prefab_resource_path,
						anchor,
						Vector3.zero,
						Quaternion.identity,
						cargoEntry.CargoId);
					if (!cargoInstance)
					{
						ClearPresentationState();
						return false;
					}

					slotIndex++;
				}
			}

			_loggerService?.Log(
				$"[TrainCargoPresentation] Presented cargo '{scenario.ActualCargo.ToDebugString()}' " +
				$"for scenario '{scenario.ScenarioId}'. CargoUnits={totalCargoUnits}, CargoAnchors={cargoAnchors.Count}, " +
				$"RandomSlots={_slotOrder.Count}.");
			return true;
		}

		private async UniTask<GameObject> SpawnAttachedPrefabAsync(
			int revision,
			TrainView trainView,
			string resourcePath,
			Transform parent,
			Vector3 localPosition,
			Quaternion localRotation,
			string cargoId)
		{
			if (string.IsNullOrWhiteSpace(resourcePath))
			{
				_loggerService?.LogError("[TrainCargoPresentation] Prefab resource path is empty.");
				return null;
			}

			if (!parent)
			{
				_loggerService?.LogError("[TrainCargoPresentation] Parent transform is missing for cargo visual spawn.");
				return null;
			}

			var instance = await _objectFactory.CreateAsync(
				resourcePath,
				parent.position,
				parent.rotation,
				parent);

			if (IsPresentationValid(revision, trainView) == false)
			{
				if (instance)
				{
					_objectFactory.Destroy(instance);
				}

				return null;
			}

			if (!instance)
			{
				_loggerService?.LogError(
					$"[TrainCargoPresentation] Failed to spawn visual prefab '{resourcePath}'.");
				return null;
			}

			instance.transform.localPosition = localPosition;
			instance.transform.localRotation = localRotation;
			ApplyCargoViewVariant(instance);
			EnsureFallbackVisual(instance, cargoId);
			_spawnedObjects.Add(instance);
			return instance;
		}

		private List<Transform> ResolveCargoAnchors(TrainView trainView)
		{
			_cargoAnchors.Clear();
			if (!trainView)
			{
				return _cargoAnchors;
			}

			var supportPoints = trainView.SupportPoints;
			for (var i = 0; i < supportPoints.Count; i++)
			{
				var supportPoint = supportPoints[i];
				if (supportPoint)
				{
					_cargoAnchors.Add(supportPoint);
				}
			}

			if (_cargoAnchors.Count > 0)
			{
				LogInsufficientCargoSlotsIfRequired(_cargoAnchors.Count);
				return _cargoAnchors;
			}

			if (trainView.CargoRoot)
			{
				_cargoAnchors.Add(trainView.CargoRoot);
				return _cargoAnchors;
			}

			_cargoAnchors.Add(trainView.transform);
			return _cargoAnchors;
		}

		private static Transform ResolveCargoAnchor(IReadOnlyList<Transform> cargoAnchors, int slotIndex)
		{
			if (cargoAnchors == null || cargoAnchors.Count == 0)
			{
				return null;
			}

			var normalizedIndex = Mathf.Clamp(slotIndex, 0, cargoAnchors.Count - 1);
			return cargoAnchors[normalizedIndex];
		}

		private void BuildRandomizedSlotOrder(int slotCount)
		{
			_slotOrder.Clear();
			for (var i = 0; i < slotCount; i++)
			{
				_slotOrder.Add(i);
			}

			for (var i = _slotOrder.Count - 1; i > 0; i--)
			{
				var swapIndex = _random.Next(i + 1);
				(_slotOrder[i], _slotOrder[swapIndex]) = (_slotOrder[swapIndex], _slotOrder[i]);
			}
		}

		private int ResolveRandomizedSlotIndex(int sequentialSlotIndex)
		{
			if (_slotOrder.Count == 0)
			{
				return sequentialSlotIndex;
			}

			return _slotOrder[Mathf.Clamp(sequentialSlotIndex, 0, _slotOrder.Count - 1)];
		}

		private void LogInsufficientCargoSlotsIfRequired(int slotCount)
		{
			if (_hasLoggedInsufficientCargoSlots || slotCount >= ExpectedCargoSlotCount)
			{
				return;
			}

			_hasLoggedInsufficientCargoSlots = true;
			_loggerService?.LogWarning(
				$"[TrainCargoPresentation] Train has {slotCount} cargo support points. " +
				$"Expected {ExpectedCargoSlotCount} for full wagon variety.");
		}

		private void ApplyCargoViewVariant(GameObject instance)
		{
			if (!instance)
			{
				return;
			}

			var cargoView = instance.GetComponent<CargoView>();
			if (cargoView)
			{
				cargoView.ApplyRandomVariant(_random);
			}
		}

		private void EnsureFallbackVisual(GameObject instance, string cargoId)
		{
			if (!instance)
			{
				return;
			}

			if (instance.GetComponentInChildren<Renderer>(true))
			{
				return;
			}

			var spec = ResolveMainCargoSpec(cargoId);
			CreatePrimitiveVisual(
				instance.transform,
				spec.PrimitiveType,
				spec.LocalPosition,
				spec.LocalScale,
				spec.Color,
				spec.LocalEulerAngles,
				"CargoVisual");
		}

		private static CargoVisualSpec ResolveMainCargoSpec(string cargoId)
		{
			switch (cargoId)
			{
				case "grain_sacks":
					return new CargoVisualSpec(
						PrimitiveType.Capsule,
						new Vector3(0f, 0.36f, 0f),
						new Vector3(0.45f, 0.75f, 0.45f),
						new Vector3(0f, 0f, 90f),
						new Color(0.71f, 0.62f, 0.38f));
				case "timber":
					return new CargoVisualSpec(
						PrimitiveType.Cube,
						new Vector3(0f, 0.16f, 0f),
						new Vector3(1.6f, 0.18f, 0.28f),
						Vector3.zero,
						new Color(0.48f, 0.29f, 0.16f));
				case "cheese_wheels":
					return new CargoVisualSpec(
						PrimitiveType.Cylinder,
						new Vector3(0f, 0.18f, 0f),
						new Vector3(0.55f, 0.18f, 0.55f),
						new Vector3(90f, 0f, 0f),
						new Color(0.94f, 0.8f, 0.36f));
				case "canned_food":
					return new CargoVisualSpec(
						PrimitiveType.Cube,
						new Vector3(0f, 0.32f, 0f),
						new Vector3(0.85f, 0.64f, 0.85f),
						Vector3.zero,
						new Color(0.56f, 0.58f, 0.64f));
				case "medical_supplies":
					return new CargoVisualSpec(
						PrimitiveType.Cube,
						new Vector3(0f, 0.28f, 0f),
						new Vector3(0.9f, 0.56f, 1.05f),
						Vector3.zero,
						new Color(0.78f, 0.82f, 0.84f));
				case "weapon_crates":
					return new CargoVisualSpec(
						PrimitiveType.Cube,
						new Vector3(0f, 0.3f, 0f),
						new Vector3(1.05f, 0.6f, 1.25f),
						Vector3.zero,
						new Color(0.24f, 0.27f, 0.2f));
				case "dynamite_crates":
					return new CargoVisualSpec(
						PrimitiveType.Cube,
						new Vector3(0f, 0.3f, 0f),
						new Vector3(1f, 0.6f, 1f),
						Vector3.zero,
						new Color(0.66f, 0.18f, 0.14f));
				case "moonshine_barrels":
					return new CargoVisualSpec(
						PrimitiveType.Cylinder,
						new Vector3(0f, 0.42f, 0f),
						new Vector3(0.48f, 0.42f, 0.48f),
						Vector3.zero,
						new Color(0.43f, 0.27f, 0.12f));
				case "prisoner_transport":
					return new CargoVisualSpec(
						PrimitiveType.Cube,
						new Vector3(0f, 0.55f, 0f),
						new Vector3(1.05f, 1.1f, 1.35f),
						Vector3.zero,
						new Color(0.16f, 0.17f, 0.2f));
				default:
					return new CargoVisualSpec(
						PrimitiveType.Cube,
						new Vector3(0f, 0.3f, 0f),
						new Vector3(0.9f, 0.6f, 0.9f),
						Vector3.zero,
						new Color(0.48f, 0.5f, 0.52f));
			}
		}

		private static void CreatePrimitiveVisual(
			Transform parent,
			PrimitiveType primitiveType,
			Vector3 localPosition,
			Vector3 localScale,
			Color color,
			Vector3 localEulerAngles,
			string name)
		{
			var visual = GameObject.CreatePrimitive(primitiveType);
			visual.name = name;
			visual.layer = parent.gameObject.layer;
			visual.transform.SetParent(parent, false);
			visual.transform.localPosition = localPosition;
			visual.transform.localEulerAngles = localEulerAngles;
			visual.transform.localScale = localScale;

			var collider = visual.GetComponent<Collider>();
			if (collider)
			{
				UnityEngine.Object.Destroy(collider);
			}

			var renderer = visual.GetComponent<Renderer>();
			if (renderer)
			{
				renderer.material.color = color;
			}
		}

		private bool IsPresentationValid(int revision, TrainView trainView)
		{
			return _isActive &&
				revision == _presentationRevision &&
				trainView &&
				_presentedTrain == trainView &&
				_trainSpawnController.CurrentTrain == trainView;
		}

		private void OnCurrentTrainChanged(TrainView currentTrain)
		{
			if (currentTrain == _presentedTrain)
			{
				return;
			}

			_presentationRevision++;
			ClearPresentationState();
		}

		private void ClearPresentationState()
		{
			for (var i = _spawnedObjects.Count - 1; i >= 0; i--)
			{
				var spawnedObject = _spawnedObjects[i];
				if (spawnedObject)
				{
					_objectFactory.Destroy(spawnedObject);
				}
			}

			_spawnedObjects.Clear();
			_cargoAnchors.Clear();
			_slotOrder.Clear();
			_presentedTrain = null;
		}

		private readonly struct CargoVisualSpec
		{
			public CargoVisualSpec(
				PrimitiveType primitiveType,
				Vector3 localPosition,
				Vector3 localScale,
				Vector3 localEulerAngles,
				Color color)
			{
				PrimitiveType = primitiveType;
				LocalPosition = localPosition;
				LocalScale = localScale;
				LocalEulerAngles = localEulerAngles;
				Color = color;
			}

			public PrimitiveType PrimitiveType { get; }
			public Vector3 LocalPosition { get; }
			public Vector3 LocalScale { get; }
			public Vector3 LocalEulerAngles { get; }
			public Color Color { get; }
		}
	}
}
