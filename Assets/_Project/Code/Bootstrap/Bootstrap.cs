using PlatformCore.Infrastructure;

namespace Project.Bootstrap
{
	public sealed class Bootstrap : BaseBootstrap
	{
		protected override BaseGameRoot CreateGameRoot()
		{
			return new GameRoot();
		}
	}
}
