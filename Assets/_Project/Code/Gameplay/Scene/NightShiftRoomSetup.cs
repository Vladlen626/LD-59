using UnityEngine;

namespace Project.Gameplay.Scene
{
	public readonly struct NightShiftRoomSetup
	{
		public NightShiftRoomSetup(
			Transform wakePoint,
			Transform doorPoint,
			Transform letterThrowStartPoint,
			Transform letterLandingPoint,
			Transform letterParent)
		{
			WakePoint = wakePoint;
			DoorPoint = doorPoint;
			LetterThrowStartPoint = letterThrowStartPoint;
			LetterLandingPoint = letterLandingPoint;
			LetterParent = letterParent;
		}

		public Transform WakePoint { get; }
		public Transform DoorPoint { get; }
		public Transform LetterThrowStartPoint { get; }
		public Transform LetterLandingPoint { get; }
		public Transform LetterParent { get; }

		public bool IsConfigured =>
			WakePoint != null &&
			DoorPoint != null &&
			LetterThrowStartPoint != null &&
			LetterLandingPoint != null;
	}
}
