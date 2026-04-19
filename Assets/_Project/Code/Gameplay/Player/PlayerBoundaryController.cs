using PlatformCore.Core;
using PlatformCore.Core.Lifecycle;
using PlatformCore.Services.Notifications;
using UnityEngine;

namespace Project.Gameplay.Player
{
	public sealed class PlayerBoundaryController : IBaseController, IActivatable, IDeactivatable, IUpdatable
	{
		private const string OutOfBoundsMessage = "Return to station.";

		private readonly SamplePlayerView _playerView;
		private readonly SamplePlayerMovementController _movementController;
		private readonly IGlobalNotificationService _globalNotificationService;
		private readonly Vector3 _homePosition;
		private readonly float _maxDistance;
		private readonly float _notificationCooldownSeconds;

		private float _nextNotificationTime;
		private bool _isActive;

		public PlayerBoundaryController(
			SamplePlayerView playerView,
			SamplePlayerMovementController movementController,
			IGlobalNotificationService globalNotificationService,
			Vector3 homePosition,
			float maxDistance = 50f,
			float notificationCooldownSeconds = 2.5f)
		{
			_playerView = playerView;
			_movementController = movementController;
			_globalNotificationService = globalNotificationService;
			_homePosition = homePosition;
			_maxDistance = Mathf.Max(1f, maxDistance);
			_notificationCooldownSeconds = Mathf.Max(0.5f, notificationCooldownSeconds);
		}

		public void Activate()
		{
			_isActive = true;
			_nextNotificationTime = 0f;
		}

		public void Deactivate()
		{
			_isActive = false;
		}

		public void OnUpdate(float _)
		{
			if (_isActive == false || !_playerView)
			{
				return;
			}

			var playerPosition = _playerView.transform.position;
			var homeFlat = new Vector3(_homePosition.x, 0f, _homePosition.z);
			var playerFlat = new Vector3(playerPosition.x, 0f, playerPosition.z);
			var awayFromHome = playerFlat - homeFlat;
			var distance = awayFromHome.magnitude;
			if (distance <= _maxDistance)
			{
				return;
			}

			if (Time.unscaledTime >= _nextNotificationTime)
			{
				_globalNotificationService?.ShowBanner(OutOfBoundsMessage, 1f, true, true);
				_nextNotificationTime = Time.unscaledTime + _notificationCooldownSeconds;
			}

			var clampedFlat = homeFlat + awayFromHome.normalized * _maxDistance;
			var clampedWorld = new Vector3(clampedFlat.x, playerPosition.y, clampedFlat.z);
			SetPlayerPosition(clampedWorld);
			RotatePlayerToHome(clampedFlat, homeFlat);
			_movementController?.SyncFromCurrentView();
		}

		private void SetPlayerPosition(Vector3 position)
		{
			var characterController = _playerView.CharacterController;
			if (characterController)
			{
				characterController.enabled = false;
			}

			_playerView.transform.position = position;

			if (characterController)
			{
				characterController.enabled = true;
			}
		}

		private void RotatePlayerToHome(Vector3 playerFlat, Vector3 homeFlat)
		{
			var toHome = homeFlat - playerFlat;
			if (toHome.sqrMagnitude <= 0.0001f)
			{
				return;
			}

			_playerView.transform.rotation = Quaternion.LookRotation(toHome.normalized, Vector3.up);
			if (_playerView.Head)
			{
				_playerView.Head.localRotation = Quaternion.identity;
			}
		}
	}
}
