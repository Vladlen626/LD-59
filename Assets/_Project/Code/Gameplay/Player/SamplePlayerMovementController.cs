using PlatformCore.Core;
using PlatformCore.Core.Lifecycle;
using PlatformCore.Services.Audio;
using PlatformCore.Services.Input;
using PlatformCore.Services.UI;
using Project.Infrastructure;
using UnityEngine;

namespace Project.Gameplay.Player
{
	public sealed class SamplePlayerMovementController : IBaseController, IActivatable, IDeactivatable, IUpdatable
	{
		private const float GroundedStickVelocity = -2f;
		private const float WalkStepDistance = 2f;
		private const float SprintStepDistance = 1.5f;

		private readonly SamplePlayerView _playerView;
		private readonly CharacterController _characterController;
		private readonly IInputService _inputService;
		private readonly ICursorService _cursorService;
		private readonly IAudioService _audioService;

		private float _verticalVelocity;
		private float _yaw;
		private float _pitch;
		private bool _isGrounded;

		private Vector3 _cameraRootBaseLocalPosition;
		private float _headBobPhase;
		private float _headBobOffsetY;
		private Vector3 _lastFootstepPosition;
		private float _remainingStepDistance;

		public SamplePlayerMovementController(
			SamplePlayerView playerView,
			IInputService inputService,
			ICursorService cursorService,
			IAudioService audioService)
		{
			_playerView = playerView;
			_inputService = inputService;
			_cursorService = cursorService;
			_audioService = audioService;
			_characterController = _playerView ? _playerView.CharacterController : null;
		}

		public void Activate()
		{
			if (!_playerView || !_characterController)
			{
				return;
			}

			_verticalVelocity = GroundedStickVelocity;
			_isGrounded = _characterController.isGrounded;
			_yaw = _playerView.transform.eulerAngles.y;

			if (_playerView.Head)
			{
				_pitch = Mathf.DeltaAngle(0f, _playerView.Head.localEulerAngles.x);
			}

			if (_playerView.CameraRoot)
			{
				_cameraRootBaseLocalPosition = _playerView.CameraRoot.localPosition;
			}

			_lastFootstepPosition = _playerView.transform.position;
			_remainingStepDistance = WalkStepDistance;
		}

		public void Deactivate()
		{
			_verticalVelocity = 0f;
			_isGrounded = false;
			_remainingStepDistance = WalkStepDistance;
			ResetHeadBob();
		}

		public void OnUpdate(float deltaTime)
		{
			if (!_playerView || !_characterController || !_playerView.gameObject.activeInHierarchy)
			{
				return;
			}

			var hasControl = _cursorService.IsCursorLocked;
			var moveInput = hasControl ? _inputService.Move : Vector2.zero;
			var lookInput = hasControl ? _inputService.Look : Vector2.zero;

			UpdateLook(lookInput);
			UpdateMovement(hasControl, moveInput, deltaTime);
			UpdateFootsteps(hasControl, moveInput);
			UpdateHeadBob(moveInput, hasControl, deltaTime);
		}

		public void SyncFromCurrentView()
		{
			if (!_playerView)
			{
				return;
			}

			_yaw = _playerView.transform.eulerAngles.y;
			if (_playerView.Head)
			{
				_pitch = Mathf.DeltaAngle(0f, _playerView.Head.localEulerAngles.x);
			}

			_lastFootstepPosition = _playerView.transform.position;
			_remainingStepDistance = WalkStepDistance;
		}

		private void UpdateLook(Vector2 lookInput)
		{
			_yaw += lookInput.x * _playerView.LookSensitivity;
			_playerView.transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

			if (!_playerView.Head)
			{
				return;
			}

			_pitch -= lookInput.y * _playerView.LookSensitivity;
			_pitch = Mathf.Clamp(_pitch, _playerView.MinPitch, _playerView.MaxPitch);
			_playerView.Head.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
		}

		private void UpdateMovement(bool hasControl, Vector2 moveInput, float deltaTime)
		{
			_isGrounded = _characterController.isGrounded;

			if (_isGrounded && _verticalVelocity < 0f)
			{
				_verticalVelocity = GroundedStickVelocity;
			}

			if (hasControl &&
			    _playerView.EnableJump &&
			    _inputService.IsJumpPressedThisFrame &&
			    _isGrounded)
			{
				_verticalVelocity = Mathf.Sqrt(_playerView.JumpHeight * -2f * _playerView.Gravity);
				_isGrounded = false;
				_audioService?.PlaySoundAt(ProjectAudioEventPaths.JumpSample, _playerView.transform.position);
			}

			_verticalVelocity += _playerView.Gravity * deltaTime;

			var moveDirection =
				_playerView.transform.right * moveInput.x +
				_playerView.transform.forward * moveInput.y;

			var moveSpeed = _playerView.MoveSpeed;
			if (hasControl && _inputService.IsSprintPressed)
			{
				moveSpeed *= _playerView.SprintSpeedMultiplier;
			}

			var velocity = moveDirection * moveSpeed;
			velocity.y = _verticalVelocity;

			_characterController.Move(velocity * deltaTime);

			_isGrounded = _characterController.isGrounded;
			if (_isGrounded && _verticalVelocity < 0f)
			{
				_verticalVelocity = GroundedStickVelocity;
			}
		}

		private void UpdateFootsteps(bool hasControl, Vector2 moveInput)
		{
			var currentPosition = _playerView.transform.position;
			var horizontalDelta = currentPosition - _lastFootstepPosition;
			horizontalDelta.y = 0f;
			_lastFootstepPosition = currentPosition;

			if (!hasControl || !_isGrounded || moveInput.sqrMagnitude <= 0.01f)
			{
				_remainingStepDistance = Mathf.Min(_remainingStepDistance, ResolveStepDistance());
				return;
			}

			_remainingStepDistance -= horizontalDelta.magnitude;
			var stepDistance = ResolveStepDistance();
			while (_remainingStepDistance <= 0f)
			{
				_audioService?.PlaySoundAt(ProjectAudioEventPaths.WoodFootSteps, currentPosition);
				_remainingStepDistance += stepDistance;
			}
		}

		private float ResolveStepDistance()
		{
			return _inputService.IsSprintPressed
				? SprintStepDistance
				: WalkStepDistance;
		}

		private void UpdateHeadBob(Vector2 moveInput, bool hasControl, float deltaTime)
		{
			if (!_playerView.EnableHeadBob || !_playerView.CameraRoot)
			{
				return;
			}

			var isMoving = hasControl && _isGrounded && moveInput.sqrMagnitude > 0.01f;
			var targetOffsetY = 0f;

			if (isMoving)
			{
				_headBobPhase += deltaTime * _playerView.HeadBobFrequency;
				targetOffsetY = Mathf.Sin(_headBobPhase) * _playerView.HeadBobAmplitude;
			}

			_headBobOffsetY = Mathf.MoveTowards(
				_headBobOffsetY,
				targetOffsetY,
				_playerView.HeadBobReturnSpeed * deltaTime);

			var localPosition = _cameraRootBaseLocalPosition;
			localPosition.y += _headBobOffsetY;
			_playerView.CameraRoot.localPosition = localPosition;
		}

		private void ResetHeadBob()
		{
			_headBobPhase = 0f;
			_headBobOffsetY = 0f;

			if (!_playerView || !_playerView.CameraRoot)
			{
				return;
			}

			_playerView.CameraRoot.localPosition = _cameraRootBaseLocalPosition;
		}
	}
}
