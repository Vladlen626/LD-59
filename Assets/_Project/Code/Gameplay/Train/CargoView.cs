using System;
using UnityEngine;

namespace Project.Gameplay.Train
{
	public sealed class CargoView : MonoBehaviour
	{
		[SerializeField] private Transform[] _visualVariants = Array.Empty<Transform>();

		public bool HasVariants => _visualVariants != null && _visualVariants.Length > 0;

		public void ApplyRandomVariant(System.Random random)
		{
			if (HasVariants == false)
			{
				return;
			}

			var variantIndex = random != null
				? random.Next(_visualVariants.Length)
				: UnityEngine.Random.Range(0, _visualVariants.Length);
			ApplyVariant(variantIndex);
		}

		public void ApplyVariant(int variantIndex)
		{
			if (HasVariants == false)
			{
				return;
			}

			var safeIndex = Mathf.Abs(variantIndex) % _visualVariants.Length;
			for (var i = 0; i < _visualVariants.Length; i++)
			{
				var variant = _visualVariants[i];
				if (!variant)
				{
					continue;
				}

				variant.gameObject.SetActive(i == safeIndex);
			}
		}
	}
}
