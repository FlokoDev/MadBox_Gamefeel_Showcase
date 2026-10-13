using Core.ServicesManager;
using Game.GamePlay.Entities;
using Game.World;
using UnityEngine;

namespace Game.GamePlay.Heroes
{
	public class HeroContainerView : MonoBehaviour
	{
		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			WorldService worldService = ServicesLocator.Instance.GetService<WorldService>();
			HeroView heroView = Instantiate(HeroConfig.Instance.HeroPrefab, transform);
			heroView.transform.position = ServicesLocator.Instance.GetService<EntitiesService>().HeroController.CurrentState.Position;
			worldService.World.Camera.Follow = heroView.transform;
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
		}
	}
}
