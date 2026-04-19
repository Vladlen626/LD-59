using System.Threading;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Core.Lifecycle;
using PlatformCore.Services;
using PlatformCore.Services.Factory;
using PlatformCore.Services.UI;
using Project.Infrastructure;
using UnityEngine;

public sealed class UIWakeTransitionController : IBaseController, IActivatable, IDeactivatable
{
	private readonly ILoggerService _loggerService;
	private readonly IObjectFactory _objectFactory;
	private UIWakeTransitionView _view;
	private bool _isActive;
	private UniTask _viewLoadTask;
	private bool _isViewLoadInProgress;

	public UIWakeTransitionController(ILoggerService loggerService, IObjectFactory objectFactory)
	{
		_loggerService = loggerService;
		_objectFactory = objectFactory;
	}

	public void Activate()
	{
		if (_isActive)
		{
			return;
		}

		_isActive = true;
		EnsureViewAsync().Forget();
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
			_objectFactory.Destroy(_view.gameObject);
			_view = null;
		}
	}

	public async UniTask PlayWakeUpAsync(int blinkCount = 2, CancellationToken cancellationToken = default)
	{
		if (_isActive == false)
		{
			return;
		}

		await EnsureViewAsync();
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

	private async UniTask EnsureViewAsync()
	{
		if (_view)
		{
			return;
		}

		if (_isViewLoadInProgress)
		{
			await _viewLoadTask;
			return;
		}

		_isViewLoadInProgress = true;
		_viewLoadTask = CreateViewAsync();
		await _viewLoadTask;
	}

	private async UniTask CreateViewAsync()
	{
		try
		{
			var view = await _objectFactory.CreateAsync<UIWakeTransitionView>(
				ProjectGameplayResourcePaths.UI_UIWakeTransitionView,
				Vector3.zero,
				Quaternion.identity);
			if (!view)
			{
				return;
			}

			if (_isActive == false)
			{
				_objectFactory.Destroy(view.gameObject);
				return;
			}

			var parent = UICanvasResolver.Resolve(view.CanvasType);
			if (!parent)
			{
				_loggerService?.LogWarning($"[WakeTransition] Could not resolve canvas '{view.CanvasType}' for wake transition.");
				_objectFactory.Destroy(view.gameObject);
				return;
			}

			view.transform.SetParent(parent, false);
			view.gameObject.layer = parent.gameObject.layer;
			view.gameObject.SetActive(false);
			_view = view;
		}
		finally
		{
			_isViewLoadInProgress = false;
		}
	}
}
