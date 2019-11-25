// Plays the attached ParticleSystem on enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InteraktVR.VRInteraction
{

	public class PlayParticleOnEnable : MonoBehaviour 
	{
		void OnEnable()
		{
			ParticleSystem ps = GetComponent<ParticleSystem>();
			if (ps != null) 
			{
				ps.Clear(true);
				ps.Play();
			}
		}
	}
}