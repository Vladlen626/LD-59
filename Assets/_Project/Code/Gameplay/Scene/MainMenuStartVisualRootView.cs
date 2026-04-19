using UnityEngine;

namespace Project.Gameplay.Scene
{
	[DisallowMultipleComponent]
	public sealed class MainMenuStartVisualRootView : MonoBehaviour
	{
		public void Hide()
		{
			gameObject.SetActive(false);
		}
	}
}
