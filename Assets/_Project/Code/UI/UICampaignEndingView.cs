using PlatformCore.Services.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UICampaignEndingView : UIBaseElement
{
	[Header("References")]
	[SerializeField] private Image _overlay;
	[SerializeField] private Image _panel;
	[SerializeField] private TMP_Text _titleLabel;
	[SerializeField] private TMP_Text _bodyLabel;
	[SerializeField] private TMP_Text _promptLabel;

	[Header("Runtime Layout")]
	[SerializeField] private bool _buildRuntimeLayout = true;
	[SerializeField] private Vector2 _panelSize = new(1040f, 620f);
	[SerializeField] private Color _overlayTint = new(0.02f, 0.015f, 0.015f, 0.82f);
	[SerializeField] private Color _panelTint = new(0.12f, 0.09f, 0.07f, 0.98f);
	[SerializeField] private Color _titleTint = new(0.95f, 0.86f, 0.67f, 1f);
	[SerializeField] private Color _bodyTint = new(0.88f, 0.82f, 0.72f, 1f);
	[SerializeField] private Color _promptTint = new(0.72f, 0.64f, 0.52f, 1f);

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

		if (_titleLabel)
		{
			_titleLabel.text = string.IsNullOrWhiteSpace(title) ? string.Empty : title.Trim();
		}

		if (_bodyLabel)
		{
			_bodyLabel.text = string.IsNullOrWhiteSpace(body) ? string.Empty : body.Trim();
		}

		if (_promptLabel)
		{
			_promptLabel.text = string.IsNullOrWhiteSpace(prompt) ? string.Empty : prompt.Trim();
		}
	}

	public void ClearContent()
	{
		if (_titleLabel)
		{
			_titleLabel.text = string.Empty;
		}

		if (_bodyLabel)
		{
			_bodyLabel.text = string.Empty;
		}

		if (_promptLabel)
		{
			_promptLabel.text = string.Empty;
		}
	}

	private void EnsureRuntimeLayout()
	{
		if (_buildRuntimeLayout == false)
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

		if (!_overlay)
		{
			_overlay = CreateImage("Overlay", transform, _overlayTint);
		}

		if (!_panel)
		{
			_panel = CreateImage("Panel", transform, _panelTint);
			var panelRect = (RectTransform)_panel.transform;
			panelRect.anchorMin = new Vector2(0.5f, 0.5f);
			panelRect.anchorMax = new Vector2(0.5f, 0.5f);
			panelRect.pivot = new Vector2(0.5f, 0.5f);
			panelRect.sizeDelta = _panelSize;
			panelRect.anchoredPosition = Vector2.zero;
		}

		_titleLabel ??= CreateText(
			"Title",
			_panel.transform,
			new Vector2(64f, -84f),
			new Vector2(-64f, -190f),
			54f,
			_titleTint,
			TextAlignmentOptions.TopLeft,
			FontStyles.Bold);

		_bodyLabel ??= CreateText(
			"Body",
			_panel.transform,
			new Vector2(64f, -208f),
			new Vector2(-64f, 116f),
			30f,
			_bodyTint,
			TextAlignmentOptions.TopLeft,
			FontStyles.Normal);

		_promptLabel ??= CreateText(
			"Prompt",
			_panel.transform,
			new Vector2(64f, 84f),
			new Vector2(-64f, 40f),
			22f,
			_promptTint,
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
		_group.interactable = false;
		_group.blocksRaycasts = true;
	}
}
