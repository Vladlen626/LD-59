using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Core.Lifecycle;
using PlatformCore.Services;
using PlatformCore.Services.Audio;
using PlatformCore.Services.Factory;
using PlatformCore.Services.Notifications;
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
		private readonly IGlobalNotificationService _globalNotificationService;

		private readonly List<LetterEntry> _archivedLetters = new(8);
		private LetterEntry _currentLetterEntry;
		private LetterEntry _openedLetterEntry;
		private UniTaskCompletionSource _letterReadSource;
		private bool _isActive;
		private bool _isIntroPlaying;
		private bool _cursorUnlockedByIntro;
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
			ISplashScreenService splashScreenService,
			IGlobalNotificationService globalNotificationService)
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
			_globalNotificationService = globalNotificationService;
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
			CleanupArchivedLetters();
			_openedLetterEntry = null;
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
			var resolvedLetterData = ResolveLetterData(letterData);
			var currentDayIndex = _stationFlowController.CurrentDayIndex;
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

				ShowCurrentDayNotification();
				await PlayDoorKnockAsync(roomTargets, cancellationToken);
				if (LetterThrowDelayAfterKnocksSeconds > 0f)
				{
					await UniTask.Delay(TimeSpan.FromSeconds(LetterThrowDelayAfterKnocksSeconds), cancellationToken: cancellationToken);
				}

				if (await SpawnLetterAsync(roomTargets, resolvedLetterData, currentDayIndex) == false)
				{
					return false;
				}

				await AnimateLetterArrivalAsync(roomTargets, _currentLetterEntry, cancellationToken);

				_currentLetterEntry.View.SetAvailable(true);

				if (focusLetter)
				{
					FocusPlayerOnLetter(_currentLetterEntry.View.transform.position);
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
				CleanupActiveLetter();
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

		private void OnLetterInteracted(LetterEntry entry)
		{
			if (_uiLetterController.IsOpen)
			{
				return;
			}

			_openedLetterEntry = entry;
			_uiLetterController.ShowLetter(entry.Data);
		}

		private void OnLetterClosed()
		{
			if (_openedLetterEntry == null)
			{
				return;
			}

			var openedLetterEntry = _openedLetterEntry;
			_openedLetterEntry = null;
			if (!ReferenceEquals(openedLetterEntry, _currentLetterEntry))
			{
				return;
			}

			ArchiveCurrentLetter();
			ClearInteractionRestriction();
			_letterReadSource?.TrySetResult();
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
			return _currentLetterEntry != null && ReferenceEquals(interactable, _currentLetterEntry.View);
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

		private void ShowCurrentDayNotification()
		{
			if (_globalNotificationService == null)
			{
				return;
			}

			var holdSeconds =
				Mathf.Max(0f, KnockCount * KnockShakeDuration + Mathf.Max(0, KnockCount - 1) * KnockIntervalSeconds);
			_globalNotificationService.ShowBanner($"Day {_stationFlowController.CurrentDayIndex}", holdSeconds, false, false);
		}

		private async UniTask AnimateLetterArrivalAsync(
			NightShiftRoomTargets roomTargets,
			LetterEntry entry,
			CancellationToken cancellationToken)
		{
			var elapsed = 0f;
			while (elapsed < LetterSlideDuration)
			{
				cancellationToken.ThrowIfCancellationRequested();
				await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
				elapsed += Time.unscaledDeltaTime;
				var t = Mathf.Clamp01(elapsed / LetterSlideDuration);
				var eased = Mathf.SmoothStep(0f, 1f, t);
				entry.View.transform.position = Vector3.Lerp(roomTargets.LetterThrowStartPosition, roomTargets.LetterLandingPosition, eased);
				entry.View.transform.rotation = Quaternion.Slerp(roomTargets.LetterStartRotation, roomTargets.LetterLandingRotation, eased);
			}

			entry.View.transform.position = roomTargets.LetterLandingPosition;
			entry.View.transform.rotation = roomTargets.LetterLandingRotation;
			await UniTask.Delay(TimeSpan.FromSeconds(LetterSettleDelay), cancellationToken: cancellationToken);
		}

		private async UniTask<bool> SpawnLetterAsync(
			NightShiftRoomTargets roomTargets,
			NightShiftLetterData letterData,
			int dayIndex)
		{
			CleanupActiveLetter();
			var letterView = await _objectFactory.CreateAsync<MorningLetterInteractableView>(
				ProjectGameplayResourcePaths.Letters_MorningLetterInteractableView,
				roomTargets.LetterThrowStartPosition,
				roomTargets.LetterStartRotation,
				roomTargets.LetterParent);
			if (!letterView)
			{
				return false;
			}

			SetLayerRecursively(letterView.gameObject, _playerView.gameObject.layer);
			letterView.SetDayIndex(dayIndex);
			letterView.SetAvailable(false);
			_currentLetterEntry = CreateLetterEntry(letterView, letterData, dayIndex);
			return true;
		}

		private void CleanupActiveLetter()
		{
			var currentLetterEntry = _currentLetterEntry;
			if (currentLetterEntry == null)
			{
				return;
			}

			DestroyLetterEntry(currentLetterEntry);
			if (ReferenceEquals(_openedLetterEntry, currentLetterEntry))
			{
				_openedLetterEntry = null;
			}

			_currentLetterEntry = null;
		}

		private void CleanupArchivedLetters()
		{
			for (var i = 0; i < _archivedLetters.Count; i++)
			{
				DestroyLetterEntry(_archivedLetters[i]);
			}

			_archivedLetters.Clear();
		}

		private void ArchiveCurrentLetter()
		{
			var currentLetterEntry = _currentLetterEntry;
			if (currentLetterEntry == null)
			{
				return;
			}

			PlaceArchivedLetter(currentLetterEntry, _archivedLetters.Count);
			_archivedLetters.Add(currentLetterEntry);
			_currentLetterEntry = null;
		}

		private LetterEntry CreateLetterEntry(
			MorningLetterInteractableView letterView,
			NightShiftLetterData letterData,
			int dayIndex)
		{
			var entry = new LetterEntry(letterView, letterData, dayIndex);
			entry.InteractedHandler = () => OnLetterInteracted(entry);
			letterView.Interacted += entry.InteractedHandler;
			return entry;
		}

		private void DestroyLetterEntry(LetterEntry entry)
		{
			if (entry == null)
			{
				return;
			}

			if (entry.View)
			{
				if (entry.InteractedHandler != null)
				{
					entry.View.Interacted -= entry.InteractedHandler;
				}

				_objectFactory.Destroy(entry.View.gameObject);
			}

			if (ReferenceEquals(_openedLetterEntry, entry))
			{
				_openedLetterEntry = null;
			}
		}

		private void PlaceArchivedLetter(LetterEntry entry, int archiveIndex)
		{
			var anchor = ResolveArchivePose(archiveIndex);
			entry.View.transform.SetParent(anchor, false);
			entry.View.transform.localPosition = Vector3.zero;
			entry.View.transform.localRotation = Quaternion.identity;
			entry.View.SetAvailable(true);
		}

		private Transform ResolveArchivePose(int archiveIndex)
		{
			var letterPoses = _sceneSetup.RoomSetup.LetterPoses;
			return letterPoses[archiveIndex];
		}

		private sealed class LetterEntry
		{
			public LetterEntry(MorningLetterInteractableView view, NightShiftLetterData data, int dayIndex)
			{
				View = view;
				Data = data;
				DayIndex = dayIndex;
			}

			public MorningLetterInteractableView View { get; }
			public NightShiftLetterData Data { get; }
			public int DayIndex { get; }
			public Action InteractedHandler { get; set; }
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
