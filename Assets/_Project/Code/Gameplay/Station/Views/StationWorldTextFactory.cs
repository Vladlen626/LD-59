using TMPro;
using UnityEngine;

namespace Project.Gameplay.Station.Views
{
	internal static class StationWorldTextFactory
	{
		public static TMP_Text CreateBillboardLabel(
			Transform parent,
			string name,
			Vector3 localPosition,
			Vector3 localScale,
			float fontSize,
			TextAlignmentOptions alignment)
		{
			var labelObject = new GameObject(name);
			var labelTransform = labelObject.transform;
			labelTransform.SetParent(parent, false);
			labelTransform.localPosition = localPosition;
			labelTransform.localRotation = Quaternion.identity;
			labelTransform.localScale = localScale;

			var text = labelObject.AddComponent<TextMeshPro>();
			text.fontSize = fontSize;
			text.alignment = alignment;
			text.textWrappingMode = TextWrappingModes.NoWrap;
			text.overflowMode = TextOverflowModes.Overflow;
			text.text = string.Empty;

			labelObject.AddComponent<StationWorldBillboard>();
			return text;
		}
	}
}
