using PlatformCore.Services.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIHintView : UIBaseElement
{
	[Header("References")]
	[SerializeField] private TMP_Text _hintText;
	[SerializeField] private Image _backgroundImage;

	[Header("Runtime Layout")]
	[SerializeField] private bool _autoCreateRuntimeLayout = true;
	[SerializeField] private Vector2 _minSize = new(420f, 72f);
	[SerializeField] private Vector2 _padding = new(24f, 18f);
	[SerializeField] private float _fontSize = 28f;

	[Header("Style")]
	[SerializeField] private Color _backgroundColor = new(0.09f, 0.08f, 0.06f, 0.88f);
	[SerializeField] private Color _textColor = new(0.95f, 0.91f, 0.81f, 1f);

	protected override void OnAwake()
	{
		EnsureRuntimeLayout();
		ApplyCanvasGroupState();
		ClearHint();
	}

	protected override void OnShow()
	{
		ApplyCanvasGroupState();
	}

	public void SetHint(string hint)
	{
		EnsureRuntimeLayout();
		if (_hintText)
		{
			_hintText.text = string.IsNullOrWhiteSpace(hint) ? string.Empty : hint;
		}
	}

	public void ClearHint()
	{
		if (_hintText)
		{
			_hintText.text = string.Empty;
		}
	}

	private void EnsureRuntimeLayout()
	{
		if (_autoCreateRuntimeLayout == false)
		{
			return;
		}

		var rootRect = transform as RectTransform;
		if (rootRect && rootRect.sizeDelta.sqrMagnitude < 0.01f)
		{
			rootRect.sizeDelta = _minSize;
		}

		if (!_backgroundImage)
		{
			var backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			var backgroundTransform = (RectTransform)backgroundObject.transform;
			backgroundTransform.SetParent(transform, false);
			backgroundTransform.anchorMin = Vector2.zero;
			backgroundTransform.anchorMax = Vector2.one;
			backgroundTransform.offsetMin = Vector2.zero;
			backgroundTransform.offsetMax = Vector2.zero;

			_backgroundImage = backgroundObject.GetComponent<Image>();
			_backgroundImage.color = _backgroundColor;
			_backgroundImage.raycastTarget = false;
		}

		if (_hintText)
		{
			return;
		}

		var textObject = new GameObject("HintText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
		var textTransform = (RectTransform)textObject.transform;
		textTransform.SetParent(_backgroundImage ? _backgroundImage.transform : transform, false);
		textTransform.anchorMin = Vector2.zero;
		textTransform.anchorMax = Vector2.one;
		textTransform.offsetMin = _padding;
		textTransform.offsetMax = -_padding;

		var hintText = textObject.GetComponent<TextMeshProUGUI>();
		hintText.fontSize = _fontSize;
		hintText.color = _textColor;
		hintText.alignment = TextAlignmentOptions.Center;
		hintText.textWrappingMode = TextWrappingModes.NoWrap;
		hintText.overflowMode = TextOverflowModes.Overflow;
		hintText.raycastTarget = false;
		if (TMP_Settings.defaultFontAsset)
		{
			hintText.font = TMP_Settings.defaultFontAsset;
		}

		_hintText = hintText;
	}

	private void ApplyCanvasGroupState()
	{
		if (!_group)
		{
			return;
		}

		_group.interactable = false;
		_group.blocksRaycasts = false;
	}
}
