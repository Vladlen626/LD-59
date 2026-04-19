using System.Threading;
using Cysharp.Threading.Tasks;
using PlatformCore.Services.UI;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIWakeTransitionView : UIBaseElement
{
	[Header("References")]
	[SerializeField] private Image _backdrop;
	[SerializeField] private RectTransform _upperLidRect;
	[SerializeField] private RectTransform _lowerLidRect;

	[Header("Runtime Layout")]
	[SerializeField] private bool _buildRuntimeLayout = true;
	[SerializeField] private Color _backdropTint = new(0.02f, 0.02f, 0.02f, 0.12f);
	[SerializeField] private Color _lidTint = new(0f, 0f, 0f, 1f);

	[Header("Timing")]
	[SerializeField] private float _closedHoldSeconds = 0.16f;
	[SerializeField] private float _initialOpenSeconds = 0.24f;
	[SerializeField] private float _blinkInterval = 0.08f;
	[SerializeField] private float _blinkCloseSeconds = 0.06f;
	[SerializeField] private float _blinkHoldSeconds = 0.03f;
	[SerializeField] private float _blinkOpenSeconds = 0.12f;
	[SerializeField] private float _settleSeconds = 0.08f;

	private float _closedAmount = 1f;

	protected override void OnAwake()
	{
		EnsureRuntimeLayout();
		SetClosedAmount(1f);
		ApplyCanvasGroupState();
	}

	protected override void OnShow()
	{
		EnsureRuntimeLayout();
		ApplyCanvasGroupState();
	}

	protected override void OnHide()
	{
		SetClosedAmount(1f);
	}

	public async UniTask PlayWakeUpAsync(int blinkCount = 2, CancellationToken cancellationToken = default)
	{
		EnsureRuntimeLayout();
		SetClosedAmount(1f);

		if (_closedHoldSeconds > 0f)
		{
			await UniTask.Delay((int)(_closedHoldSeconds * 1000f), cancellationToken: cancellationToken);
		}

		await AnimateClosedAmountAsync(0f, _initialOpenSeconds, cancellationToken);

		for (var i = 1; i < blinkCount; i++)
		{
			if (_blinkInterval > 0f)
			{
				await UniTask.Delay((int)(_blinkInterval * 1000f), cancellationToken: cancellationToken);
			}

			await AnimateClosedAmountAsync(1f, _blinkCloseSeconds, cancellationToken);

			if (_blinkHoldSeconds > 0f)
			{
				await UniTask.Delay((int)(_blinkHoldSeconds * 1000f), cancellationToken: cancellationToken);
			}

			await AnimateClosedAmountAsync(0f, _blinkOpenSeconds, cancellationToken);
		}

		if (_settleSeconds > 0f)
		{
			await UniTask.Delay((int)(_settleSeconds * 1000f), cancellationToken: cancellationToken);
		}
	}

	private async UniTask AnimateClosedAmountAsync(float target, float duration, CancellationToken cancellationToken)
	{
		var start = _closedAmount;
		if (duration <= 0f)
		{
			SetClosedAmount(target);
			return;
		}

		var elapsed = 0f;
		while (elapsed < duration)
		{
			cancellationToken.ThrowIfCancellationRequested();
			await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
			elapsed += Time.unscaledDeltaTime;
			var t = Mathf.Clamp01(elapsed / duration);
			SetClosedAmount(Mathf.SmoothStep(start, target, t));
		}

		SetClosedAmount(target);
	}

	private void EnsureRuntimeLayout()
	{
		if (!_buildRuntimeLayout)
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

		if (!_backdrop)
		{
			_backdrop = CreateFullscreenImage("Backdrop", transform, _backdropTint);
		}

		if (!_upperLidRect)
		{
			_upperLidRect = CreateLid("UpperLid", new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
		}

		if (!_lowerLidRect)
		{
			_lowerLidRect = CreateLid("LowerLid", new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0f));
		}
	}

	private RectTransform CreateLid(string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
	{
		var image = CreateFullscreenImage(objectName, transform, _lidTint);
		var rect = (RectTransform)image.transform;
		rect.anchorMin = anchorMin;
		rect.anchorMax = anchorMax;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
		rect.pivot = pivot;
		return rect;
	}

	private static Image CreateFullscreenImage(string objectName, Transform parent, Color color)
	{
		var obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		var rect = (RectTransform)obj.transform;
		rect.SetParent(parent, false);
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;

		var image = obj.GetComponent<Image>();
		image.color = color;
		image.raycastTarget = false;
		return image;
	}

	private void SetClosedAmount(float amount)
	{
		_closedAmount = Mathf.Clamp01(amount);

		if (_upperLidRect)
		{
			_upperLidRect.localScale = new Vector3(1f, _closedAmount, 1f);
		}

		if (_lowerLidRect)
		{
			_lowerLidRect.localScale = new Vector3(1f, _closedAmount, 1f);
		}
	}

	private void ApplyCanvasGroupState()
	{
		_group.interactable = false;
		_group.blocksRaycasts = true;
	}
}
