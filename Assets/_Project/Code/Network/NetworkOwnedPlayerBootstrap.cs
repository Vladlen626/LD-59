using Cysharp.Threading.Tasks;
using FishNet.Object;
using PlatformCore.Infrastructure;
using PlatformCore.Services;
using PlatformCore.Services.Input;
using PlatformCore.Services.UI;
using Project.Gameplay.Player;
using UnityEngine;

namespace Project.Network
{
	[RequireComponent(typeof(SamplePlayerView))]
	public sealed class NetworkOwnedPlayerBootstrap : NetworkBehaviour
	{
		[SerializeField] private SamplePlayerView _playerView;

		private LifecycleService _lifecycleService;
		private ICursorService _cursorService;
		private SamplePlayerMovementController _movementController;
		private bool _registered;

		private void Awake()
		{
			if (!_playerView)
			{
				_playerView = GetComponent<SamplePlayerView>();
			}
		}

		public override void OnStartClient()
		{
			base.OnStartClient();

			if (!IsOwner)
			{
				return;
			}

			RegisterOwnerControllerAsync().Forget();
		}

		public override void OnStopClient()
		{
			if (IsOwner)
			{
				UnregisterOwnerController();
			}

			base.OnStopClient();
		}

		private async UniTaskVoid RegisterOwnerControllerAsync()
		{
			if (!_playerView)
			{
				return;
			}

			var inputService = Locator.Resolve<IInputService>();
			_cursorService = Locator.Resolve<ICursorService>();
			var cameraService = Locator.Resolve<ICameraService>();
			_lifecycleService = Locator.Resolve<LifecycleService>();

			if (inputService == null || _cursorService == null || _lifecycleService == null)
			{
				return;
			}

			_movementController = new SamplePlayerMovementController(_playerView, inputService, _cursorService);
			await _lifecycleService.RegisterAsync(_movementController);
			_registered = true;

			if (_playerView.CameraRoot && cameraService != null)
			{
				cameraService.AttachPrimaryCameraTo(_playerView.CameraRoot);
			}

			_cursorService.LockCursor();
		}

		private void UnregisterOwnerController()
		{
			if (_registered && _lifecycleService != null && _movementController != null)
			{
				_lifecycleService.Unregister(_movementController);
			}

			_registered = false;
			_movementController = null;
			_lifecycleService = null;

			if (_cursorService != null && _cursorService.IsCursorLocked)
			{
				_cursorService.UnlockCursor();
			}
		}
	}
}
