using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using ModuleWheels;

namespace ParkingBrake
{
	public enum State {True, False, Unknown};

	public class ParkingBrake : PartModule
	{
		[KSPField]
		public int resetRate = 60;
		[KSPField]
		public float velocityThreshold = 0.1f;
		[KSPField]
		public float partVelocityThreshold = 0.1f;

		public bool isAvailable = false;
		[KSPField(isPersistant = true)]
		public State isInstalled;
		[KSPField(isPersistant = true)]
		public State engaged;
		[KSPField(isPersistant = true)]
		private State isMaster;
		private Part mirrorBrake = null;
		private bool needsSave = false;
		private int frameCount;

		private Dictionary<Part, Vector3> partsPosition;

		public ParkingBrake ()
		{
		}

		public override string GetInfo ()
		{
			return "ParkingBrake - available in mirrored setup";
		}

		public override void OnStart(StartState state)
		{
			switch (state) {
			case StartState.Editor:
				isAvailable = this.part.isMirrored;
				if (isInstalled == State.Unknown && isAvailable) {
					Events ["InstallParkingBrake"].active = true;
					Events ["UninstallParkingBrake"].active = false;
				}
				break;

			default:

				Events ["InstallParkingBrake"].active = false;
				Events ["UninstallParkingBrake"].active = false;

				if (engaged == State.Unknown) {
					if (isInstalled == State.True)
						Events ["EngageParkingBrake"].active = true;

					Events ["DisengageParkingBrake"].active = false;
				} else {
					//Events ["EngageParkingBrake"].active = engaged == State.True;
					//Events ["DisengageParkingBrake"].active = engaged == State.False;

					if (isMaster == State.Unknown || isMaster == State.False)
						return;
					if (engaged == State.True)
						EngageParkingBrake ();
					else
						DisengageParkingBrake ();
				}
				break;
			}
				
		}

		[KSPEvent(guiActive = false, guiActiveEditor = true, name = "InstallParkingBrake", guiName = "Install Parking Brake")]
		public void InstallParkingBrake() {

			isInstalled = State.True;
			Events ["InstallParkingBrake"].active = false;
			Events ["UninstallParkingBrake"].active = true;
		}

		[KSPEvent(guiActive = false, guiActiveEditor = true, name = "UninstallParkingBrake", guiName = "Uninstall Parking Brake")]
		public void UninstalParkingBrake() {

			isInstalled = State.False;
			Events ["InstallParkingBrake"].active = true;
			Events ["UninstallParkingBrake"].active = false;
		}

		[KSPEvent(guiActive = true, guiActiveEditor = false, name = "EngageParkingBrake", guiName = "Engage Parking Brake")]
		public void EngageParkingBrake()
		{
			if (isInstalled == State.False || isMaster == State.False)
				return;

			// TODO: Check if we are standing still enough
			engaged = State.True;
			isMaster = State.True;

			// Register this parking brake
			ParkedVessel.parkVessel(this.vessel.id, this.part);
			Events ["EngageParkingBrake"].active = false;
			Events ["DisengageParkingBrake"].active = true;
			String nm = vessel.GetName () + "(" + this.GetInstanceID() + "): ";
			print (nm + "Engaged parking brake");

			// saving part positions
			// TODO: Only save after we're settled
			needsSave  = true;
			partsPosition = new Dictionary<Part, Vector3> ();

			// engage real brakes
			foreach (ModuleWheelBrakes m in this.part.Modules.OfType<ModuleWheelBrakes>()) {
				m.brakeInput = 1;
			}
		}

		[KSPEvent(guiActive = true, guiActiveEditor = false, name = "DisengageParkingBrake", guiName = "Disengage Parking Brake")]
		public void DisengageParkingBrake()
		{
			if (mirrorBrake == null) {
				engaged = State.False;
				Events ["EngageParkingBrake"].active = true;
				Events ["DisengageParkingBrake"].active = false;

				ParkedVessel.releaseVessel (this.vessel.id, this.part);
				String nm = vessel.GetName () + "(" + this.GetInstanceID() + "): ";
				print (nm + "Parking brake disengaged");
			} else {
				ParkedVessel.releaseVessel (this.vessel.id, this.part);
			}

			// disengage real brakes
			foreach (ModuleWheelBrakes m in this.part.Modules.OfType<ModuleWheelBrakes>()) {
				m.brakeInput = 0;
			}
		}

		public void EngageMirroredParkingBrake(Part mirror)
		{
			String nm = vessel.GetName () + "(" + this.GetInstanceID() + "): ";
			print (nm + "Engaging by mirrored part; my id=" + this.part.GetInstanceID () + "; other id=" + mirror.GetInstanceID());

			isMaster = State.False;
			mirrorBrake = mirror;
			engaged = State.True;
			Events ["EngageParkingBrake"].active = false;
			Events ["DisengageParkingBrake"].active = true;

			// engage real brakes
			foreach (ModuleWheelBrakes m in this.part.Modules.OfType<ModuleWheelBrakes>()) {
				m.brakeInput = 1;
			}
		}

		public void DisengageMirroredParkingBrake()
		{
			isMaster = State.Unknown;
			mirrorBrake = null;
			engaged = State.False;
			Events ["EngageParkingBrake"].active = true;
			Events ["DisengageParkingBrake"].active = false;

			// disengage real brakes
			foreach (ModuleWheelBrakes m in this.part.Modules.OfType<ModuleWheelBrakes>()) {
				m.brakeInput = 0;
			}

			String nm = vessel.GetName () + "(" + this.GetInstanceID() + "): ";
			print (nm + "Disengaged mirrored parking brake");
		}

		// TODO: This!
		public override void OnLoad(ConfigNode node)
		{

			isAvailable = this.part.isMirrored;
		}

		public void FixedUpdate()
		{
			String nm = vessel.GetName () + "(" + this.GetInstanceID() + "): ";


			if (shouldStabilize())
			{
				if (needsSave) {
					foreach (Part p in this.vessel.parts)
						partsPosition [p] = p.Rigidbody.position;

					print (nm + "Saved parts : " + partsPosition.Count ());
					frameCount = 0;
					needsSave = false;
				}

				frameCount++;
				print (nm + "Stabilizing (frameCount=" + frameCount + ")");

				// Ok, let's try to cancel velocity of every part
				int i = 0;
				int j = 0;
				int k = 0;
				foreach (Part p in this.vessel.parts) {
					//if (p.physicalSignificance != Part.PhysicalSignificance.FULL || p.Rigidbody == null)
					if (p.Rigidbody == null)
						continue;
					j++;
					// Eliminating small phantom velocity change
					//print (nm + "Veocity magnitude: " + p.Rigidbody.velocity.magnitude);
					if (p.Rigidbody.velocity.magnitude < partVelocityThreshold) {
						p.Rigidbody.velocity = Vector3.zero;
						k++;
						if (frameCount >= resetRate) {
							try {
								p.Rigidbody.position = partsPosition [p];
								i++;
							} catch (KeyNotFoundException e) {
								print ("Part " + p.name + " not found (" + e.ToString () + ")");
								print ("Parts Dict=" + partsPosition.ToString ());
								print ("First part: " + partsPosition.First ().Key.ToString ()
								+ "; name=" + partsPosition.First ().Key.name);
							}
						}
					}
				}
				if (i > 0)
					frameCount = 0;
				print (nm+"Reset velocity for " + k + " parts out of " + j + " with Rigidbody; reset position for " + i + " parts");
				Vector3d sp = vessel.GetSrfVelocity ();
				print (nm+"Vessel speed: x=" + sp.x + "; y=" + sp.y + "; z=" + sp.z);
			}
			else print (nm+"Not stabilizing");
		}

		private bool shouldStabilize() {

			if (this.engaged != State.True)
				return false;
			if (this.mirrorBrake != null)
				return false;
			if (this.vessel == null)
				return false; 
			if (!this.vessel.checkLanded())
				return false;
			if (this.vessel.checkSplashed ())
				return false;
			if (this.partsPosition == null)
				return false;
			if (this.vessel.GetSrfVelocity ().magnitude > velocityThreshold) {
				// trigger saving new vehicle position
				needsSave = true;
				return false;
			}
			
			return true;
		}
	}
}

