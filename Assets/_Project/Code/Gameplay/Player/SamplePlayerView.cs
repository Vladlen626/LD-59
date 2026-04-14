using UnityEngine;

namespace Project.Gameplay.Player
{
	public sealed class SamplePlayerView : MonoBehaviour
	{
		[Header("References")]
		[SerializeField] private CharacterController _characterController;
		[SerializeField] private Transform _cameraRoot;
		[SerializeField] private Transform _head;

		[Header("Movement")]
		[SerializeField] [Min(0f)] private float _moveSpeed = 5f;
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

		public CharacterController CharacterController => _characterController;
		public Transform CameraRoot => _cameraRoot;
		public Transform Head => _head;
		public float MoveSpeed => _moveSpeed;
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
	}
}
