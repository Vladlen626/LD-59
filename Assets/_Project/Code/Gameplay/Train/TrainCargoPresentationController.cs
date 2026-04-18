using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Core.Lifecycle;
using PlatformCore.Services;
using PlatformCore.Services.Factory;
using Project.Gameplay.Station.Configs;
using Project.Gameplay.Station.Domain;
using Project.Infrastructure;
using UnityEngine;

namespace Project.Gameplay.Train
{
	public sealed class TrainCargoPresentationController : IBaseController, IActivatable, IDeactivatable
	{
		private readonly IObjectFactory _objectFactory;
		private readonly TrainSpawnController _trainSpawnController;
		private readonly CargoCatalogLoader _cargoCatalogLoader;
		private readonly ILoggerService _loggerService;
		private readonly List<GameObject> _spawnedObjects = new List<GameObject>();
		private readonly List<Transform> _cargoAnchors = new List<Transform>();

		private bool _isActive;
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

			var cargoConfig = await _cargoCatalogLoader.GetByIdAsync(scenario.ActualCargoId);
			if (IsPresentationValid(revision, trainView) == false)
			{
				return false;
			}

			if (cargoConfig == null || string.IsNullOrWhiteSpace(cargoConfig.prefab_resource_path))
			{
				_loggerService?.LogError(
					$"[TrainCargoPresentation] Cargo config is missing for id '{scenario.ActualCargoId}'.");
				ClearPresentationState();
				return false;
			}

			var cargoAnchors = ResolveCargoAnchors(trainView);
			for (var i = 0; i < cargoAnchors.Count; i++)
			{
				var anchor = cargoAnchors[i];
				var cargoInstance = await SpawnAttachedPrefabAsync(
					revision,
					trainView,
					cargoConfig.prefab_resource_path,
					anchor,
					Vector3.zero,
					Quaternion.identity,
					CargoVisualKind.MainCargo,
					scenario.ActualCargoId);
				if (!cargoInstance)
				{
					ClearPresentationState();
					return false;
				}
			}

			if (scenario.HasHiddenContraband)
			{
				var hiddenAnchor = cargoAnchors[Mathf.Clamp(cargoAnchors.Count / 2, 0, cargoAnchors.Count - 1)];
				var hiddenClue = await SpawnAttachedPrefabAsync(
					revision,
					trainView,
					ProjectGameplayResourcePaths.CargoClues_HiddenContraband,
					hiddenAnchor,
					new Vector3(0f, 0.45f, 0f),
					Quaternion.identity,
					CargoVisualKind.HiddenContraband,
					scenario.ActualCargoId);
				if (!hiddenClue)
				{
					ClearPresentationState();
					return false;
				}
			}

			if (scenario.HasIllegalMarker)
			{
				var markerAnchor = cargoAnchors[0];
				var markerClue = await SpawnAttachedPrefabAsync(
					revision,
					trainView,
					ProjectGameplayResourcePaths.CargoClues_IllegalMarker,
					markerAnchor,
					new Vector3(0f, 1.05f, 0f),
					Quaternion.identity,
					CargoVisualKind.IllegalMarker,
					scenario.ActualCargoId);
				if (!markerClue)
				{
					ClearPresentationState();
					return false;
				}
			}

			_loggerService?.Log(
				$"[TrainCargoPresentation] Presented cargo '{scenario.ActualCargoId}' " +
				$"for scenario '{scenario.ScenarioId}'. CargoCount={cargoAnchors.Count}, " +
				$"HiddenContraband={scenario.HasHiddenContraband}, IllegalMarker={scenario.HasIllegalMarker}.");
			return true;
		}

		private async UniTask<GameObject> SpawnAttachedPrefabAsync(
			int revision,
			TrainView trainView,
			string resourcePath,
			Transform parent,
			Vector3 localPosition,
			Quaternion localRotation,
			CargoVisualKind visualKind,
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
			EnsureFallbackVisual(instance, visualKind, cargoId);
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

		private void EnsureFallbackVisual(GameObject instance, CargoVisualKind visualKind, string cargoId)
		{
			if (!instance)
			{
				return;
			}

			if (instance.GetComponentInChildren<Renderer>(true))
			{
				return;
			}

			switch (visualKind)
			{
				case CargoVisualKind.HiddenContraband:
					CreatePrimitiveVisual(
						instance.transform,
						PrimitiveType.Sphere,
						new Vector3(0f, 0f, 0f),
						new Vector3(0.24f, 0.24f, 0.24f),
						new Color(0.74f, 0.12f, 0.08f),
						Vector3.zero,
						"HiddenContraband");
					return;
				case CargoVisualKind.IllegalMarker:
					CreatePrimitiveVisual(
						instance.transform,
						PrimitiveType.Cube,
						Vector3.zero,
						new Vector3(0.1f, 0.7f, 0.08f),
						new Color(0.88f, 0.14f, 0.12f),
						new Vector3(0f, 0f, 45f),
						"IllegalMarkerA");
					CreatePrimitiveVisual(
						instance.transform,
						PrimitiveType.Cube,
						Vector3.zero,
						new Vector3(0.1f, 0.7f, 0.08f),
						new Color(0.88f, 0.14f, 0.12f),
						new Vector3(0f, 0f, -45f),
						"IllegalMarkerB");
					return;
				default:
					var spec = ResolveMainCargoSpec(cargoId);
					CreatePrimitiveVisual(
						instance.transform,
						spec.PrimitiveType,
						spec.LocalPosition,
						spec.LocalScale,
						spec.Color,
						spec.LocalEulerAngles,
						"CargoVisual");
					return;
			}
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
			_presentedTrain = null;
		}

		private enum CargoVisualKind
		{
			MainCargo = 0,
			HiddenContraband = 1,
			IllegalMarker = 2,
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
