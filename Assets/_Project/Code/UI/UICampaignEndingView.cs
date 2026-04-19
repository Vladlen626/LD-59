using PlatformCore.Services.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UICampaignEndingView : UIBaseElement
{
	[Header("References")]
	[SerializeField] private Image _overlayImage;
	[SerializeField] private Image _panelImage;
	[SerializeField] private TMP_Text _titleText;
	[SerializeField] private TMP_Text _bodyText;
	[SerializeField] private TMP_Text _promptText;

	[Header("Runtime Layout")]
	[SerializeField] private bool _autoCreateRuntimeLayout = true;
	[SerializeField] private Vector2 _panelSize = new(1040f, 620f);
	[SerializeField] private Color _overlayColor = new(0.02f, 0.015f, 0.015f, 0.82f);
	[SerializeField] private Color _panelColor = new(0.12f, 0.09f, 0.07f, 0.98f);
	[SerializeField] private Color _titleColor = new(0.95f, 0.86f, 0.67f, 1f);
	[SerializeField] private Color _bodyColor = new(0.88f, 0.82f, 0.72f, 1f);
	[SerializeField] private Color _promptColor = new(0.72f, 0.64f, 0.52f, 1f);

	protected override void OnAwake()
	{
		EnsureRuntimeLayout();
		ApplyCanvasGroupState();
		ClearContent();
	}

	protected override void OnShow()
	{
		EnsureRuntimeLayout();
		ApplyCanvasGroupState();
	}

	public void SetContent(string title, string body, string prompt)
	{
		EnsureRuntimeLayout();

		if (_titleText)
		{
			_titleText.text = string.IsNullOrWhiteSpace(title) ? string.Empty : title.Trim();
		}

		if (_bodyText)
		{
			_bodyText.text = string.IsNullOrWhiteSpace(body) ? string.Empty : body.Trim();
		}

		if (_promptText)
		{
			_promptText.text = string.IsNullOrWhiteSpace(prompt) ? string.Empty : prompt.Trim();
		}
	}

	public void ClearContent()
	{
		if (_titleText)
		{
			_titleText.text = string.Empty;
		}

		if (_bodyText)
		{
			_bodyText.text = string.Empty;
		}

		if (_promptText)
		{
			_promptText.text = string.Empty;
		}
	}

	private void EnsureRuntimeLayout()
	{
		if (_autoCreateRuntimeLayout == false)
		{
			return;
		}

		var rootRect = transform as RectTransform;
		if (rootRect)
		{
			rootRect.anchorMin = Vector2.zero;
			rootRect.anchorMax = Vector2.one;
			rootRect.offsetMin = Vector2.zero;
			rootRect.offsetMax = Vector2.zero;
		}

		if (!_overlayImage)
		{
			_overlayImage = CreateImage("Overlay", transform, _overlayColor);
		}

		if (!_panelImage)
		{
			_panelImage = CreateImage("Panel", transform, _panelColor);
			var panelRect = (RectTransform)_panelImage.transform;
			panelRect.anchorMin = new Vector2(0.5f, 0.5f);
			panelRect.anchorMax = new Vector2(0.5f, 0.5f);
			panelRect.pivot = new Vector2(0.5f, 0.5f);
			panelRect.sizeDelta = _panelSize;
			panelRect.anchoredPosition = Vector2.zero;
		}

		_titleText ??= CreateText(
			"Title",
			_panelImage.transform,
			new Vector2(64f, -84f),
			new Vector2(-64f, -190f),
			54f,
			_titleColor,
			TextAlignmentOptions.TopLeft,
			FontStyles.Bold);

		_bodyText ??= CreateText(
			"Body",
			_panelImage.transform,
			new Vector2(64f, -208f),
			new Vector2(-64f, 116f),
			30f,
			_bodyColor,
			TextAlignmentOptions.TopLeft,
			FontStyles.Normal);

		_promptText ??= CreateText(
			"Prompt",
			_panelImage.transform,
			new Vector2(64f, 84f),
			new Vector2(-64f, 40f),
			22f,
			_promptColor,
			TextAlignmentOptions.BottomRight,
			FontStyles.Bold);
	}

	private static Image CreateImage(string objectName, Transform parent, Color color)
	{
		var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		var rect = (RectTransform)imageObject.transform;
		rect.SetParent(parent, false);
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;

		var image = imageObject.GetComponent<Image>();
		image.color = color;
		image.raycastTarget = false;
		return image;
	}

	private static TMP_Text CreateText(
		string objectName,
		Transform parent,
		Vector2 offsetMin,
		Vector2 offsetMax,
		float fontSize,
		Color color,
		TextAlignmentOptions alignment,
		FontStyles fontStyle)
	{
		var textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
		var rect = (RectTransform)textObject.transform;
		rect.SetParent(parent, false);
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = offsetMin;
		rect.offsetMax = offsetMax;

		var text = textObject.GetComponent<TextMeshProUGUI>();
		text.fontSize = fontSize;
		text.color = color;
		text.alignment = alignment;
		text.fontStyle = fontStyle;
		text.textWrappingMode = TextWrappingModes.Normal;
		text.overflowMode = TextOverflowModes.Overflow;
		text.raycastTarget = false;
		if (TMP_Settings.defaultFontAsset)
		{
			text.font = TMP_Settings.defaultFontAsset;
		}

		return text;
	}

	private void ApplyCanvasGroupState()
	{
		if (!_group)
		{
			return;
		}

		_group.interactable = false;
		_group.blocksRaycasts = true;
	}
}
