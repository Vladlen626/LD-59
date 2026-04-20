using System;
using System.Collections.Generic;
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
			Transform letterParent,
			IReadOnlyList<Transform> letterPoses)
		{
			WakePoint = wakePoint;
			DoorPoint = doorPoint;
			LetterThrowStartPoint = letterThrowStartPoint;
			LetterLandingPoint = letterLandingPoint;
			LetterParent = letterParent;
			LetterPoses = letterPoses ?? Array.Empty<Transform>();
		}

		public Transform WakePoint { get; }
		public Transform DoorPoint { get; }
		public Transform LetterThrowStartPoint { get; }
		public Transform LetterLandingPoint { get; }
		public Transform LetterParent { get; }
		public IReadOnlyList<Transform> LetterPoses { get; }

		public bool IsConfigured =>
			WakePoint != null &&
			DoorPoint != null &&
			LetterThrowStartPoint != null &&
			LetterLandingPoint != null;
	}
}
