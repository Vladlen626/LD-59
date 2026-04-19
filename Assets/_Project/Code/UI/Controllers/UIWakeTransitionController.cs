using System.Threading;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Core.Lifecycle;
using PlatformCore.Services;
using PlatformCore.Services.UI;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class UIWakeTransitionController : IBaseController, IActivatable, IDeactivatable
{
	private readonly ILoggerService _loggerService;
	private UIWakeTransitionView _view;
	private bool _isActive;

	public UIWakeTransitionController(ILoggerService loggerService)
	{
		_loggerService = loggerService;
	}

	public void Activate()
	{
		if (_isActive)
		{
			return;
		}

		_isActive = true;
		EnsureView();
		if (_view)
		{
			_view.Hide();
			_view.gameObject.SetActive(false);
		}
	}

	public void Deactivate()
	{
		if (_isActive == false)
		{
			return;
		}

		_isActive = false;
		if (_view)
		{
			Object.Destroy(_view.gameObject);
			_view = null;
		}
	}

	public async UniTask PlayWakeUpAsync(int blinkCount = 2, CancellationToken cancellationToken = default)
	{
		if (_isActive == false)
		{
			return;
		}

		EnsureView();
		if (!_view)
		{
			return;
		}

		_view.gameObject.SetActive(true);
		_view.Show();
		await _view.PlayWakeUpAsync(blinkCount, cancellationToken);
		_view.Hide();
		_view.gameObject.SetActive(false);
	}

	private void EnsureView()
	{
		if (_view)
		{
			return;
		}

		var parent = ResolveCanvasTransform();
		if (!parent)
		{
			_loggerService?.LogWarning("[WakeTransition] Could not resolve target canvas for wake transition.");
			return;
		}

		var viewObject = new GameObject(
			nameof(UIWakeTransitionView),
			typeof(RectTransform),
			typeof(CanvasGroup),
			typeof(UIWakeTransitionView));
		viewObject.layer = parent.gameObject.layer;
		viewObject.transform.SetParent(parent, false);
		_view = viewObject.GetComponent<UIWakeTransitionView>();
		viewObject.SetActive(false);
	}

	private static Transform ResolveCanvasTransform()
	{
		var canvases = Object.FindObjectsByType<Canvas>();
		Canvas fallbackCanvas = null;

		for (var i = 0; i < canvases.Length; i++)
		{
			var canvas = canvases[i];
			if (!canvas || !canvas.isActiveAndEnabled)
			{
				continue;
			}

			if (canvas.sortingOrder == (int)UICanvasType.ModalOverlay)
			{
				return canvas.transform;
			}

			if (fallbackCanvas == null || canvas.sortingOrder > fallbackCanvas.sortingOrder)
			{
				fallbackCanvas = canvas;
			}
		}

		return fallbackCanvas ? fallbackCanvas.transform : null;
	}
}
