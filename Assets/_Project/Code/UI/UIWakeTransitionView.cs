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
		SetClosedAmount(1f);
		ApplyCanvasGroupState();
	}

	protected override void OnShow()
	{
		ApplyCanvasGroupState();
	}

	protected override void OnHide()
	{
		SetClosedAmount(1f);
	}

	public async UniTask PlayWakeUpAsync(int blinkCount = 2, CancellationToken cancellationToken = default)
	{
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
