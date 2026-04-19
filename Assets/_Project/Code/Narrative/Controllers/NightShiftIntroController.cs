using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Core.Lifecycle;
using PlatformCore.Services;
using PlatformCore.Services.Audio;
using PlatformCore.Services.Factory;
using PlatformCore.Services.UI;
using PlatformCore.Services.UI.SplashScreen;
using Project.Gameplay.Interaction;
using Project.Gameplay.Player;
using Project.Gameplay.Scene;
using Project.Gameplay.Station.Runtime;
using Project.Gameplay.Station.Views;
using Project.Infrastructure;
using UnityEngine;

namespace Project.Gameplay.Station.Controllers
{
	public sealed class NightShiftIntroController : IBaseController, IActivatable, IDeactivatable
	{
		private const float KnockShakeIntensity = 0.18f;
		private const float KnockShakeDuration = 0.18f;
		private const int KnockCount = 3;
		private const float KnockIntervalSeconds = 0.3f;
		private const float LetterThrowDelayAfterKnocksSeconds = 1f;
		private const float LetterSlideDuration = 0.55f;
		private const float LetterSettleDelay = 0.08f;
		private const float FirstNightFocusHold = 0.85f;

		private readonly SamplePlayerView _playerView;
		private readonly SamplePlayerMovementController _playerMovementController;
		private readonly SceneRuntimeSetup _sceneSetup;
		private readonly StationFlowController _stationFlowController;
		private readonly StationInteractionController _stationInteractionController;
		private readonly UIWakeTransitionController _wakeTransitionController;
		private readonly UILetterController _uiLetterController;
		private readonly ICursorService _cursorService;
		private readonly ICameraShakeService _cameraShakeService;
		private readonly IAudioService _audioService;
		private readonly IObjectFactory _objectFactory;
		private readonly ISplashScreenService _splashScreenService;

		private MorningLetterInteractableView _activeLetter;
		private NightShiftLetterData _currentLetterData;
		private UniTaskCompletionSource _letterReadSource;
		private bool _isActive;
		private bool _isIntroPlaying;
		private bool _cursorUnlockedByIntro;
		private bool _currentIntroLetterOpened;
		private bool _hasShownFirstNightGuidance;

		public NightShiftIntroController(
			SamplePlayerView playerView,
			SamplePlayerMovementController playerMovementController,
			SceneRuntimeSetup sceneSetup,
			StationFlowController stationFlowController,
			StationInteractionController stationInteractionController,
			UIWakeTransitionController wakeTransitionController,
			UILetterController uiLetterController,
			ICursorService cursorService,
			ICameraShakeService cameraShakeService,
			IAudioService audioService,
			IObjectFactory objectFactory,
			ISplashScreenService splashScreenService)
		{
			_playerView = playerView;
			_playerMovementController = playerMovementController;
			_sceneSetup = sceneSetup;
			_stationFlowController = stationFlowController;
			_stationInteractionController = stationInteractionController;
			_wakeTransitionController = wakeTransitionController;
			_uiLetterController = uiLetterController;
			_cursorService = cursorService;
			_cameraShakeService = cameraShakeService;
			_audioService = audioService;
			_objectFactory = objectFactory;
			_splashScreenService = splashScreenService;
		}

		public void Activate()
		{
			if (_isActive)
			{
				return;
			}

			_isActive = true;
			_stationFlowController.DayStarted += OnDayStarted;
			_uiLetterController.LetterClosed += OnLetterClosed;
		}

		public void Deactivate()
		{
			if (_isActive == false)
			{
				return;
			}

			_isActive = false;
			_stationFlowController.DayStarted -= OnDayStarted;
			_uiLetterController.LetterClosed -= OnLetterClosed;

			_letterReadSource?.TrySetResult();
			_letterReadSource = null;
			CleanupActiveLetter();
			ClearInteractionRestriction();
			RestorePlayerControl();
		}

		public async UniTask<bool> PlayNightShiftIntroAsync(
			NightShiftLetterData letterData,
			bool focusLetter,
			CancellationToken cancellationToken = default)
		{
			if (_isActive == false || !_playerView)
			{
				return false;
			}

			if (_isIntroPlaying)
			{
				return false;
			}

			_isIntroPlaying = true;
			_currentLetterData = ResolveLetterData(letterData);
			_currentIntroLetterOpened = false;
			_letterReadSource = new UniTaskCompletionSource();
			ApplyInteractionRestriction();

			try
			{
				var roomTargets = ResolveRoomTargets();
				TeleportPlayerToWakePoint(roomTargets);
				SuspendPlayerControl();

				if (ProjectSplashScreenSettings.BeforeWakeBlackHoldSeconds > 0f)
				{
					await UniTask.Delay(
						TimeSpan.FromSeconds(ProjectSplashScreenSettings.BeforeWakeBlackHoldSeconds),
						cancellationToken: cancellationToken);
				}

				await _splashScreenService.FadeOutAsync(ProjectSplashScreenSettings.BeforeWakeFadeOutSeconds);
				await _wakeTransitionController.PlayWakeUpAsync(2, cancellationToken);

				await PlayDoorKnockAsync(roomTargets, cancellationToken);
				if (LetterThrowDelayAfterKnocksSeconds > 0f)
				{
					await UniTask.Delay(TimeSpan.FromSeconds(LetterThrowDelayAfterKnocksSeconds), cancellationToken: cancellationToken);
				}

				if (await SpawnLetterAsync(roomTargets) == false)
				{
					return false;
				}

				await AnimateLetterArrivalAsync(roomTargets, cancellationToken);

				if (_activeLetter)
				{
					_activeLetter.SetAvailable(true);
				}

				if (focusLetter && _activeLetter)
				{
					FocusPlayerOnLetter(_activeLetter.transform.position);
					await UniTask.Delay(TimeSpan.FromSeconds(FirstNightFocusHold), cancellationToken: cancellationToken);
					_hasShownFirstNightGuidance = true;
				}

				RestorePlayerControl();
				await _letterReadSource.Task.AttachExternalCancellation(cancellationToken);
				_stationFlowController.TryCompleteDayIntro();
				return true;
			}
			catch (OperationCanceledException)
			{
				return false;
			}
			finally
			{
				_isIntroPlaying = false;
				ClearInteractionRestriction();
				RestorePlayerControl();
			}
		}

		private void OnDayStarted(StationDayContent dayContent)
		{
			if (_isActive == false || _isIntroPlaying)
			{
				return;
			}

			var focusLetter = _hasShownFirstNightGuidance == false;
			PlayNightShiftIntroAsync(ResolveIntroLetter(dayContent), focusLetter).Forget();
		}

		private void OnLetterInteracted()
		{
			if (_uiLetterController.IsOpen)
			{
				return;
			}

			_currentIntroLetterOpened = true;
			_uiLetterController.ShowLetter(_currentLetterData);
		}

		private void OnLetterClosed()
		{
			if (_currentIntroLetterOpened)
			{
				ClearInteractionRestriction();
				_letterReadSource?.TrySetResult();
			}
		}

		private void ApplyInteractionRestriction()
		{
			_stationInteractionController.SetInteractionRestriction(IsLetterInteractionAllowed);
		}

		private void ClearInteractionRestriction()
		{
			_stationInteractionController.ClearInteractionRestriction();
		}

		private bool IsLetterInteractionAllowed(IPlayerInteractable interactable)
		{
			return _activeLetter != null && ReferenceEquals(interactable, _activeLetter);
		}

		private async UniTask PlayDoorKnockAsync(NightShiftRoomTargets roomTargets, CancellationToken cancellationToken)
		{
			for (var i = 0; i < KnockCount; i++)
			{
				_audioService.PlaySoundAt(ProjectAudioEventPaths.DoorKnocking, roomTargets.DoorPosition);
				await _cameraShakeService.ShakeAsync(KnockShakeIntensity, KnockShakeDuration);
				if (i < KnockCount - 1 && KnockIntervalSeconds > 0f)
				{
					await UniTask.Delay(TimeSpan.FromSeconds(KnockIntervalSeconds), cancellationToken: cancellationToken);
				}
			}
		}

		private async UniTask AnimateLetterArrivalAsync(NightShiftRoomTargets roomTargets, CancellationToken cancellationToken)
		{
			if (!_activeLetter)
			{
				return;
			}

			var elapsed = 0f;
			while (elapsed < LetterSlideDuration)
			{
				cancellationToken.ThrowIfCancellationRequested();
				await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
				elapsed += Time.unscaledDeltaTime;
				var t = Mathf.Clamp01(elapsed / LetterSlideDuration);
				var eased = Mathf.SmoothStep(0f, 1f, t);
				_activeLetter.transform.position = Vector3.Lerp(roomTargets.LetterThrowStartPosition, roomTargets.LetterLandingPosition, eased);
				_activeLetter.transform.rotation = Quaternion.Slerp(roomTargets.LetterStartRotation, roomTargets.LetterLandingRotation, eased);
			}

			_activeLetter.transform.position = roomTargets.LetterLandingPosition;
			_activeLetter.transform.rotation = roomTargets.LetterLandingRotation;
			await UniTask.Delay(TimeSpan.FromSeconds(LetterSettleDelay), cancellationToken: cancellationToken);
		}

		private async UniTask<bool> SpawnLetterAsync(NightShiftRoomTargets roomTargets)
		{
			CleanupActiveLetter();
			_activeLetter = await _objectFactory.CreateAsync<MorningLetterInteractableView>(
				ProjectGameplayResourcePaths.Letters_MorningLetterInteractableView,
				roomTargets.LetterThrowStartPosition,
				roomTargets.LetterStartRotation,
				roomTargets.LetterParent);
			if (!_activeLetter)
			{
				return false;
			}

			SetLayerRecursively(_activeLetter.gameObject, _playerView.gameObject.layer);
			_activeLetter.SetAvailable(false);
			_activeLetter.Interacted += OnLetterInteracted;
			return true;
		}

		private void CleanupActiveLetter()
		{
			if (!_activeLetter)
			{
				return;
			}

			_activeLetter.Interacted -= OnLetterInteracted;
			_objectFactory.Destroy(_activeLetter.gameObject);
			_activeLetter = null;
		}

		private static void SetLayerRecursively(GameObject target, int layer)
		{
			target.layer = layer;
			var rootTransform = target.transform;
			for (var i = 0; i < rootTransform.childCount; i++)
			{
				SetLayerRecursively(rootTransform.GetChild(i).gameObject, layer);
			}
		}

		private void TeleportPlayerToWakePoint(NightShiftRoomTargets roomTargets)
		{
			var characterController = _playerView.CharacterController;
			if (characterController)
			{
				characterController.enabled = false;
			}

			_playerView.transform.position = roomTargets.WakePosition;
			_playerView.transform.rotation = roomTargets.WakeRotation;

			if (_playerView.Head)
			{
				_playerView.Head.localRotation = Quaternion.identity;
			}

			if (characterController)
			{
				characterController.enabled = true;
			}

			_playerMovementController?.SyncFromCurrentView();
		}

		private void FocusPlayerOnLetter(Vector3 worldTarget)
		{
			var playerPosition = _playerView.transform.position;
			var flatDirection = Vector3.ProjectOnPlane(worldTarget - playerPosition, Vector3.up);
			if (flatDirection.sqrMagnitude > 0.001f)
			{
				_playerView.transform.rotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
			}

			if (_playerView.Head)
			{
				var headPosition = _playerView.Head.position;
				var localDirection = Quaternion.Inverse(_playerView.transform.rotation) * (worldTarget - headPosition).normalized;
				var flatMagnitude = Mathf.Sqrt(localDirection.x * localDirection.x + localDirection.z * localDirection.z);
				var pitch = -Mathf.Atan2(localDirection.y, Mathf.Max(0.001f, flatMagnitude)) * Mathf.Rad2Deg;
				pitch = Mathf.Clamp(pitch, _playerView.MinPitch, _playerView.MaxPitch);
				_playerView.Head.localRotation = Quaternion.Euler(pitch, 0f, 0f);
			}

			_playerMovementController?.SyncFromCurrentView();
		}

		private void SuspendPlayerControl()
		{
			_cursorUnlockedByIntro = false;
			if (_cursorService.IsCursorLocked)
			{
				_cursorService.UnlockCursor();
				_cursorUnlockedByIntro = !_cursorService.IsCursorLocked;
			}
		}

		private void RestorePlayerControl()
		{
			if (_cursorUnlockedByIntro && !_cursorService.IsCursorLocked)
			{
				_cursorService.LockCursor();
			}

			_cursorUnlockedByIntro = false;
		}

		private NightShiftLetterData ResolveLetterData(NightShiftLetterData letterData)
		{
			if (letterData != null && letterData.HasContent)
			{
				return letterData;
			}

			return new NightShiftLetterData(
				"Night Orders",
				"Read the papers, inspect the wagon, and keep the line clean. The debt will not pay itself.",
				string.Empty);
		}

		private NightShiftLetterData ResolveIntroLetter(StationDayContent dayContent)
		{
			if (_stationFlowController.IsCurrentDayLastAuthored)
			{
				var finalNotice = dayContent?.FinalNotice;
				if (finalNotice != null && finalNotice.HasContent)
				{
					return finalNotice;
				}

				return new NightShiftLetterData(
					"Final Notice",
					"This is your last shift. Be careful tonight. The debt must be cleared before dawn.",
					"No more mistakes.");
			}

			return dayContent?.ShiftLetter;
		}

		private NightShiftRoomTargets ResolveRoomTargets()
		{
			var configured = _sceneSetup.RoomSetup;
			if (configured.IsConfigured)
			{
				return new NightShiftRoomTargets(
					configured.WakePoint.position,
					Quaternion.Euler(0f, configured.WakePoint.eulerAngles.y, 0f),
					configured.DoorPoint.position,
					configured.LetterThrowStartPoint.position,
					configured.LetterLandingPoint.position,
					Quaternion.Euler(0f, configured.DoorPoint.eulerAngles.y, 0f),
					configured.LetterLandingPoint.rotation,
					configured.LetterParent);
			}

			var forward = Vector3.ProjectOnPlane(_playerView.transform.forward, Vector3.up);
			if (forward.sqrMagnitude <= 0.001f)
			{
				forward = Vector3.forward;
			}

			forward.Normalize();
			var wakeRotation = Quaternion.LookRotation(forward, Vector3.up);
			var wakePosition = _playerView.transform.position;
			var groundOffset = 0.04f;
			var doorPosition = wakePosition + forward * 2.2f + Vector3.up * 1.1f;
			var throwStart = wakePosition + forward * 1.45f + Vector3.up * 0.08f;
			var landing = wakePosition + forward * 0.95f + Vector3.up * groundOffset;
			var flatRotation = Quaternion.Euler(0f, wakeRotation.eulerAngles.y, 0f);

			return new NightShiftRoomTargets(
				wakePosition,
				wakeRotation,
				doorPosition,
				throwStart,
				landing,
				flatRotation,
				flatRotation,
				null);
		}

		private readonly struct NightShiftRoomTargets
		{
			public NightShiftRoomTargets(
				Vector3 wakePosition,
				Quaternion wakeRotation,
				Vector3 doorPosition,
				Vector3 letterThrowStartPosition,
				Vector3 letterLandingPosition,
				Quaternion letterStartRotation,
				Quaternion letterLandingRotation,
				Transform letterParent)
			{
				WakePosition = wakePosition;
				WakeRotation = wakeRotation;
				DoorPosition = doorPosition;
				LetterThrowStartPosition = letterThrowStartPosition;
				LetterLandingPosition = letterLandingPosition;
				LetterStartRotation = letterStartRotation;
				LetterLandingRotation = letterLandingRotation;
				LetterParent = letterParent;
			}

			public Vector3 WakePosition { get; }
			public Quaternion WakeRotation { get; }
			public Vector3 DoorPosition { get; }
			public Vector3 LetterThrowStartPosition { get; }
			public Vector3 LetterLandingPosition { get; }
			public Quaternion LetterStartRotation { get; }
			public Quaternion LetterLandingRotation { get; }
			public Transform LetterParent { get; }
		}
	}
}
