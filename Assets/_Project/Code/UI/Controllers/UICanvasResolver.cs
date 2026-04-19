using PlatformCore.Services.UI;
using UnityEngine;

public static class UICanvasResolver
{
	public static Transform Resolve(UICanvasType canvasType)
	{
		var canvases = Object.FindObjectsByType<Canvas>();
		var sortingOrder = (int)canvasType;
		for (var i = 0; i < canvases.Length; i++)
		{
			var canvas = canvases[i];
			if (!canvas || !canvas.isActiveAndEnabled)
			{
				continue;
			}

			if (canvas.sortingOrder == sortingOrder)
			{
				return canvas.transform;
			}
		}

		return null;
	}
}
