using System.Threading;
using Cysharp.Threading.Tasks;
using PlatformCore.Services.UI;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIWakeTransitionView : UIBaseElement
{
	private const float ClosedHoldSeconds = 0.24f;
	private const float InitialOpenSeconds = 0.34f;
	private const float BlinkIntervalSeconds = 0.12f;
	private const float BlinkCloseSeconds = 0.08f;
	private const float BlinkHoldSeconds = 0.05f;
	private const float BlinkOpenSeconds = 0.2f;
	private const float SettleSeconds = 0.1f;

	[Header("References")]
	[SerializeField] private Image _backdrop;
	[SerializeField] private RectTransform _upperLidRect;
	[SerializeField] private RectTransform _lowerLidRect;

	private float _closedAmount = 1f;

	protected override void OnAwake()
	{
		SetClosedAmount(1f);
		ApplyCanvasGroupState();
	}

	protected override void OnShow()
	{
		SetClosedAmount(1f);
		ApplyCanvasGroupState();
	}

	protected override void OnHide()
	{
		SetClosedAmount(1f);
	}

	public async UniTask PlayWakeUpAsync(int blinkCount = 2, CancellationToken cancellationToken = default)
	{
		SetClosedAmount(1f);

		if (ClosedHoldSeconds > 0f)
		{
			await UniTask.Delay((int)(ClosedHoldSeconds * 1000f), cancellationToken: cancellationToken);
		}

		await AnimateClosedAmountAsync(0f, InitialOpenSeconds, cancellationToken);

		for (var i = 1; i < blinkCount; i++)
		{
			if (BlinkIntervalSeconds > 0f)
			{
				await UniTask.Delay((int)(BlinkIntervalSeconds * 1000f), cancellationToken: cancellationToken);
			}

			await AnimateClosedAmountAsync(1f, BlinkCloseSeconds, cancellationToken);

			if (BlinkHoldSeconds > 0f)
			{
				await UniTask.Delay((int)(BlinkHoldSeconds * 1000f), cancellationToken: cancellationToken);
			}

			await AnimateClosedAmountAsync(0f, BlinkOpenSeconds, cancellationToken);
		}

		if (SettleSeconds > 0f)
		{
			await UniTask.Delay((int)(SettleSeconds * 1000f), cancellationToken: cancellationToken);
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
