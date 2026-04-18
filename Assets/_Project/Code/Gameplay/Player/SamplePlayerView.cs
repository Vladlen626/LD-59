using UnityEngine;

namespace Project.Gameplay.Player
{
	public sealed class SamplePlayerView : MonoBehaviour
	{
		[Header("References")]
		[SerializeField] private CharacterController _characterController;
		[SerializeField] private Transform _cameraRoot;
		[SerializeField] private Transform _head;
		[SerializeField] [Tooltip("Origin for interaction raycasts. If empty, CameraRoot/Head will be used.")]
		private Transform _interactionRayOrigin;

		[Header("Movement")]
		[SerializeField] [Min(0f)] private float _moveSpeed = 5f;
		[SerializeField] [Min(1f)] private float _sprintSpeedMultiplier = 1.7f;
		[SerializeField] private float _gravity = -20f;
		[SerializeField] [Min(0f)] private float _jumpHeight = 1.2f;
		[SerializeField] private bool _enableJump = true;

		[Header("Look")]
		[SerializeField] [Min(0f)] private float _lookSensitivity = 0.08f;
		[SerializeField] private float _minPitch = -80f;
		[SerializeField] private float _maxPitch = 80f;

		[Header("Head Bob")]
		[SerializeField] private bool _enableHeadBob = true;
		[SerializeField] [Min(0f)] private float _headBobAmplitude = 0.03f;
		[SerializeField] [Min(0f)] private float _headBobFrequency = 8f;
		[SerializeField] [Min(0f)] private float _headBobReturnSpeed = 8f;

		[Header("Interaction")]
		[SerializeField] [Min(0.5f)] [Tooltip("Max distance for interactable detection.")]
		private float _interactionDistance = 4f;
		[SerializeField] [Tooltip("Physics layers that can be interacted with.")]
		private LayerMask _interactionMask = ~0;

		public CharacterController CharacterController => _characterController;
		public Transform CameraRoot => _cameraRoot;
		public Transform Head => _head;
		public Transform InteractionRayOrigin => _interactionRayOrigin;
		public float MoveSpeed => _moveSpeed;
		public float SprintSpeedMultiplier => _sprintSpeedMultiplier;
		public float Gravity => _gravity;
		public float JumpHeight => _jumpHeight;
		public bool EnableJump => _enableJump;
		public float LookSensitivity => _lookSensitivity;
		public float MinPitch => _minPitch;
		public float MaxPitch => _maxPitch;
		public bool EnableHeadBob => _enableHeadBob;
		public float HeadBobAmplitude => _headBobAmplitude;
		public float HeadBobFrequency => _headBobFrequency;
		public float HeadBobReturnSpeed => _headBobReturnSpeed;
		public float InteractionDistance => _interactionDistance;
		public LayerMask InteractionMask => _interactionMask;
	}
}
