using System.Collections.Generic;
using Project.Gameplay.Station.Domain;

namespace Project.Gameplay.Station.Runtime
{
	public sealed class InspectionJournalModel
	{
		private readonly List<InspectionCaseRecord> _records = new List<InspectionCaseRecord>();

		public IReadOnlyList<InspectionCaseRecord> Records => _records;
		public int Count => _records.Count;

		public void AddRecord(InspectionCaseRecord record)
		{
			if (record == null)
			{
				return;
			}

			_records.Add(record);
		}

		public void Clear()
		{
			_records.Clear();
		}
	}
}
