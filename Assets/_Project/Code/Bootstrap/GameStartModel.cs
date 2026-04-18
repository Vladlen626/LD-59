using System;
using Cysharp.Threading.Tasks;

namespace Project.Bootstrap
{
	public sealed class GameStartModel
	{
		private readonly UniTaskCompletionSource _startedSource = new();

		public event Action Started;

		public bool IsStarted { get; private set; }

		public void Start()
		{
			if (IsStarted)
			{
				return;
			}

			IsStarted = true;
			Started?.Invoke();
			_startedSource.TrySetResult();
		}

		public UniTask WaitUntilStartedAsync()
		{
			return IsStarted ? UniTask.CompletedTask : _startedSource.Task;
		}
	}
}
