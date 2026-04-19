namespace Project.Gameplay.Station.Runtime
{
	public sealed class NightShiftLetterData
	{
		public NightShiftLetterData(string title, string body, string hint)
		{
			Title = Normalize(title);
			Body = Normalize(body);
			Hint = Normalize(hint);
		}

		public string Title { get; }
		public string Body { get; }
		public string Hint { get; }
		public bool HasContent => string.IsNullOrEmpty(Title) == false || string.IsNullOrEmpty(Body) == false;

		private static string Normalize(string value)
		{
			return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
		}
	}
}
