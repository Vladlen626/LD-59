using System;
using System.Collections.Generic;
using System.Text;

namespace Project.Gameplay.Station.Domain
{
	public sealed class CargoLoadout
	{
		private readonly List<CargoQuantity> _entries = new List<CargoQuantity>();
		private readonly Dictionary<string, int> _quantities =
			new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		public CargoLoadout(IEnumerable<CargoQuantity> entries)
		{
			if (entries == null)
			{
				return;
			}

			foreach (var entry in entries)
			{
				if (entry == null || entry.IsValid == false)
				{
					continue;
				}

				if (_quantities.TryGetValue(entry.CargoId, out var existingQuantity))
				{
					_quantities[entry.CargoId] = existingQuantity + entry.Quantity;
					continue;
				}

				_quantities[entry.CargoId] = entry.Quantity;
				_entries.Add(new CargoQuantity(entry.CargoId, entry.Quantity));
			}
		}

		public IReadOnlyList<CargoQuantity> Entries => _entries;
		public IReadOnlyDictionary<string, int> Quantities => _quantities;
		public bool IsEmpty => _entries.Count == 0;

		public int TotalQuantity
		{
			get
			{
				var total = 0;
				for (var i = 0; i < _entries.Count; i++)
				{
					total += _entries[i].Quantity;
				}

				return total;
			}
		}

		public string SingleCargoId =>
			_entries.Count == 1 && _entries[0].Quantity > 0
				? _entries[0].CargoId
				: string.Empty;

		public bool Matches(CargoLoadout other)
		{
			if (ReferenceEquals(this, other))
			{
				return true;
			}

			if (other == null || _quantities.Count != other._quantities.Count)
			{
				return false;
			}

			foreach (var pair in _quantities)
			{
				if (other._quantities.TryGetValue(pair.Key, out var otherQuantity) == false ||
					otherQuantity != pair.Value)
				{
					return false;
				}
			}

			return true;
		}

		public bool ContainsCargo(string cargoId)
		{
			return string.IsNullOrWhiteSpace(cargoId) == false &&
				_quantities.ContainsKey(cargoId.Trim());
		}

		public string ToDebugString()
		{
			if (_entries.Count == 0)
			{
				return "<empty>";
			}

			var builder = new StringBuilder();
			for (var i = 0; i < _entries.Count; i++)
			{
				if (i > 0)
				{
					builder.Append(", ");
				}

				builder.Append(_entries[i].CargoId);
				builder.Append(" x");
				builder.Append(_entries[i].Quantity);
			}

			return builder.ToString();
		}
	}
}
