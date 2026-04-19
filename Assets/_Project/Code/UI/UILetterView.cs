using PlatformCore.Services.UI;
using Project.Gameplay.Station.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UILetterView : UIBaseElement
{
	[Header("References")]
	[SerializeField] private Image _overlayImage;
	[SerializeField] private Image _paperImage;
	[SerializeField] private TMP_Text _titleText;
	[SerializeField] private TMP_Text _bodyText;
	[SerializeField] private TMP_Text _hintText;
	[SerializeField] private TMP_Text _closePromptText;

	[Header("Runtime Layout")]
	[SerializeField] private bool _autoCreateRuntimeLayout = true;
	[SerializeField] private Vector2 _paperSize = new(980f, 620f);
	[SerializeField] private Color _overlayColor = new(0.03f, 0.02f, 0.02f, 0.68f);
	[SerializeField] private Color _paperColor = new(0.94f, 0.88f, 0.74f, 1f);
	[SerializeField] private Color _titleColor = new(0.18f, 0.11f, 0.05f, 1f);
	[SerializeField] private Color _bodyColor = new(0.17f, 0.1f, 0.06f, 1f);
	[SerializeField] private Color _hintColor = new(0.3f, 0.2f, 0.12f, 0.95f);
	[SerializeField] private Color _promptColor = new(0.24f, 0.15f, 0.09f, 0.95f);
	[SerializeField] private string _defaultTitle = "Night Orders";
	[SerializeField] private string _defaultBody = "The station expects results. Read the papers, inspect the wagon, and clear your debt one train at a time.";
	[SerializeField] private string _defaultHint = "Authority does not repeat itself.";
	[SerializeField] private string _closePrompt = "E / Esc - Fold Letter";

	protected override void OnAwake()
	{
		EnsureRuntimeLayout();
		ApplyCanvasGroupState();
		ClearLetter();
	}

	protected override void OnShow()
	{
		EnsureRuntimeLayout();
		ApplyCanvasGroupState();
	}

	public void SetLetter(NightShiftLetterData letterData)
	{
		EnsureRuntimeLayout();

		var title = string.IsNullOrWhiteSpace(letterData?.Title) ? _defaultTitle : letterData.Title;
		var body = string.IsNullOrWhiteSpace(letterData?.Body) ? _defaultBody : letterData.Body;
		var hint = string.IsNullOrWhiteSpace(letterData?.Hint) ? _defaultHint : letterData.Hint;

		if (_titleText)
		{
			_titleText.text = title;
		}

		if (_bodyText)
		{
			_bodyText.text = body;
		}

		if (_hintText)
		{
			_hintText.text = hint;
		}

		if (_closePromptText)
		{
			_closePromptText.text = _closePrompt;
		}
	}

	public void ClearLetter()
	{
		if (_titleText)
		{
			_titleText.text = string.Empty;
		}

		if (_bodyText)
		{
			_bodyText.text = string.Empty;
		}

		if (_hintText)
		{
			_hintText.text = string.Empty;
		}

		if (_closePromptText)
		{
			_closePromptText.text = string.Empty;
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

		if (!_paperImage)
		{
			_paperImage = CreateImage("Paper", transform, _paperColor);
			var paperRect = (RectTransform)_paperImage.transform;
			paperRect.anchorMin = new Vector2(0.5f, 0.5f);
			paperRect.anchorMax = new Vector2(0.5f, 0.5f);
			paperRect.pivot = new Vector2(0.5f, 0.5f);
			paperRect.sizeDelta = _paperSize;
			paperRect.anchoredPosition = Vector2.zero;
		}

		_titleText ??= CreateText(
			"Title",
			_paperImage.transform,
			new Vector2(36f, -36f),
			new Vector2(-36f, -132f),
			42f,
			_titleColor,
			TextAlignmentOptions.TopLeft,
			FontStyles.Bold);

		_bodyText ??= CreateText(
			"Body",
			_paperImage.transform,
			new Vector2(40f, -120f),
			new Vector2(-40f, 118f),
			30f,
			_bodyColor,
			TextAlignmentOptions.TopLeft,
			FontStyles.Normal);

		_hintText ??= CreateText(
			"Hint",
			_paperImage.transform,
			new Vector2(40f, 92f),
			new Vector2(-320f, 42f),
			22f,
			_hintColor,
			TextAlignmentOptions.BottomLeft,
			FontStyles.Italic);

		_closePromptText ??= CreateText(
			"ClosePrompt",
			_paperImage.transform,
			new Vector2(320f, 92f),
			new Vector2(-40f, 42f),
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
