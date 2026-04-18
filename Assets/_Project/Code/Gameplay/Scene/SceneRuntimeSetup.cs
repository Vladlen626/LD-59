using UnityEngine;

namespace Project.Gameplay.Scene
{
	public readonly struct SceneRuntimeSetup
	{
		public SceneRuntimeSetup(
			Transform trainSpawnPoint,
			Transform trainSpawnParent,
			float trainArrivalDistance,
			float trainDepartureDistance,
			float trainArrivalDuration,
			float trainDepartureDuration)
		{
			TrainSpawnPoint = trainSpawnPoint;
			TrainSpawnParent = trainSpawnParent;
			TrainArrivalDistance = trainArrivalDistance;
			TrainDepartureDistance = trainDepartureDistance;
			TrainArrivalDuration = trainArrivalDuration;
			TrainDepartureDuration = trainDepartureDuration;
		}

		public Transform TrainSpawnPoint { get; }
		public Transform TrainSpawnParent { get; }
		public float TrainArrivalDistance { get; }
		public float TrainDepartureDistance { get; }
		public float TrainArrivalDuration { get; }
		public float TrainDepartureDuration { get; }
	}
}
