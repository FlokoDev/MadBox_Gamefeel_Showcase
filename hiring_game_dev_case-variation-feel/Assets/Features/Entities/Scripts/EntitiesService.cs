using System;
using Cysharp.Threading.Tasks;
using Game.GamePlay.Enemies;
using Game.GamePlay.Heroes;
using Game.JoystickInput;
using Game.Weapons;
using Core.ServicesManager;
using Game.Generic.Tweening;

namespace Game.GamePlay.Entities
{
	public class EntitiesService : IService
	{
		public Type[] GetDependencies() => new[] { typeof(JoystickInputService), typeof(WeaponsService), typeof(TweenService) };

		public EnemiesController EnemiesController { get; private set; }
		public HeroController HeroController { get; private set; }

		public async UniTask<bool> Initialize()
		{
			JoystickInputService joystickInputService = ServicesLocator.Instance.GetService<JoystickInputService>();
			WeaponsService weaponsService = ServicesLocator.Instance.GetService<WeaponsService>();

			EnemiesController = new EnemiesController();
			HeroController = new HeroController();

			await HeroController.Initialize(EnemiesController, joystickInputService, weaponsService);
			await EnemiesController.Initialize(HeroController, ServicesLocator.Instance.GetService<TweenService>());

			return true;
		}

		public async UniTask Reset()
		{
			if (HeroController != null) await HeroController.Reset();
			if (EnemiesController != null) await EnemiesController.Reset();
		}
	}
}
