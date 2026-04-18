using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Gameplay.Train
{
	public sealed class TrainView : MonoBehaviour
	{
		[Header("References")]
		[SerializeField] private Transform _door;
		[SerializeField] private Transform _cargoRoot;
		[SerializeField] private Transform[] _supportPoints = Array.Empty<Transform>();

		public Transform Door => _door;
		public Transform CargoRoot => _cargoRoot;
		public IReadOnlyList<Transform> SupportPoints => _supportPoints;
	}
}
