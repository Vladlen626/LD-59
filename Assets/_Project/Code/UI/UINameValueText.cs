using TMPro;
using UnityEngine;

public sealed class UINameValueText : MonoBehaviour
{
	[SerializeField] private TMP_Text _nameText;
	[SerializeField] private TMP_Text _valueText;

	public TMP_Text NameText => _nameText;
	public TMP_Text ValueText => _valueText;

	public void SetName(string text)
	{
		if (!_nameText)
		{
			return;
		}

		_nameText.text = text ?? string.Empty;
	}

	public void SetValue(string text)
	{
		if (!_valueText)
		{
			return;
		}

		_valueText.text = text ?? string.Empty;
	}
}
