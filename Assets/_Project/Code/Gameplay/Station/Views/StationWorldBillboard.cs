using UnityEngine;

namespace Project.Gameplay.Station.Views
{
	public sealed class StationWorldBillboard : MonoBehaviour
	{
		private Camera _cachedCamera;

		private void LateUpdate()
		{
			if (!_cachedCamera || _cachedCamera.isActiveAndEnabled == false)
			{
				_cachedCamera = Camera.main;
			}

			if (!_cachedCamera)
			{
				return;
			}

			transform.forward = _cachedCamera.transform.forward;
		}
	}
}
