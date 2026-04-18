using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Project.Gameplay.Station.Configs
{
	public sealed class CargoCatalogLoader
	{
		private readonly ConfigService _configService;
		private readonly Dictionary<string, CargoTypeConfig> _cachedConfigs =
			new Dictionary<string, CargoTypeConfig>(StringComparer.OrdinalIgnoreCase);
		private bool _isLoaded;

		public CargoCatalogLoader(ConfigService configService)
		{
			_configService = configService ?? throw new ArgumentNullException(nameof(configService));
		}

		public async UniTask<IReadOnlyDictionary<string, CargoTypeConfig>> LoadAsync()
		{
			if (_isLoaded)
			{
				return _cachedConfigs;
			}

			var loadedConfigs =
				await _configService.GetConfigsAsync<CargoTypeConfig>(CargoCatalogResourcePaths.CargoTypes);

			_cachedConfigs.Clear();
			foreach (var pair in loadedConfigs)
			{
				if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null)
				{
					continue;
				}

				_cachedConfigs[pair.Key.Trim()] = pair.Value;
			}

			_isLoaded = true;
			return _cachedConfigs;
		}

		public async UniTask<CargoTypeConfig> GetByIdAsync(string cargoId)
		{
			if (string.IsNullOrWhiteSpace(cargoId))
			{
				return null;
			}

			var configs = await LoadAsync();
			configs.TryGetValue(cargoId.Trim(), out var config);
			return config;
		}

		public void ClearCache()
		{
			_cachedConfigs.Clear();
			_isLoaded = false;
		}
	}
}
