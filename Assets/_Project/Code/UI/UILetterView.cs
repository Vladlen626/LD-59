using PlatformCore.Services.UI;
using Project.Gameplay.Station.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UILetterView : UIBaseElement
{
	[Header("References")]
	[SerializeField] private Image _overlay;
	[SerializeField] private Image _paper;
	[SerializeField] private TMP_Text _titleLabel;
	[SerializeField] private TMP_Text _bodyLabel;
	[SerializeField] private TMP_Text _hintLabel;
	[SerializeField] private TMP_Text _closePromptLabel;

	[Header("Runtime Layout")]
	[SerializeField] private bool _buildRuntimeLayout = true;
	[SerializeField] private Vector2 _paperSize = new(980f, 620f);
	[SerializeField] private Color _overlayTint = new(0.03f, 0.02f, 0.02f, 0.68f);
	[SerializeField] private Color _paperTint = new(0.94f, 0.88f, 0.74f, 1f);
	[SerializeField] private Color _titleTint = new(0.18f, 0.11f, 0.05f, 1f);
	[SerializeField] private Color _bodyTint = new(0.17f, 0.1f, 0.06f, 1f);
	[SerializeField] private Color _hintTint = new(0.3f, 0.2f, 0.12f, 0.95f);
	[SerializeField] private Color _promptTint = new(0.24f, 0.15f, 0.09f, 0.95f);
	[SerializeField] private string _fallbackTitle = "Night Orders";
	[SerializeField] private string _fallbackBody = "The station expects results. Read the papers, inspect the wagon, and clear your debt one train at a time.";
	[SerializeField] private string _fallbackHint = "Authority does not repeat itself.";
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

		var title = string.IsNullOrWhiteSpace(letterData?.Title) ? _fallbackTitle : letterData.Title;
		var body = string.IsNullOrWhiteSpace(letterData?.Body) ? _fallbackBody : letterData.Body;
		var hint = string.IsNullOrWhiteSpace(letterData?.Hint) ? _fallbackHint : letterData.Hint;

		if (_titleLabel)
		{
			_titleLabel.text = title;
		}

		if (_bodyLabel)
		{
			_bodyLabel.text = body;
		}

		if (_hintLabel)
		{
			_hintLabel.text = hint;
		}

		if (_closePromptLabel)
		{
			_closePromptLabel.text = _closePrompt;
		}
	}

	public void ClearLetter()
	{
		if (_titleLabel)
		{
			_titleLabel.text = string.Empty;
		}

		if (_bodyLabel)
		{
			_bodyLabel.text = string.Empty;
		}

		if (_hintLabel)
		{
			_hintLabel.text = string.Empty;
		}

		if (_closePromptLabel)
		{
			_closePromptLabel.text = string.Empty;
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

		if (!_paper)
		{
			_paper = CreateImage("Paper", transform, _paperTint);
			var paperRect = (RectTransform)_paper.transform;
			paperRect.anchorMin = new Vector2(0.5f, 0.5f);
			paperRect.anchorMax = new Vector2(0.5f, 0.5f);
			paperRect.pivot = new Vector2(0.5f, 0.5f);
			paperRect.sizeDelta = _paperSize;
			paperRect.anchoredPosition = Vector2.zero;
		}

		_titleLabel ??= CreateText(
			"Title",
			_paper.transform,
			new Vector2(36f, -36f),
			new Vector2(-36f, -132f),
			42f,
			_titleTint,
			TextAlignmentOptions.TopLeft,
			FontStyles.Bold);

		_bodyLabel ??= CreateText(
			"Body",
			_paper.transform,
			new Vector2(40f, -120f),
			new Vector2(-40f, 118f),
			30f,
			_bodyTint,
			TextAlignmentOptions.TopLeft,
			FontStyles.Normal);

		_hintLabel ??= CreateText(
			"Hint",
			_paper.transform,
			new Vector2(40f, 92f),
			new Vector2(-320f, 42f),
			22f,
			_hintTint,
			TextAlignmentOptions.BottomLeft,
			FontStyles.Italic);

		_closePromptLabel ??= CreateText(
			"ClosePrompt",
			_paper.transform,
			new Vector2(320f, 92f),
			new Vector2(-40f, 42f),
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
