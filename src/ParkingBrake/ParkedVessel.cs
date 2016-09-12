using System;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

namespace ParkingBrake
{
	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	public class ParkedVessel : MonoBehaviour
	{
		private static Dictionary<Guid, Part> parkedVessels = new Dictionary<Guid, Part>();
		private static Dictionary<Guid, List<Part>> mirrorBrake = new Dictionary<Guid, List<Part>>();

		public static void parkVessel(Guid vessel, Part engagingPart) {

			parkedVessels[vessel] = engagingPart;

			Debug.Log ("Engaging by part:" + engagingPart.GetInstanceID());
			// Find all wheels with parking brake installed
			List<Part> mirrorPart = new List<Part>();
			foreach (Part p in engagingPart.vessel.parts) {
				foreach (ParkingBrake m in p.Modules.OfType<ParkingBrake>()) {
					if (m.isInstalled == State.True) {
						
						// There should be only one...
						Debug.Log ("Sending mirrored enagage command to: " + m.part.GetInstanceID () + " from " + engagingPart.GetInstanceID ());
						m.EngageMirroredParkingBrake (engagingPart);
						mirrorPart.Add (p);
					}
				}
			}

			mirrorBrake [vessel] = mirrorPart;
		}

		public static void releaseVessel (Guid vessel, Part part)
		{

			if (parkedVessels.ContainsKey (vessel)) {

				// If we are disengaging by master part, only disengage on slave parts
				// If we are disengaging by slave part, disengage master as well
				foreach (Part p in mirrorBrake[vessel]) {
					foreach (ParkingBrake m in p.Modules.OfType<ParkingBrake>()) {
						// There should be only one...
						m.DisengageMirroredParkingBrake ();
					}
				}
				mirrorBrake.Remove (vessel);

				Part prt = parkedVessels [vessel];
				if (prt != part) {
					foreach (ParkingBrake m in prt.Modules.OfType<ParkingBrake>())
						m.DisengageParkingBrake();
				}
				parkedVessels.Remove (vessel);
			}
		}
	}
}

