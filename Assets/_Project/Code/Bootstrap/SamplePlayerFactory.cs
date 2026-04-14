using Cysharp.Threading.Tasks;
using PlatformCore.Services.Factory;
using Project.Gameplay.Player;
using Project.Infrastructure;

namespace Project.Bootstrap
{
	public static class SamplePlayerFactory
	{
		public static UniTask<SamplePlayerView> SpawnAsync(IObjectFactory factory)
		{
			return factory.CreateAsync<SamplePlayerView>(
				ProjectResourcePaths.Characters_Player,
				SamplePlayerSpawnDefaults.Position,
				SamplePlayerSpawnDefaults.Rotation);
		}
	}
}
