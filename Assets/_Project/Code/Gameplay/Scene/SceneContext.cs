using PlatformCore.Services;
using Project.Gameplay.Station.Views;
using UnityEngine;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace Project.Gameplay.Scene
{
	public sealed class SceneContext : MonoBehaviour, ISceneContext
	{
		[SerializeField] [Tooltip("World point where train prefab is spawned.")]
		private Transform _trainSpawnPoint;
		[SerializeField] [Tooltip("Optional parent for spawned train instance.")]
		private Transform _trainSpawnParent;
		[SerializeField] [Tooltip("Single world button: call train if none, send case if train is present.")]
		private StationActionButtonInteractableView _actionButton;
		[SerializeField] [Tooltip("Single world toggle: switches player verdict Allow/SendBack.")]
		private StationDecisionToggleInteractableView _decisionToggle;

		public UnityScene Scene { get; private set; }
		public StationActionButtonInteractableView ActionButton => _actionButton;
		public StationDecisionToggleInteractableView DecisionToggle => _decisionToggle;

		public SceneRuntimeSetup BuildRuntimeSetup()
		{
			return new SceneRuntimeSetup(
				_trainSpawnPoint,
				_trainSpawnParent);
		}

		private void Awake()
		{
			Scene = gameObject.scene;
		}
	}
}
