using PlatformCore.Services.UI;
using Project.Gameplay.Station.Controllers;

public sealed class UIHintViewController : BaseContextController<UIHintView>
{
	private readonly StationInteractionController _stationInteractionController;

	public UIHintViewController(
		IUIService uiService,
		StationInteractionController stationInteractionController)
		: base(uiService)
	{
		_stationInteractionController = stationInteractionController;
	}

	protected override void OnActivate()
	{
		if (_context == null)
		{
			return;
		}

		_context.Hide();
		_context.ClearHint();

		if (_stationInteractionController == null)
		{
			return;
		}

		_stationInteractionController.HintChanged += OnHintChangedHandler;
		ApplyHint(_stationInteractionController.CurrentHint);
	}

	protected override void OnDeactivate()
	{
		if (_stationInteractionController != null)
		{
			_stationInteractionController.HintChanged -= OnHintChangedHandler;
		}

		if (_context != null)
		{
			_context.ClearHint();
		}
	}

	private void OnHintChangedHandler(string hint)
	{
		ApplyHint(hint);
	}

	private void ApplyHint(string hint)
	{
		if (_context == null)
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(hint))
		{
			_context.ClearHint();
			if (_context.IsShown())
			{
				_context.Hide();
			}

			return;
		}

		_context.SetHint(hint);
		if (_context.IsShown() == false)
		{
			_context.Show();
		}
	}
}
