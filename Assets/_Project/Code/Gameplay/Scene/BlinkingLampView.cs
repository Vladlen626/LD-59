using UnityEngine;

namespace Project.Gameplay.Scene
{
	public sealed class BlinkingLampView : MonoBehaviour
	{
		[SerializeField] private Light _pointLight;
		[SerializeField] private float _minIntensity = 0.6f;
		[SerializeField] private float _maxIntensity = 1.2f;
		[SerializeField] private float _blinkSpeed = 14f;

		private float _time;

		private void Update()
		{
			_time += Time.deltaTime * _blinkSpeed;
			var noise = Mathf.PerlinNoise(_time, 0f);
			_pointLight.intensity = Mathf.Lerp(_minIntensity, _maxIntensity, noise);
		}
	}
}
