using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PlatformCore.Services;
using Project.Gameplay.Station.Configs;
using Project.Gameplay.Station.Domain;

namespace Project.Gameplay.Station.Runtime
{
	public sealed class StationEndlessDayGenerator
	{
		private const int CleanDayChancePercent = 8;
		private readonly CargoCatalogLoader _cargoCatalogLoader;
		private readonly ILoggerService _loggerService;
		private readonly Random _random = new Random();
		private readonly List<string> _cachedCargoIds = new List<string>();
		private bool _isCargoIdsLoaded;

		public StationEndlessDayGenerator(
			CargoCatalogLoader cargoCatalogLoader,
			ILoggerService loggerService)
		{
			_cargoCatalogLoader = cargoCatalogLoader;
			_loggerService = loggerService;
		}

		public async UniTask<StationDayContent> GenerateAsync(
			int dayIndex,
			int endlessDayOrder,
			int minCases,
			int maxCases,
			int minOccupiedSlotsPerTrain,
			int maxOccupiedSlotsPerTrain)
		{
			var cargoIds = await LoadCargoIdsAsync();
			if (cargoIds.Count == 0)
			{
				_loggerService?.LogError("[StationEndlessDayGenerator] Cargo catalog is empty. Cannot generate endless day.");
				return null;
			}

			var caseCount = NextInclusive(minCases, maxCases);
			var forbiddenCargoIds = PickDistinct(cargoIds, NextInclusive(1, Math.Min(3, cargoIds.Count)));
			var allowedCargoIds = BuildAllowedCargoIds(cargoIds, forbiddenCargoIds);
			var forceCleanDay = _random.Next(100) < CleanDayChancePercent;
			var reward = 65 + Math.Min(25, endlessDayOrder * 2);
			var livingCost = 18 + Math.Min(35, endlessDayOrder * 2);
			var initialDebt = Math.Max(0, caseCount * reward - livingCost - NextInclusive(20, 55));
			var scenarios = new List<TrainScenario>(caseCount);

			for (var i = 0; i < caseCount; i++)
			{
				var occupiedSlots = NextInclusive(minOccupiedSlotsPerTrain, maxOccupiedSlotsPerTrain);
				var caseKind = forceCleanDay
					? GeneratedCaseKind.Clean
					: SelectCaseKind(forbiddenCargoIds.Count > 0);
				scenarios.Add(BuildScenario(
					$"endless_{endlessDayOrder:000}_case_{i + 1:000}",
					caseKind,
					allowedCargoIds,
					forbiddenCargoIds,
					occupiedSlots,
					reward));
			}

			var ruleSet = new DayRuleSet(
				dayIndex,
				forbiddenCargoIds,
				100 + Math.Min(60, endlessDayOrder * 5),
				45 + Math.Min(35, endlessDayOrder * 3));
			return new StationDayContent(
				$"generated/endless_{endlessDayOrder:000}",
				initialDebt,
				ruleSet,
				scenarios,
				new NightShiftLetterData(
					"Night Orders",
					"Overtime shift. Cargo rules are reshuffled. Check the manifest, count the load, and reject banned cargo.",
					"Endless mode: trains and rules are generated."),
				new NightShiftLetterData(string.Empty, string.Empty, string.Empty),
				new DayLivingCostData("Overtime supplies", livingCost),
				forceCleanDay
					? "A clean night can still make a tired inspector doubt every crate."
					: "Another generated ledger closed. The line keeps moving.");
		}

		private async UniTask<IReadOnlyList<string>> LoadCargoIdsAsync()
		{
			if (_isCargoIdsLoaded)
			{
				return _cachedCargoIds;
			}

			_cachedCargoIds.Clear();
			if (_cargoCatalogLoader == null)
			{
				_loggerService?.LogError("[StationEndlessDayGenerator] Cargo catalog loader is missing.");
				_isCargoIdsLoaded = true;
				return _cachedCargoIds;
			}

			var cargoConfigs = await _cargoCatalogLoader.LoadAsync();
			foreach (var pair in cargoConfigs)
			{
				if (string.IsNullOrWhiteSpace(pair.Key) ||
					pair.Value == null ||
					string.IsNullOrWhiteSpace(pair.Value.prefab_resource_path))
				{
					continue;
				}

				_cachedCargoIds.Add(pair.Key.Trim());
			}

			_cachedCargoIds.Sort(StringComparer.OrdinalIgnoreCase);
			_isCargoIdsLoaded = true;
			return _cachedCargoIds;
		}

		private TrainScenario BuildScenario(
			string scenarioId,
			GeneratedCaseKind caseKind,
			IReadOnlyList<string> allowedCargoIds,
			IReadOnlyList<string> forbiddenCargoIds,
			int occupiedSlots,
			int reward)
		{
			var actualCargo = caseKind == GeneratedCaseKind.ForbiddenCargo || caseKind == GeneratedCaseKind.Combo
				? BuildLoadoutWithRequiredCargo(allowedCargoIds, Pick(forbiddenCargoIds), occupiedSlots)
				: BuildLoadout(allowedCargoIds, occupiedSlots);
			var manifestCargo = BuildManifestCargo(caseKind, actualCargo, allowedCargoIds);

			return new TrainScenario(
				scenarioId,
				new ManifestData(manifestCargo, BuildManifestDescription(manifestCargo)),
				actualCargo,
				reward);
		}

		private List<CargoQuantity> BuildManifestCargo(
			GeneratedCaseKind caseKind,
			IReadOnlyList<CargoQuantity> actualCargo,
			IReadOnlyList<string> allowedCargoIds)
		{
			switch (caseKind)
			{
				case GeneratedCaseKind.CargoMismatch:
				case GeneratedCaseKind.Combo:
					return BuildCargoMismatchManifest(actualCargo, allowedCargoIds);
				case GeneratedCaseKind.QuantityMismatch:
					return BuildQuantityMismatchManifest(actualCargo);
				default:
					return CloneCargo(actualCargo);
			}
		}

		private List<CargoQuantity> BuildCargoMismatchManifest(
			IReadOnlyList<CargoQuantity> actualCargo,
			IReadOnlyList<string> allowedCargoIds)
		{
			var manifestCargo = CloneCargo(actualCargo);
			if (manifestCargo.Count == 0 || allowedCargoIds.Count == 0)
			{
				return manifestCargo;
			}

			var replacementCargoId = PickDifferentCargoId(allowedCargoIds, manifestCargo[0].CargoId);
			manifestCargo[0] = new CargoQuantity(replacementCargoId, manifestCargo[0].Quantity);
			return manifestCargo;
		}

		private static List<CargoQuantity> BuildQuantityMismatchManifest(IReadOnlyList<CargoQuantity> actualCargo)
		{
			var manifestCargo = CloneCargo(actualCargo);
			if (manifestCargo.Count == 0)
			{
				return manifestCargo;
			}

			var firstEntry = manifestCargo[0];
			var changedQuantity = firstEntry.Quantity > 1
				? firstEntry.Quantity - 1
				: firstEntry.Quantity + 1;
			manifestCargo[0] = new CargoQuantity(firstEntry.CargoId, changedQuantity);
			return manifestCargo;
		}

		private List<CargoQuantity> BuildLoadoutWithRequiredCargo(
			IReadOnlyList<string> allowedCargoIds,
			string requiredCargoId,
			int targetQuantity)
		{
			if (string.IsNullOrWhiteSpace(requiredCargoId))
			{
				return BuildLoadout(allowedCargoIds, targetQuantity);
			}

			var requiredQuantity = NextInclusive(1, Math.Min(2, targetQuantity));
			var result = new Dictionary<string, int>(
				Math.Max(1, Math.Min(10, targetQuantity)),
				StringComparer.OrdinalIgnoreCase)
			{
				[requiredCargoId] = requiredQuantity,
			};
			FillLoadout(result, allowedCargoIds, targetQuantity - requiredQuantity);
			return ToCargoQuantities(result);
		}

		private List<CargoQuantity> BuildLoadout(IReadOnlyList<string> cargoIds, int targetQuantity)
		{
			var result = new Dictionary<string, int>(
				Math.Max(1, Math.Min(10, targetQuantity)),
				StringComparer.OrdinalIgnoreCase);
			FillLoadout(result, cargoIds, targetQuantity);
			return ToCargoQuantities(result);
		}

		private void FillLoadout(
			Dictionary<string, int> result,
			IReadOnlyList<string> cargoIds,
			int remainingQuantity)
		{
			if (cargoIds == null || cargoIds.Count == 0)
			{
				return;
			}

			while (remainingQuantity > 0)
			{
				var cargoId = Pick(cargoIds);
				var quantity = NextInclusive(1, Math.Min(3, remainingQuantity));
				result.TryGetValue(cargoId, out var existingQuantity);
				result[cargoId] = existingQuantity + quantity;
				remainingQuantity -= quantity;
			}
		}

		private GeneratedCaseKind SelectCaseKind(bool canUseForbiddenCargo)
		{
			var roll = _random.Next(100);
			if (roll < 40)
			{
				return GeneratedCaseKind.Clean;
			}

			if (roll < 62)
			{
				return GeneratedCaseKind.QuantityMismatch;
			}

			if (roll < 82)
			{
				return GeneratedCaseKind.CargoMismatch;
			}

			if (roll < 95 || canUseForbiddenCargo == false)
			{
				return canUseForbiddenCargo ? GeneratedCaseKind.ForbiddenCargo : GeneratedCaseKind.CargoMismatch;
			}

			return GeneratedCaseKind.Combo;
		}

		private List<string> BuildAllowedCargoIds(
			IReadOnlyList<string> cargoIds,
			IReadOnlyList<string> forbiddenCargoIds)
		{
			var result = new List<string>(cargoIds.Count);
			for (var i = 0; i < cargoIds.Count; i++)
			{
				if (ContainsCargoId(forbiddenCargoIds, cargoIds[i]))
				{
					continue;
				}

				result.Add(cargoIds[i]);
			}

			return result.Count > 0 ? result : new List<string>(cargoIds);
		}

		private static List<CargoQuantity> CloneCargo(IReadOnlyList<CargoQuantity> cargo)
		{
			var result = new List<CargoQuantity>(cargo?.Count ?? 0);
			if (cargo == null)
			{
				return result;
			}

			for (var i = 0; i < cargo.Count; i++)
			{
				var entry = cargo[i];
				if (entry == null || entry.IsValid == false)
				{
					continue;
				}

				result.Add(new CargoQuantity(entry.CargoId, entry.Quantity));
			}

			return result;
		}

		private static List<CargoQuantity> ToCargoQuantities(Dictionary<string, int> quantities)
		{
			var result = new List<CargoQuantity>(quantities.Count);
			foreach (var pair in quantities)
			{
				result.Add(new CargoQuantity(pair.Key, pair.Value));
			}

			return result;
		}

		private List<string> PickDistinct(IReadOnlyList<string> source, int count)
		{
			var pool = new List<string>(source);
			var result = new List<string>(count);
			while (pool.Count > 0 && result.Count < count)
			{
				var index = _random.Next(pool.Count);
				result.Add(pool[index]);
				pool.RemoveAt(index);
			}

			return result;
		}

		private string Pick(IReadOnlyList<string> source)
		{
			return source == null || source.Count == 0
				? string.Empty
				: source[_random.Next(source.Count)];
		}

		private string PickDifferentCargoId(IReadOnlyList<string> source, string excludedCargoId)
		{
			if (source == null || source.Count == 0)
			{
				return excludedCargoId;
			}

			for (var i = 0; i < 16; i++)
			{
				var cargoId = Pick(source);
				if (string.Equals(cargoId, excludedCargoId, StringComparison.OrdinalIgnoreCase) == false)
				{
					return cargoId;
				}
			}

			return source[0];
		}

		private int NextInclusive(int minValue, int maxValue)
		{
			var safeMin = Math.Max(1, Math.Min(minValue, maxValue));
			var safeMax = Math.Max(safeMin, maxValue);
			return _random.Next(safeMin, safeMax + 1);
		}

		private static bool ContainsCargoId(IReadOnlyList<string> cargoIds, string cargoId)
		{
			if (cargoIds == null || string.IsNullOrWhiteSpace(cargoId))
			{
				return false;
			}

			for (var i = 0; i < cargoIds.Count; i++)
			{
				if (string.Equals(cargoIds[i], cargoId, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		private static string BuildManifestDescription(IReadOnlyList<CargoQuantity> manifestCargo)
		{
			if (manifestCargo == null || manifestCargo.Count == 0)
			{
				return "No cargo listed";
			}

			return manifestCargo.Count == 1
				? "Single cargo shipment"
				: "Mixed cargo shipment";
		}

		private enum GeneratedCaseKind
		{
			Clean = 0,
			QuantityMismatch = 1,
			CargoMismatch = 2,
			ForbiddenCargo = 3,
			Combo = 4,
		}
	}
}
