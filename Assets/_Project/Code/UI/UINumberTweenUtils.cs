using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;

public static class UINumberTweenUtils
{
	private static readonly Dictionary<TMP_Text, TweenState> States = new(8);

	public static void Animate(TMP_Text text, string format, int value, float duration)
	{
		if (!text)
		{
			return;
		}

		var state = GetState(text);
		state.Format = format;

		if (state.Tween.isAlive)
		{
			state.Tween.Stop();
		}

		ApplyValue(state, 0f);
		if (duration <= 0f)
		{
			ApplyValue(state, value);
			return;
		}

		state.Tween = Tween.Custom(state, 0f, value, duration, static (target, tweenValue) => ApplyValue(target, tweenValue))
			.OnComplete(state, static target => target.Tween = default, false);
	}

	public static void Stop(TMP_Text text)
	{
		if (!text)
		{
			return;
		}

		if (!States.TryGetValue(text, out var state))
		{
			return;
		}

		if (state.Tween.isAlive)
		{
			state.Tween.Stop();
		}
	}

	private static TweenState GetState(TMP_Text text)
	{
		if (!States.TryGetValue(text, out var state))
		{
			state = new TweenState();
			States.Add(text, state);
		}

		state.Text = text;
		return state;
	}

	private static void ApplyValue(TweenState state, float value)
	{
		if (!state.Text)
		{
			return;
		}

		state.Text.SetText(state.Format, Mathf.Round(value));
	}

	private sealed class TweenState
	{
		public TMP_Text Text;
		public string Format;
		public Tween Tween;
	}
}
